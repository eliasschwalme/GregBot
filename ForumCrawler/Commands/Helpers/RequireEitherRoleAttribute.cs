using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace ForumCrawler
{
    internal sealed class RequireEitherRoleAttribute : PreconditionAttribute
    {
        public RequireEitherRoleAttribute(params ulong[] roleIds)
        {
            RoleIds = roleIds;
        }

        public ulong[] RoleIds { get; }

        // Override the CheckPermissions method
        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context,
            CommandInfo command, IServiceProvider services)
        {
            var guild = await context.Client.GetGuildAsync(DiscordSettings.GuildId);
            var guildUser = await guild.GetUserAsync(context.User.Id);

            if (guildUser.RoleIds.Any(id => RoleIds.Contains(id)))
            {
                return PreconditionResult.FromSuccess();
            }

            return PreconditionResult.FromError(
                $"This command can only be run by a {string.Join(" or ", RoleIds.Select(r => MentionUtils.MentionRole(r)))}.");
        }
    }
}