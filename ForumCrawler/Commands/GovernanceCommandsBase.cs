using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using PastebinAPI;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ForumCrawler
{
    [Group("suggest"), Alias("suggestion")]
    public class SuggestionCommands : GovernanceCommandsBase
    {
        [
            Command(),
            Summary("Starts the amendment process."),
            Priority(0)
        ]
        public async Task Draft([Summary("The short name.")] string shortName)
        {
            await CreateSuggestionChannel(SuggestionType.Draft, shortName, Context.User,
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

            await CreateSuggestionChannel(SuggestionType.Draft, shortName, message.Author,
                async channel =>
                {
                    await channel.SendMessageAsync($"Suggestion upgraded by {MentionUtils.MentionUser(Context.User.Id)}.");
                    return await ConfirmSuggestionFromUserAsync(channel, message.Author, message.Content);
                });
        }
        private static string ToHumanCounter(int number, string unit) => $"{number} {unit}{(number == 1 ? "" : "s")}";

        [Command("finalize"), Summary("Finalizes a suggestion."), Priority(1)]
        public async Task Finalize(bool force = false)
        {
            var channel = Context.Channel;
            var oldType = channel.GetSuggestionChannelType();
            if (!channel.IsSuggestionChannelByName()) throw new InvalidOperationException("Wrong channel!");

            var user = (IGuildUser)Context.User;
            var vote = await Database.GetGovernanceVoteAsync(channel.Id);
            var age = DateTimeOffset.UtcNow - channel.CreatedAt;
            var ageInDays = age.TotalDays;
            var ageLeft = TimeSpan.FromTicks((channel.CreatedAt.AddDays(3) - DateTimeOffset.UtcNow).Ticks);


            if (vote == null) throw new Exception("Cannot find information about this suggestion in database!");
            if (!user.IsStaff() && ageInDays < 10) throw new Exception("Only the staff can finalize suggestions.");
            if (oldType == SuggestionType.RFC) throw new Exception("RFCs cannot be finalized.");
            if (oldType == SuggestionType.Vote) throw new Exception("Finalized suggestions cannot be finalized.");

            if (ageInDays < 3 && (!force || !user.IsServerOwner()))
            {
                throw new Exception($"Suggestion too young. " +
                    $"{ToHumanCounter(ageLeft.Days, "day")}, {ToHumanCounter(ageLeft.Hours, "hour")}, {ToHumanCounter(ageLeft.Minutes, "minute")} left.");
            }

            var textChannel = (SocketTextChannel)channel;
            var message = (IUserMessage)await Context.Channel.GetMessageAsync(vote.MessageId);

            await textChannel.ConvertSuggestionChannelType(SuggestionType.Vote);

            await message.AddReactionsAsync(new[] { new Emoji("👍"), new Emoji("👎") });
            await UpdateBillboardAsync(Context.Guild, message, channel, vote, SuggestionType.Vote);

            var finalizeMsg = await channel.SendMessageAsync("", embed: new EmbedBuilder()
                .WithTitle(":white_check_mark: Suggestion was finalized. You may now vote!")
                .WithDescription($"[Click here to vote]({message.GetJumpUrl()})")
                .Build());
            await finalizeMsg.PinAsync();
        }
    }

    [Group("rfc")]
    public class RFCCommands : GovernanceCommandsBase
    {
        [
            Command(),
            Summary("Starts the amendment process."),
            Priority(0)
        ]
        public async Task RFC([Summary("The short name.")] string shortName)
        {
            await CreateSuggestionChannel(SuggestionType.RFC, shortName, Context.User,
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

            await CreateSuggestionChannel(SuggestionType.RFC, shortName, message.Author,
                async channel =>
                {
                    await channel.SendMessageAsync($"Suggestion upgraded by {MentionUtils.MentionUser(Context.User.Id)}.");
                    return await ConfirmSuggestionFromUserAsync(channel, message.Author, message.Content);
                });
        }
    }

    [Group("suggest"), Alias("suggestion", "rfc")]
    public class SharedGovernanceCommands : GovernanceCommandsBase
    {

        [
            Command("convert"),
            Summary("Converts an existing suggestion's type."),
            Priority(0)
        ]
        public async Task Convert(SuggestionType type, bool force = false)
        {
            var guildUser = (IGuildUser)Context.User;
            var channel = (SocketTextChannel)Context.Channel;
            var oldType = channel.GetSuggestionChannelType();
            if (oldType == SuggestionType.Vote && !force) throw new Exception("Suggestion is in voting mode. Converting it would delete all existing votes. To confirm, run the command again with the force flag set.");
            if (!guildUser.IsServerOwner()) throw new Exception("Only server owner can convert suggestions.");
            if (!channel.IsSuggestionChannelByName()) throw new InvalidOperationException("Wrong channel!");
            if (type == SuggestionType.Vote) throw new Exception("Use g!suggest finalize to start a vote.");
            var vote = await Database.GetGovernanceVoteAsync(channel.Id);
            if (vote == null) throw new Exception("Cannot find information about this suggestion in database!");
            var message = (IUserMessage)await Context.Channel.GetMessageAsync(vote.MessageId);

            await message.RemoveAllReactionsAsync();
            await channel.ConvertSuggestionChannelType(type);
            await UpdateBillboardAsync(Context.Guild, message, channel, vote, type);

            await channel.SendMessageAsync("Suggestion type changed to " + type + ".");
        }


        [Command("rename"), Summary("Edits a suggestion."), Priority(1)]
        public async Task Rename([Summary("The short name.")] string shortName)
        {
            var channel = (SocketTextChannel)Context.Channel;
            var type = channel.GetSuggestionChannelType();
            if (!channel.IsSuggestionChannelByName()) throw new InvalidOperationException("Wrong channel!");
            if (type == SuggestionType.Vote) throw new Exception("Finalized suggestions cannot be renamed.");

            var user = (IGuildUser)Context.User;
            var vote = await Database.GetGovernanceVoteAsync(channel.Id);
            if (vote == null) throw new Exception("Cannot find information about this suggestion in database!");
            if (vote.UserId != user.Id && !user.IsStaff()) throw new Exception("Only the owner can rename suggestions.");

            var submissionOld = channel.Name;
            var submissionNew = shortName;
            var editEmbed = EditWatcher.GetEditEmbed(user, "renamed the suggestion", submissionOld, submissionNew);
            if (editEmbed != null)
            {
                await channel.RenameSuggestionChannel(submissionNew);
                await channel.SendMessageAsync(string.Empty, embed: editEmbed);
            }
        }

        [Command("edit"), Summary("Edits a suggestion."), Priority(1)]
        public async Task Edit()
        {
            var channel = (SocketTextChannel)Context.Channel;
            if (!channel.IsSuggestionChannelByName()) throw new InvalidOperationException("Wrong channel!");
            if (channel.GetSuggestionChannelType() == SuggestionType.Vote) throw new Exception("Finalized suggestions cannot be edited.");

            var user = (IGuildUser)Context.User;
            var vote = await Database.GetGovernanceVoteAsync(channel.Id);
            if (vote == null) throw new Exception("Cannot find information about this suggestion in database!");
            if (vote.UserId != user.Id && !user.IsStaff()) throw new Exception("Only the owner or staff can edit suggestions.");
            var message = (IUserMessage)await channel.GetMessageAsync(vote.MessageId);
            var author = Context.Client.GetUser(vote.UserId);

            await Context.Message.DeleteAsync();

            var suggestionMessage = await GetSuggestionFromUserAsync(channel, author);
            await (suggestionMessage?.DeleteAsync() ?? Task.CompletedTask);
            if (suggestionMessage != null)
            {
                var submissionOld = message.Embeds.OfType<Embed>().FirstOrDefault();
                var submissionNew = suggestionMessage.Embeds.OfType<Embed>().FirstOrDefault();
                var editEmbed = EditWatcher.GetEditEmbed(user, "edited their suggestion", submissionOld.Description, submissionNew.Description);
                if (editEmbed != null)
                {
                    await message.ModifyAsync(props => props.Embed = submissionNew);
                    await channel.ModifyAsync(props => props.Topic = submissionNew.Description);
                    await channel.SendMessageAsync(string.Empty, embed: editEmbed);
                }
            }
        }

        [Command("approve"), Alias("pass"), Summary("Approves an ammendment."), RequireRole(DiscordSettings.DiscordServerOwner), Priority(1)]
        public async Task Approve([Remainder] string reason = null) => await Archive("approved", Color.Green, reason);

        [Command("reject"), Alias("veto"), Summary("Rejects an ammendment"), RequireRole(DiscordSettings.DiscordServerOwner), Priority(1)]
        public async Task Reject([Remainder] string reason = null) => await Archive("rejected", Color.Red, reason);

        private async Task Archive(string status, Color color, string reason)
        {
            var channel = Context.Channel;
            if (!channel.IsSuggestionChannelByName()) throw new InvalidOperationException("Wrong channel!");

            await Context.Message.DeleteAsync();
            var textChannel = (SocketTextChannel)channel;
            await ArchiveChannel(channel, async (text, message) =>
            {
                var stream = GenerateStreamFromString(text);
                var guild = Context.Guild;
                var config = guild.GetGovernanceConfig();
                var baseEmbed = await AddVotesAsync(guild, message.Embeds.FirstOrDefault()?.ToEmbedBuilder() ?? new EmbedBuilder(), message);
                await guild.GetTextChannel(config.ChangelogChannel).SendFileAsync(stream, $"log_{channel.GetSuggestionChannelName()}.txt",
                    $"\"{channel.GetSuggestionChannelName()}\" was {status} by {MentionUtils.MentionUser(Context.User.Id)}. {reason}",
                    embed: baseEmbed
                    .WithColor(color)
                    .Build());
            });

            await textChannel.DeleteAsync();
            await Database.RemoveGovernanceVoteAsync(channel.Id);
        }

        private static async Task ArchiveChannel(ISocketMessageChannel channel, Func<string, IUserMessage, Task> callback)
        {
            var history = new StringBuilder();
            var messages = (await channel.GetMessagesAsync(limit: 5000).FlattenAsync()).ToList();
            foreach (var message in Enumerable.Reverse(messages))
            {
                history.Append('[').Append(message.Timestamp.ToUniversalTime().ToString("MM/dd/yyyy HH:mm:ss")).Append(" UTC] ");
                history.Append(message.Author.Username.DiscordEscape()).Append('#').Append(message.Author.Discriminator ?? "@" + message.Author.Id.ToString());
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

    public abstract class GovernanceCommandsBase : InteractiveBase<SocketCommandContext>
    {
        public static async Task UpdateBillboardAsync(IGuild guild, IUserMessage message, ISocketMessageChannel channel, GovernanceVote vote, SuggestionType type)
        {
            var msg = GetBillboardMessage(type);

            var billboard = (IUserMessage)await channel.GetMessageAsync(vote.VoteBillboardId);
            if (billboard == null) return;

            if (type == SuggestionType.Vote)
            {
                var embed = await AddVotesAsync(guild, new EmbedBuilder(), message);
                await billboard.ModifyAsync(props =>
                {
                    props.Content = msg;
                    props.Embed = embed.WithTitle("Votes").Build();
                });
            }
            else
            {
                await billboard.ModifyAsync(props =>
                {
                    props.Content = msg;
                    props.Embed = null;
                });
            }
        }
        protected static async Task<EmbedBuilder> AddVotesAsync(IGuild guild, EmbedBuilder builder, IUserMessage message)
        {
            var voteUsers = (await Task.WhenAll(message.Reactions.Keys
                .Select(async emoji => new { emoji, users = await message.GetReactionUsersAsync(emoji, 1000).FlattenAsync() })))
                .Select(kv => new {
                    kv.emoji,
                    users = kv.users
                        .Select(user => guild.GetUserAsync(user.Id, CacheMode.CacheOnly).Result)
                        .Where(user => user != null)
                        .ToList()
                })
                .Where(kv => kv.users.Any(u => u.IsBot || u.IsStaffOrConsultant()))
                .ToDictionary(kv => kv.emoji, kv => kv.users);

            var scoreUsers = (await Database.GetScoreUsers(voteUsers.Values
                .SelectMany(x => x)
                .Select(a => a.Id)))
                .ToDictionary(u => u.UserId, u => u);

            var getScore = new Func<IGuildUser, ScoreData>((user) =>
            {
                if (scoreUsers.TryGetValue(user.Id, out var value)) return value.ScoreData;
                return new ScoreData();
            });

            var formatting = new Func<IGuildUser, ScoreData, string>((user, score) =>
            {
                var format = user.IsStaffOrConsultant() ? "__" : "";
                return format + user.Username.DiscordEscape() + "#" + user.Discriminator + format + " (" + score.ShortBaseScoreString + ")";
            });


            foreach (var kv in voteUsers)
            {
                var reacters = kv.Value.Where(user => !user.IsBot)
                    .Select(user => new { user, score = getScore(user) })
                    .OrderByDescending(userData => userData.score.BaseScoreLevel)
                    .ThenByDescending(userData => userData.user.IsStaffOrConsultant())
                    .ThenBy(userData => userData.user.Username)
                    .Select(userData => new { formatted = formatting(userData.user, userData.score), userData.score })
                    .ToList();
                builder.AddField($"{kv.Key} **({reacters.Sum(a => a.score.BaseScoreLevel).ToString("F1", CultureInfo.InvariantCulture)})**",
                        reacters.Count == 0 ? "Nobody" : string.Join(", ", reacters.Select(a => a.formatted)));
            }
            return builder;
        }

        protected async Task CreateSuggestionChannel(SuggestionType type, string shortName, IUser owner, Func<IMessageChannel, Task<IUserMessage>> messageGenerator)
        {
            await Context.Message.DeleteAsync();

            var user = Context.User;
            var guildUser = (IGuildUser)user;
            if (!guildUser.IsStaffOrConsultant()) throw new Exception("Only staff can suggest new features.");

            var guild = Context.Guild;
            var config = guild.GetGovernanceConfig();

            var channel = await guild.CreateSuggestionChannel(config.Category, type, shortName);
            await channel.AddPermissionOverwriteAsync(user, new OverwritePermissions(viewChannel: PermValue.Allow, sendMessages: PermValue.Allow));

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

            var msg = GetBillboardMessage(type);
            var voteBillboardMessage = await channel.SendMessageAsync(msg);

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
            var targetPerms = new OverwritePermissions(
                (perms.AllowValue | config.EveryonePermissionsAfterSubmission.AllowValue) & (~config.EveryonePermissionsAfterSubmission.DenyValue),
                (perms.DenyValue | config.EveryonePermissionsAfterSubmission.DenyValue) & (~config.EveryonePermissionsAfterSubmission.AllowValue)
            );
            await channel.AddPermissionOverwriteAsync(guild.EveryoneRole, targetPerms);
        }

        protected static string GetBillboardMessage(SuggestionType type)
        {
            return type == SuggestionType.Draft
                ? $":warning: Draft mode. Edits to suggestion are allowed but voting is disabled.\n"
                + $"Available commands: `g!suggestion edit`, `g!suggestion rename [channel-name]`, `g!suggestion finalize` (owner and staff only)."
                : type == SuggestionType.RFC
                ? $":scroll: RFC mode. Suggest incremental changes, no vote is scheduled.\n"
                + $"Available commands: `g!rfc edit`, `g!rfc rename [channel-name]` (owner and staff only)."
                : "";
        }

        protected async Task<IUserMessage> GetSuggestionFromUserAsync(IMessageChannel channel, IUser author)
        {
            var tempMsg = await channel.SendMessageAsync(MentionUtils.MentionUser(Context.User.Id) + " Type your suggestion here...");
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
                return await ConfirmSuggestionFromUserAsync(channel, author, suggestion);
            }
        }

        protected async Task<IUserMessage> ConfirmSuggestionFromUserAsync(IMessageChannel channel, IUser author, string suggestion)
        {
            var embed = GetSuggestionEmbed(author, suggestion);
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
                 .WithName(user.Username.DiscordEscape() + "#" + user.Discriminator + " suggests:"))
             .WithColor(Color.Blue)
             .WithDescription(suggestion.Substring(0, Math.Min(suggestion.Length, 2048))).Build();
        }


        private Criteria<SocketMessage> GetCriteria(IMessageChannel channel)
        {
            var criteria = new Criteria<SocketMessage>();
            criteria.AddCriterion(new EnsureFromChannelCriterion(channel));
            criteria.AddCriterion(new EnsureFromUserCriterion(Context.User));
            return criteria;
        }
    }
}