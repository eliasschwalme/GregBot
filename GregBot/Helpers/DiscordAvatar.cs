using Discord;

namespace ForumCrawler
{
    static class DiscordAvatar
    {
        public static string GetAvatarUrlOrDefault(this IUser user)
        {
            return user.GetAvatarUrl() ?? GetDefaultAvatarUrl(user);
        }
        
        public static string GetDefaultAvatarUrl(this IUser user)
        {
            var imageId = user.DiscriminatorValue % 5;
            return $"https://cdn.discordapp.com/embed/avatars/{imageId}.png";
        }
    }
}
