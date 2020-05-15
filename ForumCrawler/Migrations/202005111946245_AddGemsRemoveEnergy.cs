using System.Data.Entity.Migrations;

namespace ForumCrawler.Migrations
{
    public partial class AddGemsRemoveEnergy : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ScoreUsers", "Gems", c => c.Int(false));
            AddColumn("dbo.ScoreUsers", "LastDaily", c => c.DateTime());
            DropColumn("dbo.ScoreUsers", "LastEnergy");
            DropColumn("dbo.ScoreUsers", "Energy");
        }

        public override void Down()
        {
            AddColumn("dbo.ScoreUsers", "Energy", c => c.Double(false));
            AddColumn("dbo.ScoreUsers", "LastEnergy", c => c.DateTime());
            DropColumn("dbo.ScoreUsers", "LastDaily");
            DropColumn("dbo.ScoreUsers", "Gems");
        }
    }
}