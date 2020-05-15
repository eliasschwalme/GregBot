using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace ForumCrawler
{
    public class Warning
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Index]
        public long UId { get; set; }

        [NotMapped]
        public ulong UserId
        {
            get => (ulong)UId;
            set => UId = (long)value;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long MId { get; set; }

        [NotMapped]
        public ulong MessageId
        {
            get => (ulong)MId;
            set => MId = (long)value;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long CId { get; set; }

        [NotMapped]
        public ulong ChannelId
        {
            get => (ulong)CId;
            set => CId = (long)value;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long? RemoverUId { get; set; }

        [NotMapped]
        public ulong? RemoverId
        {
            get => (ulong?)RemoverUId;
            set => RemoverUId = (long?)value;
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

        [EditorBrowsable(EditorBrowsableState.Never)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long? RemoveMId { get; set; }

        [NotMapped]
        public ulong? RemoveMessageId
        {
            get => (ulong?)RemoveMId;
            set => RemoveMId = (long?)value;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long? RemoveCId { get; set; }

        [NotMapped]
        public ulong? RemoveChannelId
        {
            get => (ulong?)RemoveCId;
            set => RemoveCId = (long?)value;
        }

        public int Amount { get; set; }
        public string Reason { get; set; }
        public string RemoveReason { get; set; }

        public DateTime IssueDate { get; set; }
        public DateTime? RemoveDate { get; set; }

        public string Type => Amount == 0 ? "Initial Warning" : Amount == 1 ? "Warning" : "Strike";
    }
}