namespace ForumCrawler.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class UtilizeStarboardReaction : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.StarReactions", "StarboardChannelId", c => c.Long(nullable: false));
            DropColumn("dbo.StarReactions", "HasReaction");
        }
        
        public override void Down()
        {
            AddColumn("dbo.StarReactions", "HasReaction", c => c.Boolean(nullable: false));
            DropColumn("dbo.StarReactions", "StarboardChannelId");
        }
    }
}
