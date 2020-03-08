using Discord;
using Discord.Commands;

using System;
using System.Linq;
using System.Threading.Tasks;

namespace ForumCrawler
{
    internal class RequireRoleAttribute : PreconditionAttribute
    {
        public ulong RoleId { get; }

        public RequireRoleAttribute(ulong roleId) => RoleId = roleId;

        // Override the CheckPermissions method
        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var guild = await context.Client.GetGuildAsync(DiscordSettings.GuildId);
            var guildUser = await guild.GetUserAsync(context.User.Id);

            if (guildUser.RoleIds.Contains(RoleId))
                return PreconditionResult.FromSuccess();
            return PreconditionResult.FromError($"This command can only be run by a {MentionUtils.MentionRole(RoleId)}.");
        }
    }
}