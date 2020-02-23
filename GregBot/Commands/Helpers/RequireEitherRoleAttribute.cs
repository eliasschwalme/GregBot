using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace ForumCrawler
{

    class RequireEitherRoleAttribute : PreconditionAttribute
    {
        public ulong[] RoleIds { get; }

        public RequireEitherRoleAttribute(params ulong[] roleIds)
        {
            this.RoleIds = roleIds;
        }

        // Override the CheckPermissions method
        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var guild = await context.Client.GetGuildAsync(DiscordSettings.GuildId);
            var guildUser = await guild.GetUserAsync(context.User.Id);

            if (guildUser.RoleIds.Any(id => this.RoleIds.Contains(id)))
                return PreconditionResult.FromSuccess();
            return PreconditionResult.FromError($"This command can only be run by a {String.Join(" or ", this.RoleIds.Select(r => MentionUtils.MentionRole(r)))}.");
        }
    }
}
