using System.ComponentModel;

namespace ForumCrawler
{
    public class StarReaction
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
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
        /// The user who has reacted to the message.
        /// </summary>
        public long StargazerId { get; set; }
    }
}