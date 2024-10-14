#pragma warning disable IDE0073

// using osu.Framework.Logging;
using System.Threading;
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
        public static void CarouselBeatmapsTrulyLoaded(SongSelect songSelect)
        {
            if (server == null && SynchronizationContext.Current != null)
            {
                server = new Server(SynchronizationContext.Current, songSelect, skinManager, defaultSkins);
                server.Listen();
            }
        }
        public static void ModPanelLoadComplete(ModPanel panel)
        {
            if (!server.ModPanels.ContainsKey(panel.Mod.Acronym)) server.ModPanels.Add(panel.Mod.Acronym, panel);
            if (server.AutoPanel == null && panel.Mod.Acronym == "AT") server.AutoPanel = panel;
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
            if (player is SoloPlayer soloPlayer) server.Player = soloPlayer;
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
    }
}
