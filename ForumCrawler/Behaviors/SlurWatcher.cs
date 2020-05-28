using Discord.WebSocket;
using System.Threading.Tasks;

namespace ForumCrawler.Behaviors
{
	public static class SlurWatcher
	{
        public static string[] Slurs = new string[] { "nigger", "nigga", "niqqer", "niqqa", "faggot" };

        public static void Bind(DiscordSocketClient client)
        {
            client.MessageReceived += MessageReceived;
        }

        private static async Task MessageReceived(SocketMessage arg)
        {
            foreach (var slur in Slurs)
            {
                if (arg.Content.Contains(slur))
                {
                    await arg.DeleteAsync();
                    return;
                }
            }
        }
    }
}
