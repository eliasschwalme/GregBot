using Discord;
using Discord.WebSocket;

using EntityFramework.Extensions;

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace ForumCrawler
{
    internal static class Database
    {
        public static async Task<StarboardInformation> GetStarboardInformation(ulong messageId)
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
                    OnStarboard = post.OnStarboard,
                    StarboardMessageId = (ulong)post.StarboardMessageId
                };
            }
        }

        internal static async Task<Dictionary<ulong, QuickReportWatcher.Report>> PullReports(DiscordSocketClient client)
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
                    if (channel == null) continue;
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

        public static async Task CreateStarboardEntry(ulong messageId, ulong starboardMessageId)
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
                return;
            }
        }

        internal static async Task UpdateReport(ulong msgId, IUser moderator, QuickReportWatcher.Report.ReportStatus status)
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

        public static async Task AddReport(QuickReportWatcher.Report report)
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

        public static async Task DeleteStarboardEntry(ulong messageId)
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

        public static async Task<IEnumerable<ScoreUser>> GetScoreUsersUserIsBoosting(Expression<Func<ScoreUser, bool>> userPredicate)
        {
            using (var context = new DatabaseContext())
            {
                var user = await context.ScoreUsers
                    .FirstAsync(userPredicate)
                    ;

                var users = await context.ScoreUsers
                    .ToListAsync();

                var query = users
                    .Where(u => u.Boosts.Keys.Contains(user.UserId))
#if DEBUG
                    .ToList()
#endif
                ;

                return query;
            }
        }

        public static async Task WithAllScoreUsersAsync(DiscordSocketClient client, Action<IEnumerable<ScoreUser>> callback)
        {
            using (var context = new DatabaseContext())
            {
                callback((await context.ScoreUsers.ToListAsync()).Select(
                    (scoreUser) =>
                    {
                        scoreUser.Update(client, scoreUser.UserId);
                        return scoreUser;
                    }));
                await context.SaveChangesAsync();
            }
        }

        public static async Task<List<ScoreUser>> GetScoreUsersByLeaderboardPositionAsync(int page)
        {
            using (var context = new DatabaseContext())
            {
                return await context.ScoreUsers.OrderByDescending(u => u.Score).Skip(10 * page).Take(10).ToListAsync();
            }
        }

        public static async Task<(ScoreUser, int)> GetOrCreateScoreUserAndLeaderboardPositionAsync(DiscordSocketClient client, ulong userId)
        {
            var myScoreUser = await GetOrCreateScoreUserAsync(client, userId);
            using (var context = new DatabaseContext())
            {
                return (myScoreUser, 1 + await context.ScoreUsers.CountAsync(u => u.Score > myScoreUser.Score));
            }
        }

        public static async Task<ScoreUser> GetOrCreateScoreUserAsync(DiscordSocketClient client, ulong userId)
        {
            using (var context = new DatabaseContext())
            {
                var res = await context.ScoreUsers.SingleOrDefaultAsync(m => m.Id == (long)userId);
                if (res == null)
                {
                    res = new ScoreUser { UserId = userId };
                    context.ScoreUsers.AddOrUpdate(res);
                    await context.SaveChangesAsync();
                }
                res.Update(client, userId);
                return res;
            }
        }
        public static async Task<ScoreUser[]> GetScoreUsers(IEnumerable<ulong> userIds)
        {
            var userIdsLong = userIds.Select(u => (long)u).ToArray();
            using (var context = new DatabaseContext())
            {
                var res = await context.ScoreUsers.Where(m => userIdsLong.Contains(m.Id)).ToArrayAsync();
                
                return res;
            }
        }

        public static async Task AddOrUpdateScoreUserAsync(ScoreUser scoreUser)
        {
            using (var context = new DatabaseContext())
            {
                context.ScoreUsers.AddOrUpdate(scoreUser);
                await context.SaveChangesAsync();
            }
        }

        public static async Task<Mute[]> GetAllExpiredMutes(DateTimeOffset time)
        {
            using (var context = new DatabaseContext())
            {
                return await context.Mutes.Where(m => m.ExpiryDate < time).ToArrayAsync();
            }
        }

        public static async Task<Mute> GetMute(ulong id)
        {
            using (var context = new DatabaseContext())
            {
                return await context.Mutes.SingleOrDefaultAsync(m => m.Id == (long)id);
            }
        }

        public static async Task RemoveMute(ulong id)
        {
            using (var context = new DatabaseContext())
            {
                await context.Mutes.Where(m => m.Id == (long)id).DeleteAsync();
            }
        }

        public static async Task RemoveAllExpiredMutes(DateTimeOffset time)
        {
            using (var context = new DatabaseContext())
            {
                await context.Mutes.Where(m => m.ExpiryDate < time).DeleteAsync();
            }
        }

        public static async Task AddOrUpdateMuteAsync(Mute mute)
        {
            using (var context = new DatabaseContext())
            {
                context.Mutes.AddOrUpdate(mute);
                await context.SaveChangesAsync();
            }
        }

        internal static async Task AddGovernanceVoteAsync(GovernanceVote vote)
        {
            using (var context = new DatabaseContext())
            {
                context.GovernanceVotes.Add(vote);
                await context.SaveChangesAsync();
            }
        }

        internal static async Task<GovernanceVote> GetGovernanceVoteAsync(ulong id)
        {
            using (var context = new DatabaseContext())
            {
                return await context.GovernanceVotes.SingleOrDefaultAsync(m => m.Id == (long)id);
            }
        }

        public static async Task RemoveGovernanceVoteAsync(ulong id)
        {
            using (var context = new DatabaseContext())
            {
                await context.GovernanceVotes
                    .Where(m => m.Id == (long)id)
                    .DeleteAsync();
            }
        }

        public static async Task RemoveWarningAsync(long id, IMessage msg, string reason)
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

        public static async Task<Warning> GetWarningAsync(long id)
        {
            using (var context = new DatabaseContext())
            {
                return await context.Warnings.SingleOrDefaultAsync(w => w.Id == id);
            }
        }

        public static async Task<Warning[]> GetWarningsAsync(ulong id)
        {
            using (var context = new DatabaseContext())
            {
                return await context.Warnings.Where(w => w.UId == (long)id).ToArrayAsync();
            }
        }

        public static async Task<Warning> AddWarningAsync(Warning warning)
        {
            using (var context = new DatabaseContext())
            {
                warning = context.Warnings.Add(warning);
                await context.SaveChangesAsync();
                return warning;
            }
        }

        public static async Task WithWarningsAsync(ulong id, Func<Warning[], Task> callback)
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