using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace ForumCrawler
{
    public class GupAliasBehaviorOverrides<T>
    {
        // this may be null
        public Dictionary<string, ulong> GupAliases { get; set; }

        public Func<IUser, T> UserToT { get; set; }

        public Func<ulong, IGuild, ValueTask<T>> UserIdToT { get; set; }
    }

    internal class MainGuildUserTypeReader<T> : TypeReader where T : class
    {
		private readonly GupAliasBehaviorOverrides<T> _behaviorOverrides;

		public MainGuildUserTypeReader(GupAliasBehaviorOverrides<T> behaviorOverrides = null)
		{
            _behaviorOverrides = behaviorOverrides ?? new GupAliasBehaviorOverrides<T>
            {
                UserToT = (user) => user as T,
                UserIdToT = async (userId, guild) => await guild.GetUserAsync(userId, CacheMode.CacheOnly).ConfigureAwait(false) as T
            };
		}

        public override async Task<TypeReaderResult> ReadAsync(ICommandContext context, string input,
            IServiceProvider services)
        {
            var results = new Dictionary<ulong, TypeReaderValue>();
            var mainGuild = await context.Client.GetGuildAsync(DiscordSettings.GuildId, CacheMode.CacheOnly)
                .ConfigureAwait(false);
            var channelUsers = context.Channel.GetUsersAsync(CacheMode.CacheOnly).Flatten(); // it's better
            var mainGuildUsers = await mainGuild.GetUsersAsync(CacheMode.CacheOnly).ConfigureAwait(false);

            // By gup alias (1.1)
            if (_behaviorOverrides.GupAliases != null
                && _behaviorOverrides.GupAliases.TryGetValue(input, out var userId))
            {
                AddResult(results, await _behaviorOverrides.UserIdToT(userId, mainGuild).ConfigureAwait(false), 1.10f);
			}

            //By Mention (1.0)
            if (MentionUtils.TryParseUser(input, out var id))
            {
                AddResult(results, await _behaviorOverrides.UserIdToT(id, mainGuild).ConfigureAwait(false), 1.00f);
            }

            //By Id (0.9)
            if (ulong.TryParse(input, NumberStyles.None, CultureInfo.InvariantCulture, out id))
            {
                AddResult(results, await _behaviorOverrides.UserIdToT(id, mainGuild).ConfigureAwait(false), 0.90f);
            }

            //By Username + Discriminator (0.7-0.85)
            var index = input.LastIndexOf('#');
            if (index >= 0)
            {
                var username = input.Substring(0, index);
                if (ushort.TryParse(input.Substring(index + 1), out var discriminator))
                {
                    var channelUser = await channelUsers.FirstOrDefaultAsync(x =>
                        x.DiscriminatorValue == discriminator &&
                        string.Equals(username, x.Username, StringComparison.OrdinalIgnoreCase)).ConfigureAwait(false);
                    AddResult(results, _behaviorOverrides.UserToT(channelUser),
                        channelUser?.Username == username ? 0.85f : 0.75f);

                    var guildUser = mainGuildUsers.FirstOrDefault(x => x.DiscriminatorValue == discriminator &&
                                                                       string.Equals(username, x.Username,
                                                                           StringComparison.OrdinalIgnoreCase));
                    AddResult(results, _behaviorOverrides.UserToT(guildUser),
                        guildUser?.Username == username ? 0.80f : 0.70f);
                }
            }

            //By Username (0.5-0.6)
            {
                await channelUsers
                    .Where(x => string.Equals(input, x.Username, StringComparison.OrdinalIgnoreCase))
                    .ForEachAsync(channelUser =>
                        AddResult(results, _behaviorOverrides.UserToT(channelUser),
                            channelUser.Username == input ? 0.65f : 0.55f))
                    .ConfigureAwait(false);

                foreach (var guildUser in mainGuildUsers.Where(x =>
                    string.Equals(input, x.Username, StringComparison.OrdinalIgnoreCase)))
                {
                    AddResult(results, _behaviorOverrides.UserToT(guildUser),
                        guildUser.Username == input ? 0.60f : 0.50f);
                }
            }

            //By Nickname (0.5-0.6)
            {
                await channelUsers
                    .Where(x => string.Equals(input, (x as IGuildUser)?.Nickname, StringComparison.OrdinalIgnoreCase))
                    .ForEachAsync(channelUser => AddResult(results,
                        _behaviorOverrides.UserToT(channelUser),
                        (channelUser as IGuildUser).Nickname == input ? 0.65f : 0.55f))
                    .ConfigureAwait(false);

                foreach (var guildUser in mainGuildUsers.Where(x =>
                    string.Equals(input, x.Nickname, StringComparison.OrdinalIgnoreCase)))
                {
                    AddResult(results, _behaviorOverrides.UserToT(guildUser),
                        guildUser.Nickname == input ? 0.60f : 0.50f);
                }
            }

            //By StartsWith Username (0.3-0.45)
            {
                await channelUsers
                    .Where(x => x.Username.StartsWith(input, StringComparison.OrdinalIgnoreCase))
                    .ForEachAsync(channelUser => AddResult(results,
                        _behaviorOverrides.UserToT(channelUser),
                        channelUser.Username.StartsWith(input) ? 0.45f : 0.35f))
                    .ConfigureAwait(false);

                foreach (var guildUser in mainGuildUsers.Where(x =>
                    x.Username.StartsWith(input, StringComparison.OrdinalIgnoreCase)))
                {
                    AddResult(results, _behaviorOverrides.UserToT(guildUser),
                        guildUser.Username.StartsWith(input) ? 0.40f : 0.30f);
                }
            }

            //By StartsWith Nickname (0.3-0.45)
            {
                await channelUsers
                    .Where(x => (x as IGuildUser)?.Nickname?.StartsWith(input, StringComparison.OrdinalIgnoreCase) ??
                                false)
                    .ForEachAsync(channelUser => AddResult(results,
                        _behaviorOverrides.UserToT(channelUser),
                        (channelUser as IGuildUser).Nickname.StartsWith(input) ? 0.45f : 0.35f))
                    .ConfigureAwait(false);

                foreach (var guildUser in mainGuildUsers.Where(x =>
                    x?.Nickname?.StartsWith(input, StringComparison.OrdinalIgnoreCase) ?? false))
                {
                    AddResult(results, _behaviorOverrides.UserToT(guildUser),
                        guildUser.Nickname.StartsWith(input) ? 0.40f : 0.30f);
                }
            }

            var topBracket = results.Values.GroupBy(i => i.Score).OrderByDescending(g => g.Key).FirstOrDefault();

            if (topBracket?.Any() == true)
            {
                return TypeReaderResult.FromSuccess(topBracket.ToImmutableArray());
            }

            return TypeReaderResult.FromError(CommandError.ObjectNotFound, "User not found.");
        }

        private void AddResult(Dictionary<ulong, TypeReaderValue> results, T user, float score)
        {
            // aaaaaaaaaaa
            IUser discordUser = null;

            if (user is IGupUser gupUser)
            {
                discordUser = gupUser.ActualUser;
			}

            if (user is IUser userAsUser)
            {
                discordUser = userAsUser;
			}

            if (discordUser != null)
            {
                if (discordUser != null && (!results.TryGetValue(discordUser.Id, out var curr) || curr.Score < score))
                {
                    results[discordUser.Id] = new TypeReaderValue(user, score);
                }
            }
        }
    }
}