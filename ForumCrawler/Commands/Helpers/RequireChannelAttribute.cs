using Discord;
using Discord.Commands;

using System;
using System.Threading.Tasks;

namespace ForumCrawler
{
    public class RequireChannelAttribute : PreconditionAttribute
    {
        public ulong ChannelId { get; }

        public RequireChannelAttribute(ulong channelId) => ChannelId = channelId;

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (context.Channel.Id == ChannelId || context.Channel.Id == DiscordSettings.StaffCommandsChannel)
                return Task.FromResult(PreconditionResult.FromSuccess());
            return Task.FromResult(PreconditionResult.FromError($"This command can only be run in {MentionUtils.MentionChannel(ChannelId)}."));
        }
    }
}