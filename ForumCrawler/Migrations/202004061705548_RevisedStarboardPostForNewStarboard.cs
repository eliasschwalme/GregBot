namespace ForumCrawler.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RevisedStarboardPostForNewStarboard : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.RevisedStarboardPosts",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        StarboardChannelId = c.Long(nullable: false),
                        MessageId = c.Long(nullable: false),
                        StarboardMessageId = c.Long(nullable: false),
                        StaffToggledVisibility = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.RevisedStarboardPosts");
        }
    }
}
