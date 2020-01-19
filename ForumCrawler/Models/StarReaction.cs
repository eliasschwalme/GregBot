using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ForumCrawler
{
	public class StarReaction
	{
		[EditorBrowsable(EditorBrowsableState.Never)]
        public int Id { get; set; }

		public long MessageId { get; set; }

		public long StargazerId { get; set; }

        public bool HasReaction { get; set; }
	}
}
