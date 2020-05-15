using System.Data.Entity.Migrations;

namespace ForumCrawler.Migrations
{
    public partial class AddMafia : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                    "dbo.MafiaSettings",
                    c => new
                    {
                        Id = c.Int(false),
                        PlayerCount = c.Int(false),
                        DayEnd = c.DateTime(false),
                        IsNight = c.Boolean(false)
                    })
                .PrimaryKey(t => t.Id);

            CreateTable(
                    "dbo.MafiaVotes",
                    c => new {UId = c.Long(false), TId = c.Long(false)})
                .PrimaryKey(t => t.UId);
        }

        public override void Down()
        {
            DropTable("dbo.MafiaVotes");
            DropTable("dbo.MafiaSettings");
        }
    }
}