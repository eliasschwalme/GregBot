using System;
using System.Linq;

namespace ForumCrawler
{
    public static class StringUtils
    {
        static readonly char[] _escapeCharacters = { '\\', '/', ':', '[', '_', '*', '~', '`' };
        public static string DiscordEscape(this string text)
        {
            var escapeChars = _escapeCharacters.Concat(new[] { '<' });
            return string.Concat(text.Select(c => escapeChars.Contains(c) ? "\\" + c : c.ToString()));
        }

        public static string DiscordEscapeWithoutMentions(this string text)
        {
            return string.Concat(text.Select(c => _escapeCharacters.Contains(c) ? "\\" + c : c.ToString()));
        }

        public static string TruncateAtWord(this string text, int maxCharacters, string trailingStringIfTextCut = " ...")
        {
            if (text == null || (text = text.Trim()).Length <= maxCharacters) return text;

            var trailLength = trailingStringIfTextCut.StartsWith("&") ? 1
                : trailingStringIfTextCut.Length;
            maxCharacters = maxCharacters - trailLength >= 0 ? maxCharacters - trailLength
                : 0;
            var pos = text.LastIndexOf(" ", maxCharacters, StringComparison.Ordinal);
            if (pos >= 0) return text.Substring(0, pos) + trailingStringIfTextCut;

            return string.Empty;
        }
    }
}