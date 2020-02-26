using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                while (await enumerator.MoveNext().ConfigureAwait(false))
                {
					list.Add(enumerator.Current);
				}
			}

			return list;
		}
	}
}
