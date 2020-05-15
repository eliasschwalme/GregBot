using System.Data.Entity.Migrations;

namespace ForumCrawler.Migrations
{
    public partial class AddGovernanceVotes : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                    "dbo.GovernanceVotes",
                    c => new {Id = c.Long(false), UId = c.Long(false), MId = c.Long(false), VId = c.Long(false)})
                .PrimaryKey(t => t.Id);
        }

        public override void Down()
        {
            DropTable("dbo.GovernanceVotes");
        }
    }
}