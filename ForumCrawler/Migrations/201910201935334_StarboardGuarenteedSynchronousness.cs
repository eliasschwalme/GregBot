using System.Data.Entity.Migrations;

namespace ForumCrawler.Migrations
{
    public partial class StarboardGuarenteedSynchronousness : DbMigration
    {
        public override void Up()
        {
            DropPrimaryKey("dbo.StarboardPosts");
            AddColumn("dbo.StarboardPosts", "MessageId", c => c.Long(false));
            AlterColumn("dbo.StarboardPosts", "Id", c => c.Int(false, true));
            AddPrimaryKey("dbo.StarboardPosts", "Id");
            DropColumn("dbo.StarReactions", "Version");
            DropColumn("dbo.StarboardPosts", "Version");
        }

        public override void Down()
        {
            AddColumn("dbo.StarboardPosts", "Version",
                c => c.Binary(false, fixedLength: true, timestamp: true, storeType: "rowversion"));
            AddColumn("dbo.StarReactions", "Version",
                c => c.Binary(false, fixedLength: true, timestamp: true, storeType: "rowversion"));
            DropPrimaryKey("dbo.StarboardPosts");
            AlterColumn("dbo.StarboardPosts", "Id", c => c.Long(false));
            DropColumn("dbo.StarboardPosts", "MessageId");
            AddPrimaryKey("dbo.StarboardPosts", "Id");
        }
    }
}