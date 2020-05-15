using System.Data.Entity.Migrations;

namespace ForumCrawler.Migrations
{
    public partial class IdForStarboardMessageInStarboardPost : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.StarboardPosts", "StarboardMessageId", c => c.Long(false));
        }

        public override void Down()
        {
            DropColumn("dbo.StarboardPosts", "StarboardMessageId");
        }
    }
}