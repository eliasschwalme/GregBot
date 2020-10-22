using Discord;
using Discord.WebSocket;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ForumCrawler.Helpers
{
	public static class GovernanceSubscriptionFeed
	{
        public static Task InformAsync(DiscordSocketClient client, Func<EmbedBuilder, EmbedBuilder> build)
        {
            var embed = build(new EmbedBuilder()).Build();

            var guild = client.GetGuild(DiscordSettings.GuildId);
            var channel = guild.GetTextChannel(DiscordSettings.GovernanceSubscriptionFeed);

            return channel.SendMessageAsync($"<@&{DiscordSettings.MapSubscriptionToRole[Commands.SubscriptionType.Governance]}>", embed: embed);
        }

		internal static Task OnUpgradeAsync(DiscordSocketClient client, ulong id)
        {
            return InformAsync(client, embed => embed.WithTitle("A suggestion has been upgraded!").WithDescription($"<#{id}>"));
        }

		internal static Task OnEditAsync(DiscordSocketClient client, ulong id)
        {
            return InformAsync(client, embed => embed.WithTitle("A suggestion has been edited!").WithDescription($"<#{id}>"));
        }

        internal static Task OnFinalizeAsync(DiscordSocketClient client, ulong id)
        {
            return InformAsync(client, embed => embed.WithTitle("A suggestion has been finalized!").WithDescription($"<#{id}>"));
		}

		internal static Task OnSuggestAsync(DiscordSocketClient client, ulong id)
        {
            return InformAsync(client, embed => embed.WithTitle("A new suggestion has appeared!").WithDescription($"<#{id}>"));
        }
	}
}
