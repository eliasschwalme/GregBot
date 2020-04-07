using Discord;
using Discord.WebSocket;

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace ForumCrawler
{
    /// <summary>
    /// Represents criteria that may or may not make a channel eligible for starboarding.
    /// </summary>
    /// <param name="channel">The channel in question to determine if it is qualified
    /// to be pinned to the designated starboard.</param>
    public delegate bool ChannelQualifier(ISocketMessageChannel channel);

    /// <summary>
    /// Determines if an emote is qualified to represent the action of starboarding
    /// a given post.
    /// </summary>
    /// <param name="emote">The emote in question.</param>
    public delegate bool EmoteQualifier(IEmote emote);

    public class StarboardWatcher
    {
        private static bool IsAdminNoVisibilityEmote(IEmote emote)
            => emote.Name == "🚫"; // no_entry_sign

        private readonly DiscordSocketClient _client;
        private readonly SocketGuild _guild;
        private readonly SocketTextChannel _starboard;
        private readonly ChannelQualifier _channelQualifier;
        private readonly EmoteQualifier _emoteQualifier;
        private readonly EmoteQualifier _adminQualifier;
        private readonly int _configuredWoots;

        public StarboardWatcher
        (
            DiscordSocketClient client,
            SocketGuild guild,
            SocketTextChannel starboard,
            ChannelQualifier channelQualifier,
            EmoteQualifier emoteQualifier,
            int configuredWoots
        )
        {
            _client = client;
            _guild = guild;
            _starboard = starboard;
            _channelQualifier = channelQualifier;
            _emoteQualifier = emoteQualifier;
            _adminQualifier = IsAdminNoVisibilityEmote;
            _configuredWoots = configuredWoots;
        }

        /// <summary>
        /// Attaches event handlers to the client.
        /// </summary>
        public void Bind()
        {
            _client.ReactionAdded += (a, b, c) => OnReactionChanged(a, b, c, true);
            _client.ReactionRemoved += (a, b, c) => OnReactionChanged(a, b, c, false);
        }

        private Task OnReactionChanged
        (
            Cacheable<IUserMessage, ulong> message,
            ISocketMessageChannel channel,
            SocketReaction reaction,
            bool added
        )
        {
            var isStarboardableChannel = _channelQualifier(channel);

            if (!isStarboardableChannel)
            {
                return Task.CompletedTask;
            }

            var isStarboardEmote = _emoteQualifier(reaction.Emote);

            if (!isStarboardEmote)
            {
                var isAdminEmote = _adminQualifier(reaction.Emote);

                if (isAdminEmote)
                {
                    return OnStaffVisibility(message, channel, reaction, added);
                }

                return Task.CompletedTask;
            }

            return OnReactionAddedImpl(message, channel, reaction, added);
        }

        private async Task OnStaffVisibility
        (
            Cacheable<IUserMessage, ulong> message,
            ISocketMessageChannel channel,
            SocketReaction reaction,
            bool added
        )
        {
            using (var ctx = new DatabaseContext())
            {
                var post = await ctx.RevisedStarboardPosts.SingleOrDefaultAsync(loadingPost => loadingPost.MessageId == (long)message.Id
                    && loadingPost.StarboardChannelId == (long)_starboard.Id);

                if (post == default)
                {
                    // a post doesn't exist to censor - let's make one
                    ctx.RevisedStarboardPosts.Add(new Models.RevisedStarboardPost
                    {
                        MessageId = (long)message.Id,
                        StaffToggledVisibility = added,
                        StarboardChannelId = (long)_starboard.Id,
                        StarboardMessageId = 0
                    });
                }
                else
                {
                    post.StaffToggledVisibility = added;
                }

                await ctx.SaveChangesAsync();
            }

            await UpdateStarboardPost(await message.GetOrDownloadAsync());
        }

        private async Task OnReactionAddedImpl
        (
            Cacheable<IUserMessage, ulong> message,
            ISocketMessageChannel channel,
            SocketReaction reaction,
            bool added
        )
        {
            var reactionMessage = await message.GetOrDownloadAsync();
            
            if (!(reactionMessage.Author is IGuildUser user))
            {
                Console.WriteLine("Handle reaction - user author not guild user");
                return;
            }

            // we only want to add a reaction to the DB if one doesn't exist
            using (var ctx = new DatabaseContext())
            {
                var reactionPost = await ctx.StarboardGazers.SingleOrDefaultAsync(gazer => gazer.MessageId == (long)message.Id
                    && gazer.StarboardChannelId == (long)_starboard.Id
                    && gazer.StargazerId == (long)reaction.UserId);

                var reactionExists = reactionPost != default;

                if (!reactionExists && added)
                {
                    ctx.StarboardGazers.Add(new StarReaction
                    {
                        MessageId = (long)message.Id,
                        StarboardChannelId = (long)_starboard.Id,
                        StargazerId = (long)reaction.UserId
                    });

                    await ctx.SaveChangesAsync();
                }
                else if (reactionExists && !added)
                {
                    ctx.StarboardGazers.Remove(reactionPost);

                    await ctx.SaveChangesAsync();
                }
            }

            await UpdateStarboardPost(reactionMessage);
        }

        /// <summary>
        /// Computes the logic necessary for if a starboard post should be shown or hidden.
        /// </summary>
        /// <param name="message">The original message posted.</param>
        private async Task UpdateStarboardPost(IUserMessage message)
        {
            using (var ctx = new DatabaseContext())
            {
                var post = await ctx.RevisedStarboardPosts.SingleOrDefaultAsync(loadingPost => loadingPost.MessageId == (long)message.Id
                    && loadingPost.StarboardChannelId == (long)_starboard.Id);
                var reactions = await ctx.StarboardGazers.CountAsync(reaction => reaction.MessageId == (long)message.Id
                    && reaction.StarboardChannelId == (long)_starboard.Id);

                var postExists = post != default;

                if (postExists)
                {
                    // if this is true, we do not want the post to show up on starboard
                    if (post.StaffToggledVisibility)
                    {
                        if (post.StarboardMessageId != 0)
                        {
                            await _starboard.DeleteMessageAsync((ulong)post.StarboardMessageId);
                            post.StarboardMessageId = 0;
                        }
                    }
                    else
                    {
                        // we want to update/create the post
                        if (post.StarboardMessageId == 0)
                        {
                            var starboardMessage = await _starboard.SendMessageAsync(embed: GetMessageEmbed(message, reactions));
                            post.StarboardMessageId = (long)starboardMessage.Id;
                        }
                        else
                        {
                            var starboardMessage = (IUserMessage)(await _starboard.GetMessageAsync((ulong)post.StarboardMessageId));
                            await starboardMessage.ModifyAsync(edit =>
                            {
                                edit.Embed = GetMessageEmbed(message, reactions);
                            });
                        }
                    }
                }
                else
                {
                    if (reactions >= _configuredWoots)
                    {
                        var starboardMessage = await _starboard.SendMessageAsync(embed: GetMessageEmbed(message, reactions));
                        ctx.RevisedStarboardPosts.Add(new Models.RevisedStarboardPost
                        {
                            MessageId = (long)message.Id,
                            StaffToggledVisibility = false,
                            StarboardChannelId = (long)_starboard.Id,
                            StarboardMessageId = (long)starboardMessage.Id
                        });
                    }
                }

                await ctx.SaveChangesAsync();
            }
        }

        private Embed GetMessageEmbed(IUserMessage message, int woots)
        {
            return DiscordFormatting.BuildStarboardEmbed((IGuildUser)message.Author, message, woots, woots >= _configuredWoots).Build();
        }
    }
}