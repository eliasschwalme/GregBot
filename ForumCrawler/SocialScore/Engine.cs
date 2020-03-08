using Discord;
using Discord.WebSocket;

using ForumCrawler;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;

namespace DiscordSocialScore
{
    public class Engine
    {
        private static readonly Regex UsernameRegex = new Regex(@"(.*) \(-?[0-9]\.[0-9]+\)$");
        private readonly DiscordSocketClient client;
        private readonly RoleCacheProvider cacheProvider;
        private readonly Dictionary<ulong, DateTimeOffset> ignoreUsers = new Dictionary<ulong, DateTimeOffset>();

        public Engine(DiscordSocketClient client, RoleCacheProvider cacheProvider)
        {
            this.client = client;
            this.cacheProvider = cacheProvider;
            client.MessageReceived += Client_MessageReceived;
            client.GuildMemberUpdated += Client_GuildMemberUpdated;
            client.UserUpdated += Client_UserUpdated;
            client.RoleUpdated += Client_RoleUpdated;
            client.Ready += Client_Ready;
            Score.OnUpdate += Score_OnUpdate;
        }

        private Task Client_Ready()
        {
            var timer = new Timer(TimeSpan.FromHours(1).TotalMilliseconds);
            timer.Elapsed += (o, e) => OnHour();
            timer.Start();
            return Task.CompletedTask;
        }

        private async Task Client_UserUpdated(SocketUser oldUser, SocketUser newUser)
        {
            if (newUser.IsBot) return;

            if (oldUser.Username != newUser.Username)
            {
                foreach (var guild in client.Guilds)
                {
                    var guildUser = guild.GetUser(oldUser.Id);
                    if (guildUser == null) continue;
                    var scoreData = await Score.GetScoreDataAsync(guildUser);

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

        private async void OnHour()
        {
            foreach (var guild in client.Guilds)
            {
                var cache = cacheProvider.Get(guild);
                await ScoreRoleManager.DeleteRolesAsync(cache);
                await Score.UpdateDecays(OnScoreChangeAsync, userId => guild.GetUser(userId));
            }
        }

        private async void Score_OnUpdate(ulong userId, ScoreData scoreData) => await OnScoreChangeAsync(userId, scoreData);

        private async Task OnScoreChangeAsync(ulong userId, ScoreData scoreData)
        {
            foreach (var guild in client.Guilds)
            {
                var user = guild.GetUser(userId);
                if (user != null)
                {
                    try
                    {
                        await UpdateUsernameAsync(user, scoreData);
                    }
                    catch { }
                }
            }
        }

        private async Task Client_RoleUpdated(SocketRole arg1, SocketRole arg2)
        {
            if (arg1.Name != arg2.Name)
            {
                await ScoreRoleManager.OrderRolesAsync(arg2.Guild);
            }
        }

        private async Task Client_GuildMemberUpdated(SocketGuildUser oldUser, SocketGuildUser newUser)
        {
            if (newUser == null)
            {
                Console.WriteLine("Client_GuildMemberUpdated in Engine.cs has a null newUser");
                return;
            }

            if (oldUser == null)
            {
                Console.WriteLine("Client_GuildMemberUpdated in Engine.cs has a null oldUser");
                return;
            }

            ignoreUsers.TryGetValue(newUser.Id, out var lastCall);
            if ((DateTimeOffset.UtcNow - lastCall).Minutes < 1) return;

            await UpdateUsernameAsync(newUser, await Score.GetScoreDataAsync(newUser));
        }

        private async Task Client_MessageReceived(SocketMessage message)
        {
            if (message.Author.IsBot) return;
            if (message.Channel.Id == 329634826061742081 || // bot-commands
                message.Channel.Id == 329339732662419457 || // funposting
                message.Channel.Id == 596114917380325387)
            {
                return; // other-languages
            }

            if (!(message.Author is SocketGuildUser guildUser)) return;

            var scoreData = await Score.CreditActivityScoreAsync(guildUser);
            await UpdateUsernameAsync(guildUser, scoreData);
        }

        private async Task UpdateUsernameAsync(SocketGuildUser user, ScoreData scoreData)
        {
            if (user.IsBot) return;
            if (user.Guild.CurrentUser.Hierarchy <= user.Hierarchy) return;
            var targetNick = GetTargetNick(user.Username, user.Nickname, scoreData);

            var cache = cacheProvider.Get(user.Guild);

            var muted = (await Database.GetMute(user.Id)) != null;
            var roles = new List<IRole> { await ScoreRoleManager.GetScoreRoleForUserAsync(cache, user, scoreData) };
            if (!muted) roles.Add(await ScoreRoleManager.GetClassRole(cache, scoreData));

            var toDelete = user.Roles.GetBotRoles().Where(r => roles.All(r2 => r.Id != r2.Id)).ToList();
            var toAdd = roles.Where(r => user.Roles.All(r2 => r.Id != r2.Id)).ToList();

            if (user.Nickname != targetNick || toAdd.Any() || toDelete.Any())
            {
                ignoreUsers[user.Id] = DateTimeOffset.UtcNow;

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