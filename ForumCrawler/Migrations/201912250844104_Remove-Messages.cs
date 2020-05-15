using System.Data.Entity.Migrations;

namespace ForumCrawler.Migrations
{
    public partial class RemoveMessages : DbMigration
    {
        public override void Up()
        {
            DropTable("dbo.Messages");
        }

        public override void Down()
        {
            CreateTable(
                    "dbo.Messages",
                    c => new {Id = c.Long(false), UId = c.Long(false), Content = c.String(), Attachment = c.String()})
                .PrimaryKey(t => t.Id);
        }
    }
}