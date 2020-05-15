using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace ForumCrawler
{
    public class MuteCommands : ModuleBase<SocketCommandContext>
    {
        private string GetUnmentionedUser(ulong userId)
        {
            var user = Context.Client.GetGuild(DiscordSettings.GuildId).GetUser(userId);
            return user.Username.DiscordEscape() + "#" + user.Discriminator;
        }

        public static Task<Mute> MuteUser(IUser issuer, IUser user, DateTimeOffset expiry, string reason, bool shorten,
            bool sameAuthorShorten)
        {
            return MuteWatcher.MuteUser(
                new Mute
                {
                    UserId = user.Id,
                    IssuerId = issuer.Id,
                    ExpiryDate = expiry.UtcDateTime,
                    IssueDate = DateTime.UtcNow
                }, reason, shorten, sameAuthorShorten);
        }

        public async Task UnmuteUser(IUser user, bool force)
        {
            var mute = await Database.UNSAFE_GetMute(user.Id);
            if (mute == null)
            {
                throw new Exception("User is currently not muted.");
            }

            if (mute.IssuerId != Context.User.Id && !force)
            {
                throw new Exception("Mute can only be removed by its issuer, " +
                                    MentionUtils.MentionUser(mute.IssuerId) + ".");
            }

            var state = WarningState.FromDatabase(await Database.UNSAFE_GetWarningsAsync(user.Id));
            if (!state.MutedUntil.HasValue)
            {
                await MuteWatcher.UnmuteUser(user.Id, null);
                await ReplyAsync($"{MentionUtils.MentionUser(user.Id)} unmuted.");
            }
            else if (state.MutedUntil.Value < mute.ExpiryDate)
            {
                var res = await MuteUser(Context.Client.CurrentUser, user, state.MutedUntil.Value, "Mute shortened.",
                    true, false);
                await ReplyAsync($"{MentionUtils.MentionUser(user.Id)} muted until {res.ExpiryDate}. " +
                                 "A reduction of the mute duration beyond this point is not possible due to an active auto-mute from the warnings system.");
            }
            else
            {
                await ReplyAsync($"{MentionUtils.MentionUser(user.Id)} muted until {mute.ExpiryDate}. " +
                                 "A reduction of the mute duration beyond this point is not possible due to an active auto-mute from the warnings system.");
            }
        }

        [Command("mute")]
        [RequireRole(DiscordSettings.DiscordStaff)]
        public async Task Mute(IGuildUser user, TimeSpan duration, [Remainder] string reason)
        {
            var mute = await Database.UNSAFE_GetMute(user.Id);
            if (mute != null)
            {
                throw new Exception("User is already muted.");
            }

            if (user.IsStaff())
            {
                throw new Exception("User is staff.");
            }

            var res = await MuteUser(Context.User, user, DateTimeOffset.UtcNow + duration, reason, false, false);
            await ReplyAsync($"{MentionUtils.MentionUser(user.Id)} muted until {res.ExpiryDate}.");
        }

        [Command("force unmute")]
        [RequireRole(DiscordSettings.DiscordStaff)]
        public async Task ForceUnmute(IUser user)
        {
            await UnmuteUser(user, true);
        }

        [Command("unmute")]
        [RequireRole(DiscordSettings.DiscordStaff)]
        public async Task Unmute(IUser user)
        {
            await UnmuteUser(user, false);
        }

        [Command("mute status")]
        [Alias("mute stats")]
        [RequireChannel(DiscordSettings.BotCommandsChannel)]
        public async Task MuteState(IUser user = null)
        {
            if (user == null)
            {
                user = Context.User;
            }

            var mute = await Database.UNSAFE_GetMute(user.Id);
            await ReplyAsync($"{GetUnmentionedUser(user.Id)} muted until {mute.ExpiryDate} UTC.");
        }
    }
}