using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace ForumCrawler
{
    class VoiceChat
    {
        public static void Bind(DiscordSocketClient client)
        {
            client.UserVoiceStateUpdated += Client_UserVoiceStateUpdated;
        }

        private static async Task Client_UserVoiceStateUpdated(SocketUser user, SocketVoiceState old, SocketVoiceState state)
        {
            if (old.VoiceChannel?.Id != state.VoiceChannel?.Id)
            {
                if (old.VoiceChannel != null)
                await UpdatePermissionsAsync(user, old.VoiceChannel,false);
                if (state.VoiceChannel != null)
                    await UpdatePermissionsAsync(user, state.VoiceChannel, true);
            }
        }

        private static Task UpdatePermissionsAsync(SocketUser user, SocketVoiceChannel channel, bool allow)
        {
            var textChannel = channel.Guild.TextChannels.FirstOrDefault(t => t.Topic?.EndsWith("for:" + channel.Name) == true);
            if (textChannel == null) return Task.Delay(0);

            if (allow)
            {
                var overwrite = textChannel.GetPermissionOverwrite(user) ?? new OverwritePermissions();
                overwrite = overwrite.Modify(viewChannel: PermValue.Allow);
                return textChannel.AddPermissionOverwriteAsync(user, overwrite);
            }
            return textChannel.RemovePermissionOverwriteAsync(user);
        }
    }
}