using System;
using System.Threading.Tasks;
using Discord.Commands;

namespace ForumCrawler
{
    internal class IgnoreCaseEnumTypeReader<TEnum> : TypeReader where TEnum : struct
    {
        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input,
            IServiceProvider services)
        {
            TEnum result;
            if (Enum.TryParse(input, true, out result))
            {
                return Task.FromResult(TypeReaderResult.FromSuccess(result));
            }

            return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed,
                $"Value is not a {typeof(TEnum).Name}."));
        }
    }
}