using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace ForumCrawler
{
    public class GovernanceVote
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
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
            get => (ulong)UId;
            set => UId = (long)value;
        }

        [NotMapped]
        public ulong ChannelId
        {
            get => (ulong)Id;
            set => Id = (long)value;
        }

        [NotMapped]
        public ulong MessageId
        {
            get => (ulong)MId;
            set => MId = (long)value;
        }

        [NotMapped]
        public ulong VoteBillboardId
        {
            get => (ulong)VId;
            set => VId = (long)value;
        }
    }
}