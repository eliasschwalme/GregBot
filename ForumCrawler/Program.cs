 using DiscordSocialScore;
using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace ForumCrawler
{
    public static class Program
    {
        public static string DatabasePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\greg_alts.db";
        private static void Main()
        {
            MainAsync().Wait();

            throw new Exception("Restart me!");
        }

        public static async Task MainAsync()
        {
			var client = await DiscordSettings.GetClient();
            var services = DiscordSettings.ConfigureServices(client);
            await DiscordSettings.InstallCommands(client, services);

            services.GetService(typeof(Engine));

            CoronaWatcher.Bind(client);
            await QuickReportWatcher.Bind(client).ConfigureAwait(false);
            MuteWatcher.Bind(client);
            VoiceChat.Bind(client);
            GovernanceVoteWatcher.Bind(client);
            EditWatcher.Bind(client);
			StarboardWatcher.Bind(client);

            await Task.Delay(10000).ConfigureAwait(false);
			var crawler = new Crawler(client);
            
            await Task.WhenAny(Task.Delay(TimeSpan.FromDays(2)), crawler.StartAsync());
        }
    }
}
