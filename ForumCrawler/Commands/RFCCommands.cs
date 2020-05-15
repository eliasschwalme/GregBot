using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace ForumCrawler
{
    [Group("rfc")]
    public class RFCCommands : GovernanceCommandsBase
    {
        [Command]
        [Summary("Starts the amendment process.")]
        [Priority(0)]
        public async Task RFC([Summary("The short name.")] string shortName)
        {
            await CreateSuggestionChannel(SuggestionType.RFC, shortName, Context.User,
                channel => GetSuggestionFromUserAsync(channel, Context.User));
        }

        [Command("upgrade")]
        [Alias("import")]
        [Summary("Upgrades an existing message to a suggestion")]
        [Priority(1)]
        public async Task Upgrade(ulong messageId, string shortName)
        {
            var message = await Context.Channel.GetMessageAsync(messageId);
            if (message == null)
            {
                throw new Exception("Message with the given ID was not found in this channel.");
            }

            await CreateSuggestionChannel(SuggestionType.RFC, shortName, message.Author,
                async channel =>
                {
                    await channel.SendMessageAsync(
                        $"Suggestion upgraded by {MentionUtils.MentionUser(Context.User.Id)}.");
                    return await ConfirmSuggestionFromUserAsync(channel, message.Author, message.Content);
                });
        }
    }
}