using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;

using DiscordSocialScore;

using Microsoft.Extensions.DependencyInjection;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace ForumCrawler
{
    public static class DiscordSettings
    {
#if DEBUG
        public const string CommandPrefix = "dbg!";
#else
        public const string CommandPrefix = "g!";
#endif
        public const string UrlPrefix = "https://greg.cf/";

        public const ulong GuildId = 329335303963803649;
        public const ulong BotCommandsChannel = 329634826061742081;
        public const ulong LogsChannel = 330328534780346378;
        public const ulong ForumChannel = 329855149549813760;
        public const ulong DiscordStaffConsultant = 529386148682924036;
        public const ulong DiscordStaff = 329400136021049348;
        public const ulong DiscordServerOwner = 395419811372597249;
        public const ulong MutedRole = 333618650634387458;
        public const ulong EighteenRole = 407999201830764544;
        public const ulong ReportsChannel = 538738875229667329;
        public const ulong StarboardChannel = 589926820825006120;
        public const ulong BannedEmote = 614156104682962954;

        public const ulong DSGuildId = 705927053874167909;
        public const ulong DSDiscordStaffConsultant = 705928172641714227;
        public const ulong DSDiscordStaff = 705932947294781510;
        public const ulong DSDiscordServerOwner = 705928497851531325;
        public const ulong DSStaffCommandsChannel = 705936434489589822;

        public static Dictionary<ulong, GovernanceConfig> GovernanceConfigs = new Dictionary<ulong, GovernanceConfig> {
            { GuildId, new GovernanceConfig(549399741994237984, 549402714103087144) },
            { DSGuildId, new GovernanceConfig(705937563021737985, 705940856099569714) } 
        };

        public const int MinStarboardReactions = 10;

        public class GovernanceConfig
        {
            public ulong Category { get; }
            public ulong ChangelogChannel { get; }

            public GovernanceConfig(ulong category, ulong changelogChannel)
            {
                Category = category;
                ChangelogChannel = changelogChannel;
            }
        }

        public static async Task<DiscordSocketClient> GetClient()
        {
            var client = new DiscordSocketClient(new DiscordSocketConfig
            {
                MessageCacheSize = 50,
                AlwaysDownloadUsers = true
            });
            client.Log += Log;
            client.Ready += () => Ready(client);

            await client.LoginAsync(TokenType.Bot, Passwords.DiscordToken);
            await client.StartAsync();
            await client.SetStatusAsync(UserStatus.Online);

            return client;
        }

        private static async Task Ready(DiscordSocketClient client) => await client.SetGameAsync("Greg Simulator " + DateTimeOffset.UtcNow.Year);

        private static Task Log(LogMessage message)
        {
            Console.WriteLine(message.ToString());
            return Task.CompletedTask;
        }

        public static async Task<CommandService> InstallCommands(DiscordSocketClient client, IServiceProvider services)
        {
            // Hook the MessageReceived Event into our Command Handler
            var commands = new CommandService();
            commands.Log += Log;
            client.MessageReceived += msg => HandleCommand(client, services, commands, msg);
            // Discover all of the commands in this assembly and load them.
            await commands.AddModulesAsync(Assembly.GetEntryAssembly(), services);
            await commands.AddModulesAsync(Assembly.GetAssembly(typeof(Engine)), services);
            return commands;
        }

        public static IServiceProvider ConfigureServices(DiscordSocketClient client)
        {
            var services = new ServiceCollection()
                .AddSingleton(client)
                .AddSingleton<InteractiveService>()
                .AddSingleton(new RoleCacheProvider(client))
                .AddSingleton<Engine>();
            var provider = services.BuildServiceProvider();
            return provider;
        }

        public static Task HandleCommand(DiscordSocketClient client, IServiceProvider services, CommandService commands, SocketMessage messageParam)
        {
            if (!(messageParam is SocketUserMessage message)) return Task.Delay(0);

            Task.Run(async () =>
            {
                var argPos = 0;

                // add ability to run report command via d!report
                // lol this is so bad
                if (message.HasStringPrefix("d!", ref argPos))
                {
                    var argPosCopy = argPos;
                    if (message.HasStringPrefix("d!report", ref argPosCopy))
                    {
                        var context = new SocketCommandContext(client, message);

                        await context.Message.Channel.SendMessageAsync($":slight_frown: | [Warning] {message.Author.Mention} - d!report is obsolete! Please use g!report.")
                            ;

                        var result = await commands.ExecuteAsync(context, argPos, services);

                        if (!result.IsSuccess)
                        {
                            await context.Message.Channel.SendErrorAsync(result.ErrorReason);
                        }
                    }

                    return;
                }

                if (!(message.HasStringPrefix(CommandPrefix, ref argPos) || message.HasMentionPrefix(client.CurrentUser, ref argPos))) return;

                {
                    var context = new SocketCommandContext(client, message);
                    var result = await commands.ExecuteAsync(context, argPos, services);
                    if (!result.IsSuccess)
                    {
                        await context.Message.Channel.SendErrorAsync(result.ErrorReason);
                    }
                }
            });

            return Task.Delay(0);
        }
    }
}