using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;

using ForumCrawler;
using ForumCrawler.Helpers;

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ForumCrawler
{
    [Group("suggest"), Alias("suggestion"), Summary("These commands are used to manage the server.")]
    public class GovernanceCommands : InteractiveBase<SocketCommandContext>
    {
        [
            Command,
            Summary("Starts the amendment process."),
            Priority(0)
        ]
        public async Task Suggest([Summary("The short name.")] string shortName)
        {
            await CreateSuggestionChannel(shortName, Context.User,
                channel => GetSuggestionFromUserAsync(channel, Context.User));
        }

        [
            Command("upgrade"), Alias("import"),
            Summary("Upgrades an existing message to a suggestion"),
            Priority(1)
        ]
        public async Task Upgrade(ulong messageId, string shortName)
        {
            var message = await Context.Channel.GetMessageAsync(messageId);
            if (message == null) throw new Exception("Message with the given ID was not found in this channel.");

            await CreateSuggestionChannel(shortName, message.Author,
                async channel =>
                {
                    await channel.SendMessageAsync($"Suggestion upgraded by {MentionUtils.MentionUser(Context.User.Id)}.");
                    return await ConfirmSuggestionFromUserAsync(channel, message.Author, message.Content);
                });
        }

        // imagine using a library for 4 lines of code
        private static string ToHumanCounter(int number, string unit) => $"{number} {unit}{(number == 1 ? "" : "s")}";

        [Command("finalize"), Summary("Finalizes a suggestion."), Priority(1)]
        public async Task Finalize(bool force = false)
        {
            var channel = Context.Channel;
            if (!channel.IsSuggestionChannelByName()) throw new InvalidOperationException("Wrong channel!");
            if (channel.IsSuggestionChannelFinalized()) throw new Exception("Finalized suggestions cannot be finalized.");

            var user = (IGuildUser)Context.User;
            var guild = Context.Guild;
            var vote = await Database.GetGovernanceVoteAsync(channel.Id);
            var age = DateTimeOffset.UtcNow - channel.CreatedAt;
            var ageInDays = age.TotalDays;
            var ageLeft = TimeSpan.FromTicks((channel.CreatedAt.AddDays(3) - DateTimeOffset.UtcNow).Ticks);
            if (vote == null) throw new Exception("Cannot find information about this suggestion in database!");
            if (vote.UserId != user.Id && !user.IsStaff() && ageInDays < 10) throw new Exception("Only the owner or staff can finalize suggestions.");
            if (ageInDays < 3 && (!force || !user.IsStaff()))
            {
                throw new Exception($"Suggestion too young. " +
                $"{ToHumanCounter(ageLeft.Days, "day")}, {ToHumanCounter(ageLeft.Hours, "hour")}, {ToHumanCounter(ageLeft.Minutes, "minute")} left.");
            }

            var textChannel = (SocketTextChannel)channel;
            var message = (IUserMessage)await Context.Channel.GetMessageAsync(vote.MessageId);

            await Context.Message.DeleteAsync();

            await textChannel.ModifyAsync(props => props.Name = "vote" + textChannel.Name);
            await ReorderChannels();

            await message.AddReactionsAsync(new[] { new Emoji("👍"), new Emoji("👎") });
            await UpdateBillboardAsync(Context.Guild, message, channel, vote);

            await channel.SendMessageAsync(":white_check_mark: Suggestion was finalized. You may now vote!");
        }

        [Command("rename"), Summary("Edits a suggestion."), Priority(1)]
        public async Task Rename([Summary("The short name.")] string shortName)
        {
            var channel = Context.Channel;
            if (!channel.IsSuggestionChannelByName()) throw new InvalidOperationException("Wrong channel!");
            if (channel.IsSuggestionChannelFinalized()) throw new Exception("Finalized suggestions cannot be renamed.");

            var user = (IGuildUser)Context.User;
            var guild = Context.Guild;
            var vote = await Database.GetGovernanceVoteAsync(channel.Id);
            if (vote == null) throw new Exception("Cannot find information about this suggestion in database!");
            if (vote.UserId != user.Id && !user.IsStaff()) throw new Exception("Only the owner can rename suggestions.");

            var textChannel = (SocketTextChannel)channel;
            var message = (IUserMessage)await Context.Channel.GetMessageAsync(vote.MessageId);

            await Context.Message.DeleteAsync();

            var submissionOld = channel.Name;
            var submissionNew = shortName;
            var editEmbed = EditWatcher.GetEditEmbed(user, "renamed their suggestion", submissionOld, submissionNew);
            if (editEmbed != null)
            {
                await textChannel.ModifyAsync(props =>
                {
                    props.Name = "_" + submissionNew;
                });
                await channel.SendMessageAsync(string.Empty, embed: editEmbed);
            }
        }

        [Command("edit"), Summary("Edits a suggestion."), Priority(1)]
        public async Task Edit()
        {
            var channel = Context.Channel;
            if (!channel.IsSuggestionChannelByName()) throw new InvalidOperationException("Wrong channel!");
            if (channel.IsSuggestionChannelFinalized()) throw new Exception("Finalized suggestions cannot be edited.");

            var user = (IGuildUser)Context.User;
            var guild = Context.Guild;
            var vote = await Database.GetGovernanceVoteAsync(channel.Id);
            if (vote == null) throw new Exception("Cannot find information about this suggestion in database!");
            if (vote.UserId != user.Id && !user.IsStaff()) throw new Exception("Only the owner can edit suggestions.");
            var textChannel = (SocketTextChannel)channel;
            var message = (IUserMessage)await Context.Channel.GetMessageAsync(vote.MessageId);

            await Context.Message.DeleteAsync();

            var suggestionMessage = await GetSuggestionFromUserAsync(channel, user);
            await (suggestionMessage?.DeleteAsync() ?? Task.CompletedTask);
            if (suggestionMessage != null)
            {
                var submissionOld = message.Embeds.OfType<Embed>().FirstOrDefault();
                var submissionNew = suggestionMessage.Embeds.OfType<Embed>().FirstOrDefault();
                var editEmbed = EditWatcher.GetEditEmbed(user, "edited their suggestion", submissionOld.Description, submissionNew.Description);
                if (editEmbed != null)
                {
                    await message.ModifyAsync(props => props.Embed = submissionNew);
                    await textChannel.ModifyAsync(props => props.Topic = submissionNew.Description);
                    await channel.SendMessageAsync(string.Empty, embed: editEmbed);
                }
            }
        }

        [Command("approve"), Alias("pass"), Summary("Approves an ammendment."), RequireRole(DiscordSettings.DiscordServerOwner), Priority(1)]
        public async Task Approve() => await Archive("approved", Color.Green);

        [Command("reject"), Alias("veto"), Summary("Rejects an ammendment"), RequireRole(DiscordSettings.DiscordServerOwner), Priority(1)]
        public async Task Reject() => await Archive("rejected", Color.Red);

        private async Task CreateSuggestionChannel(string shortName, IUser owner, Func<IMessageChannel, Task<IUserMessage>> messageGenerator)
        {
            await Context.Message.DeleteAsync();

            var user = Context.User;
            var guildUser = (IGuildUser)user;
            if (!guildUser.IsStaffOrConsultant()) throw new Exception("Only staff can suggest new features.");

            var guild = Context.Guild;

            var channels = guild.CategoryChannels.First(c => c.Id == DiscordSettings.GovernanceArea).Channels.OrderBy(c => c.Position).ToList();
            await guild.ReorderChannelsAsync(channels.Select((c, i) => new ReorderChannelProperties(c.Id, i)));
            var channel = await guild.CreateTextChannelAsync("_" + shortName, props =>
            {
                props.CategoryId = DiscordSettings.GovernanceArea;
            });
            await ReorderChannels();

            await channel.AddPermissionOverwriteAsync(user,
                new OverwritePermissions(viewChannel: PermValue.Allow, sendMessages: PermValue.Allow));

            var message = await messageGenerator(channel);
            if (message == null)
            {
                await channel.DeleteAsync();
                return;
            }

            await channel.ModifyAsync(props =>
            {
                props.Topic = message.Embeds.First().Description;
            });

            var voteBillboardMessage = await channel.SendMessageAsync($":warning: Draft mode, edits to suggestion are allowed but voting is disabled.\n"
                + $"{MentionUtils.MentionUser(owner.Id)} Available commands: `g!suggestion edit`, `g!suggestion rename [channel-name]`, `g!suggestion finalize`.");

            await message.PinAsync();
            await voteBillboardMessage.PinAsync();

            await Database.AddGovernanceVoteAsync(new GovernanceVote
            {
                UserId = owner.Id,
                MessageId = message.Id,
                ChannelId = channel.Id,
                VoteBillboardId = voteBillboardMessage.Id
            });

            var perms = channel.GetPermissionOverwrite(guild.EveryoneRole) ?? new OverwritePermissions();
            await channel.AddPermissionOverwriteAsync(guild.EveryoneRole, perms.Modify(viewChannel: PermValue.Allow));
        }

        private async Task<IUserMessage> GetSuggestionFromUserAsync(IMessageChannel channel, IUser user)
        {
            var tempMsg = await channel.SendMessageAsync(MentionUtils.MentionUser(user.Id) + " type your suggestion here...");
            var result = await NextMessageAsync(GetCriteria(channel), timeout: TimeSpan.FromMinutes(5));
            await tempMsg.DeleteAsync();
            await (result?.DeleteAsync() ?? Task.CompletedTask);
            if (result == null)
            {
                return null;
            }
            else
            {
                var suggestion = result.Content;
                return await ConfirmSuggestionFromUserAsync(channel, user, suggestion);
            }
        }

        private async Task<IUserMessage> ConfirmSuggestionFromUserAsync(IMessageChannel channel, IUser user, string suggestion)
        {
            var embed = GetSuggestionEmbed(user, suggestion);
            var message = await channel.SendMessageAsync(embed: embed);

            var tempMsg2 = await channel.SendMessageAsync("Looks good? Type `submit` to publish this suggestion!");

            var result2 = await NextMessageAsync(GetCriteria(channel), timeout: TimeSpan.FromMinutes(5));
            await tempMsg2.DeleteAsync();
            await result2?.DeleteAsync();

            if (result2?.Content.ToLowerInvariant() == "submit")
            {
                return message;
            }
            else
            {
                await message.DeleteAsync();
                return null;
            }
        }

        private static Embed GetSuggestionEmbed(IUser user, string suggestion)
        {
            return new EmbedBuilder()
             .WithAuthor(author => author
                 .WithIconUrl(user.GetAvatarUrlOrDefault())
                 .WithName(user.Username + "#" + user.Discriminator + " suggests:"))
             .WithColor(Color.Blue)
             .WithDescription(suggestion.Substring(0, Math.Min(suggestion.Length, 2048))).Build();
        }

        private async Task Archive(string status, Color color)
        {
            var channel = Context.Channel;
            if (!channel.IsSuggestionChannelByName()) throw new InvalidOperationException("Wrong channel!");

            await Context.Message.DeleteAsync();
            var textChannel = (SocketTextChannel)channel;
            await ArchiveChannel(channel, async (text, message) =>
            {
                var stream = GenerateStreamFromString(text);
                var baseEmbed = await AddVotesAsync(Context.Guild, message.Embeds.FirstOrDefault()?.ToEmbedBuilder() ?? new EmbedBuilder(), message);
                await Context.Guild.GetTextChannel(DiscordSettings.ChangelogChannel).SendFileAsync(stream, $"log_{channel.Name}.txt",
                    $"\"{channel.Name}\" was {status} by {MentionUtils.MentionUser(Context.User.Id)}.",
                    embed: baseEmbed
                    .WithColor(color)
                    .Build());
            });

            await textChannel.DeleteAsync();
            await Database.RemoveGovernanceVoteAsync(channel.Id);
        }

        public async Task ReorderChannels()
        {
            var guild = Context.Guild;
            var channels = guild.CategoryChannels.First(c => c.Id == DiscordSettings.GovernanceArea).Channels.OrderByDescending(c => c.Id).ToList();
            var normal = channels.Where(c => !c.IsSuggestionChannelByName()).Reverse();
            var vote = channels.Where(c => c.Name.StartsWith("vote_"));
            var draft = channels.Where(c => c.Name.StartsWith("_"));
            await guild.ReorderChannelsAsync(normal.Concat(vote).Concat(draft).Select((c, i) => new ReorderChannelProperties(c.Id, i)));
        }

        private static async Task ArchiveChannel(ISocketMessageChannel channel, Func<string, IUserMessage, Task> callback)
        {
            var history = new StringBuilder();
            var messages = (await channel.GetMessagesAsync(limit: 5000).FlattenAsync()).ToList();
            foreach (var message in Enumerable.Reverse(messages))
            {
                history.Append('[').Append(message.Timestamp.ToUniversalTime().ToString("MM/dd/yyyy HH:mm:ss")).Append(" UTC] ");
                history.Append(message.Author.Username).Append('#').Append(message.Author.Discriminator ?? "@" + message.Author.Id.ToString());
                history.Append(": ");

                if (message is IUserMessage userMsg)
                {
                    history.Append(userMsg.Resolve());

                    if (userMsg.Reactions.Count > 0)
                    {
                        history.Append(" (").Append(string.Join(", ", userMsg.Reactions.Select(r => r.Key.Name + ": " + r.Value.ReactionCount))).Append(')');
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
                    history.Append(" (Attachment: ").Append(attachment.Url).Append(')');
                }

                if (message.EditedTimestamp.HasValue)
                {
                    history.Append(" (Edited: ").Append(message.EditedTimestamp.Value.ToUniversalTime().ToString("MM/dd/yyyy HH:mm:ss")).Append(" UTC)");
                }
                history.AppendLine();
            }
            await callback(history.ToString(), (IUserMessage)messages.Last(m => m.Author.IsBot && m.Embeds.Any()));
        }

        public static async Task UpdateBillboardAsync(IGuild guild, IUserMessage message, ISocketMessageChannel channel, GovernanceVote vote)
        {
            var billboard = (IUserMessage)await channel.GetMessageAsync(vote.VoteBillboardId);
            if (billboard == null) return;
            var embed = await AddVotesAsync(guild, new EmbedBuilder(), message, user => user.IsStaffOrConsultant());
            await billboard.ModifyAsync(props =>
            {
                props.Content = string.Empty;
                props.Embed = embed
                .WithTitle("Staff votes").Build();
            });
        }

        private static async Task<EmbedBuilder> AddVotesAsync(IGuild guild, EmbedBuilder builder, IUserMessage message, Func<IGuildUser, bool> filter = null)
        {
            var formatting = new Func<IGuildUser, string>(user =>
            {
                var format = user.IsStaffOrConsultant() ? "__" : "";
                return format + user.Username + "#" + user.Discriminator + format;
            });
            var approvers = (await message.GetReactionUsersAsync(new Emoji("👍"), 1000).FlattenAsync())
				.Select(user => guild.GetUserAsync(user.Id).Result)
				.Where(user => user != null)
				.OrderByDescending(user => user.IsStaffOrConsultant())
				.Where(user => (filter?.Invoke(user) ?? true) && !user.IsBot)
				.Select(user => formatting(user))
                .ToList();
            var decliners = (await message.GetReactionUsersAsync(new Emoji("👎"), 1000).FlattenAsync())
				.Select(user => guild.GetUserAsync(user.Id).Result)
				.Where(user => user != null)
				.OrderByDescending(user => user.IsStaffOrConsultant())
				.Where(user => (filter?.Invoke(user) ?? true) && !user.IsBot)
				.Select(user => formatting(user))
                .ToList();
            return builder
                .AddField($":thumbsup: ({approvers.Count})", approvers.Count == 0 ? "Nobody" : string.Join(", ", approvers))
                .AddField($":thumbsdown: ({decliners.Count})", decliners.Count == 0 ? "Nobody" : string.Join(", ", decliners));
        }

        private Criteria<SocketMessage> GetCriteria(IMessageChannel channel)
        {
            var criteria = new Criteria<SocketMessage>();
            criteria.AddCriterion(new EnsureFromChannelCriterion(channel));
            criteria.AddCriterion(new EnsureFromUserCriterion(Context.User));
            return criteria;
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

public static class UserExtensions
{
    public static bool IsStaff(this IGuildUser guildUser) => guildUser.RoleIds.Contains(DiscordSettings.DiscordStaff);

    public static bool IsStaffOrConsultant(this IGuildUser guildUser)
    {
        return guildUser.RoleIds.Contains(DiscordSettings.DiscordStaff) ||
            guildUser.RoleIds.Contains(DiscordSettings.DiscordStaffConsultant);
    }

    public static bool IsSuggestionChannelByName(this IChannel channel) => channel.Name.StartsWith("_") || channel.Name.StartsWith("vote_");

    public static bool IsSuggestionChannelFinalized(this IChannel channel) => channel.Name.StartsWith("vote_");
}