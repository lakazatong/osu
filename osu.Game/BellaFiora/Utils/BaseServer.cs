#pragma warning disable IDE0073

using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using osu.Framework.Logging;

namespace osu.Game.BellaFiora.Utils
{
    public class BaseServer
    {
        protected HttpListener Listener;
        protected HttpListenerContext Context = null!;
#pragma warning disable IDE1006
        private Dictionary<string, Func<HttpListenerRequest, bool>> GETHandlers = new Dictionary<string, Func<HttpListenerRequest, bool>>();
        private Dictionary<string, Func<HttpListenerRequest, bool>> POSTHandlers = new Dictionary<string, Func<HttpListenerRequest, bool>>();
        private Dictionary<string, Func<HttpListenerRequest, bool>> PUTHandlers = new Dictionary<string, Func<HttpListenerRequest, bool>>();
#pragma warning restore IDE1006
        private Dictionary<string, Dictionary<string, Func<HttpListenerRequest, bool>>> getHandlers = new Dictionary<string, Dictionary<string, Func<HttpListenerRequest, bool>>>();
        public BaseServer()
        {
            Listener = new HttpListener();
            Listener.Prefixes.Add("http://localhost:8080/");
            getHandlers.Add("GET", GETHandlers);
            getHandlers.Add("POST", POSTHandlers);
            getHandlers.Add("PUT", PUTHandlers);
        }
        public void Listen()
        {
            Listener.Start();
            beginListening();
        }
        protected void AddGET(string path, Func<HttpListenerRequest, bool> handler) => GETHandlers[path] = handler;
        protected void AddPOST(string path, Func<HttpListenerRequest, bool> handler) => POSTHandlers[path] = handler;
        protected void AddPUT(string path, Func<HttpListenerRequest, bool> handler) => PUTHandlers[path] = handler;
        public byte[] BuildHTML(params object[] args)
        {
            StringBuilder htmlBuilder = new StringBuilder();
            htmlBuilder.Append("<!DOCTYPE html><html><body>");

            if (args is not null && args.Length > 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i] is null) throw new ArgumentNullException();

                    string tag = args[i].ToString() ?? string.Empty;

                    switch (tag.ToLowerInvariant())
                    {
                        case "h1":
                        case "h2":
                            htmlBuilder.AppendFormat($"<{tag}>{args[++i]}</{tag}>");
                            break;

                        case "p":
                            htmlBuilder.AppendFormat($"<p>{args[++i]}</p>");
                            break;

                        case "ul":
                            i++;
                            IEnumerable<object> items = (IEnumerable<object>)args[i];
                            dynamic formatItem = (Func<dynamic, string>)args[i + 1];
                            htmlBuilder.Append("<ul>");
                            foreach (object item in items) htmlBuilder.AppendFormat($"<li>{formatItem(item)}</li>");
                            htmlBuilder.Append("</ul>");
                            i++;
                            break;

                        default:
                            throw new ArgumentException($"Unsupported tag: {tag}");
                    }
                }

            }

            htmlBuilder.Append("</body></html>");
            return Encoding.UTF8.GetBytes(htmlBuilder.ToString());
        }
        public void Respond(params object[] args)
        {
            byte[] buffer = BuildHTML(args);
            Context.Response.ContentLength64 = buffer.Length;
            Context.Response.ContentType = "text/html";
            Context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            Context.Response.OutputStream.Close();
        }
        private bool tryHandleRequest(IAsyncResult result)
        {
            try
            {
                var request = Listener.EndGetContext(result).Request;
                if (request.Url == null) return false;
                var handlers = getHandlers[request.HttpMethod];
                if (handlers != null && handlers.TryGetValue(request.Url.AbsolutePath, out var handler) && handler != null && handler(request)) return true;
            }
            catch (Exception ex)
            {
                Logger.Log("Error in tryHandleRequest: " + ex.Message, LoggingTarget.Information, LogLevel.Error);
            }
            return false;
        }
        private void handleRequest(IAsyncResult result)
        {
            if (!tryHandleRequest(result))
            {
                Respond(
                    "h1", "Invalid request"
                );
            }
            Context.Response.Close();
            beginListening();
        }
        private void beginListening()
        {
            Listener.BeginGetContext(new AsyncCallback(handleRequest), Listener);
        }
    }
}
