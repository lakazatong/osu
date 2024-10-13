#pragma warning disable IDE0073

using osu.Framework.Logging;
// using osu.Game.Beatmaps;

namespace osu.Game.BellaFiora
{
    public class Triggers
    {
        // protected static RulesetStore RulesetStore = null!;
        // protected static readonly int OSU_RULESET_ID = 0;
        // protected static readonly int TAIKO_RULESET_ID = 1;
        // protected static readonly int CATCH_RULESET_ID = 2;
        // protected static readonly int MANIA_RULESET_ID = 3;
        public static class SongSelect
        {
            public static void CarouselBeatmapsTrulyLoaded(Screens.Select.SongSelect songSelect)
            {
                Logger.Log($"CarouselBeatmapsTrulyLoaded called", LoggingTarget.Runtime, LogLevel.Debug);
                songSelect.StartMap(453358);
            }
        }

        // public static class BeatmapCarousel
        // {
        //     public static void Constructor(Screens.Select.BeatmapCarousel carousel)
        //     {
        //     }
        // }

        // public static class OsuGameBase
        // {
        //     public static void load(RulesetStore rulesetStore)
        //     {
        //         RulesetStore = rulesetStore;
        //     }
        // }
    }
}
