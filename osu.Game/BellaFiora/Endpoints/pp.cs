#pragma warning disable IDE0073

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using osu.Game.Beatmaps;
using osu.Game.BellaFiora.Utils;
using osu.Game.Overlays.Mods;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Mods;
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
            string? beatmapIdStr = queryString["beatmapId"];
            string? modsParam = queryString["mods"];
            string? accsParam = queryString["accs"];

            if (int.TryParse(beatmapIdStr, out int beatmapId) && modsParam != null && accsParam != null)
            {
                callback(beatmapId, modsParam, accsParam);
                return true;
            }
            return false;
        };
        private void callback(int beatmapId, string modsParam, string accsParam)
        {
            Server.UpdateThread.Post(async _ =>
            {
                CarouselBeatmap? carouselBeatmap = Server.SongSelect.GetCarouselBeatmap(beatmapId);
                if (carouselBeatmap == null)
                {
                    Server.RespondHTML(
                        "h1", "Received recordMap request",
                        "p", $"Beatmap ID: {beatmapId}",
                        "p", "Requested Mods:",
                        "p", "Do not have this beatmap"
                    );
                    return;
                }
                BeatmapInfo beatmapInfo = carouselBeatmap.BeatmapInfo;
                int endTimeObjectCount = beatmapInfo.EndTimeObjectCount;
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
                        // scoreInfo.Statistics = new Dictionary<HitResult, int> {
                        //     { HitResult.None, 0 },
                        //     { HitResult.Miss, 0 },
                        //     { HitResult.Meh, 0 },
                        //     { HitResult.Ok, 0 },
                        //     { HitResult.Good, 0 },
                        //     { HitResult.Great, 133 },
                        //     { HitResult.Perfect, 0 },
                        //     { HitResult.SmallTickMiss, 0 },
                        //     { HitResult.SmallTickHit, 0 },
                        //     { HitResult.LargeTickMiss, 0 },
                        //     { HitResult.LargeTickHit, 1 },
                        //     { HitResult.SmallBonus, 0 },
                        //     { HitResult.LargeBonus, 0 },
                        //     { HitResult.IgnoreMiss, 0 },
                        //     { HitResult.IgnoreHit, 45 },
                        //     { HitResult.ComboBreak, 0 },
                        //     { HitResult.SliderTailHit, 45 },
                        //     { HitResult.LegacyComboIncrease, 0 },
                        // };
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
