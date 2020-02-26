namespace ForumCrawler.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ReportModelInitialIntroduction : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ReportModels",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        _reportId = c.Long(nullable: false),
                        _reporters = c.String(),
                        _suspectId = c.Long(nullable: false),
                        _channelId = c.Long(nullable: false),
                        _messageId = c.Long(nullable: false),
                        _reportsMessage = c.Long(nullable: false),
                        Timestamp = c.DateTimeOffset(nullable: false, precision: 7),
                        Status = c.Int(nullable: false),
                        _moderatorId = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.ReportModels");
        }
    }
}
