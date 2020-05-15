namespace ForumCrawler
{
    public interface ICoronaEntry
    {
        int Cases { get; }
        int CaseIncrease { get; }
        int Deaths { get; }
        int Recovered { get; }
        int Serious { get; }
        int Active { get; }
    }
}