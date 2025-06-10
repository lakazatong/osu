#pragma warning disable IDE0073

using System;
using System.Collections.Generic;
using System.Threading;
using osu.Framework.Configuration;
using osu.Game.Beatmaps;
using osu.Game.BellaFiora.Endpoints;
using osu.Game.BellaFiora.Utils;
using osu.Game.Configuration;
using osu.Game.Overlays.Mods;
using osu.Game.Rulesets.Mods;
using osu.Game.Screens.Play;
using osu.Game.Screens.Select;
using osu.Game.Skinning;

namespace osu.Game.BellaFiora
{
    public class Server : BaseServer
    {
        public readonly SynchronizationContext UpdateThread;
        public SongSelect SongSelect = null!;
        public SkinManager SkinManager = null!;
        public Skin[] DefaultSkins = null!;
        public List<Skin> CustomSkins { get; internal set; } = [];

        public Dictionary<string, ModPanel> ModPanels = new Dictionary<string, ModPanel>();
        public Mod NMmod = new ModNoMod();
        public ModPanel AutoPanel = null!;
        public ModPanel HDPanel = null!;
        public ModPanel HRPanel = null!;
        public ModPanel DTPanel = null!;
        public ReplayPlayer? Player = null;
        public HotkeyExitOverlay? HotkeyExitOverlay = null;
        public OsuConfigManager OsuConfigManager = null!;
        public BeatmapDifficultyCache BeatmapDifficultyCache = null!;
        public FrameworkConfigManager FrameworkConfigManager = null!;
        public BeatmapManager BeatmapManager = null!;
        public Server(SynchronizationContext syncContext) : base()
        {
            UpdateThread = syncContext;
            AddGET("/loadConfig", new loadConfigEndpoint(this).Handler);
            AddGET("/saveConfig", new saveConfigEndpoint(this).Handler);
            AddGET("/startMap", new startMapEndpoint(this).Handler);
            AddGET("/stopMap", new stopMapEndpoint(this).Handler);
            AddGET("/pp", new ppEndpoint(this).Handler);
            AddGET("/loadOsuFile", new loadOsuFileEndpoint(this).Handler);
            AddGET("/star", new starEndpoint(this).Handler);
        }
        public static Func<object, string?> FormatPanel { get; } = o =>
        {
            var p = (ModPanel)o;
            return $"{p.Mod.Acronym}: {p.Mod.Name}";
        };
    }
}
