using Discord;
using Discord.Commands;
using ForumCrawler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordSocialScore
{
    [Group]
    public class ScoreCommands : ModuleBase<SocketCommandContext>
    {
        private readonly RoleCacheProvider provider;

        public ScoreCommands(RoleCacheProvider provider)
        {
            this.provider = provider;
        }

        [Command("help"), RequireChannel(DiscordSettings.BotCommandsChannel)]
        public async Task GetHelp()
        {
            await this.ReplyAsync($"Help is here! <https://gist.github.com/Yonom/126e7b6741f4e865c8b41e9488cf578a>");
        }


        [Command("history"), RequireChannel(DiscordSettings.BotCommandsChannel), Priority(0)]
        public async Task GetMyHistory()
        {
            await GetHistory(this.Context.User as IGuildUser);
        }

        [Command("history"), RequireChannel(DiscordSettings.BotCommandsChannel), Priority(1)]
        public async Task GetHistory(IGuildUser user)
        {
            if (user == null)
            {
				throw new Exception("GetHistory GuildUser is null.");
			}

            var history = await Score.GetHistoryAsync(user);
            var historyString = history.Count == 0
                ? " nobody."
                : history.Count == 1
                    ? $" {this.GetHistoryAsync(history[0])}."
                    : ": ```yml\n" +
                        String.Join("\n", history.Select(a => $"- {this.GetHistoryAsync(a)}")) + "\n" +
                        "```";
            await this.ReplyAsync($"{user.GetBaseNick()} has been boosted by{historyString}");

        }


        [Command("boosts"), RequireChannel(DiscordSettings.BotCommandsChannel), Priority(0)]
        public async Task GetMyBoosts()
        {
            await GetBoosts(this.Context.User as IGuildUser);
        }
               
        [Command("boosts"), RequireChannel(DiscordSettings.BotCommandsChannel), Priority(1)]
        public async Task GetBoosts(IGuildUser user)
        {
            var boosts = await Score.GetBoostsAsync(user);
            var boostString = boosts.Count == 0
                ? " nobody."
                : boosts.Count == 1
                    ? $" {this.GetBoostString(boosts[0])}."
                    : ": ```yml\n" +
                        String.Join("\n", boosts.Select(a => $"- {this.GetBoostString(a)}")) + "\n" +
                        "```";
            await this.ReplyAsync($"{user.GetBaseNick()} is being boosted by{boostString}");

        }

        [Command("boosting"), RequireChannel(DiscordSettings.BotCommandsChannel), Priority(2)]
        public async Task GetUsersUserIsBoosting()
        {
			var boosting = await Score.GetUsersUserHasBoosted(Context.Guild, Context.User);

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

					await ReplyAsync($"You are boosting `{GetHistoryAsync((userId, boostTime))}`").ConfigureAwait(false);
				}
				break;

				default:
                {
					StringBuilder strb = new StringBuilder();

                    foreach(var user in boosting.OrderByDescending(x => x.Item2.Boosts[Context.User.Id]))
                    {
						var userId = user.Item1.Id;
						var boostTime = user.Item2.Boosts[Context.User.Id];

						strb.AppendLine(GetHistoryAsync((userId, boostTime)));
					}

					await ReplyAsync($"You are boosting: ```yml\n{strb}```").ConfigureAwait(false);
				}
				break;
			}
		}

        [Command("score show"), RequireChannel(DiscordSettings.BotCommandsChannel)]
        public async Task ShowUsername()
        {
			var guildUser = this.Context.User as IGuildUser;

			if (guildUser == null)
			{
				throw new Exception("You're not a guild user!");
			}

			await Score.UpdateUserVisibilityAsync(guildUser, true);
            await this.ReplyAsync("Your score is now shown in your nickname.");
        }

        [Command("score hide"), RequireChannel(DiscordSettings.BotCommandsChannel)]
        public async Task HideUsername()
        {
			var guildUser = this.Context.User as IGuildUser;

            if (guildUser == null)
            {
				throw new Exception("You're not a guild user!");
			}

			await Score.UpdateUserVisibilityAsync(guildUser, false);
            await this.ReplyAsync("Your score is now hidden in your nickname.");
        }

        [Command("stats"), Alias("score"), Priority(0)]
        public async Task GetMyStats()
        {
            await GetStats(this.Context.User);
        }

        [Command("stats"), Alias("score"), Priority(1)]
        public async Task GetStats(IUser userObj)
        {
            var user = this.Context.Client.GetGuild(DiscordSettings.GuildId).GetUser(userObj.Id);
            const string WootString = "<:woot:329697747701727235>";
            var hs = await Database.GetOrCreateScoreUserAndLeaderboardPositionAsync(user);
            var score = hs.Item1;
            var boostStr = score.BonusScore > 0 ? $" (+{score.BonusScore:F1})" : "";
            await this.ReplyAsync($"[#{hs.Item2}] **{user.GetBaseNick()}**'s stats:", embed: new EmbedBuilder().WithDescription(
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

            var score = await Score.SetScoreAsync(this.Context.Client.GetGuild(DiscordSettings.GuildId).GetUser(user.Id), value);
            await this.ReplyAsync($"Set {MentionUtils.MentionUser(user.Id)}'s score to {score:F3}.");
        }

        [Command("energy set"), RequireRole(DiscordSettings.DiscordServerOwner)]
        public async Task SetEnergy(IGuildUser user, double value)
		{
			if (user == null)
			{
				throw new Exception("Invalid user specified!");
			}

			var energy = Math.Floor(await Score.SetEnergyAsync(user, value));
            await this.ReplyAsync($"Set {MentionUtils.MentionUser(user.Id)}'s energy to {energy}.");
        }

        [Command("inertia set"), RequireRole(DiscordSettings.DiscordServerOwner)]
        public async Task SetInertia(IUser user, double value)
		{
			if (user == null)
			{
				throw new Exception("Invalid user specified!");
			}

			var inertia = Math.Floor(await Score.SetInertiaAsync(this.Context.Guild.GetUser(user.Id), value / 100) * 100);
            await this.ReplyAsync($"Set {MentionUtils.MentionUser(user.Id)}'s inertia to {inertia}%.");
        }

        [Command("up")]
        public async Task UpUser(IGuildUser user)
        {
            if (user == null)
            {
				throw new Exception("Invalid user specified!");
			}

            var guildUser = this.Context.User as IGuildUser;

            if (guildUser == null)
            {
				throw new Exception("You aren't a guild user!");
			}

            if ((DateTimeOffset.UtcNow - guildUser.JoinedAt)?.TotalDays < 3) throw new Exception("You have recently joined this server and may not g!up other users yet!");

            var oldScoreData = await Score.GetScoreDataAsync(user);
            var (scoreData, efficiency) = await Score.UpvoteAsync(user, guildUser);

            await this.ReplyAsync($"{MentionUtils.MentionUser(this.Context.User.Id)} gave {MentionUtils.MentionUser(user.Id)} a boost. Their score increased by {scoreData.Score - oldScoreData.Score:F3} (Efficiency: {efficiency * 100:F0}%).");
            
            if (scoreData.BonusScore != oldScoreData.BonusScore)
            {
                await this.ReplyAsync($"{MentionUtils.MentionUser(user.Id)} reached boost level {scoreData.BoostLevel}! +{scoreData.BonusScore:F1} temporary bonus score and +{scoreData.BonusEnergy} bonus max energy.");
            }
        }


        [Command("top"), RequireChannel(DiscordSettings.BotCommandsChannel)]
        public async Task Top(int page = 1)
        {
            var topPlayers = await Database.GetScoreUsersByLeaderboardPositionAsync(page - 1);
            var myPlayer = await Database.GetOrCreateScoreUserAndLeaderboardPositionAsync(this.Context.User as IGuildUser);

            var topPlayerStrings = topPlayers.Select(t => GetLeaderboardPlayerString(t, 10 * (page - 1) + 1 + topPlayers.Count(t2 => t2.Score > t.Score)));
            var myPlayerString = GetLeaderboardPlayerString(myPlayer.Item1, myPlayer.Item2);

            await this.ReplyAsync("```py\n" +
                "Pos  Username                          Score\n" +
                "--------------------------------------------\n" +
                String.Join("\n", topPlayerStrings) + "\n" +
                "--------------------------------------------\n" +
                myPlayerString +
                "```");
        }

        private string GetHistoryAsync((ulong Key, DateTime LastBoost) user)
        {
            return $"{ this.Context.Guild.GetUser(user.Key)?.GetBaseNick() ?? $"<{user.Key}>"} ({ (DateTime.UtcNow - user.LastBoost).ToHumanReadableString()} ago)";
        }

        private string GetBoostString((ulong Key, TimeSpan TimeLeft) user)
        {
            return $"{ this.Context.Guild.GetUser(user.Key)?.GetBaseNick() ?? $"<{user.Key}>"} ({ user.TimeLeft.ToHumanReadableString()} left)";
        }

        private string GetLeaderboardPlayerString(ScoreUser scoreUser, int position)
        {
            var user = this.Context.Guild.GetUser(scoreUser.UserId);
            var username = user?.GetBaseNick() ?? $"<{scoreUser.UserId}>";
            var positionStr = position == 0 ? "" : position.ToString();
            return $"{positionStr.PadRight(3)}  {username.PadRight(32)}  {scoreUser.Score:F3}";
        }
    }
}
