namespace ForumCrawler.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddWarnings : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Warnings",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        UId = c.Long(nullable: false),
                        MId = c.Long(nullable: false),
                        CId = c.Long(nullable: false),
                        RemoverUId = c.Long(),
                        IssuerUId = c.Long(nullable: false),
                        RemoveMId = c.Long(),
                        RemoveCId = c.Long(),
                        Amount = c.Int(nullable: false),
                        Reason = c.String(),
                        RemoveReason = c.String(),
                        IssueDate = c.DateTime(nullable: false),
                        RemoveDate = c.DateTime(),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.UId);
            
        }
        
        public override void Down()
        {
            DropIndex("dbo.Warnings", new[] { "UId" });
            DropTable("dbo.Warnings");
        }
    }
}
