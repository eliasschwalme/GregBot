namespace ForumCrawler.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddStarboardModel : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.StarboardPosts",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    Author = c.Long(nullable: false),
                    OnStarboard = c.Boolean(nullable: false),
                })
                .PrimaryKey(t => t.Id);
        }

        public override void Down() => DropTable("dbo.StarboardPosts");
    }
}