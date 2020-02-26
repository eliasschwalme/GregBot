namespace ForumCrawler.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
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
