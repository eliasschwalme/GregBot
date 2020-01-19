namespace ForumCrawler.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class DeleteMafia : DbMigration
    {
        public override void Up()
        {
            DropTable("dbo.MafiaSettings");
            DropTable("dbo.MafiaVotes");
        }
        
        public override void Down()
        {
            CreateTable(
                "dbo.MafiaVotes",
                c => new
                    {
                        UId = c.Long(nullable: false),
                        TId = c.Long(nullable: false),
                        LastUpdated = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.UId);
            
            CreateTable(
                "dbo.MafiaSettings",
                c => new
                    {
                        Id = c.Int(nullable: false),
                        PlayerCount = c.Int(nullable: false),
                        DayEnd = c.DateTime(nullable: false),
                        IsNight = c.Boolean(nullable: false),
                        GameConfigUrl = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
        }
    }
}
