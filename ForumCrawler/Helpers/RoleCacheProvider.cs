using System.Collections.Generic;
using Discord.WebSocket;

namespace ForumCrawler
{
    public class RoleCacheProvider
    {
        private readonly Dictionary<ulong, RoleCache> cacheCollection = new Dictionary<ulong, RoleCache>();

        public RoleCacheProvider(DiscordSocketClient client)
        {
            Client = client;
        }

        public DiscordSocketClient Client { get; }

        public RoleCache Get(SocketGuild guild)
        {
            if (!cacheCollection.TryGetValue(guild.Id, out var result))
            {
                result = cacheCollection[guild.Id] = new RoleCache(Client, guild);
            }

            return result;
        }
    }
}