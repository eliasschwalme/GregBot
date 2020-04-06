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
        private readonly DiscordSocketClient _client;
        private readonly SocketGuild _guild;
        private readonly SocketGuildChannel _starboard;
        private readonly ChannelQualifier _channelQualifier;
        private readonly EmoteQualifier _emoteQualifier;

        public StarboardWatcher
        (
            DiscordSocketClient client,
            SocketGuild guild,
            SocketTextChannel starboard,
            ChannelQualifier channelQualifier,
            EmoteQualifier emoteQualifier
        )
        {
            _client = client;
            _guild = guild;
            _starboard = starboard;
            _channelQualifier = channelQualifier;
            _emoteQualifier = emoteQualifier;
        }

        /// <summary>
        /// Attaches event handlers to the client.
        /// </summary>
        public void Bind()
        {
            _client.ReactionAdded += OnReactionAdded;
        }

        private Task OnReactionAdded
        (
            Cacheable<IUserMessage, ulong> message,
            ISocketMessageChannel channel,
            SocketReaction reaction
        )
        {
            var isStarboardEmote = _emoteQualifier(reaction.Emote);
            
            if (!isStarboardEmote)
            {
                return Task.CompletedTask;
            }

            var isStarboardableChannel = _channelQualifier(channel);

            if (!isStarboardableChannel)
            {
                return Task.CompletedTask;
            }

            return OnReactionAddedImpl(message, channel, reaction);
        }

        private async Task OnReactionAddedImpl
        (
            Cacheable<IUserMessage, ulong> message,
            ISocketMessageChannel channel,
            SocketReaction reaction
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
                var reactionExists = await ctx.StarboardGazers.AnyAsync(gazer => gazer.MessageId == (long)message.Id
                    && gazer.StarboardChannelId == (long)_starboard.Id
                    && gazer.StargazerId == (long)user.Id);

                if (!reactionExists)
                {
                    ctx.StarboardGazers.Add(new StarReaction
                    {
                        MessageId = (long)message.Id,
                        StarboardChannelId = (long)_starboard.Id,
                        StargazerId = (long)user.Id
                    });

                    await ctx.SaveChangesAsync();
                }
            }

            // 
        }

        /*
        private static async Task HandleReaction(SocketReaction reaction)
        {
            var reactionMessage = await reaction.Channel.GetMessageAsync(reaction.MessageId);

            if (!(reactionMessage is IUserMessage userMessage))
            {
                Console.WriteLine("Handle reaction - reaction message not user message");
                return;
            }

            if (!(userMessage.Author is IGuildUser user))
            {
                Console.WriteLine("Handle reaction - user author not guild user");
                return;
            }

            var gazers = (await userMessage.GetReactionUsersAsync(reaction.Emote, 1000).ToList())
                .SelectMany(page => page)
                .Where(u => u != null)
                .ToList();

            var shouldBeOnStarboard = gazers.Count >= DiscordSettings.MinStarboardReactions;
            var messageId = reaction.MessageId;

            var starboardInformation = await Database.GetStarboardInformation(reaction.MessageId);

            var currentlyOnStarboard = starboardInformation.OnStarboard;
            var starboardMessageId = starboardInformation.StarboardMessageId;

            // don't do anything in these circumstances
            if ((currentlyOnStarboard && shouldBeOnStarboard)
                || (!currentlyOnStarboard && !shouldBeOnStarboard))
            {
                // if we shouldn't be on starboard, don't update anything
                if (!shouldBeOnStarboard)
                {
                    return;
                }

                var starboardMessage = await GetStarboardMessage(starboardMessageId);

                // just update the message to reflect the current value
                var message = await reaction.Channel.GetMessageAsync(reaction.MessageId);

                var embed = DiscordFormatting.BuildStarboardEmbed(user, message, gazers.Count)
                    .Build();

                await starboardMessage.ModifyAsync(properties => properties.Embed = embed);
            }
            else if (currentlyOnStarboard && !shouldBeOnStarboard)
            {
                // get off the board

                var starboardMessage = await GetStarboardMessage(starboardMessageId);
                await starboardMessage.DeleteAsync();
                await Database.DeleteStarboardEntry(messageId);
            }
            else if (!currentlyOnStarboard && shouldBeOnStarboard)
            {
                // get on the board

                var message = await reaction.Channel.GetMessageAsync(reaction.MessageId);
                var embed = DiscordFormatting.BuildStarboardEmbed(user, message, gazers.Count)
                    .Build();

                var starboardMessage = await GetStarboardChannel()
                    .SendMessageAsync(embed: embed)
                    ;

                await Database.CreateStarboardEntry(messageId, starboardMessage.Id);
            }

            return;

            async Task<IUserMessage> GetStarboardMessage(ulong starboardMessageId2)
            {
                return (IUserMessage)(await GetStarboardChannel()
                    .GetMessageAsync(starboardMessageId2));
            }

            SocketTextChannel GetStarboardChannel()
            {
                return socketGuildChannel.Guild
                    .GetTextChannel(DiscordSettings.StarboardChannel);
            }
        }

        private static Task Client_ReactionRemoved
        (
            Cacheable<IUserMessage, ulong> cachableMessage,
            ISocketMessageChannel socketChannel,
            SocketReaction reaction
        )
            => HandleReaction(reaction);

        private static Task Client_ReactionAdded
        (
            Cacheable<IUserMessage, ulong> message,
            ISocketMessageChannel channel,
            SocketReaction reaction
        )
            => HandleReaction(reaction);*/
    }
}