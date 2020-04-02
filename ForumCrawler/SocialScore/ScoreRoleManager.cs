using Discord;
using Discord.WebSocket;

using ForumCrawler;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordSocialScore
{
    public static class ScoreRoleManager
    {
        public const char RolePrefix = '\u17B5';

        private static readonly List<(string, (Color, Color))> SpecialRoles = new List<(string, (Color, Color))>
        {
            ("Nitro Booster", (new Color(116, 0, 127), new Color(244, 127, 255))),
            ("Staff", (Color.DarkTeal, Color.Teal)),
            ("EE Staff", (new Color(76, 161, 175), new Color(166, 209, 215)))
        };

        private static Color GetGradient(double step, Color from, Color target)
        {
            var min = from;
            var max = target;
            var location = Math.Max(0, Math.Min(1, (step - 0.5) / 4.5));

            var rAverage = min.R + (int)((max.R - min.R) * location);
            var gAverage = min.G + (int)((max.G - min.G) * location);
            var bAverage = min.B + (int)((max.B - min.B) * location);
            return new Color(rAverage, gAverage, bAverage);
        }

        public static async Task<IRole> GetScoreRoleForUserAsync(RoleCache cache, SocketGuildUser user, ScoreData scoreData)
        {
            var specialRole = SpecialRoles.Find(kv => user.Roles.Any(r => r.Name == kv.Item1));
            return specialRole.Item1 == null
                ? await GetScoreRole(cache, scoreData)
                : await GetSpecialScoreRole(cache, specialRole.Item1, specialRole.Item2, scoreData);
        }

        private static async Task<IRole> GetScoreRole(RoleCache guild, ScoreData scoreData)
        {
            var color = GetGradient(scoreData.ScoreLevel, new Color(215, 109, 119), Color.Gold);
            return await guild.CreateOrUpdateRoleAsync(scoreData.ShortScoreString, permissions: new GuildPermissions(), color: color);
        }

        private static async Task<IRole> GetSpecialScoreRole(RoleCache guild, string specialRole, (Color, Color) specialColor, ScoreData scoreData)
        {
            var color = GetGradient(scoreData.ScoreLevel, specialColor.Item1, specialColor.Item2);
            return await guild.CreateOrUpdateRoleAsync($"{scoreData.ShortScoreString} ({specialRole})", permissions: new GuildPermissions(), color: color);
        }

        public static async Task<IRole> GetClassRole(RoleCache guild, ScoreData scoreData) => await guild.CreateOrUpdateRoleAsync($"Class {scoreData.Class} members", permissions: new GuildPermissions(), isHoisted: true);

        public static async Task OrderRolesAsync(SocketGuild guild)
        {
            var sortedRoles = guild.Roles.GetBotRoles()
                .OrderBy(r => r.Name)
                .Select((role, index) => new { role, index })
                .Where(item => item.role.Position != item.index)
                .Select(item => new ReorderRoleProperties(item.role.Id, item.index));

            await guild.ReorderRolesAsync(sortedRoles);
        }

        public static async Task DeleteRolesAsync(RoleCache cache)
        {
            foreach (var role in cache.Guild.Roles.GetBotRoles())
            {
                if (role.Name.StartsWith(RolePrefix.ToString()) && !role.Members.Any())
                {
                    await role.DeleteAsync();
                }
            }
        }

        public static string GetBaseNick(this IGuildUser user) => Engine.GetBaseNick(user.Username, user.Nickname);

        public static IEnumerable<SocketRole> GetBotRoles(this IEnumerable<SocketRole> roles) => roles.Where(r => r.Name.StartsWith(RolePrefix.ToString()));
    }
}
