#pragma warning disable IDE0073

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Game.BellaFiora.Utils;
using osu.Game.Overlays.Mods;
using osu.Game.Rulesets.Mods;
using osu.Game.Screens.Select.Carousel;

namespace osu.Game.BellaFiora.Endpoints
{
    public class startMapEndpoint : Endpoint<Server>
    {
        public startMapEndpoint(Server server) : base(server) { }
        public override Func<HttpListenerRequest, bool> Handler => request =>
        {
            var QueryString = request.QueryString;
            string? beatmapIdStr = QueryString["beatmapId"];
            string? modsStr = QueryString["mods"];
            string? skinStr = QueryString["skin"];

            if (int.TryParse(beatmapIdStr, out int beatmapId) &&
                modsStr != null &&
                int.TryParse(skinStr, out int skin))
            {
                callback(beatmapId, modsStr, skin);
                return true;
            }
            return false;
        };
        private void callback(int beatmapId, string modsStr, int skin)
        {
            Server.UpdateThread.Post(_ =>
            {
                CarouselBeatmap? carouselBeatmap = Server.SongSelect.GetCarouselBeatmap(beatmapId);
                if (carouselBeatmap == null)
                {
                    Server.RespondHTML(
                        "h1", "Received recordMap request",
                        "p", $"Beatmap ID: {beatmapId}",
                        "p", $"Skin: {skin}",
                        "p", "Requested Mods:",
                        "ul",
                            modsStr.Split('+'),
                            (Func<object, string?>)(e => e.ToString()),
                        "p", "Do not have this beatmap"
                    );
                    return;
                }

                Server.ModPanels.Values.ForEach(p => p.ForceDeselect());

                var selectedModPanels = new List<ModPanel>();

                string pattern = string.Join("|", Server.ModPanels.Keys.Select(k => Regex.Escape(k)));
                Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);

                var matches = regex.Matches(modsStr);

                foreach (Match match in matches)
                {
                    if (Server.ModPanels.TryGetValue(match.Value, out var panel))
                    {
                        if (!(panel.Mod.Type is ModType.Automation)) selectedModPanels.Add(panel);
                    }
                }

                selectedModPanels.ForEach(p => p.ForceSelect());
                Server.AutoPanel.ForceSelect();

                if (skin < 10)
                {
                    // reserved to default skins
                    // 0: DefaultLegacySkin
                    // 1: TrianglesSkin
                    // 2: ArgonSkin
                    // 3: ArgonProSkin
                    // 4-9: fallback to 0
                    if (skin is < 0 or > 3) skin = 0;
                    Server.SkinManager.CurrentSkinInfo.Value = Server.DefaultSkins[skin].SkinInfo;
                }
                else
                {
                    // custom skin ID
                    if (skin - 10 < Server.CustomSkins.Count) Server.SkinManager.CurrentSkinInfo.Value = Server.CustomSkins[skin - 10].SkinInfo;
                    else Server.SkinManager.CurrentSkinInfo.Value = Server.DefaultSkins[0].SkinInfo;
                }

                bool started = Server.SongSelect.StartMap(beatmapId);

                Server.RespondHTML(
                    "h1", "Received recordMap request",
                    "p", $"Started: {started}",
                    "p", $"Beatmap ID: {beatmapId}",
                    "p", $"Skin: {skin}",
                    "p", "Requested Mods:",
                    "ul",
                        modsStr.Split('+'),
                        BaseServer.UnitFormatter,
                    "p", "Selected Mods:",
                    "ul",
                        selectedModPanels,
                        Server.FormatPanel,
                    "p", "All Mods:",
                    "ul",
                        Server.ModPanels.Values,
                        Server.FormatPanel
                );

            }, null);
        }
    }
}
