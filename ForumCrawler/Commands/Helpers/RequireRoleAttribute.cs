using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace ForumCrawler
{

    class RequireRoleAttribute : PreconditionAttribute
    {
        public ulong RoleId { get; }

        public RequireRoleAttribute(ulong roleId)
        {
            this.RoleId = roleId;
        }

        // Override the CheckPermissions method
        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var guild = await context.Client.GetGuildAsync(DiscordSettings.GuildId);
            var guildUser = await guild.GetUserAsync(context.User.Id);

            if (guildUser.RoleIds.Contains(this.RoleId))
                return PreconditionResult.FromSuccess();
            return PreconditionResult.FromError($"This command can only be run by a {MentionUtils.MentionRole(this.RoleId)}.");
        }
    }
}
