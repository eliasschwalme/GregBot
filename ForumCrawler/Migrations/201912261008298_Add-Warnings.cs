using System.Data.Entity.Migrations;

namespace ForumCrawler.Migrations
{
    public partial class AddWarnings : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                    "dbo.Warnings",
                    c => new
                    {
                        Id = c.Long(false, true),
                        UId = c.Long(false),
                        MId = c.Long(false),
                        CId = c.Long(false),
                        RemoverUId = c.Long(),
                        IssuerUId = c.Long(false),
                        RemoveMId = c.Long(),
                        RemoveCId = c.Long(),
                        Amount = c.Int(false),
                        Reason = c.String(),
                        RemoveReason = c.String(),
                        IssueDate = c.DateTime(false),
                        RemoveDate = c.DateTime()
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.UId);
        }

        public override void Down()
        {
            DropIndex("dbo.Warnings", new[] {"UId"});
            DropTable("dbo.Warnings");
        }
    }
}