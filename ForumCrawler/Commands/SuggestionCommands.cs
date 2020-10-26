using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

using ForumCrawler.Helpers;

namespace ForumCrawler
{
    [Group("suggest")]
    [Alias("suggestion")]
    public class SuggestionCommands : GovernanceCommandsBase
    {
        [Command]
        [Summary("Starts the amendment process.")]
        [Priority(0)]
        public async Task Draft([Summary("The short name.")] string shortName)
        {
            await CreateSuggestionChannel(SuggestionType.Draft, shortName, Context.User,
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

            var suggestionChannel = await CreateSuggestionChannel(SuggestionType.Draft, shortName, message.Author,
                async channel =>
                {
                    await channel.SendMessageAsync(
                        $"Suggestion upgraded by {MentionUtils.MentionUser(Context.User.Id)}.");
                    return await ConfirmSuggestionFromUserAsync(channel, message.Author, message.Content);
                });

            if (suggestionChannel != null)
            {
                await GovernanceSubscriptionFeed.OnUpgradeAsync(Context.Client, suggestionChannel.Id);
            }
        }

        private static string ToHumanCounter(int number, string unit)
        {
            return $"{number} {unit}{(number == 1 ? "" : "s")}";
        }

        [Command("finalize")]
        [Summary("Finalizes a suggestion.")]
        [Priority(1)]
        public async Task Finalize(bool force = false)
        {
            var channel = Context.Channel;
            var oldType = channel.GetSuggestionChannelType();
            if (!channel.IsSuggestionChannelByName())
            {
                throw new InvalidOperationException("Wrong channel!");
            }

            var user = (IGuildUser)Context.User;
            var vote = await Database.UNSAFE_GetGovernanceVoteAsync(channel.Id);
            var age = DateTimeOffset.UtcNow - channel.CreatedAt;
            var ageInDays = age.TotalDays;
            var ageLeft = TimeSpan.FromTicks((channel.CreatedAt.AddDays(3) - DateTimeOffset.UtcNow).Ticks);


            if (vote == null)
            {
                throw new Exception("Cannot find information about this suggestion in database!");
            }

            if (!user.IsStaff() && ageInDays < 10)
            {
                throw new Exception("Only the staff can finalize suggestions.");
            }

            if (oldType == SuggestionType.RFC)
            {
                throw new Exception("RFCs cannot be finalized.");
            }

            if (oldType == SuggestionType.Vote)
            {
                throw new Exception("Finalized suggestions cannot be finalized.");
            }

            if (ageInDays < 3 && (!force || !user.IsServerOwner()))
            {
                throw new Exception("Suggestion too young. " +
                                    $"{ToHumanCounter(ageLeft.Days, "day")}, {ToHumanCounter(ageLeft.Hours, "hour")}, {ToHumanCounter(ageLeft.Minutes, "minute")} left.");
            }

            var textChannel = (SocketTextChannel)channel;
            var message = (IUserMessage)await Context.Channel.GetMessageAsync(vote.MessageId);

            await textChannel.ConvertSuggestionChannelType(SuggestionType.Vote);

            await message.AddReactionsAsync(new[] {new Emoji("👍"), new Emoji("👎")});
            await UpdateBillboardAsync(Context.Guild, message, channel, vote, SuggestionType.Vote);

            var finalizeMsg = await channel.SendMessageAsync("", embed: new EmbedBuilder()
                .WithTitle(":white_check_mark: Suggestion was finalized. You may now vote!")
                .WithDescription($"[Click here to vote]({message.GetJumpUrl()})")
                .Build());
            await finalizeMsg.PinAsync();

            await GovernanceSubscriptionFeed.OnFinalizeAsync(Context.Client, message.Channel.Id);
        }
    }
}