namespace ForumCrawler.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddAlts : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ScoreUsers", "AltOfId", c => c.Long());
        }
        
        public override void Down()
        {
            DropColumn("dbo.ScoreUsers", "AltOfId");
        }
    }
}
