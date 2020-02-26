using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace ForumCrawler
{
    public class GovernanceVote
    {
        [EditorBrowsable(EditorBrowsableState.Never),
            DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long Id { get; set; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public long UId { get; set; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public long MId { get; set; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public long VId { get; set; }

        [NotMapped]
        public ulong UserId
        {
            get { return (ulong)this.UId; }
            set { this.UId = (long)value; }
        }

        [NotMapped]
        public ulong ChannelId
        {
            get { return (ulong)this.Id; }
            set { this.Id = (long)value; }
        }

        [NotMapped]
        public ulong MessageId
        {
            get { return (ulong)this.MId; }
            set { this.MId = (long)value; }
        }

        [NotMapped]
        public ulong VoteBillboardId
        {
            get { return (ulong)this.VId; }
            set { this.VId = (long)value; }
        }
    }
}