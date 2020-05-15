using System.Data.Entity.Migrations;

namespace ForumCrawler.Migrations
{
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                    "dbo.Messages",
                    c => new {Id = c.Long(false), UId = c.Long(false), Content = c.String(), Attachment = c.String()})
                .PrimaryKey(t => t.Id);
        }

        public override void Down()
        {
            DropTable("dbo.Messages");
        }
    }
}