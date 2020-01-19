using Discord;
using Discord.Commands;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ForumCrawler.Commands
{
    public class ReportCommand : ModuleBase<SocketCommandContext>
    {
        [Command("report"), Summary("Make a report on a user"), Priority(1)]
        public async Task ReportAsync(IUser user, [Remainder] string reportMessage = null)
        {
            if (this.Context.Guild != null)
            {
                await this.Context.Channel.DeleteMessageAsync(this.Context.Message);
            }

            if (string.IsNullOrEmpty(reportMessage))
            {
                throw new Exception("Please enter in a description of the report!");
            }

            var previous = this.Context.Guild != null ? GetPreviousMessage(user) : null;
            var reportId = previous?.Id ?? SnowflakeUtils.ToSnowflake(DateTimeOffset.UtcNow);
            await QuickReportWatcher.FileReport(reportId, this.Context.User, user, this.Context.Channel, previous, reportMessage);
        }

        [Command("report"), Summary("Make a report on a user"), Priority(1)]
        public async Task ReportAsync(IUserMessage message, [Remainder] string reportMessage = null)
        {
            await this.Context.Channel.DeleteMessageAsync(this.Context.Message);
            await QuickReportWatcher.FileReport(message.Id, this.Context.User, message.Author, message.Channel, message, reportMessage);
        }

        [Command("report"), Alias("report edit"), Summary("Make a report on a user"), Priority(0)]
        public async Task ReportAsync(ulong reportId, [Remainder] string reportMessage = null)
        {
            if (this.Context.Channel is IGuildChannel)
                await this.Context.Channel.DeleteMessageAsync(this.Context.Message);
            await QuickReportWatcher.FileReport(reportId, this.Context.User, null, null, null, reportMessage);
        }

        private IUserMessage GetPreviousMessage(IUser user)
        {
            foreach (var message in this.Context.Channel.CachedMessages.OrderByDescending(w => SnowflakeUtils.FromSnowflake(w.Id)).Take(20))
            {
                if (message.Author.Id == user.Id &&
                    message.Id != this.Context.Message.Id)
                {
                    return message as IUserMessage;
                }
            }
            return null;
        }
    }
}
