using Discord;
using Discord.WebSocket;

using System;
using System.Collections.Concurrent;
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

        public static ConcurrentDictionary<ulong, DateTimeOffset> Renames = new ConcurrentDictionary<ulong, DateTimeOffset>();

        public static bool CanChangeNick(ulong user)
        {
            if (Renames.TryGetValue(user, out var time))
            {
                return DateTimeOffset.UtcNow >= time;
			}

            return true;
		}

        public static TimeSpan GetTimeLeft(ulong user)
        {
            if (Renames.TryGetValue(user, out var time))
            {
                var difference = time - DateTimeOffset.UtcNow;

                if (difference < TimeSpan.Zero)
                {
                    Renames.TryRemove(user, out _);
                    return TimeSpan.Zero;
				}

                return difference;
			}

            return TimeSpan.Zero;
		}

        public static void Bind(DiscordSocketClient client)
        {
            client.MessageReceived += async (msg) =>
            {
                var match = DadbotRegex.Match(msg.Content);

                if (!match.Success) return;
                if (match.Groups.Count != 2) return;

#if DEBUG
                // give me (sirjosh) more chance to trigger rename for debugging
                int Maximum = msg.Author.Id == 172465767000965120 ? 5 : 100;
#else
                const int Maximum = 100;
#endif
                if (Rng.Next(0, Maximum) <= 5)
                {
                    if (Rng.Next(0, Maximum) <= 20)
                    {
                        var author = (IGuildUser)msg.Author;
                        Renames.AddOrUpdate(msg.Author.Id, DateTimeOffset.UtcNow.AddHours(1), (_1, _2) => DateTimeOffset.UtcNow.AddHours(1));
                        await author.ModifyAsync(prop => prop.Nickname = match.Groups[1].Value);
                    }

                    await msg.Channel.SendMessageAsync("Hi " + Format.Sanitize(match.Groups[1].Value) + ", I'm Dad!");
                }
            };
		}
    }
}
