using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace ForumCrawler
{
    public class RoleCommands : ModuleBase<SocketCommandContext>
    {
        [Command("halt")]
        [RequireRole(DiscordSettings.DiscordStaff)]
        public async Task Halt()
        {
            await ReplyAsync("Goodbye.");
            Environment.Exit(0);
        }

        [Command("prunegregroles")]
        [RequireRole(DiscordSettings.DiscordServerOwner)]
        public async Task RoleTest()
        {
            foreach (var role in Context.Guild.Roles.Where(r => r.Name[0] == ScoreRoleManager.RolePrefix))
            {
                await role.DeleteAsync();
            }

            await ReplyAsync("Pruned greg roles.");
        }

        [Command("rolecleanup")]
        [RequireRole(DiscordSettings.DiscordStaff)]
        public async Task RoleCleanup()
        {
            foreach (var r in Context.Guild.Roles.Where(r => r.Name == "new role"))
            {
                await r.DeleteAsync();
            }

            await ReplyAsync("Role cleanup complete.");
        }

        [Command("nick")]
        [RequireChannel(DiscordSettings.BotCommandsChannel)]
        [Priority(0)]
        public async Task ChangeNickname([Remainder] string nick)
        {
            var user = (IGuildUser)Context.User;
            var score = await Score.GetScoreDataAsync(Context.Client, user.Id);
            if (score.ScoreLevel < 4)
            {
                throw new Exception("You must be a class of 4 or higher to change your nick.");
            }

            if (!DadbotWatcher.CanChangeNick(user.Id))
            {
                var target = Format.Sanitize(user.Username);
                var duration = DadbotWatcher.GetTimeLeft(user.Id).ToHumanReadableString();
                throw new Exception($"Sorry {target}, but you can't change your nickname for another {duration} (due to Dadbot).");
			}

            await user.ModifyAsync(u => u.Nickname = nick);
            await ReplyAsync("Your nickname was updated.");
        }

        [Command("nick reset")]
        [RequireChannel(DiscordSettings.BotCommandsChannel)]
        [Priority(1)]
        public async Task ResetNickname()
        {
            var user = (IGuildUser)Context.User;
            await user.ModifyAsync(u => u.Nickname = null);
            await ReplyAsync("Your nickname was reset.");
        }

        [Group("mod")]
        [Summary("These are moderator-specific commands.")]
        [RequireRole(DiscordSettings.DiscordStaff)]
        public class ModeratorCommands : ModuleBase<SocketCommandContext>
        {
            [Group("18+")]
            [Alias("18+", "18")]
            [Summary("Give 18+ role to people")]
            public class EighteenPlusRole : ModuleBase<SocketCommandContext>
            {
                [Command("give")]
                public async Task GiveRole(IGuildUser user)
                {
                    if (user.IsStaff())
                    {
                        throw new Exception("You may not give the role to staff.");
                    }

                    if (!TryGetRole(user.Guild, out var muted))
                    {
                        throw new Exception("Unable to get role from guild user.");
                    }

                    await user.AddRoleAsync(muted,
                        new RequestOptions
                        {
                            AuditLogReason = "Manual 18+ role given by " + Context.User.Username.DiscordEscape()
                        });

                    await Context.Message.DeleteAsync();
                }

                [Command("remove")]
                [Alias("rm")]
                public async Task RemoveRole(IGuildUser user)
                {
                    // intentionally allowing removing the muted role from staff

                    if (!TryGetRole(user.Guild, out var muted))
                    {
                        throw new Exception("Unable to get role from guild user.");
                    }

                    await user.RemoveRoleAsync(muted,
                        new RequestOptions
                        {
                            AuditLogReason = "Manual 18+ role removed by " + Context.User.Username.DiscordEscape()
                        });

                    await Context.Message.DeleteAsync();
                }

                private static bool TryGetRole(IGuild guild, out IRole muted)
                {
                    var query = guild.Roles
                        .Where(role => role.Id == DiscordSettings.EighteenRole)
                        .ToList();

                    if (query.Count == 0)
                    {
                        muted = default;
                        return false;
                    }

                    muted = query[0];
                    return true;
                }
            }
        }
    }
}