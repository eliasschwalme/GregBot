using System.Data.Entity;
using MySql.Data.Entity;

namespace ForumCrawler
{
    internal class DatabaseContext : DbContext
    {
        public DbSet<ScoreUser> ScoreUsers { get; set; }
        public DbSet<GovernanceVote> GovernanceVotes { get; set; }
        public DbSet<StarboardPost> StarboardPosts { get; set; }
        public DbSet<StarReaction> StarboardGazers { get; set; }
        public DbSet<Warning> Warnings { get; set; }
        public DbSet<Mute> Mutes { get; set; }
        public DbSet<ReportModel> Reports { get; set; }
    }
}