using Discord;
using Discord.Commands;

using System;
using System.Linq;
using System.Threading.Tasks;

namespace ForumCrawler
{
    internal sealed class RequireRoleParameterAttribute : ParameterPreconditionAttribute
    {
        public ulong RoleId { get; }

        public RequireRoleParameterAttribute(ulong roleId) => RoleId = roleId;

        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, ParameterInfo parameter, object value, IServiceProvider services)
        {
            var guild = await context.Client.GetGuildAsync(DiscordSettings.GuildId);
            var guildUser = await guild.GetUserAsync(((IUser)value).Id);

            if (guildUser.RoleIds.Contains(RoleId))
                return PreconditionResult.FromSuccess();
            return PreconditionResult.FromError($"This command can only target a {MentionUtils.MentionRole(RoleId)}.");
        }
    }
}