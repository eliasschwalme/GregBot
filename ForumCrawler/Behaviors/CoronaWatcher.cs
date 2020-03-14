using Discord;
using Discord.WebSocket;
using HtmlAgilityPack;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ForumCrawler
{
    internal class CoronaData
    {
        public List<int> CaseHistory { get; set; }
        public Dictionary<string, CoronaEntry> Entries { get; set; } = new Dictionary<string, CoronaEntry>();
        public DateTime LastUpdated { get; set; }

        public int Cases => this.Entries.Values.Sum(e => e.Cases);
        public int Active => this.Entries.Values.Sum(e => e.Active);
        public int Deaths => this.Entries.Values.Sum(e => e.Deaths);
        public int Recovered => this.Entries.Values.Sum(e => e.Recovered);
        public int Serious => this.Entries.Values.Sum(e => e.Serious);

        public double GrowthFactorAveraged => new[] { GetGrowthFactor(0), GetGrowthFactor(1), GetGrowthFactor(2) }.Average();

        public int RegionsActive => this.Entries.Values.Count(e => e.Active > 0);
        public int RegionsRecovered => this.Entries.Values.Count(e => e.Active == 0);

        public double DeathRate => (double)this.Deaths / (this.Recovered + this.Deaths);

        public double GetGrowthFactor(int offset)
        {
            var growthDay = this.CaseHistory[this.CaseHistory.Count - 1 - offset] - this.CaseHistory[this.CaseHistory.Count - 2 - offset];
            var growthPrevious = this.CaseHistory[this.CaseHistory.Count - 2 - offset] - this.CaseHistory[this.CaseHistory.Count - 3 - offset];
            return (double)growthDay / growthPrevious;
        }

        public enum CoronaEntryType
        {
            Country,
            Other
        }

        public class CoronaEntry
        {
            public CoronaEntryType Type { get; set; }
            public string Name { get; set; }
            public int Cases { get; set; }
            public int Deaths { get; set; }
            public int Recovered { get; set; }
            public int Serious { get; set; }
            public int Active => this.Cases - this.Deaths - this.Recovered;
            public double DeathRate => (double)this.Deaths / (this.Recovered + this.Deaths);
        }
    }

    internal static class CoronaWatcher
    {
        public static string API_URL = "https://sand-grandiose-draw.glitch.me/";
        public static string ARCHIVE_API_URL = "https://web.archive.org/web/{0}/https://www.worldometers.info/coronavirus/";

        public static async void Bind(DiscordSocketClient client)
        {
            await Task.Delay(TimeSpan.FromMinutes(1));

            while (true)
            {
                try
                {
                    var datas = await Task.WhenAll(
                        GetData(API_URL),
                        GetData(String.Format(ARCHIVE_API_URL, DateTime.UtcNow.Subtract(TimeSpan.FromDays(1)).ToString("yyyyMMddHHmmss"))));
                    var current = datas[0];
                    var past = datas[1];

                    var regionsNames = new StringBuilder();
                    var regionsActive = new StringBuilder();
                    var regionDeaths = new StringBuilder();
                    foreach (var currentRegion in current.Entries.Values.OrderByDescending(e => e.Active).Take(20))
                    {
                        if (!past.Entries.TryGetValue(currentRegion.Name, out var pastRegion))
                            pastRegion = new CoronaData.CoronaEntry();
                        regionsNames.AppendLine(currentRegion.Name);
                        regionsActive.AppendLine(RelativeChangeString(currentRegion.Active, pastRegion.Active));
                        regionDeaths.AppendLine(RelativeChangeString(currentRegion.Deaths, pastRegion.Deaths));
                    }

                    var embedBuilder = new EmbedBuilder()
                        .WithTitle("COVID-19 Live Tracker")
                        .WithTimestamp(current.LastUpdated)
                        .WithUrl("https://www.worldometers.info/coronavirus/")

                        .AddField("Cases", RelativeChangeString(current.Cases, past.Cases), true)
                        .AddField("Growth Factor*", AbsoluteFactorChangeString(current.GrowthFactorAveraged, past.GrowthFactorAveraged), true)
                        .AddField("Global Death Rate", AbsolutePercentageChangeString(current.DeathRate, past.DeathRate), true)

                        .AddField("Region", regionsNames.TrimEnd().ToString(), true)
                        .AddField("Infected", regionsActive.TrimEnd().ToString(), true)
                        .AddField("Deaths", regionDeaths.TrimEnd().ToString(), true)

                        .AddField("Regions Affected", AbsoluteChangeString(current.RegionsActive, past.RegionsActive), true)
                        .AddField("Total Infected", RelativeChangeString(current.Active, past.Active), true)
                        .AddField("Total Deaths", RelativeChangeString(current.Deaths, past.Deaths), true)

                        .AddField("Regions Recovered", AbsoluteChangeString(current.RegionsRecovered, past.RegionsRecovered), true)
                        .AddField("Total Serious", RelativeChangeString(current.Serious, past.Serious), true)
                        .AddField("Total Recovered", RelativeChangeString(current.Recovered, past.Recovered), true)
                        .AddField("Notes", "*: Growth factor is the factor by which a quantity multiplies itself over time. The average of the growth factors observed in the past three days is shown here.")
                        .AddField("Links", "[WHO](https://www.who.int/emergencies/diseases/novel-coronavirus-2019/advice-for-public) | [CDC (USA)](https://www.cdc.gov/coronavirus/2019-nCoV/index.html) | [Reddit](https://www.reddit.com/r/Coronavirus/)");

                    var msg = (IUserMessage)await client
                        .GetGuild(DiscordSettings.GuildId)
                        .GetTextChannel(688447767529521332)
                        .GetMessageAsync(688448057121046637);
                    await msg.ModifyAsync(m => m.Embed = embedBuilder.Build());
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    throw;
                }

                await Task.Delay(TimeSpan.FromMinutes(2.5));
            }
        }

        public static string AbsoluteChangeString(int current, int past)
        {
            var change = current - past;
            return $"{current}{change: (+0); (-0);#}";
        }

        public static string AbsoluteFactorChangeString(double current, double past)
        {
            var change = current - past;
            return $"{current:0.00}x{change: (+0.00); (-0.00);#}";
        }

        public static string RelativeChangeString(int current, int past)
        {
            var nfi = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
            nfi.NumberDecimalDigits = 0;
            nfi.NumberGroupSeparator = " ";

            var change = (double)current / past - 1;
            var inf = Double.IsPositiveInfinity(change) ? " (+∞)" : Double.IsNegativeInfinity(change) ? " (+∞)" : "";
            if (Double.IsNaN(change) || Double.IsInfinity(change)) change = 0;
            return String.Format(nfi, "{0:N}{1}{2: (+0%); (-0%);#}", current, inf, change);
        }

        public static string AbsolutePercentageChangeString(double current, double past)
        {
            var change = current - past;
            return $"{current:0.0%}{change: (+0.0%); (-0.0%);#}";
        }

        public static double ParseDouble(HtmlNode node)
        {
            Double.TryParse(node.InnerText, NumberStyles.Number, CultureInfo.InvariantCulture, out var num);
            return num;
        }

        public static async Task<CoronaData> GetData(string url)
        {
            var web = new HtmlWeb();
            var coronaStats = await web.LoadFromWebAsync(url);
            var result = new CoronaData();
            result.LastUpdated = DateTimeOffset.Parse(
                    coronaStats.DocumentNode.SelectNodes("//div")
                    .First(e => e.GetAttributeValue("style", "") == "font-size:13px; color:#999; text-align:center")
                    .InnerText
                    .Substring("Last updated: ".Length)
                ).UtcDateTime;

            result.CaseHistory = Regex.Match(
                    coronaStats.DocumentNode.SelectNodes("//script")
                        .First(e => e.InnerText.StartsWith(" Highcharts.chart('coronavirus-cases-linear'"))
                    .InnerText,
                    @"\[([0-9,]+)\]"
                ).Groups[1].Value.Split(',')
                .Select(i => Int32.Parse(i))
                .ToList();


            var countries = coronaStats.DocumentNode.SelectNodes("//*[@id=\"main_table_countries\"]/tbody[1]/tr");
            foreach (var country in countries)
            {
                var entry = new CoronaData.CoronaEntry();
                var cells = country.SelectNodes("td");
                entry.Name = cells[0].InnerText.Trim();
                entry.Type = cells[0].SelectSingleNode("span") == null ? CoronaData.CoronaEntryType.Country : CoronaData.CoronaEntryType.Other;
                entry.Cases = (int)ParseDouble(cells[1]);
                entry.Deaths = (int)ParseDouble(cells[3]);
                entry.Recovered = (int)ParseDouble(cells[5]);
                entry.Serious = (int)ParseDouble(cells[7]);
                result.Entries.Add(entry.Name, entry);
            }
            return result;
        }
    }
}