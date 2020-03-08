namespace ForumCrawler.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class DontGenerateIdForStarboardPost : DbMigration
    {
        public override void Up()
        {
            DropPrimaryKey("dbo.StarboardPosts");
            AlterColumn("dbo.StarboardPosts", "Id", c => c.Long(nullable: false));
            AddPrimaryKey("dbo.StarboardPosts", "Id");
        }

        public override void Down()
        {
            DropPrimaryKey("dbo.StarboardPosts");
            AlterColumn("dbo.StarboardPosts", "Id", c => c.Long(nullable: false, identity: true));
            AddPrimaryKey("dbo.StarboardPosts", "Id");
        }
    }
}