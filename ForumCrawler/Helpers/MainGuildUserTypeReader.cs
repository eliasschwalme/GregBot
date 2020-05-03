using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ForumCrawler.Helpers
{
    interface IMainGuildUser
    {

    }

    class MainGuildUserTypeReader<T> : TypeReader where T : class, IUser
    {
        public override async Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            var results = new Dictionary<ulong, TypeReaderValue>();
            var mainGuild = await context.Client.GetGuildAsync(DiscordSettings.GuildId, CacheMode.CacheOnly).ConfigureAwait(false);
            IAsyncEnumerable<IUser> channelUsers = context.Channel.GetUsersAsync(CacheMode.CacheOnly).Flatten(); // it's better
            IReadOnlyCollection<IGuildUser> mainGuildUsers = await mainGuild.GetUsersAsync(CacheMode.CacheOnly).ConfigureAwait(false);

            //By Mention (1.0)
            if (MentionUtils.TryParseUser(input, out var id))
            {
                AddResult(results, await context.Guild.GetUserAsync(id, CacheMode.CacheOnly).ConfigureAwait(false) as T, 1.00f);
            }

            //By Id (0.9)
            if (ulong.TryParse(input, NumberStyles.None, CultureInfo.InvariantCulture, out id))
            {
                AddResult(results, await context.Guild.GetUserAsync(id, CacheMode.CacheOnly).ConfigureAwait(false) as T, 0.90f);
            }

            //By Username + Discriminator (0.7-0.85)
            int index = input.LastIndexOf('#');
            if (index >= 0)
            {
                string username = input.Substring(0, index);
                if (ushort.TryParse(input.Substring(index + 1), out ushort discriminator))
                {
                    var channelUser = await channelUsers.FirstOrDefaultAsync(x => x.DiscriminatorValue == discriminator &&
                        string.Equals(username, x.Username, StringComparison.OrdinalIgnoreCase)).ConfigureAwait(false);
                    AddResult(results, channelUser as T, channelUser?.Username == username ? 0.85f : 0.75f);

                    var guildUser = mainGuildUsers.FirstOrDefault(x => x.DiscriminatorValue == discriminator &&
                        string.Equals(username, x.Username, StringComparison.OrdinalIgnoreCase));
                    AddResult(results, guildUser as T, guildUser?.Username == username ? 0.80f : 0.70f);
                }
            }

            //By Username (0.5-0.6)
            {
                await channelUsers
                    .Where(x => string.Equals(input, x.Username, StringComparison.OrdinalIgnoreCase))
                    .ForEachAsync(channelUser => AddResult(results, channelUser as T, channelUser.Username == input ? 0.65f : 0.55f))
                    .ConfigureAwait(false);

                foreach (var guildUser in mainGuildUsers.Where(x => string.Equals(input, x.Username, StringComparison.OrdinalIgnoreCase)))
                    AddResult(results, guildUser as T, guildUser.Username == input ? 0.60f : 0.50f);
            }

            //By Nickname (0.5-0.6)
            {
                await channelUsers
                    .Where(x => string.Equals(input, (x as IGuildUser)?.Nickname, StringComparison.OrdinalIgnoreCase))
                    .ForEachAsync(channelUser => AddResult(results, channelUser as T, (channelUser as IGuildUser).Nickname == input ? 0.65f : 0.55f))
                    .ConfigureAwait(false);

                foreach (var guildUser in mainGuildUsers.Where(x => string.Equals(input, x.Nickname, StringComparison.OrdinalIgnoreCase)))
                    AddResult(results, guildUser as T, guildUser.Nickname == input ? 0.60f : 0.50f);
            }

            //By StartsWith Username (0.3-0.45)
            {
                await channelUsers
                    .Where(x => x.Username.StartsWith(input, StringComparison.OrdinalIgnoreCase))
                    .ForEachAsync(channelUser => AddResult(results, channelUser as T, channelUser.Username.StartsWith(input) ? 0.45f : 0.35f))
                    .ConfigureAwait(false);

                foreach (var guildUser in mainGuildUsers.Where(x => x.Username.StartsWith(input, StringComparison.OrdinalIgnoreCase)))
                    AddResult(results, guildUser as T, guildUser.Username.StartsWith(input) ? 0.40f : 0.30f);
            }

            //By StartsWith Nickname (0.3-0.45)
            {
                await channelUsers
                    .Where(x => (x as IGuildUser)?.Nickname?.StartsWith(input, StringComparison.OrdinalIgnoreCase) ?? false)
                    .ForEachAsync(channelUser => AddResult(results, channelUser as T, (channelUser as IGuildUser).Nickname.StartsWith(input) ? 0.45f : 0.35f))
                    .ConfigureAwait(false);

                foreach (var guildUser in mainGuildUsers.Where(x => (x as IGuildUser)?.Nickname?.StartsWith(input, StringComparison.OrdinalIgnoreCase) ?? false))
                    AddResult(results, guildUser as T, guildUser.Nickname.StartsWith(input) ? 0.40f : 0.30f);
            }


            if (results.Count > 0)
                return TypeReaderResult.FromSuccess(results.Values.ToImmutableArray());
            return TypeReaderResult.FromError(CommandError.ObjectNotFound, "User not found.");
        }

        private void AddResult(Dictionary<ulong, TypeReaderValue> results, T user, float score)
        {
            if (user != null && !results.ContainsKey(user.Id))
                results.Add(user.Id, new TypeReaderValue(user, score));
        }
    }
}