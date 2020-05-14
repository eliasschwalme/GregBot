using Discord;
using Discord.WebSocket;

using System.Threading.Tasks;

namespace ForumCrawler
{
    internal static class GovernanceVoteWatcher
    {
        public static void Bind(DiscordSocketClient client)
        {
            client.ReactionAdded += Client_ReactionAdded;
            client.ReactionRemoved += Client_ReactionAdded;
            client.MessageReceived += Client_MessageReceived;
        }

        private static async Task Client_MessageReceived(SocketMessage arg)
        {
            if (arg is SocketSystemMessage msg)
            {
                if (msg.Type == MessageType.ChannelPinnedMessage && msg.Author.IsBot)
                {
                    await msg.DeleteAsync();
                }
            }
        }

        private static async Task Client_ReactionAdded(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (!channel.IsSuggestionChannelByName()) return;

            if (!(reaction.User.GetValueOrDefault() is IGuildUser guildUser)) return;

            var msg = await message.GetOrDownloadAsync();
            if (channel.GetSuggestionChannelType() != SuggestionType.Vote && msg.Author.IsBot && !guildUser.IsBot)
            {
                await msg.RemoveAllReactionsAsync();
                return;
            }

            var governanceVote = await Database.UNSAFE_GetGovernanceVoteAsync(channel.Id);
            if (governanceVote == null) return;
            if (governanceVote.MessageId != message.Id) return;

            await GovernanceCommandsBase.UpdateBillboardAsync(guildUser.Guild, msg, channel, governanceVote, channel.GetSuggestionChannelType());
        }
    }
}