using DiscordSocialScore;

using System;
using System.Linq;
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

            var tcs = new TaskCompletionSource<bool>();
            client.Ready += () =>
            {
                tcs.TrySetResult(true);
                return Task.CompletedTask;
            };

            CoronaWatcher.Bind(client);
            MuteWatcher.Bind(client);
            VoiceChat.Bind(client);
            GovernanceVoteWatcher.Bind(client);
            EditWatcher.Bind(client);

            var guild = client.GetGuild(DiscordSettings.GuildId);

            /*
            var textChannels = guild.GetCategoryChannel(360825166437285891);
            var generalStarboard = new StarboardWatcher
            (
                client,
                client.GetGuild(DiscordSettings.GuildId),
                guild.GetTextChannel(DiscordSettings.StarboardChannel),
                channel => textChannels.Channels.Any(textChannel => channel.Id == textChannel.Id),
                emote => emote.Name == "woot"
            );
            generalStarboard.Bind();
            */

            var staffChannels = guild.GetCategoryChannel(360824776635318284);
            var staffStarboard = new StarboardWatcher
            (
                client,
                client.GetGuild(DiscordSettings.GuildId),
                guild.GetTextChannel(DiscordSettings.StarboardChannel),
                channel => staffChannels.Channels.Any(staffChannel => channel.Id == staffChannel.Id),
                emote => emote.Name == "woot"
            );
            staffStarboard.Bind();

            await Task.WhenAll
            (
                QuickReportWatcher.BindAsync(client, tcs.Task)
            );

            await Task.Delay(10000);
            var crawler = new Crawler(client);

            await Task.WhenAny(Task.Delay(TimeSpan.FromDays(2)), crawler.StartAsync());
            throw new Exception("Restart me!");
        }
    }
}