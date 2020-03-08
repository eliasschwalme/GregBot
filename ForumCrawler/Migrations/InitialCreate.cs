namespace ForumCrawler.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Messages",
                c => new
                {
                    Id = c.Long(nullable: false),
                    UId = c.Long(nullable: false),
                    Content = c.String(),
                    Attachment = c.String(),
                })
                .PrimaryKey(t => t.Id);
        }

        public override void Down() => DropTable("dbo.Messages");
    }
}