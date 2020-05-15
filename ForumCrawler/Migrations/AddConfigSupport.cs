using System.Data.Entity.Migrations;

namespace ForumCrawler.Migrations
{
    public partial class AddConfigSupport : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.MafiaSettings", "GameConfigUrl", c => c.String());
        }

        public override void Down()
        {
            DropColumn("dbo.MafiaSettings", "GameConfigUrl");
        }
    }
}