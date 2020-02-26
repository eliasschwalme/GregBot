using Discord;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ForumCrawler
{
    public class Warning
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }


        [EditorBrowsable(EditorBrowsableState.Never),
            DatabaseGenerated(DatabaseGeneratedOption.None), Index]
        public long UId { get; set; }

        [NotMapped]
        public ulong UserId
        {
            get { return (ulong)this.UId; }
            set { this.UId = (long)value; }
        }

        [EditorBrowsable(EditorBrowsableState.Never),
            DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long MId { get; set; }

        [NotMapped]
        public ulong MessageId
        {
            get { return (ulong)this.MId; }
            set { this.MId = (long)value; }
        }

        [EditorBrowsable(EditorBrowsableState.Never),
            DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long CId { get; set; }

        [NotMapped]
        public ulong ChannelId
        {
            get { return (ulong)this.CId; }
            set { this.CId = (long)value; }
        }

        [EditorBrowsable(EditorBrowsableState.Never),
            DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long? RemoverUId { get; set; }

        [NotMapped]
        public ulong? RemoverId { 
            get { return (ulong?)this.RemoverUId; }
            set { this.RemoverUId = (long?)value; }
        }

        [EditorBrowsable(EditorBrowsableState.Never),
            DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long IssuerUId { get; set; }

        [NotMapped]
        public ulong IssuerId
        {
            get { return (ulong)this.IssuerUId; }
            set { this.IssuerUId = (long)value; }
        }

        [EditorBrowsable(EditorBrowsableState.Never),
            DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long? RemoveMId { get; set; }

        [NotMapped]
        public ulong? RemoveMessageId
        {
            get { return (ulong?)this.RemoveMId; }
            set { this.RemoveMId = (long?)value; }
        }

        [EditorBrowsable(EditorBrowsableState.Never),
            DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long? RemoveCId { get; set; }

        [NotMapped]
        public ulong? RemoveChannelId
        {
            get { return (ulong?)this.RemoveCId; }
            set { this.RemoveCId = (long?)value; }
        }

        public int Amount { get; set; }
        public string Reason { get; set; }
        public string RemoveReason { get; set; }

        public DateTime IssueDate { get; set; }
        public DateTime? RemoveDate { get; set; }

        public string Type => this.Amount == 0 ? "Initial Warning" : this.Amount == 1 ? "Warning" : "Strike";
    }
}
