namespace ForumCrawler.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemoveMessages : DbMigration
    {
        public override void Up()
        {
            DropTable("dbo.Messages");
        }
        
        public override void Down()
        {
            CreateTable(
                "dbo.Messages",
                c => new
                    {
                        Id = c.Long(nullable: false),
                        UId = c.Long(nullable: false),
                        Content = c.String(),
                        Attachment = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
        }
    }
}
