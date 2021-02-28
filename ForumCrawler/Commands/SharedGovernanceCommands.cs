using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

using ForumCrawler.Helpers;

namespace ForumCrawler
{
    [Group("suggest")]
    [Alias("suggestion", "rfc")]
    public class SharedGovernanceCommands : GovernanceCommandsBase
    {
        [Command("convert")]
        [Summary("Converts an existing suggestion's type.")]
        [Priority(0)]
        public async Task Convert(SuggestionType type, bool force = false)
        {
            var guildUser = (IGuildUser)Context.User;
            var channel = (SocketTextChannel)Context.Channel;
            var oldType = channel.GetSuggestionChannelType();
            if (oldType == SuggestionType.Vote && !force)
            {
                throw new Exception(
                    "Suggestion is in voting mode. Converting it would delete all existing votes. To confirm, run the command again with the force flag set.");
            }

            if (!guildUser.IsServerOwner())
            {
                throw new Exception("Only server owner can convert suggestions.");
            }

            if (!channel.IsSuggestionChannelByName())
            {
                throw new InvalidOperationException("Wrong channel!");
            }

            if (type == SuggestionType.Vote)
            {
                throw new Exception("Use g!suggest finalize to start a vote.");
            }

            var vote = await Database.UNSAFE_GetGovernanceVoteAsync(channel.Id);
            if (vote == null)
            {
                throw new Exception("Cannot find information about this suggestion in database!");
            }

            var message = (IUserMessage)await Context.Channel.GetMessageAsync(vote.MessageId);

            await message.RemoveAllReactionsAsync();
            await channel.ConvertSuggestionChannelType(type);
            await UpdateBillboardAsync(Context.Guild, message, channel, vote, type);

            await channel.SendMessageAsync("Suggestion type changed to " + type + ".");
        }


        [Command("rename")]
        [Summary("Edits a suggestion.")]
        [Priority(1)]
        public async Task Rename([Summary("The short name.")] string shortName)
        {
            var channel = (SocketTextChannel)Context.Channel;
            var type = channel.GetSuggestionChannelType();
            if (!channel.IsSuggestionChannelByName())
            {
                throw new InvalidOperationException("Wrong channel!");
            }

            if (type == SuggestionType.Vote)
            {
                throw new Exception("Finalized suggestions cannot be renamed.");
            }

            var user = (IGuildUser)Context.User;
            var vote = await Database.UNSAFE_GetGovernanceVoteAsync(channel.Id);
            if (vote == null)
            {
                throw new Exception("Cannot find information about this suggestion in database!");
            }

            if (vote.UserId != user.Id && !user.IsStaff())
            {
                throw new Exception("Only the owner can rename suggestions.");
            }

            var submissionOld = channel.GetSuggestionChannelName();
            var submissionNew = shortName;
            var editEmbed = EditWatcher.GetEditEmbed(user, "renamed the suggestion", submissionOld, submissionNew);
            if (editEmbed != null)
            {
                await channel.RenameSuggestionChannel(submissionNew);
                await channel.SendMessageAsync(string.Empty, embed: editEmbed);
            }
        }

        [Command("edit")]
        [Summary("Edits a suggestion.")]
        [Priority(1)]
        public async Task Edit()
        {
            var channel = (SocketTextChannel)Context.Channel;
            if (!channel.IsSuggestionChannelByName())
            {
                throw new InvalidOperationException("Wrong channel!");
            }

            if (channel.GetSuggestionChannelType() == SuggestionType.Vote)
            {
                throw new Exception("Finalized suggestions cannot be edited.");
            }

            var user = (IGuildUser)Context.User;
            var vote = await Database.UNSAFE_GetGovernanceVoteAsync(channel.Id);
            if (vote == null)
            {
                throw new Exception("Cannot find information about this suggestion in database!");
            }

            if (vote.UserId != user.Id && !user.IsStaff())
            {
                throw new Exception("Only the owner or staff can edit suggestions.");
            }

            var message = (IUserMessage)await channel.GetMessageAsync(vote.MessageId);
            var author = Context.Client.GetUser(vote.UserId);

            await Context.Message.DeleteAsync();

            var suggestionMessage = await GetSuggestionFromUserAsync(channel, author);
            await (suggestionMessage?.DeleteAsync() ?? Task.CompletedTask);
            if (suggestionMessage != null)
            {
                var submissionOld = message.Embeds.OfType<Embed>().FirstOrDefault();
                var submissionNew = suggestionMessage.Embeds.OfType<Embed>().FirstOrDefault();
                var editEmbed = EditWatcher.GetEditEmbed(user, "edited their suggestion", submissionOld.Description,
                    submissionNew.Description);
                if (editEmbed != null)
                {
                    await message.ModifyAsync(props => props.Embed = submissionNew);
                    await channel.ModifyAsync(props => props.Topic = submissionNew.Description);
                    await channel.SendMessageAsync(string.Empty, embed: editEmbed);
                    await GovernanceSubscriptionFeed.OnEditAsync(Context.Client, channel.Id);
                }
            }
        }

        [Command("approve")]
        [Alias("pass")]
        [Summary("Approves an ammendment.")]
        [RequireRole(DiscordSettings.DiscordServerOwner)]
        [Priority(1)]
        public async Task Approve([Remainder] string reason = null)
        {
            await Archive("approved", Color.Green, reason);
        }

        [Command("reject")]
        [Alias("veto")]
        [Summary("Rejects an ammendment")]
        [RequireRole(DiscordSettings.DiscordServerOwner)]
        [Priority(1)]
        public async Task Reject([Remainder] string reason = null)
        {
            await Archive("rejected", Color.Red, reason);
        }

        private async Task Archive(string status, Color color, string reason)
        {
            var channel = Context.Channel;
            if (!channel.IsSuggestionChannelByName())
            {
                throw new InvalidOperationException("Wrong channel!");
            }

            await Context.Message.DeleteAsync();
            var textChannel = (SocketTextChannel)channel;
            await ArchiveChannel(channel, async (text, message) =>
            {
                var stream = GenerateStreamFromString(text);
                var guild = Context.Guild;
                var config = guild.GetGovernanceConfig();
                // this is me being lazy. if the changelog is on the main server,
                // then we'll ping governance subscriber
                var beginning = config.ChangelogChannel == 549402714103087144 ? $"[<@&{DiscordSettings.MapSubscriptionToRole[Commands.SubscriptionType.Governance]}>] " : "";
                var baseEmbed = await AddVotesAsync(guild,
                    message.Embeds.FirstOrDefault()?.ToEmbedBuilder() ?? new EmbedBuilder(), message);
                await guild.GetTextChannel(config.ChangelogChannel).SendFileAsync(stream,
                    $"log_{channel.GetSuggestionChannelName()}.txt",
                    $"{beginning}\"{channel.GetSuggestionChannelName()}\" was {status} by {MentionUtils.MentionUser(Context.User.Id)}. {reason}",
                    embed: baseEmbed
                        .WithColor(color)
                        .Build());
            });

            await textChannel.DeleteAsync();
            await Database.UNSAFE_RemoveGovernanceVoteAsync(channel.Id);
        }

        // DI would be nice wouldn't it
        private static System.Net.Http.HttpClient _httpClient = new System.Net.Http.HttpClient();

		private async Task ArchiveChannel(ISocketMessageChannel channel,
            Func<string, IUserMessage, Task> callback)
        {
            var guild = Context.Client.GetGuild(DiscordSettings.GuildId);
            var uploadAttachments = guild.GetTextChannel(DiscordSettings.ImageArchiveChannel);

            var history = new StringBuilder();
            var messages = (await channel.GetMessagesAsync(5000).FlattenAsync()).ToList();
            foreach (var message in Enumerable.Reverse(messages))
            {
                history.Append('[').Append(message.Timestamp.ToUniversalTime().ToString("MM/dd/yyyy HH:mm:ss"))
                    .Append(" UTC] ");
                history.Append(message.Author.Username.DiscordEscape()).Append('#')
                    .Append(message.Author.Discriminator ?? "@" + message.Author.Id);
                history.Append(": ");

                if (message is IUserMessage userMsg)
                {
                    history.Append(userMsg.Resolve());

                    if (userMsg.Reactions.Count > 0)
                    {
                        history.Append(" (").Append(string.Join(", ",
                            userMsg.Reactions.Select(r => r.Key.Name + ": " + r.Value.ReactionCount))).Append(')');
                    }
                }

                if (message.Author.IsBot)
                {
                    foreach (var embed in message.Embeds)
                    {
                        history.Append(' ').Append(embed.Author?.Name).Append(' ').Append(embed.Description);
                    }
                }

                foreach (var attachment in message.Attachments)
                {
                    try
                    {
                        var get = await _httpClient.GetAsync(attachment.Url);
                        var stream = await get.Content.ReadAsStreamAsync();
                        var file = await uploadAttachments.SendFileAsync(stream, attachment.Filename, "for " + channel.Name);

                        history.Append(" (Attachment: ").Append(file.Attachments.First().Url).Append(')');
                    }
                    catch (Exception ex)
                    {
                        history.Append(" (Attachment: ").Append(attachment.Url).Append(" | error uploading to image archive: ")
                            .Append(ex.ToString()).Append(')');
					}
                }

                if (message.EditedTimestamp.HasValue)
                {
                    history.Append(" (Edited: ")
                        .Append(message.EditedTimestamp.Value.ToUniversalTime().ToString("MM/dd/yyyy HH:mm:ss"))
                        .Append(" UTC)");
                }

                history.AppendLine();
            }

            await callback(history.ToString(), (IUserMessage)messages.Last(m => m.Author.IsBot && m.Embeds.Any()));
        }

        private static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }
}