using Discord;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ForumCrawler
{
    public class ScoreUser
    {
        const double ScorePoint_Multiplier = 0.0015;
        const double ReserveRatio_Multiplier = 0.01;

        [EditorBrowsable(EditorBrowsableState.Never),
            DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long Id { get; set; }

        [NotMapped]
        public ulong UserId
        {
            get { return (ulong)this.Id; }
            set { this.Id = (long)value; }
        }

        public bool EarlyUserExempt { get; set; }

        public DateTime? LastEnergy { get; set; }
        public DateTime? LastActivity { get; set; }
        public DateTime? LastDecay { get; set; }
        public double Energy { get; set; }
        public double ReservePoints { get; set; }
        public bool ShowInUsername { get; set; }

        [Index]
        public double Score { get; set; } = 1;
        public Dictionary<ulong, DateTime> Boosts { get; set; } = new Dictionary<ulong, DateTime>();
            
        public string BoostsAsString
        {
            get { return JsonConvert.SerializeObject(this.Boosts); }
            set { this.Boosts = JsonConvert.DeserializeObject<Dictionary<ulong, DateTime>>(value ?? "{}"); }
        }

        [NotMapped]
        public int BonusEnergy
        {
            get
            {
                var boostLevel = this.GetBoostLevel();
                return boostLevel >= 3
                    ? 250
                    : boostLevel >= 2
                    ? 100
                    : boostLevel >= 1
                    ? 25
                    : 0;
            }
        }

        [NotMapped]
        public int MaxEnergy
        {
            get
            {
                return 250 + this.BonusEnergy;
            }
        }

        [NotMapped]
        public double ScorePoints
        {
            get
            {
                return ToScorePoints(this.Score);
            }
            set
            {
                this.Score = ToScore(value);
            }
        }

        [NotMapped]
        public double TotalPoints
        {
            get
            {
                return this.ScorePoints + ReservePoints;
            }
            set
            {
                this.AddTotalPoints(value - this.TotalPoints);
            }
        }

        [NotMapped]
        public double Inertia
        {
            get
            {
                return 1 - Math.Exp(-ReserveRatio_Multiplier * this.ReservePoints); // 1 - e ^(-0.01x))
            }
            set
            {
                this.ReservePoints = Math.Log(1 - value) / -ReserveRatio_Multiplier; // ln(1-x)/-0.01
            }
        }

        [NotMapped]
        public double BonusScore
        {
            get
            {

                var boostLevel = this.GetBoostLevel();
                return boostLevel >= 3
                    ? 1.0
                    : boostLevel >= 2
                    ? 0.4
                    : boostLevel >= 1
                    ? 0.1
                    : 0d;
            }
        }

        [NotMapped]
        public double ScoreAfterBoost
        {
            get
            {
                return this.Score + this.BonusScore;
            }
        }

        [NotMapped]
        public ScoreData ScoreData
        {
            get
            {
                return new ScoreData
                {
                    Score = this.Score,
                    BoostLevel = this.GetBoostLevel(),
                    BonusScore = this.BonusScore,
                    BonusEnergy = this.BonusEnergy,
                    ShowInUsername = this.ShowInUsername
                };
            }
        }

        [NotMapped]
        public string LongScoreString
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture, "{0:F3}", this.ScoreAfterBoost);
            }
        }

        [NotMapped]
        public TimeSpan NextEnergy
        {
            get
            {
                return TimeSpan.FromSeconds(288 * (1 - this.Energy % 1));
            }
        }

        [NotMapped]
        public bool IsPremium { get; set; }

        public int GetBoostLevel()
        {
            if (this.IsPremium) return 3;

            var boosts = this.GetBoostsLeft().Count;
            return boosts >= 10
                ? 3
                : boosts >= 5
                ? 2
                : boosts >= 2
                ? 1
                : 0;
        }

        public Dictionary<ulong, TimeSpan> GetBoostsLeft()
        {
            return this.Boosts
                .Select(kv => new KeyValuePair<ulong, TimeSpan>(kv.Key, TimeSpan.FromDays(1) - (DateTime.UtcNow - kv.Value)))
                .Where(boost => boost.Value.TotalSeconds > 0)
                .ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        public TimeSpan GetBoostLeft(ulong userId)
        {
            var boostDate = this.GetLastBoost(userId);
            return TimeSpan.FromDays(1) - (DateTime.UtcNow - boostDate);
        }

        public DateTime GetLastBoost(ulong userId)
        {
            this.Boosts.TryGetValue(userId, out var boostDate);
            return boostDate;
        }

        public static double ToScorePoints(double score)
        {
            return Math.Log((5 - score) / 5) / -ScorePoint_Multiplier; // ln((5 - x) / 5) / -0.0015
        }


        public static double ToScore(double scorePoints)
        {
            return 5 - 5 * Math.Exp(-ScorePoint_Multiplier * scorePoints); // 5 - 5 * e^(-0.0015x)
        }

        private void AddTotalPoints(double value)
        {
            if (value > 0)
            {
                var p = 0.50 + 0.50 * this.Inertia;
                var q = 1 - p;
                this.ReservePoints += q * value;
                this.ScorePoints += p * value;
            }
            else
            {
                var consumed = Math.Max(-this.ReservePoints, value);
                this.ReservePoints += consumed;
                this.ScorePoints += value - consumed;
            }
        }

        public void Update(IGuildUser user)
        {
            this.IsPremium = user.PremiumSince.HasValue;
            this.UpdateDecay();
            this.UpdateEnergy();
        }

        private void UpdateEnergy()
        {
            var time = DateTime.UtcNow;
            var timeSinceLastEnergy = time - (LastEnergy ?? default(DateTime));
            var energyGenerated = timeSinceLastEnergy.TotalSeconds / 288;
            this.Energy = Math.Min(this.MaxEnergy, this.Energy + energyGenerated);
            this.LastEnergy = time;
            if (energyGenerated < 0)
            {
                Console.WriteLine($"{this.Energy}, {energyGenerated}");
            }
        }

        private void UpdateDecay()
        {
            var lastDecay = this.LastDecay ?? DateTime.UtcNow;
            var lastActivity = this.LastActivity ?? default(DateTime);
            var duration = DateTime.UtcNow - lastDecay;

            if (this.Score >= 1.0)
            {
                this.TotalPoints -= duration.TotalHours * (Math.Pow(this.Score, 2) * 0.02 + Math.Pow(this.Score, 6) * 0.00002);

                var activityDays = (DateTime.UtcNow - lastActivity).TotalDays;
                var durationDays = duration.TotalDays;
                var inactivityTotal = Math.Max(0, activityDays * (activityDays - 1) / 2);
                var inactivitySubstracted = Math.Max(0, (activityDays - durationDays) * (activityDays - durationDays - 1) / 2);
                this.TotalPoints -= (inactivityTotal - inactivitySubstracted) * 5;

                if (this.Score < 1.0) this.Score = 1;
            }
            else
            {
                this.Score += duration.TotalHours * 0.02;
            }

            this.LastDecay = DateTime.UtcNow;
        }
    }

    public class ScoreData
    {
        public double Score { get; set; }
        public double BoostLevel { get; set; }
        public double BonusScore { get; set; }
        public int BonusEnergy { get; set; }
        public bool ShowInUsername { get; set; }
        public double ScoreAfterBoost => this.Score + this.BonusScore;

        public string Class {
            get
            {
                return ToRoman((int)Math.Truncate(this.ScoreLevel));
            }
        }

        public double ScoreLevel
        {
            get
            {
                return Math.Truncate(Math.Round(this.ScoreAfterBoost * 1000) / 100) / 10;
            }
        }

        public string ShortScoreString
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture, "{0:F1}", this.ScoreLevel);
            }
        }



        public static string ToRoman(int number)
        {
            if ((number < 0) || (number > 3999)) throw new ArgumentOutOfRangeException("insert value betwheen 1 and 3999");
            if (number < 1) return string.Empty;
            if (number >= 1000) return "M" + ToRoman(number - 1000);
            if (number >= 900) return "CM" + ToRoman(number - 900);
            if (number >= 500) return "D" + ToRoman(number - 500);
            if (number >= 400) return "CD" + ToRoman(number - 400);
            if (number >= 100) return "C" + ToRoman(number - 100);
            if (number >= 90) return "XC" + ToRoman(number - 90);
            if (number >= 50) return "L" + ToRoman(number - 50);
            if (number >= 40) return "XL" + ToRoman(number - 40);
            if (number >= 10) return "X" + ToRoman(number - 10);
            if (number >= 9) return "IX" + ToRoman(number - 9);
            if (number >= 5) return "V" + ToRoman(number - 5);
            if (number >= 4) return "IV" + ToRoman(number - 4);
            if (number >= 1) return "I" + ToRoman(number - 1);
            throw new ArgumentOutOfRangeException("Could not convert to roman");
        }
    }
}
