using System;
using System.Linq;
using Discord;
using Discord.WebSocket;

namespace ForumCrawler
{
    public static class StarboardWatcherConfigurator
    {
        public static StarboardWatcher GeneralStarboard(DiscordSocketClient client)
        {
            var guild = client.GetGuild(DiscordSettings.GuildId);
            var starboard = new StarboardWatcher
            (
                client,
                guild,
                guild.GetTextChannel(DiscordSettings.StarboardChannel),
                ChannelCategoryQualifier(guild.GetCategoryChannel(360825166437285891)), // text channels
                WootQualifier,
                10
            );

            return starboard;
        }

        public static StarboardWatcher StaffVoteStarboard(DiscordSocketClient client)
        {
            var guild = client.GetGuild(DiscordSettings.DSGuildId);
            var starboard = new StarboardWatcher
            (
                client,
                guild,
                guild.GetTextChannel(DiscordSettings.DSVoteboardChannel), // #vote-board
                channel => channel.Id == DiscordSettings.DSModerationChannel, // #staff
                StaffVoteQualifier,
                1
            );

            return starboard;
        }

        private static bool WootQualifier(IEmote emote)
        {
            return emote.Name == "woot";
        }

        private static bool StaffVoteQualifier(IEmote emote)
        {
            return emote.Name == "👍" || emote.Name == "👎";
        }

        private static ChannelQualifier ChannelCategoryQualifier(SocketCategoryChannel category)
        {
            return channel => category.Channels.Any(categoryChannel => categoryChannel.Id == channel.Id);
        }

		internal static StarboardWatcher SuggestionStarboard(DiscordSocketClient client)
		{
            var guild = client.GetGuild(DiscordSettings.GuildId);
            var starboard = new StarboardWatcher
            (
                client,
                guild,
                guild.GetTextChannel(DiscordSettings.SuggestionBoard), // #suggestion-board
                channel => channel.Id == DiscordSettings.Feedback, // #feedback
                emote => emote.Name == "woot",
                4,
                emote => emote.Name == "doot"
            );

            return starboard;
		}
	}
}