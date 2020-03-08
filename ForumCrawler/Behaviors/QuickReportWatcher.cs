using Discord;
using Discord.WebSocket;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ForumCrawler
{
    public static class QuickReportWatcher
    {
        private static event Func<ulong, IUser, IUser, IMessageChannel, IUserMessage, string, Task> OnReport;

        private static event Func<ulong, IUser, Report.ReportStatus, Task> OnResponse;

        public static async Task Bind(DiscordSocketClient client)
        {
            var reports = await Database.PullReports(client);

            client.ReactionAdded += Client_ReactionAdded;
            OnReport += async (a, b, c, d, e, f) =>
            {
                await QuickReportWatcher_OnReport(client, reports, a, b, c, d, e, f);
                await SaveReportToDatabase(client, reports, a, b, c, d, e, f);
            };

            OnResponse += async (a, b, c) =>
            {
                await QuickReportWatcher_OnResponse(client, reports, a, b, c);
                await UpdateReportInDatabase(client, reports, a, b, c);
            };
        }

        private static Task SaveReportToDatabase(DiscordSocketClient client, Dictionary<ulong, Report> reports, ulong reportId, IUser reporter, IUser suspect, IMessageChannel channel, IUserMessage message, string reason)
        {
            // we expect QuickReportWatcher_OnReport to have executed first
            return Database.AddReport(reports[reportId]);
        }

        private static Task UpdateReportInDatabase(DiscordSocketClient client, Dictionary<ulong, Report> reports, ulong msgId, IUser moderator, Report.ReportStatus status)
        {
            // we expect QuickReportWatcher_OnResponse to have executed first
            return Database.UpdateReport(client, reports, msgId, moderator, status);
        }

        private static async Task QuickReportWatcher_OnResponse(DiscordSocketClient client, Dictionary<ulong, Report> reports, ulong msgId, IUser moderator, Report.ReportStatus status)
        {
            var report = reports.SingleOrDefault(x => x.Value.ReportsMessage?.Id == msgId).Value;
            if (report == null) return;
            if (report.Status == status) return;

            var guildUserMod = (IGuildUser)moderator;
            if (!guildUserMod.RoleIds.Contains(DiscordSettings.DiscordStaff)) return;

            report.Moderator = moderator;
            report.Status = status;
            await Task.WhenAll(report.Reporters.Keys.Select(u =>
                client.GetUser(u).SendMessageAsync("Your report was updated.", embed: GetReportEmbed(client, report, u))));
            await SendOrUpdateReport(client, report);
        }

        private static async Task QuickReportWatcher_OnReport(DiscordSocketClient client, Dictionary<ulong, Report> reports, ulong reportId, IUser reporter, IUser suspect, IMessageChannel channel, IUserMessage message, string reason)
        {
            if (!reports.TryGetValue(reportId, out var report))
            {
                if (suspect == null || channel == null) throw new Exception("Unknown user / message / report id.");
                reports.Add(reportId, report = new Report
                {
                    Id = reportId,
                    Timestamp = DateTimeOffset.UtcNow
                });
            }

            if (report.Status != Report.ReportStatus.Open)
            {
                await reporter.SendMessageAsync("Message already handled by a moderator. Your report was cancelled.",
                    embed: GetReportEmbed(client, report, reporter.Id));
                return;
            }

            var update = report.Reporters.TryGetValue(reporter.Id, out var oldReason);
            report.Reporters[reporter.Id] = reason ?? oldReason;
            report.Suspect = suspect ?? report.Suspect;
            report.Channel = channel ?? report.Channel;
            report.Message = message ?? report.Message;

            var embed = GetReportHeaderEmbed(report);
            if (report.Reporters[reporter.Id] != null)
                embed.AddField("Reason", report.Reporters[reporter.Id]);
            await SendOrUpdateReport(client, report);
            await reporter?.SendMessageAsync($"You {(update ? "updated" : "sent")} a report.", embed: embed.Build());
        }

        private static async Task SendOrUpdateReport(DiscordSocketClient client, Report report)
        {
            var embed = GetReportEmbed(client, report, null);

            if (report.ReportsMessage != null)
            {
                await report.ReportsMessage.ModifyAsync(m => m.Embed = embed);
            }
            else
            {
                report.ReportsMessage = await client
                    .GetGuild(DiscordSettings.GuildId)
                    .GetTextChannel(DiscordSettings.ReportsChannel)
#if DEBUG
                    .SendMessageAsync("@<debug> A report has been sent in!", embed: embed);
#else
                    .SendMessageAsync("@here A report has been sent in!", embed: embed);
#endif
            }
        }

        private static string GetUnmentionedUser(IUser user) => user.Username.DiscordEscape() + "#" + user.Discriminator;

        private static Embed GetReportEmbed(DiscordSocketClient client, Report report, ulong? privacy)
        {
            var embed = GetReportHeaderEmbed(report);

            var reportersStr = string.Join(", ", report.Reporters.Keys.Where(k => privacy == null || k == privacy.Value).Select(u => MentionUtils.MentionUser(u)));
            // Reported by
            embed.AddField($"Reported by {report.Reporters.Count} {(report.Reporters.Count == 1 ? "user" : "users")}",
                    string.IsNullOrEmpty(reportersStr) ? "<Hidden>" : reportersStr);

            // Reasons
            foreach (var kv in report.Reporters.Where(kv => privacy == null || kv.Key == privacy.Value).Where(kv => kv.Value != null))
            {
                embed.AddField(GetUnmentionedUser(client.GetUser(kv.Key)) + "'s reason", kv.Value);
            }

            // Status
            embed.AddField("Status", report.Status, true)
                .WithColor(report.Status == Report.ReportStatus.Accepted
                    ? Color.Green
                    : report.Status == Report.ReportStatus.Rejected
                        ? Color.Red
                        : Color.Blue);

            // Moderator
            if (report.Moderator != null)
                embed.AddField("Moderator", MentionUtils.MentionUser(report.Moderator.Id), true);

            return embed.Build();
        }

        private static EmbedBuilder GetReportHeaderEmbed(Report report)
        {
            // User and Channel
            var embed = new EmbedBuilder()
                .WithAuthor(report.Suspect)
                .AddField("User", MentionUtils.MentionUser(report.Suspect.Id), true)
                .AddField("Channel", MentionUtils.MentionChannel(report.Channel.Id), true)
                .WithFooter("ID: " + report.Id);

            // Message
            if (report.Message != null)
                embed.AddField("Message", $"[Link](https://discordapp.com/channels/{DiscordSettings.GuildId}/{report.Channel.Id}/{report.Message.Id})", true)
                    .WithTimestamp(SnowflakeUtils.FromSnowflake(report.Message.Id));
            else
                embed.WithTimestamp(report.Timestamp);
            return embed;
        }

        private static async Task Client_ReactionAdded(Cacheable<IUserMessage, ulong> msgCache, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (reaction.Emote is Emote emote && emote.Id == DiscordSettings.BannedEmote)
            {
                var msg = await msgCache.GetOrDownloadAsync();
                if (msg == null || !reaction.User.IsSpecified) return;
                await FileReport(msg.Id, reaction.User.Value, msg.Author, channel, msg, null);
            }
            else if (channel.Id == DiscordSettings.ReportsChannel && reaction.Emote.Name == "✅")
            {
                if (!reaction.User.IsSpecified) return;
                await FileResponse(msgCache.Id, reaction.User.Value, Report.ReportStatus.Accepted);
            }
            else if (channel.Id == DiscordSettings.ReportsChannel && reaction.Emote.Name == "❌")
            {
                if (!reaction.User.IsSpecified) return;
                await FileResponse(msgCache.Id, reaction.User.Value, Report.ReportStatus.Rejected);
            }
        }

        public static async Task FileReport(ulong reportId, IUser reporter, IUser suspect = null, IMessageChannel channel = null, IUserMessage message = null, string reason = null) => await OnReport(reportId, reporter, suspect, channel, message, reason);

        public static async Task FileResponse(ulong msgId, IUser moderator, Report.ReportStatus status) => await OnResponse(msgId, moderator, status);

        public class Report
        {
            public ulong Id { get; set; }

            public Dictionary<ulong, string> Reporters { get; set; } = new Dictionary<ulong, string>();
            public IUser Suspect { get; set; }
            public IMessageChannel Channel { get; set; }
            public IUserMessage Message { get; set; }
            public IUserMessage ReportsMessage { get; set; }

            public DateTimeOffset Timestamp { get; set; }
            public ReportStatus Status { get; set; }
            public IUser Moderator { get; set; }

            public enum ReportStatus
            {
                Open,
                Accepted,
                Rejected
            }
        }
    }
}