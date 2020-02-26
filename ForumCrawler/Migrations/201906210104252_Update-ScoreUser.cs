namespace ForumCrawler.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class UpdateScoreUser : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ScoreUsers", "BoostsAsString", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.ScoreUsers", "BoostsAsString");
        }
    }
}
