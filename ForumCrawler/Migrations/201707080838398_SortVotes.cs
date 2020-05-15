using System.Data.Entity.Migrations;

namespace ForumCrawler.Migrations
{
    public partial class SortVotes : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.MafiaVotes", "LastUpdated", c => c.DateTime(false));
        }

        public override void Down()
        {
            DropColumn("dbo.MafiaVotes", "LastUpdated");
        }
    }
}