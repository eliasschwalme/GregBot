using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace ForumCrawler
{
    public class Message
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long Id { get; set; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public long UId { get; set; }

        [NotMapped]
        public ulong MessageId
        {
            get => (ulong)Id;
            set => Id = (long)value;
        }

        [NotMapped]
        public ulong UserId
        {
            get => (ulong)UId;
            set => UId = (long)value;
        }

        public string Content { get; set; }

        public string Attachment { get; set; }
    }
}