using DiffMatchPatch;

using Discord;
using Discord.WebSocket;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ForumCrawler
{
    internal static class EditWatcher
    {
        public static void Bind(DiscordSocketClient client)
        {
            client.MessageUpdated += (o, n, c) => Client_MessageUpdated(client, o, n, c);
            client.MessageDeleted += (o, c) => Client_MessageDeleted(client, o, c);
        }

        private static async Task Client_MessageUpdated(DiscordSocketClient client, Cacheable<IMessage, ulong> oldCache, SocketMessage newMessage, ISocketMessageChannel channel)
        {
            if (newMessage.Author.IsBot) return;
            if (newMessage.Content == null) return;

            if (newMessage is IUserMessage userMessage)
            {
                var oldMessage = ToMessage(oldCache);

                var oldContent = oldMessage?.Content ?? string.Empty;
                var newContent = userMessage.Resolve();
                if (oldContent == newContent) return;

                var diffMatchPatch = new diff_match_patch();
                var diffs = diffMatchPatch.diff_main(oldContent, newContent);
                diffMatchPatch.diff_cleanupSemantic(diffs);

                var md = ToMarkdown(diffs);
                await PostEmbedAsync(client, "edited", new Color(0xF0E68C), userMessage.Author.Id, userMessage.Author, channel, md, oldMessage?.Attachment, userMessage.Id);
            }
        }

        private static async Task Client_MessageDeleted(DiscordSocketClient client, Cacheable<IMessage, ulong> oldCache, ISocketMessageChannel channel)
        {
            var oldMessage = ToMessage(oldCache);
            if (oldMessage == null) return;

            var oldUser = client.GetUser(oldMessage.UserId);
            if (oldUser?.IsBot == true) return;

            var oldContent = oldMessage.Content.DiscordEscapeWithoutMentions();

            await PostEmbedAsync(client, "deleted", new Color(0xD62D20), oldMessage.UserId, oldUser, channel, oldContent, oldMessage.Attachment, oldMessage.MessageId);
        }

        private static Message ToMessage(Cacheable<IMessage, ulong> cached)
        {
            if (cached.HasValue)
            {
                if (cached.Value is IUserMessage userMessage)
                {
                    return ToMessage(userMessage);
                }
            }

            return null;
        }

        private static Message ToMessage(IUserMessage message)
        {
            var attachment = message.Attachments.FirstOrDefault();
            var attachUrl = attachment?.Url;
            if (attachUrl != null)
            {
                var ext = Path.GetExtension(attachUrl);
                if (ext == ".png" || ext == ".gif" || ext == ".jpg" || ext == ".jpeg")
                {
                    attachUrl = attachment.ProxyUrl;
                }
            }

            return new Message
            {
                MessageId = message.Id,
                UserId = message.Author.Id,
                Content = message.Resolve(),
                Attachment = attachUrl
            };
        }

        public static Embed GetEditEmbed(IUser author, string title, string oldContent, string newContent)
        {
            if (oldContent == newContent) return null;

            var diffMatchPatch = new diff_match_patch();
            var diffs = diffMatchPatch.diff_main(oldContent, newContent);
            diffMatchPatch.diff_cleanupSemantic(diffs);

            var md = ToMarkdown(diffs);
            return GetEmbed(title, new Color(0xF0E68C), author.Id, author, null, md, null, null);
        }

        private static async Task PostEmbedAsync(DiscordSocketClient client, string title, Color color, ulong userId, IUser user, IMessageChannel channel, string diff, string attachment, ulong messageId)
        {
            var embed = GetEmbed(title, color, userId, user, channel, diff, attachment, messageId);

            await client
                .GetGuild(DiscordSettings.GuildId)
                .GetTextChannel(DiscordSettings.LogsChannel)
                .SendMessageAsync(string.Empty, embed: embed);
        }

        private static Embed GetEmbed(string title, Color color, ulong userId, IUser user, IMessageChannel channel, string diff, string attachment, ulong? messageId)
        {
            var addId = messageId == null ? "" : " (" + messageId + ")";

            var builder = new EmbedBuilder()
                .WithAuthor(author => author
                    .WithIconUrl(user?.GetAvatarUrlOrDefault())
                    .WithName(user?.Username + "#" + (user?.Discriminator ?? "@" + userId.ToString()) + " " + title + ":"))
                .WithColor(color)
                .WithDescription(diff);
            if (messageId.HasValue)
            {
                builder.WithTimestamp(SnowflakeUtils.FromSnowflake(messageId.Value));
            }
            if (channel != null)
            {
                builder.WithFooter(footer => footer
                    .WithText($"In #{channel}" + addId));
            }
            if (attachment != null)
            {
                var host = new Uri(attachment).Host;
                if (host == "images.discordapp.net")
                {
                    builder.WithImageUrl(attachment);
                }
                else
                {
                    builder.AddField(field => field
                        .WithName("Attachment")
                        .WithValue(attachment));
                }
            }

            return builder.Build();
        }

        private static string ToMarkdown(List<Diff> diffs)
        {
            var md = new StringBuilder();
            foreach (var diff in diffs)
            {
                var text = diff.text.DiscordEscapeWithoutMentions();
                switch (diff.operation)
                {
                    case Operation.INSERT:
                        md.Append("__**").Append(text).Append("**__");
                        break;

                    case Operation.DELETE:
                        md.Append("**~~").Append(text).Append("~~**");
                        break;

                    case Operation.EQUAL:
                        md.Append(text);
                        break;
                }
            }
            return md.ToString();
        }
    }
}