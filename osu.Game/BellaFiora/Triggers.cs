#pragma warning disable IDE0073

// using osu.Framework.Logging;
using System.Collections.Generic;
using System.Threading;
using osu.Framework.Configuration;
using osu.Game.Configuration;
using osu.Game.Overlays.Mods;
using osu.Game.Screens.Play;
using osu.Game.Screens.Select;
using osu.Game.Skinning;

namespace osu.Game.BellaFiora
{
    public class Triggers
    {
        private static Server server = null!;
        private static SkinManager skinManager = null!;
        private static Skin[] defaultSkins = null!;
        private static bool footerButtonModsLoadCompleteTrigged = false;
        private static OsuConfigManager osuConfigManager = null!;
        private static FrameworkConfigManager frameworkConfigManager = null!;
        private static Dictionary<string, ModPanel> modPanels = new Dictionary<string, ModPanel>();
        private static ModPanel autoPanel = null!;
        public static void CarouselBeatmapsTrulyLoaded(SongSelect songSelect)
        {
            if (server == null && SynchronizationContext.Current != null)
            {
                server = new Server(SynchronizationContext.Current, songSelect, skinManager, defaultSkins, osuConfigManager, frameworkConfigManager)
                {
                    ModPanels = modPanels,
                    AutoPanel = autoPanel
                };
                server.Start();
            }
        }
        public static void ModPanelLoadComplete(ModPanel panel)
        {
            if (server == null)
            {
                if (!modPanels.ContainsKey(panel.Mod.Acronym)) modPanels.Add(panel.Mod.Acronym, panel);
                if (autoPanel == null && panel.Mod.Acronym == "AT") autoPanel = panel;
            }
            else
            {
                if (!server.ModPanels.ContainsKey(panel.Mod.Acronym)) server.ModPanels.Add(panel.Mod.Acronym, panel);
                if (server.AutoPanel == null && panel.Mod.Acronym == "AT") server.AutoPanel = panel;
            }
        }
        public static void FooterButtonModsLoadComplete(FooterButtonMods button)
        {
            if (!footerButtonModsLoadCompleteTrigged)
            {
                // this will trigger the creation of all mod panels
                button.TriggerClick();
                footerButtonModsLoadCompleteTrigged = true;
            }
        }
        public static void PlayerLoaded(Player player, HotkeyExitOverlay hotkeyExitOverlay)
        {
            if (player is ReplayPlayer replayPlayer) server.Player = replayPlayer;
            server.HotkeyExitOverlay = hotkeyExitOverlay;
        }
        public static void SkinManagerCreated(SkinManager sm, Skin[] ds)
        {
            if (skinManager == null)
            {
                skinManager = sm;
                defaultSkins = ds;
            }
        }
        public static void LocalConfigLoaded(OsuConfigManager lc, FrameworkConfigManager fcm)
        {
            osuConfigManager = lc;
            frameworkConfigManager = fcm;
        }
    }
}
