using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;

namespace ForumCrawler
{
    public static class UserExtensions
    {
        public static Dictionary<SuggestionType, string> ChannelSuffixes = new Dictionary<SuggestionType, string>
        {
            {SuggestionType.Draft, "draft_"}, {SuggestionType.RFC, "rfc_"}, {SuggestionType.Vote, "vote_"}
        };

        public static string GetName(this IGuildUser user)
        {
            return user.Nickname ?? user.Username;
        }

        public static bool IsServerOwner(this IGuildUser guildUser)
        {
            return guildUser.RoleIds.Contains(DiscordSettings.DiscordServerOwner) ||
                   guildUser.RoleIds.Contains(DiscordSettings.DSDiscordServerOwner);
        }

        public static bool IsStaff(this IGuildUser guildUser)
        {
            return guildUser.RoleIds.Contains(DiscordSettings.DiscordStaff) ||
                   guildUser.RoleIds.Contains(DiscordSettings.DSDiscordStaff) ||
                   guildUser.RoleIds.Contains(DiscordSettings.DiscordServerOwner) ||
                   guildUser.RoleIds.Contains(DiscordSettings.DSDiscordServerOwner);
        }

        public static bool IsStaffOrConsultant(this IGuildUser guildUser)
        {
            return guildUser.RoleIds.Contains(DiscordSettings.DiscordStaff) ||
                   guildUser.RoleIds.Contains(DiscordSettings.DiscordStaffConsultant) ||
                   guildUser.RoleIds.Contains(DiscordSettings.DSDiscordStaff) ||
                   guildUser.RoleIds.Contains(DiscordSettings.DSDiscordStaffConsultant) ||
                   guildUser.RoleIds.Contains(DiscordSettings.DiscordServerOwner) ||
                   guildUser.RoleIds.Contains(DiscordSettings.DSDiscordServerOwner);
        }

        public static bool IsSuggestionChannelByName(this IChannel channel)
        {
            return ChannelSuffixes
                .Where(kv => channel.Name.StartsWith(kv.Value, StringComparison.InvariantCultureIgnoreCase)).Any();
        }

        public static SuggestionType GetSuggestionChannelType(this IChannel channel)
        {
            return ChannelSuffixes
                .Where(kv => channel.Name.StartsWith(kv.Value, StringComparison.InvariantCultureIgnoreCase)).First()
                .Key;
        }

        public static string GetSuggestionChannelName(this IChannel channel)
        {
            var type = channel.GetSuggestionChannelType();
            var suffix = ChannelSuffixes[type];
            return channel.Name.Substring(suffix.Length);
        }

        public static async Task ConvertSuggestionChannelType(this SocketGuildChannel channel, SuggestionType type)
        {
            var name = GetSuggestionChannelName(channel);
            var suffix = ChannelSuffixes[type];
            var res = channel.ModifyAsync(c =>
            {
                c.Name = suffix + name;
            });
            await ReorderChannels(channel.Guild);
        }

        public static Task RenameSuggestionChannel(this SocketGuildChannel channel, string name)
        {
            var type = channel.GetSuggestionChannelType();
            var suffix = ChannelSuffixes[type];
            return channel.ModifyAsync(c =>
            {
                c.Name = suffix + name;
            });
        }

        public static async Task<RestTextChannel> CreateSuggestionChannel(this SocketGuild guild, ulong categoryId,
            SuggestionType type, string name)
        {
            var suffix = ChannelSuffixes[type];
            var res = await guild.CreateTextChannelAsync(suffix + name, c =>
            {
                c.CategoryId = categoryId;
            });
            await ReorderChannels(guild);
            return res;
        }

        private static async Task ReorderChannels(SocketGuild guild)
        {
            var config = guild.GetGovernanceConfig();
            var channels = guild.CategoryChannels.First(c => c.Id == config.Category).Channels
                .OrderByDescending(c => c.Id).ToList();

            var normal = channels.Where(c => !c.IsSuggestionChannelByName()).Reverse();
            var vote = channels.Except(normal).Where(c => c.GetSuggestionChannelType() == SuggestionType.Vote);
            var rfc = channels.Except(normal).Where(c => c.GetSuggestionChannelType() == SuggestionType.RFC);
            var draft = channels.Except(normal).Where(c => c.GetSuggestionChannelType() == SuggestionType.Draft);
            await guild.ReorderChannelsAsync(normal.Concat(vote).Concat(rfc).Concat(draft)
                .Select((c, i) => new ReorderChannelProperties(c.Id, i)));
        }

        public static DiscordSettings.GovernanceConfig GetGovernanceConfig(this IGuild guild)
        {
            return DiscordSettings.GovernanceConfigs[guild.Id];
        }
    }

    public enum SuggestionType
    {
        Draft,
        RFC,
        Vote
    }
}