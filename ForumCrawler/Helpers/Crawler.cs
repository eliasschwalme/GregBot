using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace ForumCrawler
{
    public class Crawler
    {
        private readonly List<int> _consumedPosts = new List<int>();
        private readonly DiscordSocketClient _discord;

        public Crawler(DiscordSocketClient discord)
        {
            _discord = discord;
        }

        public async Task StartAsync()
        {
            var isDown = false;
            while (true)
            {
                try
                {
                    if (DateTimeOffset.UtcNow.Second % 60 == 0)
                    {
                        await UpdateOnlineUsersAsync();
                    }

                    if (await CrawlAsync())
                    {
                        isDown = false;
                    }
                }
                catch
                {
                    if (!isDown)
                    {
                        isDown = true;
                        await DiscordFormatting.AnnounceDownAsync(_discord);
                    }
                }

                await WaitABitAsync();
            }
        }

        private async Task<bool> CrawlAsync()
        {
            var items = await Forum.GetNewAsync();

            if (_consumedPosts.Count == 0)
            {
                // if we've consumed nothing, consume all the items
                // that way we don't spam #forums with non-new stuff
                _consumedPosts.AddRange(items.Select(x => x.PostId));
            }

            var filtered = items
                .Where(item => !_consumedPosts.Contains(item.PostId))
                .ToArray();

            foreach (var item in filtered)
            {
                _consumedPosts.Add(item.PostId);
                await DiscordFormatting.AnnouncePostAsync(_discord, item);
            }

            return filtered.Any();
        }

        private async Task UpdateOnlineUsersAsync()
        {
            var items = await Forum.GetOnlineUsers();
            await _discord.GetGuild(DiscordSettings.GuildId)
                .GetTextChannel(DiscordSettings.ForumChannel)
                .ModifyAsync(x =>
                {
                    x.Topic = $"Online: {string.Join(", ", items)}";
                });
        }

        private static async Task WaitABitAsync()
        {
            var now = DateTimeOffset.UtcNow;
            now = now.AddSeconds(10);
            now = now.AddSeconds(-now.Second % 10);
            var waitTime = now - DateTimeOffset.UtcNow;
            if (waitTime.TotalMilliseconds > 0)
            {
                await Task.Delay(waitTime);
            }
        }
    }
}