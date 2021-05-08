using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace ForumCrawler
{
    public class ReportCommand : ModuleBase<SocketCommandContext>
    {
        [Command("report")]
        [Summary("Make a report on a user")]
        [Priority(1)]
        public async Task ReportAsync(IUser user, [Remainder] string reportMessage = null)
        {
            await ReplyAsync("The `g!report` command is deprecated! Please use `n!report <user> [reason]`.");
            /*
            if (Context.Guild != null)
            {
                await Context.Channel.DeleteMessageAsync(Context.Message);
            }

            if (string.IsNullOrEmpty(reportMessage))
            {
                throw new Exception("Please enter in a description of the report!");
            }

            var previous = Context.Guild != null ? GetPreviousMessage(user) : null;
            var reportId = previous?.Id ?? SnowflakeUtils.ToSnowflake(DateTimeOffset.UtcNow);
            await QuickReportWatcher.FileReport(reportId, Context.User, user, Context.Channel, previous, reportMessage);
            */
        }

        [Command("report")]
        [Summary("Make a report on a user")]
        [Priority(1)]
        public async Task ReportAsync(IUserMessage message, [Remainder] string reportMessage = null)
        {
            await ReplyAsync("The `g!report` command is deprecated! Please use `n!report <user> [reason]`.");
            /*
            await Context.Channel.DeleteMessageAsync(Context.Message);
            await QuickReportWatcher.FileReport(message.Id, Context.User, message.Author, message.Channel, message,
                reportMessage);
            */
        }

        [Command("report")]
        [Alias("report edit")]
        [Summary("Make a report on a user")]
        [Priority(0)]
        public async Task ReportAsync(ulong reportId, [Remainder] string reportMessage = null)
        {
            await ReplyAsync("The `g!report` command is deprecated! Please use `n!report <user> [reason]`.");
            /*
            if (Context.Channel is IGuildChannel)
            {
                await Context.Channel.DeleteMessageAsync(Context.Message);
            }

            await QuickReportWatcher.FileReport(reportId, Context.User, null, null, null, reportMessage);
            */
        }

        private IUserMessage GetPreviousMessage(IUser user)
        {
            foreach (var message in Context.Channel.CachedMessages
                .OrderByDescending(w => SnowflakeUtils.FromSnowflake(w.Id)).Take(20))
            {
                if (message.Author.Id == user.Id &&
                    message.Id != Context.Message.Id)
                {
                    return message as IUserMessage;
                }
            }

            return null;
        }
    }
}
