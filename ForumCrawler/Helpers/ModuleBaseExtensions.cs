using Discord;
using Discord.Commands;

using System.Threading.Tasks;

namespace ForumCrawler
{
    internal static class ModuleBaseExtensions
    {
        public static async Task<IUserMessage> ReplyAsync<T>(this ModuleBase<T> moduleBase, string emoji, string message, bool isTTS = false, Embed embed = null, RequestOptions options = null) where T : class, ICommandContext
        {
            return await moduleBase.Context.Channel.SendMessageAsync(emoji, message, isTTS, embed, options)
                ;
        }

        public static async Task<IUserMessage> ReplyErrorAsync<T>(this ModuleBase<T> moduleBase, string message, bool isTTS = false, Embed embed = null, RequestOptions options = null) where T : class, ICommandContext
        {
            return await moduleBase.Context.Channel.SendErrorAsync(message, isTTS, embed, options)
                ;
        }

        public static async Task<IUserMessage> SendMessageAsync(this IMessageChannel channel, string emoji, string message, bool isTTS = false, Embed embed = null, RequestOptions options = null) => await channel.SendMessageAsync(":" + emoji + ": | " + message, isTTS, embed, options);

        public static async Task<IUserMessage> SendErrorAsync(this IMessageChannel channel, string message, bool isTTS = false, Embed embed = null, RequestOptions options = null)
        {
            var res = await channel.SendMessageAsync("frowning", message, isTTS, embed, options);
            var bg = Task.Run(async () =>
            {
                await Task.Delay(30000);
                await res.DeleteAsync();
            });
            return res;
        }
    }
}