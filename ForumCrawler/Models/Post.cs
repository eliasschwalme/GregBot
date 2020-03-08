public class Post
{
    public Post(string topic, int topicId, string poster, int posterId, string forum, int pnumber, int postId, string time)
    {
        Topic = topic;
        TopicId = topicId;
        Poster = poster;
        PosterId = posterId;
        Forum = forum;
        Pnumber = pnumber;
        PostId = postId;
        Time = time;
    }

    public string Topic { get; }

    public int TopicId { get; }

    public string Poster { get; }

    public int PosterId { get; }

    public string Forum { get; }

    public int Pnumber { get; }

    public int PostId { get; }

    public string Time { get; }

    public string Content { get; set; }
}