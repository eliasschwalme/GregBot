namespace ForumCrawler.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddStreaks : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ScoreUsers", "DailyCount", c => c.Int(nullable: false));
            AddColumn("dbo.ScoreUsers", "DailyStreakCount", c => c.Int(nullable: false));
            AddColumn("dbo.ScoreUsers", "HasDisabledThresholdWarning", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.ScoreUsers", "HasDisabledThresholdWarning");
            DropColumn("dbo.ScoreUsers", "DailyStreakCount");
            DropColumn("dbo.ScoreUsers", "DailyCount");
        }
    }
}
