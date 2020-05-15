using System.Data.Entity.Migrations;

namespace ForumCrawler.Migrations
{
    public partial class ExemptUser : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ScoreUsers", "EarlyUserExempt", c => c.Boolean(false));
        }

        public override void Down()
        {
            DropColumn("dbo.ScoreUsers", "EarlyUserExempt");
        }
    }
}