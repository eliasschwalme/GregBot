namespace ForumCrawler.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemoveExemptAndShowScore : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.ScoreUsers", "EarlyUserExempt");
            DropColumn("dbo.ScoreUsers", "ShowInUsername");
        }
        
        public override void Down()
        {
            AddColumn("dbo.ScoreUsers", "ShowInUsername", c => c.Boolean(nullable: false));
            AddColumn("dbo.ScoreUsers", "EarlyUserExempt", c => c.Boolean(nullable: false));
        }
    }
}
