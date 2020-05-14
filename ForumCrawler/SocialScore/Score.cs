using Discord;
using Discord.WebSocket;
using ForumCrawler;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordSocialScore
{
    public static class Score
    {

        public static event Action<ulong, ScoreData> OnUpdate;

        private static async Task<T> WithUserAsync<T>(DiscordSocketClient client, ulong userId, Func<ScoreUser, Task<T>> callback)
        {
            return await WithUserUpdateAsync(client, userId, async u => (false, await callback(u)));
        }

        public static async Task<List<(IGuildUser, ScoreUser)>> GetUsersUserHasBoosted(IGuild guild, IEntity<ulong> entityUser)
        {
            var boosting = await Database.GetScoreUsersUserIsBoosting(user => user.Id == (long)entityUser.Id);

            var guildUsers = new List<(IGuildUser, ScoreUser)>();

            foreach (var boostingUser in boosting)
            {
                var user = await guild.GetUserAsync(boostingUser.UserId);

                if (user == null)
                {
                    continue;
                }

                guildUsers.Add((user, boostingUser));
            }

            return guildUsers;
        }

        private static async Task<T> WithUserUpdateAsync<T>(DiscordSocketClient client, ulong userId, Func<ScoreUser, Task<(bool, T)>> callback)
        {
            var userObj = await Database.GetOrCreateScoreUserAsync(client, userId);
            var (shouldUpdate, res) = await callback(userObj);
            if (shouldUpdate)
            {
                await Database.AddOrUpdateScoreUserAsync(userObj);
                OnUpdate?.Invoke(userObj.UserId, userObj.ScoreData);
            }
            return res;
        }

        private static async Task<(ScoreData, T)> WithTargetAndInvokerAsync<T>(DiscordSocketClient client, ulong targetUserId, ulong invokerUserId, Func<ScoreUser, ScoreUser, T> callback)
        {
            return await WithUserUpdateAsync(client, targetUserId, async invoker => (true, await WithUserUpdateAsync(client, invokerUserId, voter =>
            {
                var res = callback(invoker, voter);

                return Task.FromResult((true, (invoker.ScoreData, res)));
            })));
        }

        public static async Task<(ScoreData, int)> DailyAsync(DiscordSocketClient client, ulong targetUserId, ulong invokerUserId)
        {
            var upvoterGuildUser = client.GetGuild(DiscordSettings.GuildId).GetUser(invokerUserId);
            var targetGuildUser = client.GetGuild(DiscordSettings.GuildId).GetUser(targetUserId);
            if ((DateTimeOffset.UtcNow - upvoterGuildUser.JoinedAt)?.TotalDays < 3) 
                throw new Exception("You have recently joined this server and may not use g!daily yet!");
            if ((DateTimeOffset.UtcNow - targetGuildUser.JoinedAt)?.TotalDays < 3) 
                throw new Exception("The target has recently joined this server and may not receive g!daily yet!");

            return await WithTargetAndInvokerAsync(client, targetUserId, invokerUserId, (target, invoker) =>
            {
                return invoker.Daily(target);
            });
        }

        public static async Task<(ScoreData, double)> UpvoteAsync(DiscordSocketClient client, ulong targetUserId, ulong invokerUserId)
        {
            var upvoterGuildUser = client.GetGuild(DiscordSettings.GuildId).GetUser(invokerUserId);
            var targetGuildUser = client.GetGuild(DiscordSettings.GuildId).GetUser(targetUserId);
            if ((DateTimeOffset.UtcNow - upvoterGuildUser.JoinedAt)?.TotalDays < 3) throw new Exception("You have recently joined this server and may not g!up other users yet!");
            if ((DateTimeOffset.UtcNow - targetGuildUser.JoinedAt)?.TotalDays < 3) throw new Exception("The target has recently joined this server and may not receive g!up from other users yet!");

            return await WithTargetAndInvokerAsync(client, targetUserId, invokerUserId, (target, upvoter) =>
            {
                return upvoter.Upvote(target);
            });
        }

        public static async Task<(ScoreData, double)> DownvoteAsync(DiscordSocketClient client, ulong targetUserId, ulong invokerUserId)
        {
            var upvoterGuildUser = client.GetGuild(DiscordSettings.GuildId).GetUser(invokerUserId);
            var targetGuildUser = client.GetGuild(DiscordSettings.GuildId).GetUser(targetUserId);
            if ((DateTimeOffset.UtcNow - upvoterGuildUser.JoinedAt)?.TotalDays < 3) throw new Exception("You have recently joined this server and may not g!down other users yet!");
            if ((DateTimeOffset.UtcNow - targetGuildUser.JoinedAt)?.TotalDays < 3) throw new Exception("The target has recently joined this server and may not receive g!down from other users yet!");

            return await WithTargetAndInvokerAsync(client, targetUserId, invokerUserId, (target, upvoter) =>
            {
                return upvoter.Downvote(target);
            });
        }

        public static async Task<ScoreData> GetScoreDataAsync(DiscordSocketClient client, ulong userId)
        {
            return await WithUserAsync(client, userId, u => Task.FromResult(u.ScoreData));
        }

        public static async Task<List<(ulong Key, DateTime LastBoost)>> GetHistoryAsync(DiscordSocketClient client, ulong userId)
        {
            return await WithUserAsync(client, userId, u => Task.FromResult(u.Boosts
                .Where(kv => (DateTime.UtcNow - kv.Value).TotalDays < 7)
                .Select(kv => (kv.Key, TimeLeft: kv.Value))
                .OrderByDescending(boost => boost.TimeLeft)
                .ToList()
            ));
        }

        public static async Task<List<(ulong Key, TimeSpan TimeLeft)>> GetBoostsAsync(DiscordSocketClient client, ulong userId)
        {
            return await WithUserAsync(client, userId, u => Task.FromResult(u.GetBoostsLeft()
                .Select(kv => (kv.Key, TimeLeft: kv.Value))
                .OrderByDescending(boost => boost.TimeLeft)
                .ToList()
            ));
        }

        public static async Task<double> SetScoreAsync(DiscordSocketClient client, ulong userId, double score)
        {
            return await WithUserUpdateAsync(client, userId, u =>
            {
                u.Score = Math.Max(-10, Math.Min(5, score));
                return Task.FromResult((true, u.Score));
            });
        }

        public static async Task<double> SetInertiaAsync(DiscordSocketClient client, ulong userId, double inertia)
        {
            return await WithUserUpdateAsync(client, userId, u =>
            {
                u.Inertia = Math.Max(0, Math.Min(1, inertia));
                return Task.FromResult((true, u.Inertia));
            });
        }

        public static async Task<ScoreData> CreditActivityScoreAsync(DiscordSocketClient client, ulong activityUserId)
        {
#if DEBUG
            return await GetScoreDataAsync(client, activityUserId);
#else
            return await WithUserUpdateAsync(client, activityUserId, async user =>
            {
                var res = user.CreditActivity();
                return (res, user.ScoreData);
            });
#endif
        }

        public static async Task UpdateDecays(DiscordSocketClient client, Func<ulong, ScoreData, Task> callback)
        {
            await Database.WithAllScoreUsersAsync(client, async scoreUsers =>
            {
                foreach (var user in scoreUsers)
                {
                    await callback(user.UserId, user.ScoreData);
                }
            });
        }
    }

    public static class MyEnumerableExtensions
    {
        public static IEnumerable<T> LastN<T>(this IList<T> list, int n)
        {
            if (list == null)
            {
                throw new ArgumentNullException(nameof(list));
            }

            return LastN2();

            IEnumerable<T> LastN2()
            {
                if (list.Count - n < 0)
                {
                    n = list.Count;
                }

                for (var i = list.Count - n; i < list.Count; i++)
                {
                    yield return list[i];
                }
            }
        }

        public static double RandomNormal(this Random random, double mean, double stdDev)
        {
            var u1 = 1.0 - random.NextDouble(); //uniform(0,1] random doubles
            var u2 = 1.0 - random.NextDouble();
            var randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                         Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
            var randNormal =
                         mean + (stdDev * randStdNormal); //random normal(mean,stdDev^2)
            return randNormal;
        }
    }
}