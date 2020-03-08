namespace ForumCrawler
{
    // Used to prevent exceptions when casting to/from u/longs for the EF db.
    public static class UlongLongExtensions
    {
        public static long ToLong(this ulong @ulong)
        {
            unchecked
            {
                return (long)@ulong;
            }
        }

        public static ulong ToULong(this long @long)
        {
            unchecked
            {
                return (ulong)@long;
            }
        }
    }
}