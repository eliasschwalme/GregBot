using Discord;
using Discord.WebSocket;

using Nito.AsyncEx;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ForumCrawler
{
    public class RoleCacheProvider
    {
        public DiscordSocketClient Client { get; }
        private readonly Dictionary<ulong, RoleCache> cacheCollection = new Dictionary<ulong, RoleCache>();

        public RoleCacheProvider(DiscordSocketClient client) => Client = client;

        public RoleCache Get(SocketGuild guild)
        {
            if (!cacheCollection.TryGetValue(guild.Id, out var result))
            {
                result = cacheCollection[guild.Id] = new RoleCache(Client, guild);
            }
            return result;
        }
    }

    public class RoleCache
    {
        private readonly AsyncLock _mutex = new AsyncLock();
        public SocketGuild Guild { get; }
        private readonly Dictionary<string, IRole> cache = new Dictionary<string, IRole>();

        public RoleCache(DiscordSocketClient client, SocketGuild guild)
        {
            Guild = guild;
            client.RoleCreated += Client_RoleCreated;
        }

        private Task Client_RoleCreated(SocketRole arg)
        {
            if (arg.Guild == Guild)
            {
                var toDelete = cache.Where(kv => Guild.Roles.Any(r => r.Id == kv.Value.Id)).Select(kv => kv.Key).ToList();
                foreach (var key in toDelete)
                {
                    cache.Remove(key);
                }
            }
            return Task.CompletedTask;
        }

        public async Task<IRole> CreateOrUpdateRoleAsync(string roleName, GuildPermissions? permissions = null, Color color = default, bool isHoisted = false)
        {
            var name = ScoreRoleManager.RolePrefix + roleName;
            IRole res = Guild.Roles.FirstOrDefault(r => r.Name == name);

            if (res == null)
            {
                using (await _mutex.LockAsync())
                {
                    cache.TryGetValue(name, out res);

                    if (res == null)
                    {
                        res = cache[name] = await Guild.CreateRoleAsync(name, permissions, color, isHoisted, false);
                    }
                }
            }

            if (res.Color != color || res.IsHoisted != isHoisted)
            {
                await res.ModifyAsync(p =>
                {
                    p.Color = color;
                    p.Hoist = isHoisted;
                });
            }

            return res;
        }
    }
}