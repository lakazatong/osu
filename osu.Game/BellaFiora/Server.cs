#pragma warning disable IDE0073

using System.Collections.Generic;
using System.Threading;
using osu.Framework.Configuration;
using osu.Game.BellaFiora.Endpoints;
using osu.Game.BellaFiora.Utils;
using osu.Game.Configuration;
using osu.Game.Overlays.Mods;
using osu.Game.Screens.Play;
using osu.Game.Screens.Select;
using osu.Game.Skinning;

namespace osu.Game.BellaFiora
{
    public class Server : BaseServer
    {
        public readonly SynchronizationContext UpdateThread;
        public SongSelect SongSelect;
        public SkinManager SkinManager;
        public Skin[] DefaultSkins;
        public List<Skin> CustomSkins { get; internal set; } = [];

        public Dictionary<string, ModPanel> ModPanels = new Dictionary<string, ModPanel>();
        public ModPanel AutoPanel = null!;
        public ReplayPlayer? Player = null;
        public HotkeyExitOverlay? HotkeyExitOverlay = null;
        public OsuConfigManager OsuConfigManager = null!;
        public FrameworkConfigManager FrameworkConfigManager = null!;
        public Server(
            SynchronizationContext syncContext,
            SongSelect songSelect,
            SkinManager skinManager,
            Skin[] defaultSkins,
            OsuConfigManager osuConfigManager,
            FrameworkConfigManager frameworkConfigManager
        ) : base()
        {
            UpdateThread = syncContext;
            SongSelect = songSelect;
            SkinManager = skinManager;
            DefaultSkins = defaultSkins;
            OsuConfigManager = osuConfigManager;
            FrameworkConfigManager = frameworkConfigManager;
            AddGET("/loadConfig", new loadConfigEndpoint(this).Handler);
            AddGET("/saveConfig", new saveConfigEndpoint(this).Handler);
            AddGET("/startMap", new startMapEndpoint(this).Handler);
            AddGET("/stopMap", new stopMapEndpoint(this).Handler);
        }
    }
}
