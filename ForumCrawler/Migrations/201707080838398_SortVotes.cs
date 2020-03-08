namespace ForumCrawler.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class SortVotes : DbMigration
    {
        public override void Up() => AddColumn("dbo.MafiaVotes", "LastUpdated", c => c.DateTime(nullable: false));

        public override void Down() => DropColumn("dbo.MafiaVotes", "LastUpdated");
    }
}