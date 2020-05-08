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
        private static readonly Random random = new Random();

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

        private static async Task<(ScoreData, T)> WithWootAsync<T>(DiscordSocketClient client, ulong targetUserId, ulong voterUserId, Func<ScoreUser, ScoreUser, T> callback)
        {
            return await WithUserUpdateAsync(client, targetUserId, async target => (true, await WithUserUpdateAsync(client, voterUserId, voter =>
            {
                if (target == voter) return Task.FromResult((false, (target.ScoreData, default(T))));

                var res = callback(target, voter);

                target.Boosts[voter.UserId] = DateTime.UtcNow;
                return Task.FromResult((true, (target.ScoreData, res)));
            })));
        }

        public static async Task<(ScoreData, double)> UpvoteAsync(DiscordSocketClient client, ulong targetUserId, ulong upvoterUserId)
        {
            if (targetUserId == upvoterUserId) throw new Exception($"Sorry, upvoting yourself is not allowed!");

            return await WithWootAsync(client, targetUserId, upvoterUserId, (target, upvoter) =>
            {
                if (target.Score < 1.0995 || upvoter.Score < 1.0995) throw new Exception("Users under 1.1 cannot not send or receive upvotes.");
                if (Math.Abs(target.Score - upvoter.Score) > 1) throw new Exception("The score difference between upvoters cannot be over 1.0.");

                var lastBoost = target.GetLastBoost(upvoter.UserId);
                var boostLeft = target.GetBoostLeft(upvoter.UserId);
                var sinceLastBoost = DateTimeOffset.UtcNow - lastBoost;
                if (boostLeft.TotalSeconds > 0) throw new Exception($"Please wait {boostLeft.ToHumanReadableString()} before upvoting this person again.");

                if (upvoter.Energy < 100) throw new Exception($"An upvote costs 100 energy! You currently have __**{Math.Floor(upvoter.Energy)}**__/{upvoter.MaxEnergy} energy.");
                upvoter.Energy -= 100;

                var randomEff = Math.Max(0.5, Math.Min(2.5, random.RandomNormal(1.5, 0.4)));

                var discount = 0.25 + (0.50 * Math.Min(3, sinceLastBoost.TotalDays) / 3) + (0.25 * Math.Min(7, sinceLastBoost.TotalDays) / 7);
                var scoreDifference = upvoter.Score - target.Score;
                var scoreDiffModifier = Math.Sqrt(1 + Math.Max(-0.75, scoreDifference));

                var efficiency = scoreDiffModifier * discount * randomEff;
                var value = 15 * efficiency;
                target.TotalPoints += value;

                return efficiency;
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

        public static async Task<double> SetEnergyAsync(DiscordSocketClient client, ulong userId, double energy)
        {
            return await WithUserUpdateAsync(client, userId, u =>
            {
                u.Energy = Math.Max(0, energy);
                return Task.FromResult((true, u.Energy));
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
#pragma warning disable CS0162 // Unreachable code detected
            //return await GetScoreDataAsync(client, activityUserId);
#endif
            return await WithUserUpdateAsync(client, activityUserId, async user =>
            {
                if (user == null || user.ScoreData == null)
                {
                    return (false, await GetScoreDataAsync(client, activityUserId));
                }

                if (user.LastActivity.HasValue
                && DateTimeOffset.UtcNow.Subtract(user.LastActivity.Value).TotalMinutes < 1.5)
                {
                    return (false, user.ScoreData);
                }

                var increase = 3.5 - Math.Max(0.25, Math.Min(3, user.Score));

                user.LastActivity = DateTime.UtcNow;
                user.TotalPoints += increase;
                return (true, user.ScoreData);
            });
        }

        public static async Task UpdateDecays(DiscordSocketClient client, Func<ulong, ScoreData, Task> callback)
        {
            await Database.WithAllScoreUsersAsync(client, async scoreUsers =>
            {
                foreach (var user in scoreUsers)
                {
                    if (user == null || user.ScoreData == null)
                    {
                        continue;
                    }

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