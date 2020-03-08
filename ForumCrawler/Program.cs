using DiscordSocialScore;

using System;
using System.Threading.Tasks;

namespace ForumCrawler
{
    public static class Program
    {
        public static string DatabasePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\greg_alts.db";

        private static async Task Main()
        {
            var client = await DiscordSettings.GetClient();
            var services = DiscordSettings.ConfigureServices(client);
            await DiscordSettings.InstallCommands(client, services);

            services.GetService(typeof(Engine));

            CoronaWatcher.Bind(client);
            await QuickReportWatcher.Bind(client);
            MuteWatcher.Bind(client);
            VoiceChat.Bind(client);
            GovernanceVoteWatcher.Bind(client);
            EditWatcher.Bind(client);
            StarboardWatcher.Bind(client);

            await Task.Delay(10000);
            var crawler = new Crawler(client);

            await Task.WhenAny(Task.Delay(TimeSpan.FromDays(2)), crawler.StartAsync());
            throw new Exception("Restart me!");
        }
    }
}