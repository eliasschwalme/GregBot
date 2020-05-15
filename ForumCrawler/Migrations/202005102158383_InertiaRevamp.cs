using System.Data.Entity.Migrations;

namespace ForumCrawler.Migrations
{
    public partial class InertiaRevamp : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ScoreUsers", "Inertia", c => c.Double(false, 0.2));
            AddColumn("dbo.ScoreUsers", "DownBoostsAsString", c => c.String());
            DropColumn("dbo.ScoreUsers", "ReservePoints");
        }

        public override void Down()
        {
            AddColumn("dbo.ScoreUsers", "ReservePoints", c => c.Double(false));
            DropColumn("dbo.ScoreUsers", "DownBoostsAsString");
            DropColumn("dbo.ScoreUsers", "Inertia");
        }
    }
}