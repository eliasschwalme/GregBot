using System;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.WebSocket;
using ForumCrawler.Helpers;

namespace ForumCrawler
{
    public static class MuteWatcher
    {
        private static event Func<Mute, string, Task> OnMute;

        private static event Func<ulong, Task> OnUnmute;

        public static void Bind(DiscordSocketClient client)
        {
            OnMute += (a, b) => MuteWatcher_OnMute(client, a, b);
            OnUnmute += mute => MuteWatcher_OnUnmute(client, mute);
            client.AddOnFirstReady(() => Client_Ready(client));
            client.UserJoined += VerifyMute;
            client.GuildMemberUpdated += (oldUser, newUser) => Client_GuildMemberUpdated(client, oldUser, newUser);
        }

        private static async Task VerifyMute(SocketGuildUser user)
        {
            var mute = await Database.UNSAFE_GetMute(user.Id);
            if (mute != null && mute.ExpiryDate > DateTimeOffset.UtcNow)
            {
                await OnMute(mute, "Mute retention after rejoin.");
            }
        }

        private static async Task Client_GuildMemberUpdated(DiscordSocketClient client, SocketGuildUser oldUser,
            SocketGuildUser newUser)
        {
            var role = client.GetGuild(DiscordSettings.GuildId).GetRole(DiscordSettings.MutedRole);
            if (oldUser.Roles.Contains(role) && !newUser.Roles.Contains(role))
            {
                await VerifyMute(newUser);
            }
        }

        private static Task Client_Ready(DiscordSocketClient client)
        {
            var timer = new Timer(TimeSpan.FromMinutes(1).TotalMilliseconds);
            timer.Elapsed += (o, e) => OnUpdate(client);
            timer.Start();
            return Task.CompletedTask;
        }

        private static async Task MuteWatcher_OnMute(DiscordSocketClient client, Mute mute, string reason)
        {
            var user = client.GetGuild(DiscordSettings.GuildId).GetUser(mute.UserId);
            await user.AddRoleAsync(client.GetGuild(DiscordSettings.GuildId).GetRole(DiscordSettings.MutedRole));
            await user.SendMessageAsync(
                $"You were muted until {mute.ExpiryDate} UTC by {MentionUtils.MentionUser(mute.IssuerId)}. Reason: {reason}");
        }

        private static async Task MuteWatcher_OnUnmute(DiscordSocketClient client, ulong userId)
        {
            var role = client.GetGuild(DiscordSettings.GuildId).GetRole(DiscordSettings.MutedRole);
            var user = client.GetGuild(DiscordSettings.GuildId).GetUser(userId);
            if (user?.Roles?.Contains(role) == true)
            {
                await user.RemoveRoleAsync(role);
            }
        }

        private static async void OnUpdate(DiscordSocketClient client)
        {
            var timestamp = DateTimeOffset.UtcNow;
            var mutes = await Database.UNSAFE_GetAllExpiredMutes(timestamp);
            foreach (var mute in mutes)
            {
                await OnUnmute(mute.UserId);
            }

            await Database.UNSAFE_RemoveAllExpiredMutes(timestamp);
        }

        public static async Task<Mute> MuteUser(Mute mute, string reason, bool shorten, bool sameAuthorShorten)
        {
            if (mute.ExpiryDate <= DateTimeOffset.UtcNow)
            {
                return null;
            }

            var lastMute = await Database.UNSAFE_GetMute(mute.UserId);
            var shorts = mute.ExpiryDate <= lastMute?.ExpiryDate;
            var sameAuthor = lastMute?.IssuerId == mute.IssuerId;
            if ((shorts && !shorten) || (shorts && !sameAuthor && sameAuthorShorten))
            {
                return lastMute;
            }

            await Database.UNSAFE_AddOrUpdateMuteAsync(mute);
            await OnMute(mute, reason);
            return mute;
        }

        public static async Task UnmuteUser(ulong userId, ulong? issuerId)
        {
            if (issuerId != null)
            {
                var lastMute = await Database.UNSAFE_GetMute(userId);
                if (lastMute == null || lastMute.IssuerId != issuerId)
                {
                    return;
                }
            }

            await Database.UNSAFE_RemoveMute(userId);
            await OnUnmute(userId);
        }
    }
}