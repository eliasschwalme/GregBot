using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ForumCrawler
{
    public class WarningState
    {
        public const int WarningsInStrike = 3;
        public const int MuteDaysPerStrike = 7;

        public int Warnings { get; private set; }
        public int Strikes { get; private set; }

        public DateTimeOffset LastTick { get; private set; }
        public DateTimeOffset? MutedUntil => this.LastTick > DateTimeOffset.UtcNow ? (DateTimeOffset?)this.LastTick : null;


        public void Add(int amount, DateTimeOffset timestamp)
        {
            this.Update(timestamp);
            if (amount == 0) return;

            this.LastTick = new DateTimeOffset(Math.Max(this.LastTick.Ticks, timestamp.Ticks), TimeSpan.Zero);
            this.Warnings += amount;

            var gotStrike = false;
            while (this.Warnings >= WarningsInStrike)
            {
                gotStrike = true;
                this.Strikes++;
                this.Warnings -= WarningsInStrike;
            }
            if (gotStrike)
            {
                this.LastTick = this.LastTick.AddDays(this.Strikes * MuteDaysPerStrike);
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
        public class WarningDelayLogicV1
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

        public class WarningDelayLogicV2
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
