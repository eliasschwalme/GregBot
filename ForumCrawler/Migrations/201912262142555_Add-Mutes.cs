using System.Data.Entity.Migrations;

namespace ForumCrawler.Migrations
{
    public partial class AddMutes : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                    "dbo.Mutes",
                    c => new
                    {
                        Id = c.Long(false),
                        IssuerUId = c.Long(false),
                        ExpiryDate = c.DateTime(false),
                        IssueDate = c.DateTime(false)
                    })
                .PrimaryKey(t => t.Id);
        }

        public override void Down()
        {
            DropTable("dbo.Mutes");
        }
    }
}