using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace ForumCrawler
{
    class RequireRoleParameterAttribute : ParameterPreconditionAttribute
    {
        public ulong RoleId { get; }

        public RequireRoleParameterAttribute(ulong roleId)
        {
            this.RoleId = roleId;
        }

        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, ParameterInfo parameter, object value, IServiceProvider services)
        {
            var guild = await context.Client.GetGuildAsync(DiscordSettings.GuildId);
            var guildUser = await guild.GetUserAsync(((IUser)value).Id);

            if (guildUser.RoleIds.Contains(this.RoleId))
                return PreconditionResult.FromSuccess();
            return PreconditionResult.FromError($"This command can only target a {MentionUtils.MentionRole(this.RoleId)}.");

        }
    }
}