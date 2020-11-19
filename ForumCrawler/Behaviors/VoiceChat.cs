using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace ForumCrawler
{
    internal static class VoiceChat
    {
        public static void Bind(DiscordSocketClient client)
        {
            client.UserVoiceStateUpdated += Client_UserVoiceStateUpdated;
        }

        private static async Task Client_UserVoiceStateUpdated(SocketUser user, SocketVoiceState old,
            SocketVoiceState state)
        {
            if (old.VoiceChannel?.Id != state.VoiceChannel?.Id)
            {
                if (old.VoiceChannel != null)
                {
                    await UpdatePermissionsAsync(user, old.VoiceChannel, false);
                }

                if (state.VoiceChannel != null)
                {
                    await UpdatePermissionsAsync(user, state.VoiceChannel, true);
                }
            }
        }

        private static async Task UpdatePermissionsAsync(SocketUser user, SocketVoiceChannel channel, bool allow)
        {
            var textChannel =
                channel.Guild.TextChannels.FirstOrDefault(t => t.Topic?.EndsWith("for:" + channel.Name) == true);
            if (textChannel == null)
            {
                return;
            }

            if (allow)
            {
                var overwrite = textChannel.GetPermissionOverwrite(user) ?? new OverwritePermissions();
                overwrite = overwrite.Modify(viewChannel: PermValue.Allow);
                await textChannel.AddPermissionOverwriteAsync(user, overwrite);

                await channel.Guild.GetUser(user.Id).AddRoleAsync(channel.Guild.GetRole(DiscordSettings.VoiceRole));
                return;
            }

            await textChannel.RemovePermissionOverwriteAsync(user);

            await channel.Guild.GetUser(user.Id).RemoveRoleAsync(channel.Guild.GetRole(DiscordSettings.VoiceRole));
            return;
        }
    }
}