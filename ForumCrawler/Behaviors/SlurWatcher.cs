using Discord.WebSocket;
using System.Threading.Tasks;

namespace ForumCrawler.Behaviors
{
	public static class SlurWatcher
	{
        // "#________SLURTEST________#" is for moderators to test if the slur filter is working
        public static string[] Slurs = new string[] { "nigger", "nigga", "niqqer", "niqqa", "faggot", "#________SLURTEST________#" };

        public static void Bind(DiscordSocketClient client)
        {
            client.MessageReceived += MessageReceived;
            client.MessageUpdated += (_, message, __) => MessageReceived(message);
        }

        private static async Task MessageReceived(SocketMessage message)
        {
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
