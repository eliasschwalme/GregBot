using System.Data.Entity.Migrations;

namespace ForumCrawler.Migrations
{
    public partial class DontGenerateIdForStarboardPost : DbMigration
    {
        public override void Up()
        {
            DropPrimaryKey("dbo.StarboardPosts");
            AlterColumn("dbo.StarboardPosts", "Id", c => c.Long(false));
            AddPrimaryKey("dbo.StarboardPosts", "Id");
        }

        public override void Down()
        {
            DropPrimaryKey("dbo.StarboardPosts");
            AlterColumn("dbo.StarboardPosts", "Id", c => c.Long(false, true));
            AddPrimaryKey("dbo.StarboardPosts", "Id");
        }
    }
}