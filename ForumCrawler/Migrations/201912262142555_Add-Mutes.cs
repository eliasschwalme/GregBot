namespace ForumCrawler.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddMutes : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Mutes",
                c => new
                {
                    Id = c.Long(nullable: false),
                    IssuerUId = c.Long(nullable: false),
                    ExpiryDate = c.DateTime(nullable: false),
                    IssueDate = c.DateTime(nullable: false),
                })
                .PrimaryKey(t => t.Id);
        }

        public override void Down() => DropTable("dbo.Mutes");
    }
}