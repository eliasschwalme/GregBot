using Discord.WebSocket;

using System.Linq;
using System.Threading.Tasks;

namespace ForumCrawler.Behaviors
{
	public static class SlurWatcher
	{
        // "#________SLURTEST________#" is for moderators to test if the slur filter is working
        public static string[] Slurs = new string[] { "nigger", "nigga", "niqqer", "niqqa", "faggot", "#SLURTEST#" }
            .Select(slur => slur.ToLower())
            .ToArray();

        public static void Bind(DiscordSocketClient client)
        {
            client.MessageReceived += MessageReceived;
            client.MessageUpdated += (_, message, __) => MessageReceived(message);
        }

        private static async Task MessageReceived(SocketMessage message)
        {
            // https://github.com/discord-net/Discord.Net/issues/621#issuecomment-296762573
            var channel = message.Channel as SocketGuildChannel;
            var guild = channel.Guild;

            if (guild.Id != DiscordSettings.GuildId) return;

            foreach (var slur in Slurs)
            {
                if (message.Content?.ToLower()?.Contains(slur) == true)
                {
                    await message.DeleteAsync();
                }
            }
        }
    }
}
