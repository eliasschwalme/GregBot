using System;
using System.Threading.Tasks;
using DiscordSocialScore;

namespace ForumCrawler
{
    public static class Program
    {
        private static async Task Main()
        {
            var client = await DiscordSettings.GetClient();

            var services = DiscordSettings.ConfigureServices(client);
            await DiscordSettings.InstallCommands(client, services);

            SocialScoreWatcher.Bind(client);
            CoronaWatcher.Bind(client);
            MuteWatcher.Bind(client);
            VoiceChat.Bind(client);
            GovernanceVoteWatcher.Bind(client);
            EditWatcher.Bind(client);
            QuickReportWatcher.Bind(client);
            StarboardWatcher.Bind(client);

            await Task.Delay(10000);
            var crawler = new Crawler(client);

            await Task.WhenAny(Task.Delay(TimeSpan.FromDays(2)), crawler.StartAsync());
            throw new Exception("Restart me!");
        }
    }
}