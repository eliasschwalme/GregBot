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

        public DateTime LastTick { get; private set; }
        public DateTime? MutedUntil => this.LastTick > DateTime.UtcNow ? (DateTime?)this.LastTick : null;


        public void Add(int amount, DateTime timestamp)
        {
            this.Update(timestamp);
            if (amount == 0) return;

            this.LastTick = new DateTime(Math.Max(this.LastTick.Ticks, timestamp.Ticks));
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

        public void Update(DateTime timestamp)
        {
            if (timestamp < new DateTime(2020, 2, 22, 0, 0, 0))
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
            state.Update(DateTime.UtcNow);
            return state;
        }
        public class WarningDelayLogicV1
        {
            public const int WarningExpiryDays = 3;
            public const int StrikeExpiryDays = WarningExpiryDays * WarningsInStrike;

            public static void Update(WarningState state, DateTime timestamp)
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

            public static void Update(WarningState state, DateTime timestamp)
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
