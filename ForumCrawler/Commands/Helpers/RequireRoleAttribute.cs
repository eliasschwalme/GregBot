using Discord;
using Discord.Commands;

using System;
using System.Linq;
using System.Threading.Tasks;

namespace ForumCrawler
{
    internal sealed class RequireRoleAttribute : PreconditionAttribute
    {
        public ulong[] RoleIds { get; }

        public RequireRoleAttribute(params ulong[] roleIds) => RoleIds = roleIds;

        // Override the CheckPermissions method
        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var guild = await context.Client.GetGuildAsync(DiscordSettings.GuildId);
            var guildUser = await guild.GetUserAsync(context.User.Id);

            if (RoleIds.Any(roleId => guildUser.RoleIds.Contains(roleId)))
                return PreconditionResult.FromSuccess();
            return PreconditionResult.FromError($"This command can only be run by a {MentionUtils.MentionRole(RoleIds.First())}.");
        }
    }
}