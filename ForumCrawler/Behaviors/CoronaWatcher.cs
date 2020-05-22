using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using HtmlAgilityPack;

namespace ForumCrawler
{
    internal static class CoronaWatcher
    {
        public static string API_URL = "https://sand-grandiose-draw.glitch.me/";

        public static string ARCHIVE_API_URL =
            "https://web.archive.org/web/{0}120000/https://www.worldometers.info/coronavirus/";

        public static CountryList Countries = new CountryList(true);

        public static void Bind(DiscordSocketClient client)
        {
            client.AddOnFirstReady(() =>
            {
                Client_Ready(client);
                return Task.CompletedTask;
            });
        }

        private static async void Client_Ready(DiscordSocketClient client)
        {
            while (true)
            {
                try
                {
                    var liveData = await GetLiveData(API_URL);

                    var datas = new[] {liveData.Today}.Concat(await Task.WhenAll(
                        GetArchiveData(liveData.LastReset),
                        GetArchiveData(liveData.LastReset.AddDays(-1)))).ToArray();

                    var now = liveData.Now;
                    var today = liveData.Today;
                    var yesterday = datas[1];
                    var twoDaysAgo = datas[2];

                    var regionsNamesSb = new StringBuilder();
                    var regionsActiveSb = new StringBuilder();
                    var growthFactorSb = new StringBuilder();
                    foreach (var regionNow in now.Entries.Values.OrderByDescending(e => e.Active))
                    {
                        var regions = datas.Select(d =>
                        {
                            d.Entries.TryGetValue(regionNow.Name, out var res);
                            return res ?? new CoronaData.CoronaEntry();
                        }).ToArray();
                        var regionToday = regions[0];
                        var regionYesterday = regions[1];
                        var regionTwoDaysAgo = regions[2];

                        var currentRegionGrowthFactor = GetCurrentGrowthFactor(regionNow, regionToday, regionYesterday);
                        var pastRegionGrowthFactor = GetGrowthFactor(regionYesterday, regionTwoDaysAgo);

                        var cc = GetCountryEmoji(regionNow);
                        regionsNamesSb.AppendLine(cc + " " + regionNow.Name);
                        regionsActiveSb.AppendLine(AbsoluteChangeString(regionNow.Active, regionToday.Active,
                            regionYesterday.Active));
                        growthFactorSb.AppendLine(AbsoluteFactorChangeString(currentRegionGrowthFactor,
                            pastRegionGrowthFactor));

                        if (regionsNamesSb.Length > 1024 || regionsActiveSb.Length > 1024 ||
                            growthFactorSb.Length > 1024)
                        {
                            break;
                        }
                    }

                    var currentGrowthFactor = GetCurrentGrowthFactor(now, today, yesterday);
                    var pastGrowthFactor = GetGrowthFactor(yesterday, twoDaysAgo);

                    var embedBuilder = new EmbedBuilder()
                        .WithTitle("COVID-19 Coronavirus Pandemic Live Tracker")
                        .WithDescription(
                            "The data is updated every 2.5 minutes and compared to numbers from yesterday.")
                        .WithTimestamp(liveData.LastUpdate)
                        .WithUrl("https://www.worldometers.info/coronavirus/")
                        .AddField("**Regions Affected**",
                            AbsoluteChangeString(now.RegionsActive, today.RegionsActive, yesterday.RegionsActive), true)
                        .AddField("**Total Cases**", AbsoluteChangeString(now.Cases, today.Cases, yesterday.Cases),
                            true)
                        .AddField("**Global Growth Factor**",
                            AbsoluteFactorChangeString(currentGrowthFactor, pastGrowthFactor), true)
                        .AddField("**Region**", GetExceptLastLine(regionsNamesSb), true)
                        .AddField("**Infected**", GetExceptLastLine(regionsActiveSb), true)
                        .AddField("**Growth Factor**", GetExceptLastLine(growthFactorSb), true)
                        .AddField("**Total Infected**",
                            AbsoluteChangeString(now.Active, today.Active, yesterday.Active), true)
                        .AddField("**Total Serious**",
                            AbsoluteChangeString(now.Serious, today.Serious, yesterday.Serious), true)
                        .AddField("**Total Mild**", AbsoluteChangeString(now.Mild, today.Mild, yesterday.Mild), true)
                        .AddField("**New Infection Every**",
                            GetPerSecondGrowthString(now.Cases, today.Cases, yesterday.Cases, twoDaysAgo.Cases), true)
                        .AddField("**New Recovery Every**",
                            GetPerSecondGrowthString(now.Recovered, today.Recovered, yesterday.Recovered,
                                twoDaysAgo.Recovered), true)
                        .AddField("**New Death Every**",
                            GetPerSecondGrowthString(now.Deaths, today.Deaths, yesterday.Deaths, twoDaysAgo.Deaths),
                            true)
                        .AddField("**Total Recovered**",
                            AbsoluteChangeString(now.Recovered, today.Recovered, yesterday.Recovered), true)
                        .AddField("**Total Deaths**", AbsoluteChangeString(now.Deaths, today.Deaths, yesterday.Deaths),
                            true)
                        .AddField("**Global Death Rate**",
                            AbsolutePercentageChangeString(now.DeathRate, today.DeathRate, yesterday.DeathRate), true)
                        .AddField("**Links**",
                            "[Worldometers](https://www.worldometers.info/coronavirus/) | [WHO](https://www.who.int/emergencies/diseases/novel-coronavirus-2019/advice-for-public) | [CDC (USA)](https://www.cdc.gov/coronavirus/2019-nCoV/index.html) | [Reddit](https://www.reddit.com/r/Coronavirus/)");

                    var msg = (IUserMessage)await client
                        .GetGuild(DiscordSettings.GuildId)
                        .GetTextChannel(688447767529521332)
                        .GetMessageAsync(688448057121046637);
                    await msg.ModifyAsync(m => m.Embed = embedBuilder.Build());
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }

                await Task.Delay(TimeSpan.FromMinutes(2.5));
            }
        }

        private static string GetExceptLastLine(StringBuilder sb)
        {
            var str = sb.ToString();
            return str.Remove(str.TrimEnd().LastIndexOf('\n'));
        }

        public static string GetCountryEmoji(CoronaData.CoronaEntry region)
        {
            var cc = "";
            if (region.Name == "UK")
            {
                cc = "GB";
            }

            if (region.Name == "S. Korea")
            {
                cc = "KR";
            }

            if (region.Name == "Diamond Princess")
            {
                return ":cruise_ship:";
            }

            if (cc == "")
            {
                cc = Countries.GetTwoLettersName(region.Name, false);
            }

            if (cc == "")
            {
                cc = new string(region.Name.Where(c => Char.IsLetter(c)).Take(2).ToArray());
            }

            return $":flag_{cc.ToLowerInvariant()}:";
        }

        public static int GetCurrentChange(int now, int today, int yesterday)
        {
            var todayChange = now - today;
            var yesterdayChange = today - yesterday;
            var moreToday = Math.Abs(todayChange) > Math.Abs(yesterdayChange);
            return moreToday ? todayChange : yesterdayChange;
        }

        public static double GetCurrentChange(double now, double today, double yesterday)
        {
            var todayChange = now - today;
            var yesterdayChange = today - yesterday;
            var moreToday = Math.Abs(todayChange) > Math.Abs(yesterdayChange);
            return moreToday ? todayChange : yesterdayChange;
        }

        private static string GetPerSecondGrowthString(int now, int today, int yesterday, int twoDaysAgo)
        {
            const double SecondsPerDay = 24 * 60 * 60;
            var daily = GetCurrentChange(now, today, yesterday);
            var pastDaily = yesterday - twoDaysAgo;
            var interval = SecondsPerDay / daily;
            var pastInterval = SecondsPerDay / pastDaily;
            var change = interval - pastInterval;
            var emojiStr = GetEmojiString(change, 0.1);
            return $"{emojiStr}{interval:0.0} seconds{change: (+#,0.0); (-#,0.0);#}";
        }

        public static double GetCurrentGrowthFactor(ICoronaEntry now, ICoronaEntry today, ICoronaEntry yesterday)
        {
            var todayGrowth = GetGrowthFactor(now, today);
            var yesteredayGrowth = GetGrowthFactor(today, yesterday);
            var moreToday = todayGrowth > yesteredayGrowth;
            return moreToday ? todayGrowth : yesteredayGrowth;
        }

        public static double GetGrowthFactor(ICoronaEntry current, ICoronaEntry past)
        {
            var res = (double)current.CaseIncrease / past.CaseIncrease;
            if (double.IsNaN(res))
            {
                return 0;
            }

            return res;
        }

        public static string AbsoluteChangeString(int now, int today, int yesterday)
        {
            var nfi = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
            nfi.NumberDecimalDigits = 0;
            nfi.NumberGroupSeparator = " ";

            var change = GetCurrentChange(now, today, yesterday);
            var emojiStr = GetEmojiString(change, 1);
            return $"{emojiStr}{now.ToString("N", nfi)}{change.ToString(" (+#,#); (-#,#);#", nfi)}";
        }

        public static string AbsoluteFactorChangeString(double current, double past)
        {
            var change = current - past;
            var emojiStr = GetEmojiString(change, 0.01);

            if (Double.IsInfinity(change))
            {
                change = 0;
            }

            var inf = Double.IsPositiveInfinity(current) ? "N/A" : current.ToString("0.00x");
            return $"{emojiStr}{inf}{change: (+#,0.00); (-#,0.00);#}";
        }

        private static string GetEmojiString(double change, double accuracy)
        {
            return change > accuracy / 2
                ? "<:u:688860234449813591>"
                : change < -accuracy / 2
                    ? "<:d:688860234474979334>"
                    : "<:n:688859293205921857>";
        }

        public static string AbsolutePercentageChangeString(double now, double today, double past)
        {
            var change = GetCurrentChange(now, today, past);
            var emojiStr = GetEmojiString(change, 0.001);
            return $"{emojiStr}{now:0.0%}{change: (+0.0%); (-0.0%);#}";
        }

        public static double ParseDouble(HtmlNode node)
        {
            Double.TryParse(node.InnerText, NumberStyles.Number, CultureInfo.InvariantCulture, out var num);
            return num;
        }

        public static async Task<CoronaData> GetArchiveData(DateTime time)
        {
            var web = new HtmlWeb();
            var coronaStats = await web.LoadFromWebAsync(String.Format(ARCHIVE_API_URL, time.ToString("yyyyMMdd")));
            return GetFromTable(coronaStats, "yesterday");
        }

        public static async Task<CoronaLiveData> GetLiveData(string url)
        {
            var web = new HtmlWeb();
            var coronaStats = await web.LoadFromWebAsync(url);

            var lastUpdate = DateTimeOffset.Parse(
                coronaStats.DocumentNode.SelectNodes("//div")
                    .First(e => e.GetAttributeValue("style", "") ==
                                "font-size:13px; color:#999; margin-top:5px; text-align:center")
                    .InnerText.Substring("Last updated: ".Length)).UtcDateTime;

            var lastReset = DateTime.Parse(Regex.Match(
                    coronaStats.DocumentNode.SelectNodes("//script")
                        .First(e => e.InnerText.Contains("Highcharts.chart('total-currently-infected-linear',"))
                        .InnerText, "categories: \\[.+\"([A-Za-z0-9 ]*)\"\\]")
                .Groups[1].Value, CultureInfo.InvariantCulture);

            return new CoronaLiveData
            {
                LastUpdate = lastUpdate,
                LastReset = lastReset,
                Now = GetFromTable(coronaStats, "today"),
                Today = GetFromTable(coronaStats, "yesterday") // this is NOT a typo
            };
        }

        private static CoronaData GetFromTable(HtmlDocument coronaStats, string time)
        {
            var result = new CoronaData();
            var countries = coronaStats.DocumentNode
                .SelectNodes($"//*[@id=\"main_table_countries_{time}\"]/tbody[1]/tr")
                .Where(t => !t.GetAttributeValue("class", "").Contains("total_row_world"));

            foreach (var country in countries)
            {
                var entry = new CoronaData.CoronaEntry();
                var cells = country.SelectNodes("td");
                entry.Name = cells[1].InnerText.Trim();
                entry.Type = cells[1].SelectSingleNode("span") == null
                    ? CoronaData.CoronaEntryType.Country
                    : CoronaData.CoronaEntryType.Other;
                entry.Cases = (int)ParseDouble(cells[2]);
                entry.CaseIncrease = (int)ParseDouble(cells[3]);
                entry.Deaths = (int)ParseDouble(cells[4]);
                entry.Recovered = (int)ParseDouble(cells[6]);
                entry.Serious = (int)ParseDouble(cells[8]);

                result.Entries.Add(entry.Name, entry);
            }

            return result;
        }

        public class CountryList
        {
            private readonly CultureTypes _AllCultures;

            public CountryList(bool AllCultures)
            {
                _AllCultures = AllCultures ? CultureTypes.AllCultures : CultureTypes.SpecificCultures;
                Countries = GetAllCountries(_AllCultures);
            }

            public List<CountryInfo> Countries { get; set; }


            public string GetTwoLettersName(string CountryName, bool NativeName)
            {
                var country = NativeName
                    ? Countries.Where(info => info.Region.NativeName == CountryName)
                        .FirstOrDefault()
                    : Countries.Where(info => info.Region.EnglishName == CountryName)
                        .FirstOrDefault();

                return country != null ? country.Region.TwoLetterISORegionName : string.Empty;
            }

            private static List<CountryInfo> GetAllCountries(CultureTypes cultureTypes)
            {
                var Countries = new List<CountryInfo>();

                foreach (var culture in CultureInfo.GetCultures(cultureTypes))
                {
                    if (culture.LCID != 127)
                    {
                        Countries.Add(new CountryInfo
                        {
                            Culture = culture, Region = new RegionInfo(culture.TextInfo.CultureName)
                        });
                    }
                }

                return Countries;
            }

            public class CountryInfo
            {
                public CultureInfo Culture { get; set; }
                public RegionInfo Region { get; set; }
            }
        }
    }
}