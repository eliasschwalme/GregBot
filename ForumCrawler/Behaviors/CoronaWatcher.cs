using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ForumCrawler
{
    class CoronaPayload
    {
        public List<CoronaEntry> entries;
        
        public class CoronaEntry
        {
            public string cases;
            public string deaths;
        }
    }

    class CoronaWatcher
    {
        public static string API_URL = "https://interactive-static.scmp.com/sheet/wuhan/viruscases.json";
        public static async void Bind(DiscordSocketClient client)
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromMinutes(1));

                using (var web = new WebClient())
                {
                    var coronaJson =  await web.DownloadStringTaskAsync(new Uri(API_URL));
                    var coronaData = JsonConvert.DeserializeObject<CoronaPayload>(coronaJson);
                    var sumCases = coronaData.entries.Sum(entry => Convert.ToInt32(entry.cases));
                    var sumDeaths = coronaData.entries.Sum(entry => Convert.ToInt32(entry.deaths));

                    await client.GetGuild(DiscordSettings.GuildId).GetTextChannel(329335303963803649).ModifyAsync(c =>
                    {
                        c.Topic = $"Casual Talk. 2019-nCoV Cases: {sumCases} Deaths: {sumDeaths}";
                    });
                }
            }
        }
    }
}