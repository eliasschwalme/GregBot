using System.Data.Entity.Migrations;

namespace ForumCrawler.Migrations
{
    public partial class AddIndexScore : DbMigration
    {
        public override void Up()
        {
            CreateIndex("dbo.ScoreUsers", "Score");
        }

        public override void Down()
        {
            DropIndex("dbo.ScoreUsers", new[] {"Score"});
        }
    }
}