using System.Collections.Generic;
using System.Threading.Tasks;

namespace ForumCrawler.Helpers
{
    public static class AsyncEnumerableExtensions
    {
        public static async Task<List<T>> ToList<T>(this IAsyncEnumerable<T> asyncEnumerable)
        {
            var list = new List<T>();

            using (var enumerator = asyncEnumerable.GetEnumerator())
            {
                while (await enumerator.MoveNext())
                {
                    list.Add(enumerator.Current);
                }
            }

            return list;
        }
    }
}