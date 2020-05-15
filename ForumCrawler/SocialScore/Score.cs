using Discord;
using Discord.WebSocket;
using ForumCrawler;
using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Threading.Tasks;

namespace ForumCrawler
{
    public static class Score
    {

        public static event Action<ulong, ScoreData> OnUpdate;


        private static async Task<T> WithTargettedScoreCommand<T>(string command, DiscordSocketClient client, ulong targetUserId, ulong invokerUserId, Func<ScoreUser, ScoreUser, T> callback)
        {
            using (var context = new DatabaseContext())
            {
                var user1 = await Database.GetOrCreateScoreUserAsync(context, client, targetUserId);
                var user2 = await Database.GetOrCreateScoreUserAsync(context, client, invokerUserId);
                var res = callback(user1, user2);
                await context.SaveChangesAsync();
                return res;
            }
        }

        public static async Task<(ScoreData, int)> DailyAsync(DiscordSocketClient client, ulong targetUserId, ulong invokerUserId)
        {
            return await WithTargettedScoreCommand("g!daily", client, targetUserId, invokerUserId, (target, invoker) =>
            {
                var increase = invoker.Daily(target);
                return (target.ScoreData, increase);
            });
        }

        public static async Task<(ScoreData, double)> UpvoteAsync(DiscordSocketClient client, ulong targetUserId, ulong invokerUserId)
        {
            return await WithTargettedScoreCommand("g!up", client, targetUserId, invokerUserId, (target, upvoter) =>
            {
                var change = upvoter.Upvote(target);
                return (target.ScoreData, change);
            });
        }

        public static async Task<(ScoreData, double)> DownvoteAsync(DiscordSocketClient client, ulong targetUserId, ulong invokerUserId)
        {
            return await WithTargettedScoreCommand("g!down", client, targetUserId, invokerUserId, (target, downvoter) =>
            {
                var change = downvoter.Downvote(target);
                return (target.ScoreData, change);
            });
        }

        internal static async Task<(ScoreData, ScoreData)> SendGems(DiscordSocketClient client, ulong targetUserId, ulong invokerUserId, int amount)
        {
            return await WithTargettedScoreCommand("g!send gem", client, targetUserId, invokerUserId, (target, sender) =>
            {
                sender.SendGems(target, amount);
                return (target.ScoreData, sender.ScoreData);
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
        public static async Task<List<(ulong Key, TimeSpan TimeLeft)>> GetBoostingsAsync(DiscordSocketClient client, ulong userId)
        {
            using (var context = new DatabaseContext())
            {
                return (await Database.GetAllScoreUsersAsync(context, client).ToListAsync())
                    .Select(u => (Key: u.UserId, BoostsLeft: u.GetBoostsLeft()))
                    .Where(u => u.BoostsLeft.ContainsKey(userId))
                    .Select(u => (u.Key, TimeLeft: u.BoostsLeft[userId]))
                    .OrderByDescending(boost => boost.TimeLeft)
                    .ToList();
            }
        }

        public static async Task SwapUsers(DiscordSocketClient client, ulong user1Id, ulong user2Id)
        {
            ScoreUser user1, user2;
            using (var context = new DatabaseContext())
            {
                user1 = await Database.GetOrCreateScoreUserAsync(context, client, user1Id);
                user2 = await Database.GetOrCreateScoreUserAsync(context, client, user2Id);
            };
            ScoreUser.SwapUsers(user1, user2);

            // EF does not let us change the primary key of an object 
            // We create a new context to make it think everything but the ID was swapped
            using (var context = new DatabaseContext())
            {
                context.ScoreUsers.Attach(user1);
                context.ScoreUsers.Attach(user2);
                context.Entry(user1).State = System.Data.Entity.EntityState.Modified;
                context.Entry(user2).State = System.Data.Entity.EntityState.Modified;
                context.SaveChanges();
            }
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