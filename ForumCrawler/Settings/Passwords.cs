using System.Configuration;

namespace ForumCrawler
{
    public static class Passwords
    {
        static Passwords()
        {
            ForumPassword = ConfigurationManager.AppSettings["ForumPassword"];
            DiscordToken = ConfigurationManager.AppSettings["DiscordToken"];
        }

        public static string ForumPassword;
        public static string DiscordToken;
    }
}