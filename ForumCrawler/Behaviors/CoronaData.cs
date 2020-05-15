using System.Collections.Generic;
using System.Linq;

namespace ForumCrawler
{
    internal class CoronaData : ICoronaEntry
    {
        public enum CoronaEntryType
        {
            Country,
            Other
        }

        public Dictionary<string, CoronaEntry> Entries { get; set; } = new Dictionary<string, CoronaEntry>();
        public int Mild => Active - Serious;

        public int RegionsActive => Entries.Values.Count(e => e.Active > 0);

        public double DeathRate => (double)Deaths / (Recovered + Deaths);

        public int Cases => Entries.Values.Sum(e => e.Cases);
        public int CaseIncrease => Entries.Values.Sum(e => e.CaseIncrease);
        public int Active => Entries.Values.Sum(e => e.Active);
        public int Deaths => Entries.Values.Sum(e => e.Deaths);
        public int Recovered => Entries.Values.Sum(e => e.Recovered);
        public int Serious => Entries.Values.Sum(e => e.Serious);

        public class CoronaEntry : ICoronaEntry
        {
            public CoronaEntryType Type { get; set; }
            public string Name { get; set; }
            public int Cases { get; set; }
            public int CaseIncrease { get; set; }
            public int Deaths { get; set; }
            public int Recovered { get; set; }
            public int Serious { get; set; }
            public int Active => Cases - Deaths - Recovered;
        }
    }
}