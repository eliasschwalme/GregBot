using Discord;
using Discord.WebSocket;
using DiscordSocialScore;

using System;
using System.Collections.Generic;
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

            await Task.WhenAll
            (
                QuickReportWatcher.BindAsync(client, tcs.Task)
            );

            var guild = client.GetGuild(DiscordSettings.GuildId);

            var generalStarboard = StarboardWatcherConfigurator.GeneralStarboard(client, guild);
            var staffStarboard = StarboardWatcherConfigurator.StaffStarboard(client, guild);
            var temp = StarboardWatcherConfigurator.TempStarboard(client, guild);

            await Task.Delay(10000);
            var crawler = new Crawler(client);

            await Task.WhenAny(Task.Delay(TimeSpan.FromDays(2)), crawler.StartAsync());
            throw new Exception("Restart me!");
        }
    }

    public static class StarboardWatcherConfigurator
    {
        public static StarboardWatcher GeneralStarboard(DiscordSocketClient client, SocketGuild guild)
        {
            var starboard = new StarboardWatcher
            (
                client,
                client.GetGuild(DiscordSettings.GuildId),
                guild.GetTextChannel(DiscordSettings.StarboardChannel),
                ChannelCategoryQualifier(guild.GetCategoryChannel(360825166437285891)), // text channels
                WootQualifier,
                10
            );

            starboard.Bind();

            return starboard;
        }

        public static StarboardWatcher StaffStarboard(DiscordSocketClient client, SocketGuild guild)
        {
            var starboard = new StarboardWatcher
            (
                client,
                client.GetGuild(DiscordSettings.GuildId),
                guild.GetTextChannel(696765428784955392),
                ChannelCategoryQualifier(guild.GetCategoryChannel(360824776635318284)), // staff channels
                WootQualifier,
                2
            );

            starboard.Bind();

            return starboard;
        }

        public static StarboardWatcher TempStarboard(DiscordSocketClient client, SocketGuild guild)
        {
            var starboard = new StarboardWatcher
            (
                client,
                client.GetGuild(DiscordSettings.GuildId),
                guild.GetTextChannel(696975128117903361),
                ChannelCategoryQualifier(guild.GetCategoryChannel(360825166437285891)), // text channels
                WootQualifier,
                2
            );

            starboard.Bind();

            return starboard;
        }

        private static bool WootQualifier(IEmote emote)
             => emote.Name == "woot";

        private static ChannelQualifier ChannelCategoryQualifier(SocketCategoryChannel category)
            => channel => category.Channels.Any(categoryChannel => categoryChannel.Id == channel.Id);
    }
}