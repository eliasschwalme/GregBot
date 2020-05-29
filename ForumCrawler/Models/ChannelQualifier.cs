using Discord.WebSocket;

namespace ForumCrawler
{
    /// <summary>
    ///     Represents criteria that may or may not make a channel eligible for starboarding.
    /// </summary>
    /// <param name="channel">
    ///     The channel in question to determine if it is qualified
    ///     to be pinned to the designated starboard.
    /// </param>
    public delegate bool ChannelQualifier(ISocketMessageChannel channel);
}