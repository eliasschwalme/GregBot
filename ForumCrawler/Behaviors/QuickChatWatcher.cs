using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace ForumCrawler
{
    class QuickChatWatcher
    {
        private static readonly string[] ValidQuickChats =
        {
            "Hi.", 
            "Goodbye.", 
            "Help me!", 
            "Thank you.", 
            "Follow me.", 
            "Stop!", 
            "Yes.", 
            "No.", 
            "Right.", 
            "Left."
        };

        public static void Bind(DiscordSocketClient client)
        {
            client.MessageReceived += Client_MessageReceived;
            client.MessageUpdated += Client_MessageUpdated;
        }

        private static async Task Client_MessageUpdated(Discord.Cacheable<Discord.IMessage, ulong> arg1, SocketMessage msg, ISocketMessageChannel arg3)
        {
            if (msg.Channel.Name == "quickchat")
            {
                await msg.DeleteAsync();
            }
        }

        private static async Task Client_MessageReceived(SocketMessage msg)
        {
            if (msg.Channel.Name == "quickchat")
            {
                if (!ValidQuickChats.Contains(msg.Content))
                {
                    await msg.DeleteAsync();
                }
            }
        }
    }
}
