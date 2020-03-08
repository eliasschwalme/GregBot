using HtmlAgilityPack;

using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ForumCrawler
{
    public static class Forum
    {
        private const string IndexUrl = @"https://forums.everybodyedits.com/index.php";
        private const string DiffIsBad = @"https://forums.everybodyedits.com/login.php";
        private const string LoginUrl = @"https://forums.everybodyedits.com/login.php?action=in";
        private const string Url = @"http://forums.everybodyedits.com/search.php?action=show_recent";
        private const string DirectUrl = @"https://forums.everybodyedits.com/post.php?tid={0}&qid={1}";
        private static readonly CookieContainer _cookies = new CookieContainer();

        static Forum() => Login();

        public static async Task<string> GetCSRFAsync()
        {
            using (var client = new WebClientEx(_cookies))
            {
                var html = await client.DownloadDataTaskAsync(DiffIsBad);
                var doc = new HtmlDocument();
                doc.Load(new MemoryStream(html), Encoding.UTF8);
                return doc
                    .DocumentNode
                    .Descendants()
                    .Where(node => node.GetAttributeValue("name", "") == "csrf_token")
                    .Select(node => node.Attributes["value"].Value)
                    .First();
            }
        }

        public static void Login()
        {
            var csrf_token = GetCSRFAsync().Result;
            using (var client = new WebClientEx(_cookies))
            {
                client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                client.Headers[HttpRequestHeader.Referer] = LoginUrl;
                client.Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2228.0 Safari/537.36";

                var reqparm = new NameValueCollection
                {
                    { "form_sent", "1" },
                    { "csrf_token", csrf_token },
                    { "redirect_url", "https://forums.everybodyedits.com/index.php" },
                    { "req_username", "GregBot" },
                    { "req_password", Passwords.ForumPassword },
                    { "save_pass", "1" },
                    { "login", "Login" }
                };
                client.UploadValues(LoginUrl, "post", reqparm);
            }
        }

        public static async Task<Post[]> GetNewAsync()
        {
            var client = new WebClientEx(_cookies);
            var html = await client.DownloadDataTaskAsync(Url);
            var doc = new HtmlDocument();
            doc.Load(new MemoryStream(html), Encoding.UTF8);

            var res = doc
                .GetElementbyId("vf")
                .ChildNodes["div"]
                .ChildNodes["div"]
                .ChildNodes["table"]
                .ChildNodes["tbody"]
                .SelectNodes("tr").Select(item =>
                {
                    var content = item.ChildNodes[1].ChildNodes[3].ChildNodes["div"].ChildNodes[1];
                    if (content.Name != "strong") return null;
                    var topic = WebUtility.HtmlDecode(content.ChildNodes["a"].InnerText);
                    var topicId = int.Parse(content.ChildNodes["a"].Attributes["href"].Value.Split('=')[1]);

                    var lastPostPoster = item.ChildNodes[7].ChildNodes["span"];
                    var poster = WebUtility.HtmlDecode(lastPostPoster.InnerText.Substring(3));
                    var posterId = int.Parse(lastPostPoster.ChildNodes["a"]?.Attributes["href"].Value.Split('=')[1] ?? "-1");

                    var forum = item.ChildNodes[3].ChildNodes["a"].InnerText;
                    var pnumber = int.Parse(item.ChildNodes[5].InnerText.Replace(",", "")) + 1;

                    var lastPost = item.ChildNodes[7].ChildNodes["a"];
                    var postId = int.Parse(lastPost.Attributes["href"].Value.Split('#')[1].Substring(1));
                    var time = lastPost.InnerText.Split(' ')[1];
                    return new Post(topic, topicId, poster, posterId, forum, pnumber, postId, time);
                })
                .Where(i => i != null)
                .ToArray();

            foreach (var item in res)
            {
                item.Content = await GetContentAsync(item.TopicId, item.PostId);
            }

            return res;
        }

        public static async Task<string> GetContentAsync(int topicId, int postId)
        {
            var client = new WebClientEx(_cookies);
            var html = await client.DownloadDataTaskAsync(string.Format(DirectUrl, topicId, postId));
            var doc = new HtmlDocument();
            doc.Load(new MemoryStream(html), Encoding.UTF8);

            return doc
                .DocumentNode
                .Descendants()
                .Where(node => node.GetAttributeValue("name", "") == "req_message")
                .Select(node => WebUtility.HtmlDecode(node.InnerText))
                .FirstOrDefault();
        }

        public static async Task<string[]> GetOnlineUsers()
        {
            var html = await new WebClient().DownloadDataTaskAsync(IndexUrl);
            var doc = new HtmlDocument();

            doc.Load(new MemoryStream(html), Encoding.UTF8);
            return doc.DocumentNode.Descendants()
                .First(node => node.Id == "onlinelist")
                .ChildNodes
                .Where(node => node.Name == "dd")
                .Select(node => node.ChildNodes[1].InnerText)
                .ToArray();
        }
    }
}