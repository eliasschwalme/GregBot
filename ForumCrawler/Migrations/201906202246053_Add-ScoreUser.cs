namespace ForumCrawler.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddScoreUser : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ScoreUsers",
                c => new
                    {
                        Id = c.Long(nullable: false),
                        LastEnergy = c.DateTime(),
                        LastActivity = c.DateTime(),
                        LastDecay = c.DateTime(),
                        Energy = c.Double(nullable: false),
                        ReservePoints = c.Double(nullable: false),
                        Score = c.Double(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.ScoreUsers");
        }
    }
}
