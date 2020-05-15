using System.Data.Entity.Migrations;

namespace ForumCrawler.Migrations
{
    public partial class RevisedStarboardPostForNewStarboard : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                    "dbo.RevisedStarboardPosts",
                    c => new
                    {
                        Id = c.Int(false, true),
                        StarboardChannelId = c.Long(false),
                        MessageId = c.Long(false),
                        StarboardMessageId = c.Long(false),
                        StaffToggledVisibility = c.Boolean(false)
                    })
                .PrimaryKey(t => t.Id);
        }

        public override void Down()
        {
            DropTable("dbo.RevisedStarboardPosts");
        }
    }
}