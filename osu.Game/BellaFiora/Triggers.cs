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
using osu.Game.Screens.Select;

namespace osu.Game.BellaFiora
{
    public class Server
    {
        private HttpListener listener;
        private HttpListenerContext context = null!;
        private readonly SynchronizationContext syncContext;
        private SongSelect songSelect;
        public Dictionary<string, ModPanel> ModPanels = new Dictionary<string, ModPanel>();
        public ModPanel AutoPanel = null!;

        public Server(SynchronizationContext syncContext, SongSelect songSelect)
        {
            listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:8080/");
            this.syncContext = syncContext;
            this.songSelect = songSelect;
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
        private void startMap(int beatmapId, string modsStr, int skin)
        {
            syncContext.Post(_ =>
            {
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

                songSelect.StartMap(beatmapId);

                string responseString =
                    $"<html><body><h1>Received recordMap request</h1>" +
                    $"<p>Beatmap ID: {beatmapId}</p>" +
                    $"<p>Skin: {skin}</p>" +
                    $"<p>Requested Mods:</p>" +
                    $"<ul>{string.Join("", modsStr.Split('+').Select(acronym => $"<li>{acronym}</li>"))}</ul>" +
                    $"<p>Selected Mods:</p>" +
                    $"<ul>{string.Join("", selectedModPanels.Select(p => $"<li>{p.Mod.Acronym}: {p.Mod.Name}</li>"))}</ul>" +
                    $"<p>All Mods:</p>" +
                    $"<ul>{string.Join("", ModPanels.Values.Select(p => $"<li>{p.Mod.Acronym}: {p.Mod.Name}</li>"))}</ul>" +
                    $"</body></html>";

                byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                context.Response.ContentLength64 = buffer.Length;
                context.Response.ContentType = "text/html";
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                context.Response.OutputStream.Close();
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
                    // || request.Url?.AbsolutePath == "/endpoint2" && handleEnpoint2(request)
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

            string errorResponse = "<html><body><h1>Invalid request</h1></body></html>";
            byte[] buffer = Encoding.UTF8.GetBytes(errorResponse);
            context.Response.ContentLength64 = buffer.Length;
            context.Response.ContentType = "text/html";
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.OutputStream.Close();
            beginListening();
        }
    }

    public class Triggers
    {
        private static Server server = null!;

        public static void CarouselBeatmapsTrulyLoaded(SongSelect songSelect)
        {
            if (SynchronizationContext.Current != null && server == null)
            {
                server = new Server(SynchronizationContext.Current, songSelect);
                server.Listen();
            }
        }

        public static void ModPanelLoadComplete(ModPanel panel)
        {
            server.ModPanels.Add(panel.Mod.Acronym, panel);
            if (panel.Mod.Acronym == "AT") server.AutoPanel = panel;
        }

        public static void FooterButtonModsLoadComplete(FooterButtonMods button)
        {
            button.TriggerClick();
        }
    }
}
