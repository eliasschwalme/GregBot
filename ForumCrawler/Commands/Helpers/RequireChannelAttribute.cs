using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace ForumCrawler
{
    public class RequireChannelAttribute : PreconditionAttribute
    {
        public ulong ChannelId { get; }

        public RequireChannelAttribute(ulong channelId)
        {
            this.ChannelId = channelId;
        }

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (context.Channel.Id == this.ChannelId || context.Channel.Id == DiscordSettings.StaffCommandsChannel)
                return Task.FromResult(PreconditionResult.FromSuccess());
            return Task.FromResult(PreconditionResult.FromError($"This command can only be run in {MentionUtils.MentionChannel(this.ChannelId)}."));
        }
    }
}