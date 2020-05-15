using System;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace ForumCrawler
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