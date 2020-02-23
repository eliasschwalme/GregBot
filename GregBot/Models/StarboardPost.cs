using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ForumCrawler
{
    public class StarboardPost
    {
		public int Id { get; set; }

        public long MessageId { get; set; }

        public bool OnStarboard { get; set; }

        public long StarboardMessageId { get; set; }
    }
}
