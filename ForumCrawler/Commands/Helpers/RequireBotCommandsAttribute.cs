namespace ForumCrawler
{
    public sealed class RequireBotCommandsAttribute : RequireChannelAttribute
    {
        public RequireBotCommandsAttribute() : base(DiscordSettings.BotCommandsChannel)
        {
        }
    }
}