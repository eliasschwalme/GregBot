using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ForumCrawler
{
    /// <see cref="QuickReportWatcher.Report"/>
    public class ReportModel
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        public long Id { get; set; }

        // public and non browseable because they're suppose to be mapped
        // don't want EF to not see it, dunno if it's necessary
        // it's all blargh :v
        [EditorBrowsable(EditorBrowsableState.Never)]
        public long _reportId { get; set; }

        [NotMapped]
        public ulong ReportId { get => _reportId.ToULong(); set => _reportId = value.ToLong(); }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public string _reporters { get; set; }

        // /  perfect  /  //
        [NotMapped]
        public Dictionary<ulong, string> Reporters { get => Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<ulong, string>>(_reporters); set => _reporters = Newtonsoft.Json.JsonConvert.SerializeObject(value); }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public long _suspectId { get; set; }

        [NotMapped]
        public ulong SuspectId { get => _suspectId.ToULong(); set => _suspectId = value.ToLong(); }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public long _channelId { get; set; }

        [NotMapped]
        public ulong ChannelId { get => _channelId.ToULong(); set => _channelId = value.ToLong(); }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public long _messageId { get; set; }

        [NotMapped]
        public ulong MessageId { get => _messageId.ToULong(); set => _messageId = value.ToLong(); }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public long _reportsMessage { get; set; }

        [NotMapped]
        public ulong ReportsMessage { get => _reportsMessage.ToULong(); set => _reportsMessage = value.ToLong(); }

        public DateTimeOffset Timestamp { get; set; }
        public QuickReportWatcher.Report.ReportStatus Status { get; set; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public long _moderatorId { get; set; }

        [NotMapped]
        public ulong ModeratorId { get => _moderatorId.ToULong(); set => _moderatorId = value.ToLong(); }
    }
}
