#pragma warning disable IDE0073

// using osu.Framework.Logging;
using System;
using System.Net;
using System.Text;
using System.Threading;
using osu.Framework.Logging;

namespace osu.Game.BellaFiora
{
    public class Server
    {
        private HttpListener listener;
        private HttpListenerContext context = null!;
        private readonly SynchronizationContext syncContext;
        private Screens.Select.SongSelect songSelect;

        public Server(SynchronizationContext syncContext, Screens.Select.SongSelect songSelect)
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

        private void startMap(int beatmapId, int mods, int skin)
        {
            syncContext.Post(_ =>
            {
                songSelect.StartMap(beatmapId);
                string responseString = $"<html><body><h1>Received recordMap request</h1>" +
                                        $"<p>Beatmap ID: {beatmapId}</p>" +
                                        $"<p>Mods: {mods}</p>" +
                                        $"<p>Skin: {skin}</p></body></html>";
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
                int.TryParse(modsStr, out int mods) &&
                int.TryParse(skinStr, out int skin))
            {
                startMap(beatmapId, mods, skin);
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

        public static void CarouselBeatmapsTrulyLoaded(Screens.Select.SongSelect songSelect)
        {
            if (SynchronizationContext.Current != null && server == null)
            {
                server = new Server(SynchronizationContext.Current, songSelect);
                server.Listen();
            }
        }
    }
}
