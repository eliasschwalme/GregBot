using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace ForumCrawler
{
    public class Mute
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long Id { get; set; }

        [NotMapped]
        public ulong UserId
        {
            get => (ulong)Id;
            set => Id = (long)value;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long IssuerUId { get; set; }

        [NotMapped]
        public ulong IssuerId
        {
            get => (ulong)IssuerUId;
            set => IssuerUId = (long)value;
        }

        public DateTime ExpiryDate { get; set; }
        public DateTime IssueDate { get; set; }
    }
}