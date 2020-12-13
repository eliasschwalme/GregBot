using Discord;
using Discord.WebSocket;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ForumCrawler
{
	class DadbotWatcher
    {
        private static Regex DadbotRegex = new Regex(@"^[Ii]'?[Mm] (\w*)$", RegexOptions.Compiled);
        private static Random Rng = new Random();

        public static void Bind(DiscordSocketClient client)
        {
            client.MessageReceived += async (msg) =>
            {
                var match = DadbotRegex.Match(msg.Content);

                if (!match.Success) return;
                if (match.Groups.Count != 2) return;
                if (Rng.Next(0, 100) > 5) return;

                await msg.Channel.SendMessageAsync("Hi " + Format.Sanitize(match.Groups[1].Value) + ", I'm Dad!");
            };
		}
    }
}
