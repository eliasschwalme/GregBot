using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace ForumCrawler
{
    internal sealed class RequireRoleAttribute : PreconditionAttribute
    {
        public RequireRoleAttribute(ulong roleId)
        {
            RoleId = roleId;
        }

        public ulong RoleId { get; }

        // Override the CheckPermissions method
        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context,
            CommandInfo command, IServiceProvider services)
        {
            var guild = await context.Client.GetGuildAsync(DiscordSettings.GuildId);
            var guildUser = await guild.GetUserAsync(context.User.Id);

            if (guildUser.RoleIds.Contains(RoleId))
            {
                return PreconditionResult.FromSuccess();
            }

            return PreconditionResult.FromError(
                $"This command can only be run by a {MentionUtils.MentionRole(RoleId)}.");
        }
    }
}