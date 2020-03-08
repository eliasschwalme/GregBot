namespace ForumCrawler.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class ShowInUsername : DbMigration
    {
        public override void Up() => AddColumn("dbo.ScoreUsers", "ShowInUsername", c => c.Boolean(nullable: false));

        public override void Down() => DropColumn("dbo.ScoreUsers", "ShowInUsername");
    }
}