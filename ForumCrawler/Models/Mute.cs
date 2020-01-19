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
    public class Mute
    {
        [EditorBrowsable(EditorBrowsableState.Never),
            DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long Id { get; set; }

        [NotMapped]
        public ulong UserId
        {
            get { return (ulong)this.Id; }
            set { this.Id = (long)value; }
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

        public DateTime ExpiryDate { get; set; }
        public DateTime IssueDate { get; set; }
    }
}
