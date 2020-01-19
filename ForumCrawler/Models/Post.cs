public class Post
{
    public Post(string topic, int topicId, string poster, int posterId, string forum, int pnumber, int postId, string time)
    {
        this.Topic = topic;
        this.TopicId = topicId;
        this.Poster = poster;
        this.PosterId = posterId;
        this.Forum = forum;
        this.Pnumber = pnumber;
        this.PostId = postId;
        this.Time = time;
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