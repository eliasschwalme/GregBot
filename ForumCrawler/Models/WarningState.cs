using System;
using System.Linq;

namespace ForumCrawler
{
    public class WarningState
    {
        private static Warning[] DefaultWarnings = new[]
        {
            new Warning { IssueDate = new DateTime(2020, 2, 22, 0, 0, 0) },
            new Warning { IssueDate = new DateTime(2020, 5, 9, 0, 0, 0) }
        };

        public const int WarningsInStrike = 3;
        public const int MuteDaysPerStrike = 7;

        public int Warnings { get; private set; }
        public int Strikes { get; private set; }

        public DateTimeOffset LastTick { get; private set; }
        public DateTimeOffset? MutedUntil => LastTick > DateTimeOffset.UtcNow ? (DateTimeOffset?)LastTick : null;

        public void Add(int amount, DateTimeOffset timestamp)
        {
            Update(timestamp);
            if (amount == 0) return;

            LastTick = new DateTimeOffset(Math.Max(LastTick.Ticks, timestamp.Ticks), TimeSpan.Zero);
            Warnings += amount;

            var gotStrike = false;
            while (Warnings >= WarningsInStrike)
            {
                gotStrike = true;
                Strikes++;
                Warnings -= WarningsInStrike;
            }
            if (gotStrike)
            {
                LastTick = LastTick.AddDays(Strikes * MuteDaysPerStrike);
            }
        }

        public void Update(DateTimeOffset timestamp)
        {
            if (timestamp <= DefaultWarnings[0].IssueDate)
            {
                WarningDecayLogicV1.Update(this, timestamp);
            }
            else if (timestamp <= DefaultWarnings[1].IssueDate)
            {
                WarningDecayLogicV2.Update(this, timestamp);
            } 
            else
            {
                WarningDecayLogicV3.Update(this, timestamp);
            }
        }

        public static WarningState FromDatabase(Warning[] warnings)
        {
            var state = new WarningState();
            foreach (var item in warnings.Where(w => !w.RemoveDate.HasValue).Concat(DefaultWarnings).OrderBy(w => w.IssueDate))
            {
                state.Add(item.Amount, item.IssueDate);
            }
            state.Update(DateTimeOffset.UtcNow);
            return state;
        }

        public static class WarningDecayLogicV1
        {
            public const int WarningExpiryDays = 3;
            public const int StrikeExpiryDays = WarningExpiryDays * WarningsInStrike;

            public static void Update(WarningState state, DateTimeOffset timestamp)
            {
                var duration = new Func<TimeSpan>(() => timestamp - state.LastTick);
                while (state.Warnings > 0 && duration().TotalDays >= WarningExpiryDays)
                {
                    state.LastTick = state.LastTick.AddDays(WarningExpiryDays);
                    state.Warnings--;
                }
                while (state.Strikes > 0 && duration().TotalDays >= StrikeExpiryDays)
                {
                    state.LastTick = state.LastTick.AddDays(StrikeExpiryDays);
                    state.Strikes--;
                }
            }
        }

        public static class WarningDecayLogicV2
        {
            public const int WarningExpiryDays = 9;
            public const int StrikeExpiryDays = WarningExpiryDays * WarningsInStrike;

            public static void Update(WarningState state, DateTimeOffset timestamp)
            {
                var duration = new Func<TimeSpan>(() => timestamp - state.LastTick);
                while (state.Warnings > 0 && duration().TotalDays >= WarningExpiryDays)
                {
                    state.LastTick = state.LastTick.AddDays(WarningExpiryDays);
                    state.Warnings--;
                }
                while (state.Strikes > 0 && duration().TotalDays >= StrikeExpiryDays)
                {
                    state.LastTick = state.LastTick.AddDays(StrikeExpiryDays);
                    state.Strikes--;
                }
            }
        }

        public static class WarningDecayLogicV3
        {
            private const int WarningExpiryDays = 9;
            private const int WarningExpiryDaysWithStrike = StrikeExpiryDays; // 14
            public const int StrikeExpiryDays = 14;
            public const int ProbationDays = WarningExpiryDays * 2; // 18

            public static int GetWarningExpiryDays(WarningState state)
            {
                return state.Strikes > 0 ? WarningExpiryDaysWithStrike : WarningExpiryDays;
            }

            public static void UpdateWarnings(WarningState state, Func<TimeSpan> duration)
            {
                while (state.Warnings > 0 && duration().TotalDays >= GetWarningExpiryDays(state))
                {
                    state.LastTick = state.LastTick.AddDays(WarningExpiryDays);
                    state.Warnings--;
                }
            }
            public static void UpdateStrikes(WarningState state, Func<TimeSpan> duration)
            {
                while (state.Strikes > 0 && duration().TotalDays >= StrikeExpiryDays)
                {
                    state.LastTick = state.LastTick.AddDays(StrikeExpiryDays + ProbationDays);
                    state.Warnings++;
                    state.Strikes--;
                }
            }

            public static void Update(WarningState state, DateTimeOffset timestamp)
            {
                var duration = new Func<TimeSpan>(() => timestamp - state.LastTick);
                UpdateWarnings(state, duration);
                UpdateStrikes(state, duration);
                UpdateWarnings(state, duration);
            }
        }
    }
}