using System.Data.Entity.Migrations;

namespace ForumCrawler.Migrations
{
    public partial class AddScoreUser : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                    "dbo.ScoreUsers",
                    c => new
                    {
                        Id = c.Long(false),
                        LastEnergy = c.DateTime(),
                        LastActivity = c.DateTime(),
                        LastDecay = c.DateTime(),
                        Energy = c.Double(false),
                        ReservePoints = c.Double(false),
                        Score = c.Double(false)
                    })
                .PrimaryKey(t => t.Id);
        }

        public override void Down()
        {
            DropTable("dbo.ScoreUsers");
        }
    }
}