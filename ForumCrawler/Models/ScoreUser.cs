using Discord;
using Discord.WebSocket;
using DiscordSocialScore;
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

        private static readonly Random random = new Random();

        private const double ScorePoint_Multiplier = 0.0015;
        private const double InertiaPoint_Multiplier = 0.01;
        private const double Score_Epsilon = 0.1;
        private const double Inertia_Epsilon = 0.1;
        private const double Max_Score = 5;
        private const double Max_Inertia = 1;

        private const double ActivityResolutionInHours = 60.0 / 3600; // 60 seconds

        private const double InertiaPointsCapacity = 239.78952728; // ln(1 - Max_Inertia / 1.1) / -0.01
        private const double InertiaPointsPerActivityHour = InertiaPointsCapacity / 24; // 24 hours of nonstop talking with no decay fills up inertia to 100%
        private const double InertiaPointsPerActivity = InertiaPointsPerActivityHour * ActivityResolutionInHours;

        private const int Level5Decay = 32; // Math.Pow(2, 5)
        private const double InertiaPerActivityHour = InertiaPointsPerActivityHour * InertiaPoint_Multiplier * (Max_Inertia + Inertia_Epsilon / 2); // Average the effect of the epsilon to account for the slowing slope
        private const double InertiaDecayRatePerHour = 1.5 * InertiaPerActivityHour / 24 / Level5Decay; // 1.5h time investment / day required for a lvl 5 person to not decay

        private const double ScoreBaseDecayRate = 0.001;
        private const double ScoreInactvitiyDecayRate = 0.0001;

        [EditorBrowsable(EditorBrowsableState.Never),
            DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long Id { get; set; }

        [NotMapped]
        public ulong UserId
        {
            get => (ulong)Id;
            set => Id = (long)value;
        }

        public DateTime? LastEnergy { get; set; }
        public DateTime? LastActivity { get; set; }
        public DateTime? LastDecay { get; set; }
        public double Energy { get; set; }
        public double Inertia { get; set; }

        [Index]
        public double Score { get; set; } = 1;

        [NotMapped]
        public Dictionary<ulong, DateTime> Boosts { get; private set; } = new Dictionary<ulong, DateTime>();

        public string BoostsAsString
        {
            get => JsonConvert.SerializeObject(Boosts);
            set => Boosts = JsonConvert.DeserializeObject<Dictionary<ulong, DateTime>>(value ?? "{}");
        }

        [NotMapped]
        public Dictionary<ulong, DateTime> DownBoosts { get; private set; } = new Dictionary<ulong, DateTime>();

        public string DownBoostsAsString
        {
            get => JsonConvert.SerializeObject(DownBoosts);
            set => DownBoosts = JsonConvert.DeserializeObject<Dictionary<ulong, DateTime>>(value ?? "{}");
        }

        [NotMapped]
        private int BonusEnergy
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
        private double ScorePoints
        {
            get => ToPoints(Score, Max_Score, Score_Epsilon, ScorePoint_Multiplier); // ln((1 - x / 5.1) / -0.0015
            set => Score = ToValue(value, Max_Score, Score_Epsilon, ScorePoint_Multiplier); // 5.1 * (1 - e^(-0.0015x))
        }

        [NotMapped]
        public double InertiaPoints
        {
            get => ToPoints(Inertia, Max_Inertia, Inertia_Epsilon, InertiaPoint_Multiplier); // ln(1 - x / 1.1) / -0.01
            set => Inertia = ToValue(value, Max_Inertia, Inertia_Epsilon, InertiaPoint_Multiplier); // 1.1 * (1 - e ^(-0.01x)))
        }

        private static double ToPoints(double value, double max, double epsilon, double multiplier) => Math.Log(1 - value / (max + epsilon)) / -multiplier; 
        private static double ToValue(double points, double max, double epsilon, double multiplier) => Math.Max(0, Math.Min(max, (max + epsilon) * (1 - Math.Exp(-multiplier * points)))); 



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
        public TimeSpan NextEnergy => TimeSpan.FromSeconds(288 * (1 - (Energy % 1)));

        [NotMapped]
        public bool IsPremium { get; private set; }

        private int GetBoostLevel()
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

        public DateTime GetLastVoteTimestamp(ulong userId)
        {
            Boosts.TryGetValue(userId, out var boostDate);
            DownBoosts.TryGetValue(userId, out var downVoteDate);
            return boostDate > downVoteDate ? boostDate : downVoteDate;
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

        private double SumInRange(double minEcl, double maxInc)
        {
           return (maxInc * (maxInc + 1) - minEcl * (minEcl + 1)) / 2;
        }

        private void UpdateDecay()
        {
            var lastDecay = LastDecay ?? DateTime.UtcNow;
            var lastActivity = LastActivity ?? default;
            var ticks = (DateTime.UtcNow - lastDecay).TotalHours;
            var lastActivityTicks = (DateTime.UtcNow - lastActivity).TotalHours;

            var inertiaDecayRate = InertiaDecayRatePerHour * Math.Pow(2, this.Score);
            var inertiaDecay = inertiaDecayRate * ticks;
            var remainderTicks = Math.Max(0, inertiaDecay - this.Inertia) / inertiaDecayRate;
            this.Inertia -= inertiaDecay;


            if (Score > 1.0)
            { 
                this.Score -= SumInRange(lastActivityTicks - remainderTicks, lastActivityTicks) * ScoreInactvitiyDecayRate 
                    + ScoreBaseDecayRate * remainderTicks;
                if (Score < 1.0) Score = 1;
            }

            LastDecay = DateTime.UtcNow;
        }

        private bool TickActivity()
        {
            var resolution = TimeSpan.FromHours(ActivityResolutionInHours);
            var delta = DateTime.UtcNow.Subtract(this.LastActivity ?? DateTime.UtcNow.Subtract(resolution + resolution));
            if (delta < resolution)
            {
                return false;
            } 
            else if (delta < resolution + resolution)
            {
                this.LastActivity += resolution;
            } 
            else
            {
                this.LastActivity = DateTime.UtcNow;
            }

            return true;
        }

        public bool CreditActivity()
        {
            if (this.TickActivity())
            {
                this.InertiaPoints += InertiaPointsPerActivity;
                return true;
            }
            return false;
        }

        public double Upvote(ScoreUser target)
        {
            if (Math.Abs(target.Score - this.Score) > 2) throw new Exception("The score difference between upvoters cannot be over 2.0.");

            var efficiency = GetEfficiency(target);
            if (this.Energy < 25) throw new Exception($"An upvote costs 25 energy! You currently have __**{Math.Floor(this.Energy)}**__/{this.MaxEnergy} energy.");
            this.Energy -= 25;

            var lowScoreFactor = target.Score < 2 ? 3 : 1;
            target.ScorePoints += 5 * efficiency * lowScoreFactor;

            return efficiency;
        }

        public double Downvote(ScoreUser target)
        {
            var efficiency = GetEfficiency(target);
            if (this.Energy < 25) throw new Exception($"A downvote costs 50 energy! You currently have __**{Math.Floor(this.Energy)}**__/{this.MaxEnergy} energy.");
            this.Energy -= 50;
            target.ScorePoints -= 5 * efficiency;

            return efficiency;
        }

        private double GetEfficiency(ScoreUser target)
        {
            if (target.UserId == UserId) throw new Exception($"Sorry, voting yourself is not allowed!");
            if (this.Score < 1.0995) throw new Exception("Users under 1.1 cannot not send votes.");

            var lastBoost = target.GetLastVoteTimestamp(this.UserId);
            var sinceLastVote = DateTime.UtcNow - lastBoost;
            var cooldown = TimeSpan.FromDays(0.5) - sinceLastVote;
            if (cooldown.TotalSeconds > 0) throw new Exception($"Please wait {cooldown.ToHumanReadableString()} before voting this person again.");

            var randomEff = Math.Max(0.75, Math.Min(5, random.RandomNormal(1, 0.4)));
            var discountFactor = Math.Min(2, sinceLastVote.TotalDays) / 2;
            var scoreDifference = this.Score - target.Score;
            var scoreDiffModifier = 1 + Math.Max(-0.75, scoreDifference / 2);

            var efficiency = scoreDiffModifier * discountFactor * randomEff;
            return efficiency;
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