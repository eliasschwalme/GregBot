namespace ForumCrawler.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddGovernanceVotes : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.GovernanceVotes",
                c => new
                {
                    Id = c.Long(nullable: false),
                    UId = c.Long(nullable: false),
                    MId = c.Long(nullable: false),
                    VId = c.Long(nullable: false),
                })
                .PrimaryKey(t => t.Id);
        }

        public override void Down() => DropTable("dbo.GovernanceVotes");
    }
}