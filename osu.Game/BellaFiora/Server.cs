#pragma warning disable IDE0073

// using osu.Framework.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Logging;
using osu.Game.Overlays.Mods;
using osu.Game.Rulesets.Mods;
using osu.Game.Screens.Play;
using osu.Game.Screens.Select;
using osu.Game.Screens.Select.Carousel;
using osu.Game.Skinning;

namespace osu.Game.BellaFiora
{
    public class Server
    {
        private HttpListener listener;
        private HttpListenerContext context = null!;
        private readonly SynchronizationContext syncContext;
        public SongSelect SongSelect;
        public SkinManager SkinManager;
        public Skin[] DefaultSkins;
        public List<Skin> CustomSkins { get; internal set; } = [];
        public Dictionary<string, ModPanel> ModPanels = new Dictionary<string, ModPanel>();
        public ModPanel AutoPanel = null!;
        public SoloPlayer? Player = null;
        public HotkeyExitOverlay? HotkeyExitOverlay = null;
        public Server(SynchronizationContext syncContext, SongSelect songSelect, SkinManager skinManager, Skin[] defaultSkins)
        {
            listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:8080/");
            this.syncContext = syncContext;
            SongSelect = songSelect;
            SkinManager = skinManager;
            DefaultSkins = defaultSkins;
        }
        public void Listen()
        {
            listener.Start();
            beginListening();
        }
        private void beginListening()
        {
            listener.BeginGetContext(new AsyncCallback(handleRequest), listener);
        }
        private void respond(string msg)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(msg);
            context.Response.ContentLength64 = buffer.Length;
            context.Response.ContentType = "text/html";
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.OutputStream.Close();
        }
        private void startMap(int beatmapId, string modsStr, int skin)
        {
            syncContext.Post(_ =>
            {
                CarouselBeatmap? carouselBeatmap = SongSelect.GetCarouselBeatmap(beatmapId);
                if (carouselBeatmap == null)
                {
                    respond($"<html><body><h1>Received recordMap request</h1>" +
                    $"<p>Beatmap ID: {beatmapId}</p>" +
                    $"<p>Skin: {skin}</p>" +
                    $"<p>Requested Mods:</p>" +
                    $"<ul>{string.Join("", modsStr.Split('+').Select(acronym => $"<li>{acronym}</li>"))}</ul>" +
                    $"<p>Do not have this beatmap</p>" +
                    $"</body></html>");
                    return;
                }

                ModPanels.Values.ForEach(p => p.ForceDeselect());

                var selectedModPanels = new List<ModPanel>();

                string pattern = string.Join("|", ModPanels.Keys.Select(k => Regex.Escape(k)));
                Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);

                var matches = regex.Matches(modsStr);

                foreach (Match match in matches)
                {
                    if (ModPanels.TryGetValue(match.Value, out var panel))
                    {
                        if (!(panel.Mod.Type is ModType.Automation)) selectedModPanels.Add(panel);
                    }
                }

                selectedModPanels.ForEach(p => p.ForceSelect());
                AutoPanel.ForceSelect();

                if (skin < 10)
                {
                    // reserved to default skins
                    // 0: DefaultLegacySkin
                    // 1: TrianglesSkin
                    // 2: ArgonSkin
                    // 3: ArgonProSkin
                    // 4-9: fallback to 0
                    if (skin is < 0 or > 3) skin = 0;
                    SkinManager.CurrentSkinInfo.Value = DefaultSkins[skin].SkinInfo;
                }
                else
                {
                    // custom skin ID
                    if (skin < CustomSkins.Count) SkinManager.CurrentSkinInfo.Value = CustomSkins[skin].SkinInfo;
                    else SkinManager.CurrentSkinInfo.Value = DefaultSkins[0].SkinInfo;
                }

                bool started = SongSelect.StartMap(beatmapId);

                respond($"<html><body><h1>Received recordMap request</h1>" +
                    $"<p>Beatmap ID: {beatmapId}</p>" +
                    $"<p>Skin: {skin}</p>" +
                    $"<p>Requested Mods:</p>" +
                    $"<ul>{string.Join("", modsStr.Split('+').Select(acronym => $"<li>{acronym}</li>"))}</ul>" +
                    $"<p>Selected Mods:</p>" +
                    $"<ul>{string.Join("", selectedModPanels.Select(p => $"<li>{p.Mod.Acronym}: {p.Mod.Name}</li>"))}</ul>" +
                    $"<p>All Mods:</p>" +
                    $"<ul>{string.Join("", ModPanels.Values.Select(p => $"<li>{p.Mod.Acronym}: {p.Mod.Name}</li>"))}</ul>" +
                    $"</body></html>");
            }, null);
        }
        private void stopMap()
        {
            syncContext.Post(_ =>
            {
                HotkeyExitOverlay?.Action.Invoke();
                context.Response.Close();
            }, null);
        }
        private bool handleStartMap(HttpListenerRequest request)
        {
            var QueryString = request.QueryString;
            string? beatmapIdStr = QueryString["beatmapId"];
            string? modsStr = QueryString["mods"];
            string? skinStr = QueryString["skin"];

            if (int.TryParse(beatmapIdStr, out int beatmapId) &&
                !string.IsNullOrEmpty(modsStr) &&
                int.TryParse(skinStr, out int skin))
            {
                startMap(beatmapId, modsStr, skin);
                return true;
            }
            return false;
        }
        private bool handleStopMap(HttpListenerRequest request)
        {
            stopMap();
            return true;
        }
        private void handleRequest(IAsyncResult result)
        {
            context = listener.EndGetContext(result);
            var request = context.Request;

            try
            {
                if (request.HttpMethod == "GET")
                {
                    if (
                        request.Url?.AbsolutePath == "/startMap" && handleStartMap(request)
                    || request.Url?.AbsolutePath == "/stopMap" && handleStopMap(request)
                    )
                    {
                        beginListening();
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Error in handleRequest: " + ex.Message, LoggingTarget.Information, LogLevel.Error);
            }

            respond("<html><body><h1>Invalid request</h1></body></html>");
            beginListening();
        }
    }
}
