using Discord.Commands;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;
using Discord;

namespace ForumCrawler.Commands
{
    public enum SubscriptionType
    {
         Governance,
         Events,
    }

    [Group]
    public class SubscriptionCommands : ModuleBase<SocketCommandContext>
	{
        [Command("subscribe")]
        [RequireChannel(DiscordSettings.BotCommandsChannel)]
        public async Task SubscribeAsync(SubscriptionType type)
        {
            var user = Context.User as IGuildUser;
            var roles = user.RoleIds;
            var targetRoleId = DiscordSettings.MapSubscriptionToRole[type];

            if (roles.Any(roleId => roleId == targetRoleId))
            {
                await ReplyAsync($"You're already subscribed to {type}!");
                return;
            }

            await user.AddRoleAsync(Context.Guild.GetRole(targetRoleId));
            await ReplyAsync($"Subscribed to {type}!");
        }

        [Command("unsubscribe")]
        [RequireChannel(DiscordSettings.BotCommandsChannel)]
        public async Task UnsubscribeAsync(SubscriptionType type)
        {
            var user = Context.User as IGuildUser;
            var roles = user.RoleIds;
            var targetRoleId = DiscordSettings.MapSubscriptionToRole[type];

            if (!roles.Any(roleId => roleId == targetRoleId))
            {
                await ReplyAsync($"You're not subscribed to {type}!");
                return;
            }

            await user.RemoveRoleAsync(Context.Guild.GetRole(targetRoleId));
            await ReplyAsync($"Unsubscribed from {type}!");
        }
    }
}
