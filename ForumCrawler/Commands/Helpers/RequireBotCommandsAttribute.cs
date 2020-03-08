namespace ForumCrawler
{
    public class RequireBotCommandsAttribute : RequireChannelAttribute
    {
        public RequireBotCommandsAttribute() : base(DiscordSettings.BotCommandsChannel)
        {
        }
    }
}