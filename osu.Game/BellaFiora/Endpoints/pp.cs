#pragma warning disable IDE0073

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using osu.Game.Beatmaps;
using osu.Game.BellaFiora.Utils;
using osu.Game.Overlays.Mods;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Screens.Select.Carousel;

namespace osu.Game.BellaFiora.Endpoints
{
    public class ppEndpoint : Endpoint<Server>
    {
        public ppEndpoint(Server server) : base(server) { }
        public override Func<HttpListenerRequest, bool> Handler => request =>
        {
            var queryString = request.QueryString;
            string? beatmapSetIdStr = queryString["beatmapSetId"];
            string? beatmapIdStr = queryString["beatmapId"];
            string? modsParam = queryString["mods"];
            string? accsParam = queryString["accs"];

            if (int.TryParse(beatmapIdStr, out int beatmapId) && modsParam != null && accsParam != null)
            {
                callback(beatmapSetIdStr, beatmapId, modsParam, accsParam);
                return true;
            }
            return false;
        };
        private async Task<int> beatmapSetIdFromBeatmapId(int beatmapId)
        {
            using (HttpClient client = new HttpClient())
            {
                string osuFile = await client.GetStringAsync($"https://osu.ppy.sh/osu/{beatmapId}").ConfigureAwait(false);
                string? beatmapSetIdLine = osuFile
                    .Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault(line => line.StartsWith("BeatmapSetID")) ?? "";
                if (beatmapSetIdLine.Length == 0)
                    return 0;
                string[] parts = beatmapSetIdLine.Split(':');
                string rightMostValue = parts[^1].Trim();
                if (int.TryParse(rightMostValue, out int beatmapSetId))
                    return beatmapSetId;
            }
            return 0;
        }
        private void createSilentWav(string filename, int durationSeconds)
        {
            int sampleRate = 44100; // CD-quality sample rate
            int numChannels = 1; // Mono audio
            int bitsPerSample = 16; // 16-bit audio

            int byteRate = sampleRate * numChannels * (bitsPerSample / 8);
            int totalDataBytes = byteRate * durationSeconds;
            int totalFileSize = 44 + totalDataBytes;

            using (FileStream fs = new FileStream(filename, FileMode.Create))
            using (BinaryWriter writer = new BinaryWriter(fs, Encoding.UTF8))
            {
                // RIFF header
                writer.Write(Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(totalFileSize - 8); // Chunk size
                writer.Write(Encoding.ASCII.GetBytes("WAVE"));

                // fmt subchunk
                writer.Write(Encoding.ASCII.GetBytes("fmt "));
                writer.Write(16); // Subchunk size (16 for PCM)
                writer.Write((short)1); // Audio format (1 = PCM)
                writer.Write((short)numChannels); // Number of channels
                writer.Write(sampleRate); // Sample rate
                writer.Write(byteRate); // Byte rate
                writer.Write((short)(numChannels * (bitsPerSample / 8))); // Block align
                writer.Write((short)bitsPerSample); // Bits per sample

                // data subchunk
                writer.Write(Encoding.ASCII.GetBytes("data"));
                writer.Write(totalDataBytes); // Data size

                // Write silence
                for (int i = 0; i < totalDataBytes / 2; i++)
                {
                    writer.Write((short)0); // Silent sample (16-bit PCM)
                }
            }
        }
        private async Task<bool> addBeatmapSet(int beatmapSetId)
        {
            string url = $"https://osu.ppy.sh/beatmapsets/{beatmapSetId}";
            JObject root = null!;
            string[] filenames = null!;
            BeatmapSetInfo beatmapSetInfo = null!;
            int maxTotalLength = 0;
            using (HttpClient client = new HttpClient())
            {
                string response = await client.GetStringAsync(url).ConfigureAwait(false);

                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(response);

                var scriptNode = htmlDoc.DocumentNode.SelectSingleNode("//script[@id='json-beatmapset']");
                if (scriptNode != null)
                {
                    string jsonContent = scriptNode.InnerText;
                    root = JObject.Parse(jsonContent);
                }
                else
                {
                    Server.RespondJSON(new Dictionary<string, string>{
                                { "error", "Script with id 'json-beatmapset' not found" }
                            });
                    return false;
                }
                JToken? beatmaps = root["beatmaps"];
                if (beatmaps == null)
                {
                    Server.RespondJSON(new Dictionary<string, string>{
                                { "error", "Beatmaps not found" }
                            });
                    return false;
                }
                beatmapSetInfo = new()
                {
                    OnlineID = beatmapSetId,
                };
                filenames = new string[beatmaps.Count()];
                BeatmapInfo[] beatmapInfos = new BeatmapInfo[beatmaps.Count()];
                await Task.WhenAll(beatmaps.Select(async (JToken beatmap, int i) =>
                {
                    string beatmapID = beatmap["id"]?.Value<string?>() ?? "";
                    string mode = beatmap["mode"]?.Value<string?>() ?? "";
                    string filename = $"{root["artist"]} - {root["title"]} ({root["creator"]}) [{beatmap["version"]}].osu";
                    maxTotalLength = Math.Max(maxTotalLength, beatmap["total_length"]?.Value<int?>() ?? 0);
                    filenames[i] = filename;
                    beatmapSetInfo.Beatmaps.Add(new BeatmapInfo(new RulesetInfo { ShortName = mode, Available = true })
                    {
                        OnlineID = int.Parse(beatmapID),
                        BeatmapSet = beatmapSetInfo
                    });
                    string osuFile = await client.GetStringAsync($"https://osu.ppy.sh/osu/{beatmap["id"]}").ConfigureAwait(false);
                    File.WriteAllText(filename, osuFile);
                })).ConfigureAwait(false);
            }
            string dummyAudioFilename = "audio.mp3";
            createSilentWav(dummyAudioFilename, maxTotalLength);
            string oszFilename = $"{beatmapSetId} {root["artist"]} - {root["title"]}.osz";
            using (var zipStream = new FileStream(oszFilename, FileMode.Create))
            {
                using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
                {
                    foreach (string file in filenames)
                    {
                        archive.CreateEntryFromFile(file, Path.GetFileName(file));
                    }
                    archive.CreateEntryFromFile(dummyAudioFilename, dummyAudioFilename);
                }
            }
            await Server.BeatmapManager.Import(oszFilename).ConfigureAwait(false);
            await Server.SongSelect.BeatmapSetAdded(beatmapSetInfo).ConfigureAwait(false);
            return true;
        }
        // taken from osu-tools OsuSimulateCommand.cs
        private Dictionary<HitResult, int> generateHitResults(int totalObjectCount, double accuracy, int countMiss = -1)
        {
            int countGood;
            int countMeh;
            countMiss = countMiss == -1 ? (int)Math.Ceiling(totalObjectCount * (1 - accuracy)) : countMiss;
            int relevantResultCount = totalObjectCount - countMiss;
            double relevantAccuracy = accuracy * totalObjectCount / relevantResultCount;
            relevantAccuracy = Math.Clamp(relevantAccuracy, 0, 1);
            if (relevantAccuracy >= 0.25)
            {
                double ratio50To100 = Math.Pow(1 - (relevantAccuracy - 0.25) / 0.75, 2);
                double count100Estimate = 6 * relevantResultCount * (1 - relevantAccuracy) / (5 * ratio50To100 + 4);
                double count50Estimate = count100Estimate * ratio50To100;
                countGood = (int)Math.Round(count100Estimate);
                countMeh = (int)(Math.Round(count100Estimate + count50Estimate) - countGood);
            }
            else if (relevantAccuracy >= 1.0 / 6)
            {
                double count100Estimate = 6 * relevantResultCount * relevantAccuracy - relevantResultCount;
                double count50Estimate = relevantResultCount - count100Estimate;
                countGood = (int)Math.Round(count100Estimate);
                countMeh = (int)(Math.Round(count100Estimate + count50Estimate) - countGood);
            }
            else
            {
                double count50Estimate = 6 * relevantResultCount * relevantAccuracy;
                countGood = 0;
                countMeh = (int)Math.Round(count50Estimate);
                countMiss = totalObjectCount - countMeh;
            }
            int countGreat = totalObjectCount - countGood - countMeh - countMiss;
            return new Dictionary<HitResult, int>
            {
                { HitResult.Great, countGreat },
                { HitResult.Ok, countGood },
                { HitResult.Meh, countMeh },
                { HitResult.Miss, countMiss }
            };
        }
        private void callback(string? beatmapSetIdStr, int beatmapId, string modsParam, string accsParam)
        {
            Server.UpdateThread.Post(async _ =>
            {
                CarouselBeatmap? carouselBeatmap = Server.SongSelect.GetCarouselBeatmap(beatmapId);
                if (carouselBeatmap == null)
                {
                    if (!await addBeatmapSet(int.TryParse(beatmapSetIdStr, out int beatmapSetId) ? beatmapSetId : await beatmapSetIdFromBeatmapId(beatmapId).ConfigureAwait(false)).ConfigureAwait(false))
                        return;
                    carouselBeatmap = Server.SongSelect.GetCarouselBeatmap(beatmapId);
                    if (carouselBeatmap == null) return;
                }
                BeatmapInfo beatmapInfo = carouselBeatmap.BeatmapInfo;
                // int endTimeObjectCount = beatmapInfo.EndTimeObjectCount;
                int totalObjectCount = beatmapInfo.TotalObjectCount;
                RulesetInfo rulesetInfo = beatmapInfo.Ruleset;
                ScoreInfo scoreInfo = new ScoreInfo(beatmapInfo, beatmapInfo.Ruleset)
                {
                    // MaxCombo = 179,
                    // Accuracy = 1.0,
                    // Rank = ScoreRank.X,
                    // RankInt = 6,
                    // Passed = true,
                    // Combo = 179,
                    // Position = null,
                    // Statistics = stats,
                    // MaximumStatistics = stats,
                };
                PerformanceCalculator? performanceCalculator = rulesetInfo.CreateInstance().CreatePerformanceCalculator();
                double[] accs = accsParam.Split(',')
                                         .Select(accStr => double.TryParse(accStr, out double acc) ? acc : 0.0)
                                         .ToArray();
                string[] modCombinations = modsParam.Split(',');

                List<Mod[]> modsComb = new List<Mod[]>();

                foreach (string modCombination in modCombinations)
                {
                    string[] modsInCombination = modCombination.Split(' ');
                    List<Mod> modsForCurrentCombination = new List<Mod>();
                    foreach (string mod in modsInCombination)
                    {
                        if (Server.ModPanels.TryGetValue(mod, out ModPanel? panel))
                            modsForCurrentCombination.Add(panel.Mod);
                        else
                            modsForCurrentCombination.Add(Server.NMmod);
                    }
                    modsComb.Add(modsForCurrentCombination.ToArray());
                }
                // List<List<string>> all_pps = new List<List<string>>(mods_comb.Length);
                Dictionary<string, List<double>> ppData = new Dictionary<string, List<double>>();
                foreach (Mod[] mods in modsComb)
                {
                    StarDifficulty? difficulty = await Server.BeatmapDifficultyCache.GetDifficultyAsync(beatmapInfo, rulesetInfo, mods).ConfigureAwait(false);
                    List<double> pps = new List<double>(accs.Length);
                    // all_pps.Add(pps);
                    foreach (double acc in accs)
                    {
                        scoreInfo.Accuracy = acc;
                        scoreInfo.Statistics = generateHitResults(totalObjectCount, acc, 0);
                        scoreInfo.Mods = mods;
                        PerformanceAttributes? attributes = performanceCalculator?.Calculate(scoreInfo, difficulty.Value.Attributes);
                        pps.Add(attributes?.Total ?? 0);
                    }
                    string modKey = string.Join("+", mods.Select(m => m?.Acronym ?? ""));
                    ppData[modKey] = pps;
                }
                // Server.RespondHTML(
                //     "h1", "Received pp request",
                //     "p", $"PP gains (FC) for {string.Join(" / ", accs.Select(acc => $"{acc * 100}%"))}:",
                //     "ul",
                //     mods_comb.Select((mods, index) =>
                //         $"{string.Join("", mods.Select(m => m.Acronym))}: {string.Join(" / ", all_pps[index].Select(pp => $"{double.Parse(pp):0.00}"))}"
                //     ), BaseServer.UnitFormatter,
                //     "p", $"Beatmap ID: {beatmapId}"
                // );
                Server.RespondJSON(ppData);
            }, null);
        }
    }
}
