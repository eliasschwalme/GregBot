using Discord;
using Discord.WebSocket;

using ForumCrawler;
using ForumCrawler.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;

namespace DiscordSocialScore
{
    public static class SocialScoreWatcher
    {
        private static readonly Regex UsernameRegex = new Regex(@"(.*) \(-?[0-9]\.[0-9]+\)$");
        private static RoleCacheProvider CacheProvider;
        private static readonly Dictionary<ulong, DateTimeOffset> IgnoreUsers = new Dictionary<ulong, DateTimeOffset>();

        public static void Bind(DiscordSocketClient client)
        {
            CacheProvider = new RoleCacheProvider(client);
            client.MessageReceived += (a) => Client_MessageReceived(client, a);
            client.GuildMemberUpdated += (a, b) => Client_GuildMemberUpdated(client, a, b);
            client.UserUpdated += (a, b) => Client_UserUpdated(client, a, b);
            client.RoleUpdated += Client_RoleUpdated;
            client.AddOnFirstReady(() => Client_Ready(client));
            Score.OnUpdate += (a, b) => Score_OnUpdate(client, a, b);
        }

        private static Task Client_Ready(DiscordSocketClient client)
        {
            var timer = new Timer(TimeSpan.FromHours(1).TotalMilliseconds);
            timer.Elapsed += (o, e) => OnHour(client);
            timer.Start();
            return Task.CompletedTask;
        }

        private static async Task Client_UserUpdated(DiscordSocketClient client, SocketUser oldUser, SocketUser newUser)
        {
            if (newUser.IsBot) return;

            if (oldUser.Username != newUser.Username)
            {
                foreach (var guild in client.Guilds)
                {
                    var guildUser = guild.GetUser(oldUser.Id);
                    if (guildUser == null) continue;
                    var scoreData = await Score.GetScoreDataAsync(client, guildUser.Id);

                    var noNick = GetTargetNick(oldUser.Username, null, scoreData) == guildUser.Nickname;
                    if (noNick)
                    {
                        var targetNick = GetTargetNick(newUser.Username, null, scoreData);
                        await guildUser.ModifyAsync(x =>
                        {
                            x.Nickname = targetNick;
                        });
                    }
                }
            }
        }

        private static async void OnHour(DiscordSocketClient client)
        {
            await Score.UpdateDecays(client, (a, b) => OnScoreChangeAsync(client, a, b));

            foreach (var guild in client.Guilds)
            {
                var cache = CacheProvider.Get(guild);
                await ScoreRoleManager.DeleteRolesAsync(cache);
            }
        }

        private static async void Score_OnUpdate(DiscordSocketClient client, ulong userId, ScoreData scoreData) => await OnScoreChangeAsync(client, userId, scoreData);

        private static async Task OnScoreChangeAsync(DiscordSocketClient client, ulong userId, ScoreData scoreData)
        {
            foreach (var guild in client.Guilds)
            {
                var user = guild.GetUser(userId);
                if (user != null)
                {
                    try
                    {
                        await UpdateUserAsync(client, user, scoreData);
                    }
                    catch { }
                }
            }
        }

        private static async Task Client_RoleUpdated(SocketRole arg1, SocketRole arg2)
        {
            if (arg1.Name != arg2.Name)
            {
                await ScoreRoleManager.OrderRolesAsync(arg2.Guild);
            }
        }

        private static async Task Client_GuildMemberUpdated(DiscordSocketClient client, SocketGuildUser oldUser, SocketGuildUser newUser)
        {
            if (newUser == null)
            {
                Console.WriteLine("Client_GuildMemberUpdated in SocialScoreWatcher.cs has a null newUser");
                return;
            }

            if (oldUser == null)
            {
                Console.WriteLine("Client_GuildMemberUpdated in SocialScoreWatcher.cs has a null oldUser");
                return;
            }

            IgnoreUsers.TryGetValue(newUser.Id, out var lastCall);
            if ((DateTimeOffset.UtcNow - lastCall).Minutes < 1) return;

            await UpdateUserAsync(client, newUser, await Score.GetScoreDataAsync(client, newUser.Id));
        }

        private static async Task Client_MessageReceived(DiscordSocketClient client, SocketMessage message)
        {
            ScoreData scoreData;
            if (message.Author.IsBot) return;
            if (!(message.Author is SocketGuildUser guildUser)) return;
            if (guildUser.Guild.Id == DiscordSettings.GuildId &&
                message.Channel.Id != 329634826061742081 && // bot-spam
                message.Channel.Id != 329339732662419457 && // funposting
                message.Channel.Id != 596114917380325387)  // other-languages
            {
                scoreData = await Score.CreditActivityScoreAsync(client, guildUser.Id);
            } else
            {
                scoreData = await Score.GetScoreDataAsync(client, guildUser.Id); 
            }

            foreach (var guild in client.Guilds)
            {
                var user = guild.GetUser(guildUser.Id);
                await UpdateUserAsync(client, user, scoreData);
            }
        }

        private static async Task UpdateUserAsync(DiscordSocketClient client, SocketGuildUser user, ScoreData scoreData)
        {
            if (user.IsBot) return;
            if (user.Guild.CurrentUser.Hierarchy <= user.Hierarchy) return;
            var targetNick = GetTargetNick(user.Username, user.Nickname, scoreData);

            var cache = CacheProvider.Get(user.Guild);

            var muted = (await Database.GetMute(user.Id)) != null;
            var roles = new List<IRole> { await ScoreRoleManager.GetScoreRoleForUserAsync(client, cache, user.Id, scoreData) };
            if (!muted) roles.Add(await ScoreRoleManager.GetClassRole(cache, scoreData));

            var toDelete = user.Roles.GetBotRoles().Where(r => roles.All(r2 => r.Id != r2.Id)).ToList();
            var toAdd = roles.Where(r => user.Roles.All(r2 => r.Id != r2.Id)).ToList();

            if (user.Nickname != targetNick || toAdd.Any() || toDelete.Any())
            {
                IgnoreUsers[user.Id] = DateTimeOffset.UtcNow;

                Console.WriteLine("Updated username " + user.Nickname + " to " + targetNick);

                await user.ModifyAsync(x =>
                {
                    x.Nickname = targetNick;
                    x.Roles = Optional.Create(user.Roles.Concat(toAdd).Except(toDelete).Where(r => r != r.Guild.EveryoneRole));
                });
            }
        }

        private static string GetTargetNick(string username, string nickname, ScoreData scoreData)
        {
            var baseNickname = GetBaseNick(username, nickname);

            var show = scoreData.ShowInUsername;
            if (baseNickname.Contains("(") && baseNickname.Contains(")"))
                show = true;
            if (!show) return baseNickname == username ? null : baseNickname;

            var scoreSuffix = $" ({scoreData.ShortScoreString})";
            baseNickname = baseNickname.Substring(0, Math.Min(baseNickname.Length, 32 - scoreSuffix.Length));
            return baseNickname + scoreSuffix;
        }

        public static string GetBaseNick(string username, string nickname)
        {
            var trimmedNick = nickname?.TrimStart(ScoreRoleManager.RolePrefix);
            var matches = UsernameRegex.Match(trimmedNick ?? "");
            var baseNickname = trimmedNick ?? username;
            if (matches.Success)
            {
                baseNickname = matches.Groups[1].Value;
            }
            return baseNickname;
        }
    }
}