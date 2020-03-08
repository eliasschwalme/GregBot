namespace ForumCrawler.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class ExemptUser : DbMigration
    {
        public override void Up() => AddColumn("dbo.ScoreUsers", "EarlyUserExempt", c => c.Boolean(nullable: false));

        public override void Down() => DropColumn("dbo.ScoreUsers", "EarlyUserExempt");
    }
}