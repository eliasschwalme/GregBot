using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ForumCrawler
{    public static class UserExtensions
    {
        public static string GetName(this IGuildUser user)
        {
            return user.Nickname ?? user.Username;
        }


        public static bool IsStaff(this IGuildUser guildUser)
        {
            return guildUser.RoleIds.Contains(DiscordSettings.DiscordStaff) ||
                guildUser.RoleIds.Contains(DiscordSettings.DSDiscordStaff);

        }

        public static bool IsStaffOrConsultant(this IGuildUser guildUser)
        {
            return guildUser.RoleIds.Contains(DiscordSettings.DiscordStaff) ||
                guildUser.RoleIds.Contains(DiscordSettings.DiscordStaffConsultant) ||
                guildUser.RoleIds.Contains(DiscordSettings.DSDiscordStaff) ||
                guildUser.RoleIds.Contains(DiscordSettings.DSDiscordStaffConsultant);
        }

        public static bool IsSuggestionChannelByName(this IChannel channel) => channel.Name.StartsWith("_") || channel.Name.StartsWith("vote_");

        public static bool IsSuggestionChannelFinalized(this IChannel channel) => channel.Name.StartsWith("vote_");

        public static DiscordSettings.GovernanceConfig GetGovernanceConfig(this IGuild guild) => DiscordSettings.GovernanceConfigs[guild.Id];
    }
}
