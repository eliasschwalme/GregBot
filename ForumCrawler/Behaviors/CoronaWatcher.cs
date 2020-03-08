using Discord.WebSocket;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace ForumCrawler
{
    internal class CoronaPayload
    {
        public List<CoronaEntry> entries = null;

        public class CoronaEntry
        {
            public string cases = null;
            public string deaths = null;
            public string recovered = null;
        }
    }

    internal static class CoronaWatcher
    {
        public static string API_URL = "https://interactive-static.scmp.com/sheet/wuhan/viruscases.json";

        public static async void Bind(DiscordSocketClient client)
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromMinutes(1));

                try
                {
                    using (var web = new WebClient())
                    {
                        var coronaJson = await web.DownloadStringTaskAsync(new Uri(API_URL));
                        var coronaData = JsonConvert.DeserializeObject<CoronaPayload>(coronaJson);
                        var sumCases = coronaData.entries.Sum(entry => Convert.ToInt32(entry.cases?.Replace(",", "")?.Replace(".", "") ?? "0"));
                        var sumDeaths = coronaData.entries.Sum(entry => Convert.ToInt32(entry.deaths?.Replace(",", "")?.Replace(".", "") ?? "0"));
                        var sumRecovered = coronaData.entries.Sum(entry => Convert.ToInt32(entry.recovered?.Replace(",", "")?.Replace(".", "") ?? "0"));

                        await client.GetGuild(DiscordSettings.GuildId).GetTextChannel(329335303963803649).ModifyAsync(c =>
                        {
                            c.Topic = $"Casual Talk. COVID-19 Infected: {sumCases - sumDeaths - sumRecovered}, Recovered: {sumRecovered}, Deaths: {sumDeaths}, Death Rate: {sumDeaths / (double)(sumDeaths + sumRecovered):P1}";
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }
    }
}