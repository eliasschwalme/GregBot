using Discord;
using Discord.Commands;

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
            await ReplyAsync($"{user.GetBaseNick()} has been boosted by{historyString}");
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
            await ReplyAsync($"{user.GetBaseNick()} is being boosted by{boostString}");
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

        [Command("score show"), RequireChannel(DiscordSettings.BotCommandsChannel)]
        public async Task ShowUsername()
        {
            await Score.UpdateUserVisibilityAsync(Context.Client, Context.User.Id, true);
            await ReplyAsync("Your score is now shown in your nickname.");
        }

        [Command("score hide"), RequireChannel(DiscordSettings.BotCommandsChannel)]
        public async Task HideUsername()
        {
            await Score.UpdateUserVisibilityAsync(Context.Client, Context.User.Id, false);
            await ReplyAsync("Your score is now hidden in your nickname.");
        }

        [Command("stats"), Alias("score"), Priority(0)]
        public async Task GetMyStats() => await GetStats(Context.User);

        [Command("stats"), Alias("score"), Priority(1)]
        public async Task GetStats(IUser userObj)
        {
            var user = Context.Client.GetGuild(DiscordSettings.GuildId).GetUser(userObj.Id);
            const string WootString = "<:woot:329697747701727235>";
            var hs = await Database.GetOrCreateScoreUserAndLeaderboardPositionAsync(Context.Client, user.Id);
            var score = hs.Item1;
            var boostStr = score.BonusScore > 0 ? $" (+{score.BonusScore:F1})" : "";
            await ReplyAsync($"[#{hs.Item2}] **{user.GetBaseNick()}**'s stats:", embed: new EmbedBuilder().WithDescription(
                Emote.Parse(WootString).ToString() + $" **Score:** {score.Score:F3}{boostStr}\n" +
                $":zap: **Energy:** {Math.Floor(score.Energy)}/{score.MaxEnergy} (more in {score.NextEnergy:m\\:ss})\n" +
                $":rocket: **Inertia:** {Math.Round(score.Inertia * 100)}%").Build());
        }

        [Command("score set"), RequireRole(DiscordSettings.DiscordServerOwner)]
        public async Task SetScore(IUser user, double value)
        {
            if (user == null)
            {
                throw new Exception("Invalid user specified!");
            }

            var score = await Score.SetScoreAsync(Context.Client, user.Id, value);
            await ReplyAsync($"Set {MentionUtils.MentionUser(user.Id)}'s score to {score:F3}.");
        }

        [Command("exempt set"), RequireRole(DiscordSettings.DiscordServerOwner)]
        public async Task SetExempt(IUser user, bool value = true)
        {
            var scoreUser = await Database.GetOrCreateScoreUserAsync(Context.Client, user.Id);
            scoreUser.EarlyUserExempt = value;
            await Database.AddOrUpdateScoreUserAsync(scoreUser);

            await ReplyAsync($"{MentionUtils.MentionUser(user.Id)} is now{(value ? " " : " not ")}exempt from the 72 hour g!up wait period.");
        }

        [Command("energy set"), RequireRole(DiscordSettings.DiscordServerOwner)]
        public async Task SetEnergy(IUser user, double value)
        {
            if (user == null)
            {
                throw new Exception("Invalid user specified!");
            }

            var energy = Math.Floor(await Score.SetEnergyAsync(Context.Client, user.Id, value));
            await ReplyAsync($"Set {MentionUtils.MentionUser(user.Id)}'s energy to {energy}.");
        }

        [Command("inertia set"), RequireRole(DiscordSettings.DiscordServerOwner)]
        public async Task SetInertia(IUser user, double value)
        {
            if (user == null)
            {
                throw new Exception("Invalid user specified!");
            }

            var inertia = Math.Floor(await Score.SetInertiaAsync(Context.Client, user.Id, value / 100) * 100);
            await ReplyAsync($"Set {MentionUtils.MentionUser(user.Id)}'s inertia to {inertia}%.");
        }

        [Command("up")]
        public async Task UpUser(IGuildUser targetUser)
        {
            if (targetUser == null)
            {
                throw new Exception("Invalid user specified!");
            }

            if (!(Context.User is IGuildUser guildUser))
            {
                throw new Exception("You aren't a guild user!");
            }

            if ((DateTimeOffset.UtcNow - guildUser.JoinedAt)?.TotalDays < 3 && !(await Database.IsScoreUserExempt(Context.Client, guildUser.Id))) throw new Exception("You have recently joined this server and may not g!up other users yet!");
            if ((DateTimeOffset.UtcNow - targetUser.JoinedAt)?.TotalDays < 3 && !(await Database.IsScoreUserExempt(Context.Client, targetUser.Id))) throw new Exception("The target has recently joined this server and may not receive g!up from other users yet!");

            var oldScoreData = await Score.GetScoreDataAsync(Context.Client, targetUser.Id);
            var (scoreData, efficiency) = await Score.UpvoteAsync(Context.Client, targetUser.Id, guildUser.Id);

            await ReplyAsync($"{MentionUtils.MentionUser(Context.User.Id)} gave {MentionUtils.MentionUser(targetUser.Id)} a boost. Their score increased by {scoreData.Score - oldScoreData.Score:F3} (Efficiency: {efficiency * 100:F0}%).");

            if (scoreData.BonusScore != oldScoreData.BonusScore)
            {
                await ReplyAsync($"{MentionUtils.MentionUser(targetUser.Id)} reached boost level {scoreData.BoostLevel}! +{scoreData.BonusScore:F1} temporary bonus score and +{scoreData.BonusEnergy} bonus max energy.");
            }
        }

        [Command("top"), RequireChannel(DiscordSettings.BotCommandsChannel)]
        public async Task Top(int page = 1)
        {
            var topPlayers = await Database.GetScoreUsersByLeaderboardPositionAsync(page - 1);
            var myPlayer = await Database.GetOrCreateScoreUserAndLeaderboardPositionAsync(Context.Client, Context.User.Id);

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

        private string GetHistoryAsync((ulong Key, DateTimeOffset LastBoost) user) => $"{ Context.Client.GetGuild(DiscordSettings.GuildId).GetUser(user.Key)?.GetBaseNick() ?? $"<{user.Key}>"} ({ (DateTimeOffset.UtcNow - user.LastBoost).ToHumanReadableString()} ago)";

        private string GetBoostString((ulong Key, TimeSpan TimeLeft) user) => $"{ Context.Client.GetGuild(DiscordSettings.GuildId).GetUser(user.Key)?.GetBaseNick() ?? $"<{user.Key}>"} ({ user.TimeLeft.ToHumanReadableString()} left)";

        private string GetLeaderboardPlayerString(ScoreUser scoreUser, int position)
        {
            var user = Context.Client.GetGuild(DiscordSettings.GuildId).GetUser(scoreUser.UserId);
            var username = user?.GetBaseNick() ?? $"<{scoreUser.UserId}>";
            var positionStr = position == 0 ? "" : position.ToString();
            return $"{positionStr,3}  {username,32}  {scoreUser.Score:F3}";
        }
    }
}