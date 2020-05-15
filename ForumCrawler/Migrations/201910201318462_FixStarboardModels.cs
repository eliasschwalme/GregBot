using System.Data.Entity.Migrations;

namespace ForumCrawler.Migrations
{
    public partial class FixStarboardModels : DbMigration
    {
        public override void Up()
        {
            DropPrimaryKey("dbo.StarboardPosts");
            CreateTable(
                    "dbo.StarReactions",
                    c => new
                    {
                        Id = c.Int(false, true),
                        MessageId = c.Long(false),
                        StargazerId = c.Long(false),
                        HasReaction = c.Boolean(false),
                        Version = c.Binary(false, fixedLength: true, timestamp: true, storeType: "rowversion")
                    })
                .PrimaryKey(t => t.Id);

            AddColumn("dbo.StarboardPosts", "Version",
                c => c.Binary(false, fixedLength: true, timestamp: true, storeType: "rowversion"));
            AlterColumn("dbo.StarboardPosts", "Id", c => c.Long(false, true));
            AddPrimaryKey("dbo.StarboardPosts", "Id");
            DropColumn("dbo.StarboardPosts", "Author");
        }

        public override void Down()
        {
            AddColumn("dbo.StarboardPosts", "Author", c => c.Long(false));
            DropPrimaryKey("dbo.StarboardPosts");
            AlterColumn("dbo.StarboardPosts", "Id", c => c.Int(false, true));
            DropColumn("dbo.StarboardPosts", "Version");
            DropTable("dbo.StarReactions");
            AddPrimaryKey("dbo.StarboardPosts", "Id");
        }
    }
}