namespace ForumCrawler.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class StarboardGuarenteedSynchronousness : DbMigration
    {
        public override void Up()
        {
            DropPrimaryKey("dbo.StarboardPosts");
            AddColumn("dbo.StarboardPosts", "MessageId", c => c.Long(nullable: false));
            AlterColumn("dbo.StarboardPosts", "Id", c => c.Int(nullable: false, identity: true));
            AddPrimaryKey("dbo.StarboardPosts", "Id");
            DropColumn("dbo.StarReactions", "Version");
            DropColumn("dbo.StarboardPosts", "Version");
        }

        public override void Down()
        {
            AddColumn("dbo.StarboardPosts", "Version", c => c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"));
            AddColumn("dbo.StarReactions", "Version", c => c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"));
            DropPrimaryKey("dbo.StarboardPosts");
            AlterColumn("dbo.StarboardPosts", "Id", c => c.Long(nullable: false));
            DropColumn("dbo.StarboardPosts", "MessageId");
            AddPrimaryKey("dbo.StarboardPosts", "Id");
        }
    }
}