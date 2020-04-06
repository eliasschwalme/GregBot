using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ForumCrawler.Models
{
	public class RevisedStarboardPost
	{
        public int Id { get; set; }

        /// <summary>
        /// Used to determine which starboard channel this starboard reaction is
        /// related to.
        /// </summary>
        public long StarboardChannelId { get; set; }

        /// <summary>
        /// The message that this reaction applies to.
        /// </summary>
        public long MessageId { get; set; }

        /// <summary>
        /// The Id of this posted message in starboard.
        /// </summary>
        public long StarboardMessageId { get; set; }

        /// <summary>
        /// Determines if staff have modified the post to explicitly be shown or
        /// not on the starboard.
        /// </summary>
        public bool StaffToggledVisibility { get; set; }
    }
}
