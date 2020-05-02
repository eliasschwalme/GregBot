using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace ForumCrawler.Helpers
{
    public static class DiscordSocketClientExtensions
    {
        public static void AddOnFirstReady(this DiscordSocketClient client, Func<Task> callback)
        {
            Task ActionHandler()
            {
                client.Ready -= ActionHandler;
                return callback();
            }
            client.Ready += ActionHandler;
        }
    }
}
