using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Nito.AsyncEx;

namespace ForumCrawler
{
    public class RoleCache
    {
        private readonly AsyncLock _mutex = new AsyncLock();
        private readonly Dictionary<string, IRole> _cache = new Dictionary<string, IRole>();

        public RoleCache(DiscordSocketClient client, SocketGuild guild)
        {
            Guild = guild;
            client.RoleCreated += Client_RoleCreated;
        }

        public SocketGuild Guild { get; }

        private Task Client_RoleCreated(SocketRole arg)
        {
            if (arg.Guild == Guild)
            {
                var toDelete = _cache.Where(kv => Guild.Roles.Any(r => r.Id == kv.Value.Id)).Select(kv => kv.Key)
                    .ToList();
                foreach (var key in toDelete)
                {
                    _cache.Remove(key);
                }
            }

            return Task.CompletedTask;
        }

        public async Task<IRole> CreateOrUpdateRoleAsync(string roleName, GuildPermissions? permissions = null,
            Color color = default, bool isHoisted = false)
        {
            var name = ScoreRoleManager.RolePrefix + roleName;
            IRole res = Guild.Roles.FirstOrDefault(r => r.Name == name);

            if (res == null)
            {
                using (await _mutex.LockAsync())
                {
                    _cache.TryGetValue(name, out res);

                    if (res == null)
                    {
                        res = _cache[name] = await Guild.CreateRoleAsync(name, permissions, color, isHoisted, false);
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