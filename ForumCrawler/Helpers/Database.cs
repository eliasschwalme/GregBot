using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using EntityFramework.Extensions;

namespace ForumCrawler
{
    internal static class Database
    {
        //static Database()
        //{
        //    using (var ctx = new DatabaseContext())
        //    {
        //        ctx.ScoreUsers.Delete();
        //    }
        //}

        public static async Task<StarboardInformation> UNSAFE_GetStarboardInformation(ulong messageId)
        {
            using (var ctx = new DatabaseContext())
            {
                var post = await ctx.StarboardPosts.FirstOrDefaultAsync
                (
                    p => p.MessageId == (long)messageId
                );

                if (post == null)
                {
                    return default;
                }

                return new StarboardInformation
                {
                    OnStarboard = post.OnStarboard, StarboardMessageId = (ulong)post.StarboardMessageId
                };
            }
        }

        internal static async Task<Dictionary<ulong, QuickReportWatcher.Report>> UNSAFE_PullReports(
            DiscordSocketClient client)
        {
            using (var ctx = new DatabaseContext())
            {
                var reports = await ctx.Reports.ToListAsync();

                var dict = new Dictionary<ulong, QuickReportWatcher.Report>();
                var guild = client.GetGuild(DiscordSettings.DSGuildId);
                var reportChannel = guild.GetTextChannel(DiscordSettings.DSReportsChannel);

                foreach (var report in reports)
                {
                    var channel = guild.GetTextChannel(report.ChannelId);
                    if (channel == null)
                    {
                        continue;
                    }

                    dict[report.ReportId] = new QuickReportWatcher.Report
                    {
                        Channel = channel,
                        Id = report.ReportId,
                        MessageId = report.MessageId == 0 ? (ulong?)null : report.MessageId,
                        Moderator = report.ModeratorId == 0 ? null : guild.GetUser(report.ModeratorId),
                        Reporters = report.Reporters,
                        ReportsMessageId = report.ReportsMessage == 0 ? (ulong?)null : report.ReportsMessage,
                        Status = report.Status,
                        Suspect = guild.GetUser(report.SuspectId),
                        Timestamp = report.Timestamp
                    };
                }

                return dict;
            }
        }

        public static async Task UNSAFE_CreateStarboardEntry(ulong messageId, ulong starboardMessageId)
        {
            using (var ctx = new DatabaseContext())
            {
                var post = await ctx.StarboardPosts
                        .FirstOrDefaultAsync(p => p.MessageId == (long)messageId)
                    ;

                if (post == null)
                {
                    ctx.StarboardPosts.Add(new StarboardPost
                    {
                        MessageId = (long)messageId,
                        StarboardMessageId = (long)starboardMessageId,
                        OnStarboard = true
                    });

                    await ctx.SaveChangesAsync();
                    return;
                }

                post.MessageId = (long)messageId;
                post.StarboardMessageId = (long)starboardMessageId;
                post.OnStarboard = true;

                await ctx.SaveChangesAsync();
            }
        }

        internal static async Task UNSAFE_UpdateReport(ulong msgId, IUser moderator,
            QuickReportWatcher.Report.ReportStatus status)
        {
            using (var ctx = new DatabaseContext())
            {
                var msgIdLong = msgId.ToLong();
                var gregReportMessage = await ctx.Reports.FirstAsync(report => report._reportsMessage == msgIdLong);

                gregReportMessage.ModeratorId = moderator.Id;
                gregReportMessage.Status = status;

                await ctx.SaveChangesAsync();
            }
        }

        public static async Task UNSAFE_AddReport(QuickReportWatcher.Report report)
        {
            using (var ctx = new DatabaseContext())
            {
                ctx.Reports.Add(new ReportModel
                {
                    ReportId = report.Id,
                    ChannelId = report.Channel.Id,
                    MessageId = report.MessageId ?? 0,
                    ModeratorId = report.Moderator?.Id ?? 0,
                    Reporters = report.Reporters,
                    ReportsMessage = report.ReportsMessageId ?? 0,
                    Status = report.Status,
                    SuspectId = report.Suspect.Id,
                    Timestamp = report.Timestamp
                });

                await ctx.SaveChangesAsync();
            }
        }

        public static async Task UNSAFE_DeleteStarboardEntry(ulong messageId)
        {
            using (var ctx = new DatabaseContext())
            {
                var post = await ctx.StarboardPosts.FirstOrDefaultAsync
                (
                    p => p.MessageId == (long)messageId
                );

                if (post == null)
                {
                    // we're suppose to delete the entry that doesn't exist - we're good
                    return;
                }

                post.OnStarboard = false;
                post.StarboardMessageId = default;

                await ctx.SaveChangesAsync();
            }
        }

        public static IAsyncEnumerable<ScoreUser> GetAllScoreUsersAsync(DatabaseContext context,
            DiscordSocketClient client)
        {
            return context.ScoreUsers.ToAsyncEnumerable().Select(scoreUser =>
            {
                scoreUser.Update(client);
                return scoreUser;
            });
        }

        public static async Task<List<ScoreUser>> UNSAFE_GetScoreUsersByLeaderboardPositionAsync(int page)
        {
            using (var context = new DatabaseContext())
            {
                return await context.ScoreUsers.OrderByDescending(u => u.Score).Skip(10 * page).Take(10).ToListAsync();
            }
        }

        public static async Task<(ScoreUser, int)> UNSAFE_GetOrCreateScoreUserAndLeaderboardPositionAsync(
            DiscordSocketClient client, ulong userId)
        {
            using (var context = new DatabaseContext())
            {
                var myScoreUser = await GetOrCreateScoreUserAsync(context, client, userId);
                return (myScoreUser, 1 + await context.ScoreUsers.CountAsync(u => u.Score > myScoreUser.Score));
            }

            ;
        }

        public static async Task<ScoreUser> GetOrCreateScoreUserAsync(DatabaseContext context,
            DiscordSocketClient client, ulong userId)
        {
            var res = await context.ScoreUsers.SingleOrDefaultAsync(m => m.Id == (long)userId);
            if (res == null)
            {
                res = new ScoreUser {UserId = userId};
                context.ScoreUsers.AddOrUpdate(res);
                await context.SaveChangesAsync();
            }

            res.Update(client);
            return res;
        }

        public static async Task<ScoreUser[]> UNSAFE_GetScoreUsers(IEnumerable<ulong> userIds)
        {
            var userIdsLong = userIds.Select(u => (long)u).ToArray();
            using (var context = new DatabaseContext())
            {
                var res = await context.ScoreUsers.Where(m => userIdsLong.Contains(m.Id)).ToArrayAsync();

                return res;
            }
        }

        public static async Task<Mute[]> UNSAFE_GetAllExpiredMutes(DateTimeOffset time)
        {
            using (var context = new DatabaseContext())
            {
                return await context.Mutes.Where(m => m.ExpiryDate < time).ToArrayAsync();
            }
        }

        public static async Task<Mute> UNSAFE_GetMute(ulong id)
        {
            using (var context = new DatabaseContext())
            {
                return await context.Mutes.SingleOrDefaultAsync(m => m.Id == (long)id);
            }
        }

        public static async Task UNSAFE_RemoveMute(ulong id)
        {
            using (var context = new DatabaseContext())
            {
                await context.Mutes.Where(m => m.Id == (long)id).DeleteAsync();
            }
        }

        public static async Task UNSAFE_RemoveAllExpiredMutes(DateTimeOffset time)
        {
            using (var context = new DatabaseContext())
            {
                await context.Mutes.Where(m => m.ExpiryDate < time).DeleteAsync();
            }
        }

        public static async Task UNSAFE_AddOrUpdateMuteAsync(Mute mute)
        {
            using (var context = new DatabaseContext())
            {
                context.Mutes.AddOrUpdate(mute);
                await context.SaveChangesAsync();
            }
        }

        internal static async Task UNSAFE_AddGovernanceVoteAsync(GovernanceVote vote)
        {
            using (var context = new DatabaseContext())
            {
                context.GovernanceVotes.Add(vote);
                await context.SaveChangesAsync();
            }
        }

        internal static async Task<GovernanceVote> UNSAFE_GetGovernanceVoteAsync(ulong id)
        {
            using (var context = new DatabaseContext())
            {
                return await context.GovernanceVotes.SingleOrDefaultAsync(m => m.Id == (long)id);
            }
        }

        public static async Task UNSAFE_RemoveGovernanceVoteAsync(ulong id)
        {
            using (var context = new DatabaseContext())
            {
                await context.GovernanceVotes
                    .Where(m => m.Id == (long)id)
                    .DeleteAsync();
            }
        }

        public static async Task UNSAFE_RemoveWarningAsync(long id, IMessage msg, string reason)
        {
            using (var context = new DatabaseContext())
            {
                var warning = await context.Warnings.SingleOrDefaultAsync(w => w.Id == id);
                warning.RemoveDate = DateTime.UtcNow;
                warning.RemoveReason = reason;
                warning.RemoverId = msg.Author.Id;
                warning.RemoveChannelId = msg.Channel.Id;
                warning.RemoveMessageId = msg.Id;
                await context.SaveChangesAsync();
            }
        }

        public static async Task<Warning> UNSAFE_GetWarningAsync(long id)
        {
            using (var context = new DatabaseContext())
            {
                return await context.Warnings.SingleOrDefaultAsync(w => w.Id == id);
            }
        }

        public static async Task<Warning[]> UNSAFE_GetWarningsAsync(ulong id)
        {
            using (var context = new DatabaseContext())
            {
                return await context.Warnings.Where(w => w.UId == (long)id).ToArrayAsync();
            }
        }

        public static async Task<Warning> UNSAFE_AddWarningAsync(Warning warning)
        {
            using (var context = new DatabaseContext())
            {
                warning = context.Warnings.Add(warning);
                await context.SaveChangesAsync();
                return warning;
            }
        }

        public static async Task UNSAFE_WithWarningsAsync(ulong id, Func<Warning[], Task> callback)
        {
            using (var context = new DatabaseContext())
            {
                var warnings = await context.Warnings.Where(w => w.UId == (long)id).ToArrayAsync();
                await callback(warnings);
                await context.SaveChangesAsync();
            }
        }
    }
}