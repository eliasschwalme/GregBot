using System;

namespace ForumCrawler
{
    internal class CoronaLiveData
    {
        public DateTime LastUpdate { get; set; }
        public DateTime LastReset { get; set; }
        public CoronaData Now { get; set; }
        public CoronaData Today { get; set; }
        public CoronaData Yesterday { get; set; }
    }
}