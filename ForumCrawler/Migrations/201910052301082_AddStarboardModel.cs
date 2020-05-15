using System.Data.Entity.Migrations;

namespace ForumCrawler.Migrations
{
    public partial class AddStarboardModel : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                    "dbo.StarboardPosts",
                    c => new {Id = c.Int(false, true), Author = c.Long(false), OnStarboard = c.Boolean(false)})
                .PrimaryKey(t => t.Id);
        }

        public override void Down()
        {
            DropTable("dbo.StarboardPosts");
        }
    }
}