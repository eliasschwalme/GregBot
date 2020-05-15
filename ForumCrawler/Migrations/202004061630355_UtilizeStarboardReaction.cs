using System.Data.Entity.Migrations;

namespace ForumCrawler.Migrations
{
    public partial class UtilizeStarboardReaction : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.StarReactions", "StarboardChannelId", c => c.Long(false));
            DropColumn("dbo.StarReactions", "HasReaction");
        }

        public override void Down()
        {
            AddColumn("dbo.StarReactions", "HasReaction", c => c.Boolean(false));
            DropColumn("dbo.StarReactions", "StarboardChannelId");
        }
    }
}