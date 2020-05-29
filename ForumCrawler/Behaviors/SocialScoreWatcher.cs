using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.WebSocket;

namespace ForumCrawler
{
    public static class SocialScoreWatcher
    {
        private static RoleCacheProvider CacheProvider;

        public static void Bind(DiscordSocketClient client)
        {
            CacheProvider = new RoleCacheProvider(client);
            client.MessageReceived += a => Client_MessageReceived(client, a);
            client.GuildMemberUpdated += (a, b) => Client_GuildMemberUpdated(client, a, b);
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

        private static async void OnHour(DiscordSocketClient client)
        {
            await Score.UpdateDecays(client);
            foreach (var guild in client.Guilds)
            {
                var cache = CacheProvider.Get(guild);
                await ScoreRoleManager.DeleteRolesAsync(cache);
            }
        }

        private static async void Score_OnUpdate(DiscordSocketClient client, ulong userId, ScoreData scoreData)
        {
            await OnScoreChangeAsync(client, userId, scoreData);
        }

        private static async Task OnScoreChangeAsync(DiscordSocketClient client, ulong userId, ScoreData scoreData)
        {
            foreach (var guild in client.Guilds)
            {
                var user = guild.GetUser(userId);
                if (user != null)
                {
                    await UpdateUserAsync(client, user, scoreData);
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

        private static async Task Client_GuildMemberUpdated(DiscordSocketClient client, SocketGuildUser oldUser,
            SocketGuildUser newUser)
        {
            if (!oldUser.Roles.SequenceEqual(newUser.Roles))
            {
                await UpdateUserAsync(client, newUser, await Score.GetScoreDataAsync(client, newUser.Id));
            }
        }

        private static async Task Client_MessageReceived(DiscordSocketClient client, SocketMessage message)
        {
            if (message.Author.IsBot)
            {
                return;
            }

            if (!(message.Author is SocketGuildUser guildUser))
            {
                return;
            }

            if (guildUser.Guild.Id == DiscordSettings.GuildId &&
                message.Channel.Id != DiscordSettings.BotCommandsChannel && // bot-spam
                message.Channel.Id != 715720376918737028) // quickchat
            {
                
                await Score.CreditActivityScoreAsync(client, guildUser.Id);
            }
        }

        public static async Task UpdateUserAsync(DiscordSocketClient client, SocketGuildUser user, ScoreData scoreData,
            bool force = false)
        {
            if (user.IsBot)
            {
                return;
            }

            if (user.Guild.CurrentUser.Hierarchy <= user.Hierarchy)
            {
                return;
            }

            if (!force && user.Roles.Count <= 1)
            {
                return;
            }

            var cache = CacheProvider.Get(user.Guild);

            var muted = await Database.UNSAFE_GetMute(user.Id) !=
                        null; // TODO: looking up mutes on everyone every hour is not a good idea
            var roles = new List<IRole>
            {
                await ScoreRoleManager.GetScoreRoleForUserAsync(client, cache, user.Id, scoreData)
            };
            if (!muted)
            {
                roles.Add(await ScoreRoleManager.GetClassRole(cache, scoreData));
            }

            var toDelete = user.Roles.GetBotRoles().Where(r => roles.All(r2 => r.Id != r2.Id)).ToList();
            var toAdd = roles.Where(r => user.Roles.All(r2 => r.Id != r2.Id)).ToList();

            if (toAdd.Any() || toDelete.Any())
            {
                await user.ModifyAsync(x =>
                {
                    x.Roles = Optional.Create(user.Roles
                        .Concat(toAdd)
                        .Except(toDelete)
                        .Where(r => r != r.Guild.EveryoneRole));
                });
            }
        }
    }
}