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
        public const int WarningExpiryDays = 3;
        public const int StrikeExpiryDays = WarningExpiryDays * WarningsInStrike;
        public const int MuteDaysPerStrike = 2;

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
            var duration = new Func<TimeSpan>(() => timestamp - this.LastTick);
            while (this.Warnings > 0 && duration().TotalDays >= WarningExpiryDays)
            {
                this.LastTick.AddDays(WarningExpiryDays);
                this.Warnings--;
            }
            while (this.Strikes > 0 && duration().TotalDays >= StrikeExpiryDays)
            {
                this.LastTick.AddDays(StrikeExpiryDays);
                this.Strikes--;
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
    }
}
