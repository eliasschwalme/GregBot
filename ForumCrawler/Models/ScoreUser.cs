using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Linq;

namespace ForumCrawler
{
    public class ScoreUser
    {
        private const double ScorePoint_Multiplier = 0.0015;
        private const double ReserveRatio_Multiplier = 0.01;

        [EditorBrowsable(EditorBrowsableState.Never),
            DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long Id { get; set; }

        [NotMapped]
        public ulong UserId
        {
            get => (ulong)Id;
            set => Id = (long)value;
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
            get => JsonConvert.SerializeObject(Boosts);
            set => Boosts = JsonConvert.DeserializeObject<Dictionary<ulong, DateTime>>(value ?? "{}");
        }

        [NotMapped]
        public int BonusEnergy
        {
            get
            {
                var boostLevel = GetBoostLevel();
                return (IsPremium ? 250 : 0) +
                    (boostLevel >= 3
                    ? 250
                    : boostLevel >= 2
                    ? 100
                    : boostLevel >= 1
                    ? 25
                    : 0);
            }
        }

        [NotMapped]
        public int MaxEnergy => 250 + BonusEnergy;

        [NotMapped]
        public double ScorePoints
        {
            get => ToScorePoints(Score);
            set => Score = ToScore(value);
        }

        [NotMapped]
        public double TotalPoints
        {
            get => ScorePoints + ReservePoints;
            set => AddTotalPoints(value - TotalPoints);
        }

        [NotMapped]
        public double Inertia
        {
            get => 1 - Math.Exp(-ReserveRatio_Multiplier * ReservePoints); // 1 - e ^(-0.01x))
            set => ReservePoints = Math.Log(1 - value) / -ReserveRatio_Multiplier; // ln(1-x)/-0.01
        }

        [NotMapped]
        public double BonusScore
        {
            get
            {
                var boostLevel = GetBoostLevel();
                return (IsPremium ? 1 : 0) +
                    (boostLevel >= 3
                    ? 1.0
                    : boostLevel >= 2
                    ? 0.4
                    : boostLevel >= 1
                    ? 0.1
                    : 0);
            }
        }

        [NotMapped]
        public double ScoreAfterBoost => Score + BonusScore;

        [NotMapped]
        public ScoreData ScoreData
        {
            get
            {
                return new ScoreData
                {
                    Score = Score,
                    BoostLevel = GetBoostLevel(),
                    BonusScore = BonusScore,
                    BonusEnergy = BonusEnergy
                };
            }
        }

        [NotMapped]
        public string LongScoreString => string.Format(CultureInfo.InvariantCulture, "{0:F3}", ScoreAfterBoost);

        [NotMapped]
        public TimeSpan NextEnergy => TimeSpan.FromSeconds(288 * (1 - (Energy % 1)));

        [NotMapped]
        public bool IsPremium { get; set; }

        public int GetBoostLevel()
        {
            var boosts = GetBoostsLeft().Count;
            return boosts >= 6
                ? 3
                : boosts >= 3
                ? 2
                : boosts >= 1
                ? 1
                : 0;
        }

        public Dictionary<ulong, TimeSpan> GetBoostsLeft()
        {
            return Boosts
                .Select(kv => new KeyValuePair<ulong, TimeSpan>(kv.Key, TimeSpan.FromDays(2) - (DateTime.UtcNow - kv.Value)))
                .Where(boost => boost.Value.TotalSeconds > 0)
                .ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        public TimeSpan GetBoostLeft(ulong userId)
        {
            var boostDate = GetLastBoost(userId);
            return TimeSpan.FromDays(2) - (DateTime.UtcNow - boostDate);
        }

        public DateTime GetLastBoost(ulong userId)
        {
            Boosts.TryGetValue(userId, out var boostDate);
            return boostDate;
        }

        public static double ToScorePoints(double score) => Math.Log((5 - score) / 5) / -ScorePoint_Multiplier; // ln((5 - x) / 5) / -0.0015

        public static double ToScore(double scorePoints) => 5 - (5 * Math.Exp(-ScorePoint_Multiplier * scorePoints)); // 5 - 5 * e^(-0.0015x)

        private void AddTotalPoints(double value)
        {
            if (value > 0)
            {
                var p = 0.50 + (0.50 * Inertia);
                var q = 1 - p;
                ReservePoints += q * value;
                ScorePoints += p * value;
            }
            else
            {
                var consumed = Math.Max(-ReservePoints, value);
                ReservePoints += consumed;
                ScorePoints += value - consumed;
            }
        }

        public void Update(DiscordSocketClient client, ulong userId)
        {
            var guild = client.GetGuild(DiscordSettings.GuildId);
            var guildUser = guild.GetUser(userId);
            if (guildUser != null)
            {
                IsPremium = guildUser.PremiumSince.HasValue;
            }

            UpdateDecay();
            UpdateEnergy();
        }

        private void UpdateEnergy()
        {
            var time = DateTime.UtcNow;
            var timeSinceLastEnergy = time - (LastEnergy ?? default);
            var energyGenerated = timeSinceLastEnergy.TotalSeconds / 288;
            Energy = Math.Min(MaxEnergy, Energy + energyGenerated);
            LastEnergy = time;
            if (energyGenerated < 0)
            {
                Console.WriteLine($"{Energy}, {energyGenerated}");
            }
        }

        private void UpdateDecay()
        {
            var lastDecay = LastDecay ?? DateTime.UtcNow;
            var lastActivity = LastActivity ?? default;
            var duration = DateTime.UtcNow - lastDecay;

            if (Score >= 1.0)
            {
                TotalPoints -= duration.TotalHours * ((Math.Pow(Score, 2) * 0.02) + (Math.Pow(Score, 6) * 0.00002));

                var activityDays = (DateTime.UtcNow - lastActivity).TotalDays;
                var durationDays = duration.TotalDays;
                var inactivityTotal = Math.Max(0, activityDays * (activityDays - 1) / 2);
                var inactivitySubstracted = Math.Max(0, (activityDays - durationDays) * (activityDays - durationDays - 1) / 2);
                TotalPoints -= (inactivityTotal - inactivitySubstracted) * 5;

                if (Score < 1.0) Score = 1;
            }
            else
            {
                Score += duration.TotalHours * 0.02;
            }

            LastDecay = DateTime.UtcNow;
        }
    }

    public class ScoreData
    {
        public double Score { get; set; } = 1;
        public double BoostLevel { get; set; }
        public double BonusScore { get; set; }
        public int BonusEnergy { get; set; }
        public double ScoreAfterBoost => Score + BonusScore;

        public string Class => ToRoman((int)Math.Truncate(ScoreLevel));

        public double ScoreLevel => Math.Truncate(Math.Round(ScoreAfterBoost * 1000) / 100) / 10;

        public double BaseScoreLevel => Math.Truncate(Math.Round(Score * 1000) / 100) / 10;

        public string ShortBaseScoreString => string.Format(CultureInfo.InvariantCulture, "{0:F1}", BaseScoreLevel);

        public string ShortScoreString => string.Format(CultureInfo.InvariantCulture, "{0:F1}", ScoreLevel);

        public static string ToRoman(int number)
        {
            if ((number < 0) || (number > 3999)) throw new ArgumentOutOfRangeException("insert value between 0 and 3999");
            if (number == 0) return "0";
            return ToRomanInner(number);
        }

        private static string ToRomanInner(int number)
        {
            if (number == 0) return "";
            if (number >= 1000) return "M" + ToRomanInner(number - 1000);
            if (number >= 900) return "CM" + ToRomanInner(number - 900);
            if (number >= 500) return "D" + ToRomanInner(number - 500);
            if (number >= 400) return "CD" + ToRomanInner(number - 400);
            if (number >= 100) return "C" + ToRomanInner(number - 100);
            if (number >= 90) return "XC" + ToRomanInner(number - 90);
            if (number >= 50) return "L" + ToRomanInner(number - 50);
            if (number >= 40) return "XL" + ToRomanInner(number - 40);
            if (number >= 10) return "X" + ToRomanInner(number - 10);
            if (number >= 9) return "IX" + ToRomanInner(number - 9);
            if (number >= 5) return "V" + ToRomanInner(number - 5);
            if (number >= 4) return "IV" + ToRomanInner(number - 4);
            if (number >= 1) return "I" + ToRomanInner(number - 1);
            throw new ArgumentOutOfRangeException("Could not convert to roman");
        }
    }
}