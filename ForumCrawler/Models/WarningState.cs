using System;
using System.Linq;

namespace ForumCrawler
{
    public class WarningState
    {
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
            if (timestamp < new DateTimeOffset(2020, 2, 22, 0, 0, 0, TimeSpan.Zero))
            {
                WarningDelayLogicV1.Update(this, timestamp);
            }
            else
            {
                WarningDelayLogicV2.Update(this, timestamp);
            }
        }

        public static WarningState FromDatabase(Warning[] warnings)
        {
            var state = new WarningState();
            foreach (var item in warnings.Where(w => !w.RemoveDate.HasValue).OrderBy(w => w.IssueDate))
            {
                state.Add(item.Amount, item.IssueDate);
            }
            state.Update(DateTimeOffset.UtcNow);
            return state;
        }

        public static class WarningDelayLogicV1
        {
            public const int WarningExpiryDays = 3;
            public const int StrikeExpiryDays = WarningExpiryDays * WarningsInStrike;

            public static void Update(WarningState state, DateTimeOffset timestamp)
            {
                var duration = new Func<TimeSpan>(() => timestamp - state.LastTick);
                while (state.Warnings > 0 && duration().TotalDays >= WarningExpiryDays)
                {
                    state.LastTick.AddDays(WarningExpiryDays);
                    state.Warnings--;
                }
                while (state.Strikes > 0 && duration().TotalDays >= StrikeExpiryDays)
                {
                    state.LastTick.AddDays(StrikeExpiryDays);
                    state.Strikes--;
                }
            }
        }

        public static class WarningDelayLogicV2
        {
            public const int WarningExpiryDays = 9;
            public const int StrikeExpiryDays = WarningExpiryDays * WarningsInStrike;

            public static void Update(WarningState state, DateTimeOffset timestamp)
            {
                var duration = new Func<TimeSpan>(() => timestamp - state.LastTick);
                while (state.Warnings > 0 && duration().TotalDays >= WarningExpiryDays)
                {
                    state.LastTick.AddDays(WarningExpiryDays);
                    state.Warnings--;
                }
                while (state.Strikes > 0 && duration().TotalDays >= StrikeExpiryDays)
                {
                    state.LastTick.AddDays(StrikeExpiryDays);
                    state.Strikes--;
                }
            }
        }
    }
}