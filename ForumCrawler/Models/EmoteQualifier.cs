using Discord;

namespace ForumCrawler
{
    /// <summary>
    ///     Determines if an emote is qualified to represent the action of starboarding
    ///     a given post.
    /// </summary>
    /// <param name="emote">The emote in question.</param>
    public delegate bool EmoteQualifier(IEmote emote);
}