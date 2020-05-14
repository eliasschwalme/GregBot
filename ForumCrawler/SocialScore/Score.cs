using Discord;
using Discord.WebSocket;
using ForumCrawler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Threading.Tasks;

namespace DiscordSocialScore
{
    public static class Score
    {

        public static event Action<ulong, ScoreData> OnUpdate;

        public static async Task<List<(IGuildUser, ScoreUser)>> GetUsersUserHasBoosted(IGuild guild, IEntity<ulong> entityUser)
        {
            var boosting = await Database.UNSAFE_GetScoreUsersUserIsBoosting(user => user.Id == (long)entityUser.Id);

            var guildUsers = new List<(IGuildUser, ScoreUser)>();

            foreach (var boostingUser in boosting)
            {
                var user = await guild.GetUserAsync(boostingUser.UserId);
                if (user == null)  continue;

                guildUsers.Add((user, boostingUser));
            }

            return guildUsers;
        }

        private static async Task<T> WithTargettedScoreCommand<T>(string command, DiscordSocketClient client, ulong targetUserId, ulong invokerUserId, Func<ScoreUser, ScoreUser, T> callback)
        {
            var upvoterGuildUser = client.GetGuild(DiscordSettings.GuildId).GetUser(invokerUserId);
            var targetGuildUser = client.GetGuild(DiscordSettings.GuildId).GetUser(targetUserId);
            if ((DateTimeOffset.UtcNow - upvoterGuildUser.JoinedAt)?.TotalDays < 3)
                throw new Exception($"You have recently joined this server and may not use {command} yet!");
            if ((DateTimeOffset.UtcNow - targetGuildUser.JoinedAt)?.TotalDays < 3)
                throw new Exception($"The target has recently joined this server and may not receive {command} yet!");

            using (var context = new DatabaseContext())
            {
                var user1 = Database.GetOrCreateScoreUserAsync(context, client, targetUserId);
                var user2 = Database.GetOrCreateScoreUserAsync(context, client, invokerUserId);
                var res = callback(await user1, await user2);
                await context.SaveChangesAsync();
                return res;
            }
        }

        public static async Task<(ScoreData, int)> DailyAsync(DiscordSocketClient client, ulong targetUserId, ulong invokerUserId)
        {
            return await WithTargettedScoreCommand("g!daily", client, targetUserId, invokerUserId, (target, invoker) =>
            {
                return (target.ScoreData, invoker.Daily(target));
            });
        }

        public static async Task<(ScoreData, double)> UpvoteAsync(DiscordSocketClient client, ulong targetUserId, ulong invokerUserId)
        {
            return await WithTargettedScoreCommand("g!up", client, targetUserId, invokerUserId, (target, upvoter) =>
            {
                return (target.ScoreData, upvoter.Upvote(target));
            });
        }

        public static async Task<(ScoreData, double)> DownvoteAsync(DiscordSocketClient client, ulong targetUserId, ulong invokerUserId)
        {
            return await WithTargettedScoreCommand("g!down", client, targetUserId, invokerUserId, (target, upvoter) =>
            {
                return (target.ScoreData, upvoter.Downvote(target));
            });
        }

        public static async Task<ScoreUser> GetScoreUserAsync(DiscordSocketClient client, ulong userId)
        {
            using (var context = new DatabaseContext())
            {
                return await Database.GetOrCreateScoreUserAsync(context, client, userId);
            }
        }

        public static async Task<ScoreData> GetScoreDataAsync(DiscordSocketClient client, ulong userId)
        {
            return (await GetScoreUserAsync(client, userId)).ScoreData;
        }

        public static async Task<List<(ulong Key, DateTime LastBoost)>> GetHistoryAsync(DiscordSocketClient client, ulong userId)
        {
            return (await GetScoreUserAsync(client, userId)).Boosts
                .Where(kv => (DateTime.UtcNow - kv.Value).TotalDays < 7)
                .Select(kv => (kv.Key, TimeLeft: kv.Value))
                .OrderByDescending(boost => boost.TimeLeft)
                .ToList();
        }

        public static async Task<List<(ulong Key, TimeSpan TimeLeft)>> GetBoostsAsync(DiscordSocketClient client, ulong userId)
        {
            return (await GetScoreUserAsync(client, userId)).GetBoostsLeft()
                .Select(kv => (kv.Key, TimeLeft: kv.Value))
                .OrderByDescending(boost => boost.TimeLeft)
                .ToList();
        }

        public static async Task SwapUsers(DiscordSocketClient client, ulong user1Id, ulong user2Id)
        {
            await WithTargetAndInvokerAsync(client, user1Id, user2Id, (user1, user2) =>
            {
                ScoreUser.SwapUsers(user1, user2);
                return 0;
            });
        }

        public static async Task<ScoreData> CreditActivityScoreAsync(DiscordSocketClient client, ulong activityUserId)
        {
#if DEBUG
            return await GetScoreDataAsync(client, activityUserId);
#else

            using (var context = new DatabaseContext())
            {
                var user = await Database.GetOrCreateScoreUserAsync(context, client, activityUserId);
                user.CreditActivity();
                await context.SaveChangesAsync();
                return user.ScoreData;
            }
#endif
        }

        public static async Task UpdateDecays(DiscordSocketClient client, Func<ulong, ScoreData, Task> callback)
        {
            using (var context = new DatabaseContext())
            {
                // this already calls update() on all useres
                var users = await Database.GetAllScoreUsersAsync(context, client).ToListAsync();
                foreach (var user in users)
                {
                    await callback(user.UserId, user.ScoreData);
                }
                await context.SaveChangesAsync();
            }
        }
    }
}