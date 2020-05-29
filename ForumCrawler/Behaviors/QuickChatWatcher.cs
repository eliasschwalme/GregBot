using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace ForumCrawler
{
    class QuickChatWatcher
    {
        // Keep list as simple as possible, do not add duplicate messages: "Thanks!" is not allowed, because "Thank you." already exists.
        private static readonly string[] ValidQuickChats =
        {
            // EE
            "Hi.", 
            "Goodbye.", 
            "Help me!", 
            "Thank you.", 
            "Follow me.", 
            "Stop!", 
            "Yes.", 
            "No.", 
            "Right.", 
            "Left.",

            // Color
            "White.",
            "Grey.",
            "Black.",
            "Red.",
            "Orange.",
            "Yellow.",
            "Green.",
            "Cyan.",
            "Blue.",
            "Purple.",

            // Misc
            "Code?",
            "LMAO!",
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
                if (ValidQuickChats.Contains(msg.Content)) return;
                if (MentionUtils.TryParseUser(msg.Content, out _)) return;
                await msg.DeleteAsync();
            }
        }
    }
}
