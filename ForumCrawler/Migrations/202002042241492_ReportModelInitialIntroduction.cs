using System.Data.Entity.Migrations;

namespace ForumCrawler.Migrations
{
    public partial class ReportModelInitialIntroduction : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                    "dbo.ReportModels",
                    c => new
                    {
                        Id = c.Long(false, true),
                        _reportId = c.Long(false),
                        _reporters = c.String(),
                        _suspectId = c.Long(false),
                        _channelId = c.Long(false),
                        _messageId = c.Long(false),
                        _reportsMessage = c.Long(false),
                        Timestamp = c.DateTimeOffset(false, 7),
                        Status = c.Int(false),
                        _moderatorId = c.Long(false)
                    })
                .PrimaryKey(t => t.Id);
        }

        public override void Down()
        {
            DropTable("dbo.ReportModels");
        }
    }
}