using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace ForumCrawler
{
    public class RequireChannelAttribute : PreconditionAttribute
    {
        public RequireChannelAttribute(ulong channelId)
        {
            ChannelId = channelId;
        }

        public ulong ChannelId { get; }

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command,
            IServiceProvider services)
        {
            if (context.Channel.Id == ChannelId ||
                context.Channel.Id == DiscordSettings.StaffCommandsChannel ||
                context.Channel.Id == DiscordSettings.DSStaffCommandsChannel)
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }

            return Task.FromResult(
                PreconditionResult.FromError(
                    $"This command can only be run in {MentionUtils.MentionChannel(ChannelId)}."));
        }
    }
}