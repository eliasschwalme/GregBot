namespace ForumCrawler.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class FixStarboardModels : DbMigration
    {
        public override void Up()
        {
            DropPrimaryKey("dbo.StarboardPosts");
            CreateTable(
                "dbo.StarReactions",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    MessageId = c.Long(nullable: false),
                    StargazerId = c.Long(nullable: false),
                    HasReaction = c.Boolean(nullable: false),
                    Version = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                })
                .PrimaryKey(t => t.Id);

            AddColumn("dbo.StarboardPosts", "Version", c => c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"));
            AlterColumn("dbo.StarboardPosts", "Id", c => c.Long(nullable: false, identity: true));
            AddPrimaryKey("dbo.StarboardPosts", "Id");
            DropColumn("dbo.StarboardPosts", "Author");
        }

        public override void Down()
        {
            AddColumn("dbo.StarboardPosts", "Author", c => c.Long(nullable: false));
            DropPrimaryKey("dbo.StarboardPosts");
            AlterColumn("dbo.StarboardPosts", "Id", c => c.Int(nullable: false, identity: true));
            DropColumn("dbo.StarboardPosts", "Version");
            DropTable("dbo.StarReactions");
            AddPrimaryKey("dbo.StarboardPosts", "Id");
        }
    }
}