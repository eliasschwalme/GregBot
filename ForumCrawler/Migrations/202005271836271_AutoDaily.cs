namespace ForumCrawler.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AutoDaily : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ScoreUsers", "HasDisabledAutoDaily", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.ScoreUsers", "HasDisabledAutoDaily");
        }
    }
}
