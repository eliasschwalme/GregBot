using System.Data.Entity.Migrations;

namespace ForumCrawler.Migrations
{
    public partial class DeleteMafia : DbMigration
    {
        public override void Up()
        {
            DropTable("dbo.MafiaSettings");
            DropTable("dbo.MafiaVotes");
        }

        public override void Down()
        {
            CreateTable(
                    "dbo.MafiaVotes",
                    c => new {UId = c.Long(false), TId = c.Long(false), LastUpdated = c.DateTime(false)})
                .PrimaryKey(t => t.UId);

            CreateTable(
                    "dbo.MafiaSettings",
                    c => new
                    {
                        Id = c.Int(false),
                        PlayerCount = c.Int(false),
                        DayEnd = c.DateTime(false),
                        IsNight = c.Boolean(false),
                        GameConfigUrl = c.String()
                    })
                .PrimaryKey(t => t.Id);
        }
    }
}