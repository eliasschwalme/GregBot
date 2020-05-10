namespace ForumCrawler.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InertiaRevamp : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ScoreUsers", "Inertia", c => c.Double(nullable: false, defaultValue: 0.2));
            AddColumn("dbo.ScoreUsers", "DownBoostsAsString", c => c.String());
            DropColumn("dbo.ScoreUsers", "ReservePoints");
        }
        
        public override void Down()
        {
            AddColumn("dbo.ScoreUsers", "ReservePoints", c => c.Double(nullable: false));
            DropColumn("dbo.ScoreUsers", "DownBoostsAsString");
            DropColumn("dbo.ScoreUsers", "Inertia");
        }
    }
}
