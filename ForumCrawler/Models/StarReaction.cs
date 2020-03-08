using System.ComponentModel;

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