#pragma warning disable IDE0073

using System.Collections.Generic;
using System.Threading;
using osu.Game.BellaFiora.Endpoints;
using osu.Game.Configuration;
using osu.Game.Overlays.Mods;
using osu.Game.Screens.Play;
using osu.Game.Screens.Select;
using osu.Game.Skinning;

namespace osu.Game.BellaFiora
{
    public class BellaFioraServer : BaseServer
    {
        public readonly SynchronizationContext UpdateThread;
        public SongSelect SongSelect;
        public SkinManager SkinManager;
        public Skin[] DefaultSkins;
        public List<Skin> CustomSkins { get; internal set; } = [];

        public Dictionary<string, ModPanel> ModPanels = new Dictionary<string, ModPanel>();
        public ModPanel AutoPanel = null!;
        public SoloPlayer? Player = null;
        public HotkeyExitOverlay? HotkeyExitOverlay = null;
        public OsuConfigManager LocalConfig = null!;
        public BellaFioraServer(
            SynchronizationContext syncContext,
            SongSelect songSelect,
            SkinManager skinManager,
            Skin[] defaultSkins,
            OsuConfigManager localConfig
        ) : base()
        {
            UpdateThread = syncContext;
            SongSelect = songSelect;
            SkinManager = skinManager;
            DefaultSkins = defaultSkins;
            LocalConfig = localConfig;
            AddGET("/loadConfig", new loadConfigEndpoint(this).GetHandler());
            AddGET("/saveConfig", new saveConfigEndpoint(this).GetHandler());
            AddGET("/startMap", new startMapEndpoint(this).GetHandler());
            AddGET("/stopMap", new stopMapEndpoint(this).GetHandler());
        }
    }
}
