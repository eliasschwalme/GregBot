namespace ForumCrawler.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class IdForStarboardMessageInStarboardPost : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.StarboardPosts", "StarboardMessageId", c => c.Long(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.StarboardPosts", "StarboardMessageId");
        }
    }
}
