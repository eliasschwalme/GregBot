using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using ForumCrawler;
using Discord;

namespace DiscordSocialScore
{
    public static class Score
    {
        private static Random random = new Random();
        public static event Action<ulong, ScoreData> OnUpdate;

        private static async Task<T> WithUserAsync<T>(IGuildUser user, Func<ScoreUser, Task<T>> callback)
		{
			if (user == null)
			{
				throw new Exception("WithUserAsync<T> called with a null guild user.");
			}

			return await WithUserUpdateAsync(user, async u => (false, await callback(u)));
        }

        // can't do an IAsyncEnumerable because old :v
        public static async Task<List<(IGuildUser, ScoreUser)>> GetUsersUserHasBoosted(IGuild guild, IEntity<ulong> entityUser)
        {
			var boosting = await Database.GetScoreUsersUserIsBoosting(user => user.Id == (long)entityUser.Id).ConfigureAwait(false);

			var guildUsers = new List<(IGuildUser, ScoreUser)>();

            foreach(var boostingUser in boosting)
            {
				var user = await guild.GetUserAsync(boostingUser.UserId).ConfigureAwait(false);

                if (user == null)
                {
					continue;
				}

				guildUsers.Add((user, boostingUser));
			}

			return guildUsers;
		}

        private static async Task<T> WithUserUpdateAsync<T>(IGuildUser user, Func<ScoreUser, Task<(bool, T)>> callback)
        {
            if (user == null)
            {
				throw new Exception("WithUserUpdateAsync<T> called with a null guild user.");
			}

            var userObj = await Database.GetOrCreateScoreUserAsync(user);
            var (shouldUpdate, res) = await callback(userObj);
            if (shouldUpdate)
            {
                await Database.AddOrUpdateScoreUserAsync(userObj);
                OnUpdate?.Invoke(userObj.UserId, userObj.ScoreData);
            }
            return res;
        }

        private static async Task<(ScoreData, T)> WithWootAsync<T>(IGuildUser targetUser, IGuildUser voterUser, Func<ScoreUser, ScoreUser, T> callback)
        {
            return await WithUserUpdateAsync(targetUser, async target => (true, await WithUserUpdateAsync(voterUser, voter =>
            {
                if (target == voter) return Task.FromResult((false, (target.ScoreData, default(T))));

                var res = callback(target, voter);

                target.Boosts[voter.UserId] = DateTime.UtcNow;
                return Task.FromResult((true, (target.ScoreData, res)));
            })));
        }

        public static async Task<(ScoreData, double)> UpvoteAsync(IGuildUser targetUser, IGuildUser upvoterUser)
        {
            if (targetUser.Id == upvoterUser.Id) throw new Exception($"Sorry, upvoting yourself is not allowed!");

            return await WithWootAsync(targetUser, upvoterUser, (target, upvoter) =>
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
                
                var discount = 0.25 + 0.50 * Math.Min(3, sinceLastBoost.TotalDays) / 3 + 0.25 * Math.Min(7, sinceLastBoost.TotalDays) / 7;
                var scoreDifference = upvoter.Score - target.Score;
                var scoreDiffModifier = Math.Sqrt(1 + Math.Max(-0.75, scoreDifference));

                var efficiency = scoreDiffModifier * discount * randomEff;
                var value = 15 * efficiency;
                target.TotalPoints += value;

                return efficiency;
            });
        }

        public static async Task<ScoreData> GetScoreDataAsync(IGuildUser user)
		{
			if (user == null)
			{
				throw new Exception("GetScoreDataAsync<T> called with a null guild user.");
			}

			return await WithUserAsync(user, u => Task.FromResult(u.ScoreData));
        }

        public static async Task<List<(ulong Key, DateTime LastBoost)>> GetHistoryAsync(IGuildUser user)
        {
            return await WithUserAsync(user, u => Task.FromResult(u.Boosts
                .Where(kv => (DateTime.UtcNow - kv.Value).TotalDays < 7)
                .Select(kv => (kv.Key, TimeLeft: kv.Value))
                .OrderByDescending(boost => boost.TimeLeft)
                .ToList()
            ));
        }

        public static async Task<List<(ulong Key, TimeSpan TimeLeft)>> GetBoostsAsync(IGuildUser user)
        {
            return await WithUserAsync(user, u => Task.FromResult(u.GetBoostsLeft()
                .Select(kv => (kv.Key, TimeLeft: kv.Value))
                .OrderByDescending(boost => boost.TimeLeft)
                .ToList()
            ));
        }

        public static async Task<double> SetScoreAsync(IGuildUser user, double score)
        {
            return await WithUserUpdateAsync(user, u => {
                u.Score = Math.Max(-10, Math.Min(5, score));
                return Task.FromResult((true, u.Score));
            });
        }


        public static async Task<double> SetEnergyAsync(IGuildUser user, double energy)
        {
            return await WithUserUpdateAsync(user, u => {
                u.Energy = Math.Max(0, energy);
                return Task.FromResult((true, u.Energy));
            });
        }

        public static async Task<double> SetInertiaAsync(IGuildUser user, double inertia)
        {
            return await WithUserUpdateAsync(user, u => {
                u.Inertia = Math.Max(0, Math.Min(1, inertia));
                return Task.FromResult((true, u.Inertia));
            });
        }

        public static async Task UpdateUserVisibilityAsync(IGuildUser user, bool showInUsername)
        {
            await WithUserUpdateAsync(user, u => {
                u.ShowInUsername = showInUsername;
                return Task.FromResult((true, true));
            });
        }

        public static async Task<ScoreData> CreditActivityScoreAsync(IGuildUser activityUser)
        {

#if DEBUG
#pragma warning disable CS0162 // Unreachable code detected
            return await GetScoreDataAsync(activityUser);
#endif
            return await WithUserUpdateAsync(activityUser, async user =>
            {
                // null ref somewhere i guess? the fudge

                if (user == null || user.ScoreData == null)
                {
					bool userNull = user == null;
					bool scoredataNull = user.ScoreData == null;

					Console.WriteLine($"user or user.ScoreData is null - this must be looked into later. user null: {userNull}, scoredataNull: {scoredataNull}");
					return (false, await GetScoreDataAsync(activityUser));
				}

                if (user.LastActivity.HasValue
                && DateTimeOffset.UtcNow.Subtract(user.LastActivity.Value).TotalMinutes < 1.5)
                    return (false, user.ScoreData);

                var increase = 3.5 - Math.Max(0.25, Math.Min(3, user.Score));

                user.LastActivity = DateTime.UtcNow;
                user.TotalPoints += increase;
                return (true, user.ScoreData);
            });
        }

        public static async Task UpdateDecays(Func<ulong, ScoreData, Task> callback, Func<ulong, IGuildUser> userFactory)
        {
            await Database.WithAllScoreUsersAsync(userFactory, async scoreUsers =>
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
        private static Random rand = new Random();

        public static IEnumerable<T> LastN<T>(this IList<T> list, int n)
        {
            if (list == null)
            {
                throw new ArgumentNullException("list");
            }

            if (list.Count - n < 0)
            {
                n = list.Count;
            }

            for (var i = list.Count - n; i < list.Count; i++)
            {
                yield return list[i];
            }
        }

        public static double RandomNormal(this Random random, double mean, double stdDev)
        {
            double u1 = 1.0 - rand.NextDouble(); //uniform(0,1] random doubles
            double u2 = 1.0 - rand.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                         Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
            double randNormal =
                         mean + stdDev * randStdNormal; //random normal(mean,stdDev^2)
            return randNormal;
        }
    }
}