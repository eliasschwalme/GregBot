using System.Data.Entity.Migrations;

namespace ForumCrawler.Migrations
{
    public partial class RemoveExemptAndShowScore : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.ScoreUsers", "EarlyUserExempt");
            DropColumn("dbo.ScoreUsers", "ShowInUsername");
        }

        public override void Down()
        {
            AddColumn("dbo.ScoreUsers", "ShowInUsername", c => c.Boolean(false));
            AddColumn("dbo.ScoreUsers", "EarlyUserExempt", c => c.Boolean(false));
        }
    }
}