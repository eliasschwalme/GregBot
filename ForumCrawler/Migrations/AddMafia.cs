namespace ForumCrawler.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddMafia : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.MafiaSettings",
                c => new
                    {
                        Id = c.Int(nullable: false),
                        PlayerCount = c.Int(nullable: false),
                        DayEnd = c.DateTime(nullable: false),
                        IsNight = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.MafiaVotes",
                c => new
                    {
                        UId = c.Long(nullable: false),
                        TId = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.UId);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.MafiaVotes");
            DropTable("dbo.MafiaSettings");
        }
    }
}
