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