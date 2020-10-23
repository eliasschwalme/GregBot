using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;

using ForumCrawler.Helpers;

namespace ForumCrawler
{
    public abstract class GovernanceCommandsBase : InteractiveBase<SocketCommandContext>
    {
        public static async Task UpdateBillboardAsync(IGuild guild, IUserMessage message, ISocketMessageChannel channel,
            GovernanceVote vote, SuggestionType type)
        {
            var msg = GetBillboardMessage(type);

            var billboard = (IUserMessage)await channel.GetMessageAsync(vote.VoteBillboardId);
            if (billboard == null)
            {
                return;
            }

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

        protected static async Task<EmbedBuilder> AddVotesAsync(IGuild guild, EmbedBuilder builder,
            IUserMessage message)
        {
            var voteUsers = (await Task.WhenAll(message.Reactions.Keys
                    .Select(async emoji =>
                        new {emoji, users = await message.GetReactionUsersAsync(emoji, 1000).FlattenAsync()})))
                .Select(kv => new
                {
                    kv.emoji,
                    users = kv.users
                        .Select(user => guild.GetUserAsync(user.Id, CacheMode.CacheOnly).Result)
                        .Where(user => user != null)
                        .ToList()
                })
                .Where(kv => kv.users.Any(u => u.IsBot || u.IsStaffOrConsultant()))
                .ToDictionary(kv => kv.emoji, kv => kv.users);

            var scoreUsers = (await Database.UNSAFE_GetScoreUsers(voteUsers.Values
                    .SelectMany(x => x)
                    .Select(a => a.Id)))
                .ToDictionary(u => u.UserId, u => u);

            var getScore = new Func<IGuildUser, ScoreData>(user =>
            {
                if (scoreUsers.TryGetValue(user.Id, out var value))
                {
                    return value.ScoreData;
                }

                return new ScoreData();
            });

            var formatting = new Func<IGuildUser, ScoreData, string>((user, score) =>
            {
                var format = user.IsStaffOrConsultant() ? "__" : "";
                return format + user.Username.DiscordEscape() + "#" + user.Discriminator + format + " (" +
                       score.ShortBaseScoreString + ")";
            });


            foreach (var kv in voteUsers)
            {
                var reacters = kv.Value.Where(user => !user.IsBot)
                    .Select(user => new {user, score = getScore(user)})
                    .OrderByDescending(userData => userData.score.BaseScoreLevel)
                    .ThenByDescending(userData => userData.user.IsStaffOrConsultant())
                    .ThenBy(userData => userData.user.Username)
                    .Select(userData => new {formatted = formatting(userData.user, userData.score), userData.score})
                    .ToList();
                builder.AddField(
                    $"{kv.Key} **(Votes: {reacters.Count}, Sum of Scores: {reacters.Sum(a => a.score.BaseScoreLevel).ToString("F1", CultureInfo.InvariantCulture)})**",
                    reacters.Count == 0 ? "Nobody" : string.Join(", ", reacters.Select(a => a.formatted)));
            }

            return builder;
        }

        protected async Task CreateSuggestionChannel(SuggestionType type, string shortName, IUser owner,
            Func<IMessageChannel, Task<IUserMessage>> messageGenerator)
        {
            await Context.Message.DeleteAsync();

            var user = Context.User;
            var guildUser = (IGuildUser)user;
            if (!guildUser.IsStaffOrConsultant())
            {
                throw new Exception("Only staff can suggest new features.");
            }

            var guild = Context.Guild;
            var config = guild.GetGovernanceConfig();

            var channel = await guild.CreateSuggestionChannel(config.Category, type, shortName);
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

            var msg = GetBillboardMessage(type);
            var voteBillboardMessage = await channel.SendMessageAsync(msg);

            await message.PinAsync();
            await voteBillboardMessage.PinAsync();

            await Database.UNSAFE_AddGovernanceVoteAsync(new GovernanceVote
            {
                UserId = owner.Id,
                MessageId = message.Id,
                ChannelId = channel.Id,
                VoteBillboardId = voteBillboardMessage.Id
            });

            var perms = channel.GetPermissionOverwrite(guild.EveryoneRole) ?? new OverwritePermissions();
            var targetPerms = new OverwritePermissions(
                (perms.AllowValue | config.EveryonePermissionsAfterSubmission.AllowValue) &
                ~config.EveryonePermissionsAfterSubmission.DenyValue,
                (perms.DenyValue | config.EveryonePermissionsAfterSubmission.DenyValue) &
                ~config.EveryonePermissionsAfterSubmission.AllowValue
            );
            await channel.AddPermissionOverwriteAsync(guild.EveryoneRole, targetPerms);

            var letUberDoTimerStuff = new OverwritePermissions(
                readMessageHistory: PermValue.Allow,
                sendMessages: PermValue.Allow,
                viewChannel: PermValue.Allow
            );

            await channel.AddPermissionOverwriteAsync(guild.GetUser(85614143951892480 /* uber bot */), letUberDoTimerStuff);
        }

        protected static string GetBillboardMessage(SuggestionType type)
        {
            return type == SuggestionType.Draft
                ? ":warning: Draft mode. Edits to suggestion are allowed but voting is disabled.\n"
                  + "Available commands: `g!suggestion edit`, `g!suggestion rename [channel-name]`, `g!suggestion finalize` (owner and staff only)."
                : type == SuggestionType.RFC
                    ? ":scroll: RFC mode. Suggest incremental changes, no vote is scheduled.\n"
                      + "Available commands: `g!rfc edit`, `g!rfc rename [channel-name]` (owner and staff only)."
                    : "";
        }

        protected async Task<IUserMessage> GetSuggestionFromUserAsync(IMessageChannel channel, IUser author)
        {
            var tempMsg =
                await channel.SendMessageAsync(MentionUtils.MentionUser(Context.User.Id) +
                                               " Type your suggestion here...");
            var result = await NextMessageAsync(GetCriteria(channel), TimeSpan.FromMinutes(5));
            await tempMsg.DeleteAsync();
            await (result?.DeleteAsync() ?? Task.CompletedTask);
            if (result == null)
            {
                return null;
            }

            var suggestion = result.Content;
            return await ConfirmSuggestionFromUserAsync(channel, author, suggestion);
        }

        protected async Task<IUserMessage> ConfirmSuggestionFromUserAsync(IMessageChannel channel, IUser author,
            string suggestion)
        {
            var embed = GetSuggestionEmbed(author, suggestion);
            var message = await channel.SendMessageAsync(embed: embed);

            var tempMsg2 = await channel.SendMessageAsync("Looks good? Type `submit` to publish this suggestion!");

            var result2 = await NextMessageAsync(GetCriteria(channel), TimeSpan.FromMinutes(5));
            await tempMsg2.DeleteAsync();
            if (result2 != null)
            {
                await result2.DeleteAsync();

                if (result2?.Content.ToLowerInvariant() == "submit")
                {
                    return message;
                }
            }

            await message.DeleteAsync();
            return null;
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