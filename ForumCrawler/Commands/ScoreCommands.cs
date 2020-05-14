using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ForumCrawler;

using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordSocialScore
{
    [Group]
    public class ScoreCommands : ModuleBase<SocketCommandContext>
    {
        public ScoreCommands()
        {
        }

        [Command("help"), RequireChannel(DiscordSettings.BotCommandsChannel)]
        public async Task GetHelp() => await ReplyAsync($"Help is here! <https://gist.github.com/Yonom/126e7b6741f4e865c8b41e9488cf578a>");

        [Command("history"), RequireChannel(DiscordSettings.BotCommandsChannel), Priority(0)]
        public async Task GetMyHistory() => await GetHistory(Context.User as IGuildUser);

        [Command("history"), RequireChannel(DiscordSettings.BotCommandsChannel), Priority(1)]
        public async Task GetHistory(IGuildUser user)
        {
            var history = await Score.GetHistoryAsync(Context.Client, user.Id);
            var historyString = history.Count == 0
                ? " nobody."
                : history.Count == 1
                    ? $" {GetHistoryAsync(history[0])}."
                    : ": ```yml\n" +
                        string.Join("\n", history.Select(a => $"- {GetHistoryAsync(a)}")) + "\n" +
                        "```";
            await ReplyAsync($"{user.GetName()} has been boosted by{historyString}");
        }

        [Command("boosts"), RequireChannel(DiscordSettings.BotCommandsChannel), Priority(0)]
        public async Task GetMyBoosts() => await GetBoosts(Context.User as IGuildUser);

        [Command("boosts"), RequireChannel(DiscordSettings.BotCommandsChannel), Priority(1)]
        public async Task GetBoosts(IGuildUser user)
        {
            var boosts = await Score.GetBoostsAsync(Context.Client, user.Id);
            var boostString = boosts.Count == 0
                ? " nobody."
                : boosts.Count == 1
                    ? $" {GetBoostString(boosts[0])}."
                    : ": ```yml\n" +
                        string.Join("\n", boosts.Select(a => $"- {GetBoostString(a)}")) + "\n" +
                        "```";
            await ReplyAsync($"{user.GetName()} is being boosted by{boostString}");
        }

        [Command("boosting"), RequireChannel(DiscordSettings.BotCommandsChannel), Priority(2)]
        public async Task GetUsersUserIsBoosting()
        {
            var boosting = await Score.GetUsersUserHasBoosted(Context.Client.GetGuild(DiscordSettings.GuildId), Context.User);

            switch (boosting.Count)
            {
                case 0:
                {
                    await ReplyAsync("You are boosting nobody.");
                }
                break;

                case 1:
                {
                    var userId = boosting[0].Item1.Id;
                    var boostTime = boosting[0].Item2.Boosts[Context.User.Id];

                    await ReplyAsync($"You are boosting `{GetHistoryAsync((userId, boostTime))}`");
                }
                break;

                default:
                {
                    var strb = new StringBuilder();

                    foreach (var user in boosting.OrderByDescending(x => x.Item2.Boosts[Context.User.Id]))
                    {
                        var userId = user.Item1.Id;
                        var boostTime = user.Item2.Boosts[Context.User.Id];

                        strb.AppendLine(GetHistoryAsync((userId, boostTime)));
                    }

                    await ReplyAsync($"You are boosting: ```yml\n{strb}```");
                }
                break;
            }
        }

        [Command("stats"), Alias("score"), Priority(0), RequireChannel(DiscordSettings.BotCommandsChannel)]
        public async Task GetMyStats() => await GetStats(Context.User);

        [Command("stats"), Alias("score"), Priority(1), RequireChannel(DiscordSettings.BotCommandsChannel)]
        public async Task GetStats(IUser userObj)
        {
            var user = Context.Client.GetGuild(DiscordSettings.GuildId).GetUser(userObj.Id);
            const string WootString = "<:woot:329697747701727235>";
            var hs = await Database.UNSAFE_GetOrCreateScoreUserAndLeaderboardPositionAsync(Context.Client, user.Id);
            var score = hs.Item1;
            var boostStr = score.BonusScore > 0 ? $" (+{score.BonusScore:F1})" : "";
            await ReplyAsync($"[#{hs.Item2}] **{user.GetName()}**'s stats:", embed: new EmbedBuilder().WithDescription(
                Emote.Parse(WootString).ToString() + $" **Score:** {score.Score:F3}{boostStr}\n" +
                $":gem: **Gems:** {score.Gems}\n" +
                $":rocket: **Inertia:** {score.Inertia * 100:F0}%").Build());
        }

        [Command("transfer"), RequireRole(DiscordSettings.DiscordServerOwner)]
        public async Task Transfer(IUser user1, IUser user2)
        {
            await Score.SwapUsers(Context.Client, user1.Id, user2.Id);
            await ReplyAsync($"Swapped {MentionUtils.MentionUser(user1.Id)}'s user data with {MentionUtils.MentionUser(user1.Id)}'s.");
        }

        [Command("preview daily"), RequireChannel(DiscordSettings.BotCommandsChannel)]
        public async Task Daily()
        {
            await Daily((IGuildUser)Context.User);
        }

        [Command("preview daily"), RequireChannel(DiscordSettings.BotCommandsChannel)]
        public async Task Daily(IGuildUser targetUser)
        {
            var (scoreData, amount) = await Score.DailyAsync(Context.Client, targetUser.Id, Context.User.Id);

            await ReplyAsync(
                $"{MentionUtils.MentionUser(Context.User.Id)} gave {MentionUtils.MentionUser(targetUser.Id)} their {amount} daily gems. " +
                $"They now have {scoreData.Gems} in total."
            );
        }

        [Command("up"), RequireChannel(DiscordSettings.BotCommandsChannel)]
        public async Task UpUser(IGuildUser targetUser)
        {
            var oldScoreData = await Score.GetScoreDataAsync(Context.Client, targetUser.Id);
            var (scoreData, efficiency) = await Score.UpvoteAsync(Context.Client, targetUser.Id, Context.User.Id);

            await ReplyAsync($"{MentionUtils.MentionUser(Context.User.Id)} gave {MentionUtils.MentionUser(targetUser.Id)} a boost. Their score increased by {scoreData.Score - oldScoreData.Score:F3} (Efficiency: {efficiency * 100:F0}%).");

            if (scoreData.BonusScore != oldScoreData.BonusScore)
            {
                await ReplyAsync($"{MentionUtils.MentionUser(targetUser.Id)} reached boost level {scoreData.BoostLevel}! +{scoreData.BonusScore:F1} temporary bonus score.");
            }
        }

        [Command("down"), RequireChannel(DiscordSettings.BotCommandsChannel)]
        public async Task DownUser(IGuildUser targetUser)
        {
            var oldScoreData = await Score.GetScoreDataAsync(Context.Client, targetUser.Id);
            var (scoreData, efficiency) = await Score.DownvoteAsync(Context.Client, targetUser.Id, Context.User.Id);

            await ReplyAsync($"{MentionUtils.MentionUser(Context.User.Id)} gave {MentionUtils.MentionUser(targetUser.Id)} a downvote. Their score decreased by {oldScoreData.Score - scoreData.Score:F3} (Efficiency: {efficiency * 100:F0}%).");
        }

        [Command("top"), RequireChannel(DiscordSettings.BotCommandsChannel)]
        public async Task Top(int page = 1)
        {
            var topPlayers = await Database.UNSAFE_GetScoreUsersByLeaderboardPositionAsync(page - 1);
            var myPlayer = await Database.UNSAFE_GetOrCreateScoreUserAndLeaderboardPositionAsync(Context.Client, Context.User.Id);

            var topPlayerStrings = topPlayers.Select(t => GetLeaderboardPlayerString(t, (10 * (page - 1)) + 1 + topPlayers.Count(t2 => t2.Score > t.Score)));
            var myPlayerString = GetLeaderboardPlayerString(myPlayer.Item1, myPlayer.Item2);

            await ReplyAsync("```py\n" +
                "Pos  Username                          Score\n" +
                "--------------------------------------------\n" +
                string.Join("\n", topPlayerStrings) + "\n" +
                "--------------------------------------------\n" +
                myPlayerString +
                "```");
        }

        [Command("confirm"), RequireChannel(DiscordSettings.UnverifiedChannel), Priority(1)]
        public async Task Confirm()
        {
            await Confirm((SocketGuildUser)Context.User);
        }

        [Command("confirm"), RequireRole(DiscordSettings.DiscordStaff), Priority(0)]
        public async Task Confirm(SocketGuildUser user)
        {
            await SocialScoreWatcher.UpdateUserAsync(Context.Client, user, await Score.GetScoreDataAsync(Context.Client, user.Id), true);
        }

        private string GetHistoryAsync((ulong Key, DateTimeOffset LastBoost) user) => $"{ Context.Client.GetGuild(DiscordSettings.GuildId).GetUser(user.Key)?.GetName() ?? $"<{user.Key}>"} ({ (DateTimeOffset.UtcNow - user.LastBoost).ToHumanReadableString()} ago)";

        private string GetBoostString((ulong Key, TimeSpan TimeLeft) user) => $"{ Context.Client.GetGuild(DiscordSettings.GuildId).GetUser(user.Key)?.GetName() ?? $"<{user.Key}>"} ({ user.TimeLeft.ToHumanReadableString()} left)";

        private string GetLeaderboardPlayerString(ScoreUser scoreUser, int position)
        {
            var user = Context.Client.GetGuild(DiscordSettings.GuildId).GetUser(scoreUser.UserId);
            var username = user?.GetName() ?? $"<{scoreUser.UserId}>";
            var positionStr = position == 0 ? "" : position.ToString();
            return $"{positionStr,3}  {username,32}  {scoreUser.Score:F3}";
        }
    }
}