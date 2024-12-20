#pragma warning disable IDE0073

using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace osu.Game.BellaFiora.Utils
{
    public class BaseServer
    {
        private HttpListener listener;
        private HttpListenerContext context = null!;
#pragma warning disable IDE1006
        private Dictionary<string, Func<HttpListenerRequest, bool>> GETHandlers = new Dictionary<string, Func<HttpListenerRequest, bool>>();
        private Dictionary<string, Func<HttpListenerRequest, bool>> POSTHandlers = new Dictionary<string, Func<HttpListenerRequest, bool>>();
        private Dictionary<string, Func<HttpListenerRequest, bool>> PUTHandlers = new Dictionary<string, Func<HttpListenerRequest, bool>>();
#pragma warning restore IDE1006
        private Dictionary<string, Dictionary<string, Func<HttpListenerRequest, bool>>> getHandlers = new Dictionary<string, Dictionary<string, Func<HttpListenerRequest, bool>>>();
        public BaseServer()
        {
            listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:8080/");
            getHandlers.Add("GET", GETHandlers);
            getHandlers.Add("POST", POSTHandlers);
            getHandlers.Add("PUT", PUTHandlers);
        }
        public void Start()
        {
            listener.Start();
            receive();
            Console.WriteLine("Server started");
        }
        private void receive()
        {
            listener.BeginGetContext(new AsyncCallback(handleRequest), listener);
        }
        public void Stop()
        {
            listener.Stop();
            listener.Close();
            Console.WriteLine("Server stopped");
        }
        protected void AddGET(string path, Func<HttpListenerRequest, bool> handler) => GETHandlers[path] = handler;
        protected void AddPOST(string path, Func<HttpListenerRequest, bool> handler) => POSTHandlers[path] = handler;
        protected void AddPUT(string path, Func<HttpListenerRequest, bool> handler) => PUTHandlers[path] = handler;
        public string BuildHTML(params object[] args)
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
                            var formatItem = (Func<object, string?>)args[i + 1];
                            htmlBuilder.Append("<ul>");
                            foreach (object item in items) htmlBuilder.AppendFormat($"<li>{formatItem(item) ?? string.Empty}</li>");
                            htmlBuilder.Append("</ul>");
                            i++;
                            break;

                        default:
                            throw new ArgumentException($"Unsupported tag: {tag}");
                    }
                }

            }

            htmlBuilder.Append("</body></html>");
            return htmlBuilder.ToString();
        }
        public void RespondHTML(params object[] args)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(BuildHTML(args));
            context.Response.ContentLength64 = buffer.Length;
            context.Response.ContentType = "text/html";
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.OutputStream.Close();
        }
        public void RespondJSON(object obj)
        {
            string jsonResponse = JsonConvert.SerializeObject(obj);
            byte[] buffer = Encoding.UTF8.GetBytes(jsonResponse);
            context.Response.ContentLength64 = buffer.Length;
            context.Response.ContentType = "application/json";
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.OutputStream.Close();
        }
        private bool tryHandleRequest(IAsyncResult result)
        {
            try
            {
                var context = listener.EndGetContext(result);
                var request = context.Request;
                this.context = context;

                if (request.Url == null) return false;
                var handlers = getHandlers[request.HttpMethod];

                if (handlers != null && handlers.TryGetValue(request.Url.AbsolutePath, out var handler) && handler != null)
                {
                    return handler(request);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("tryHandleRequest: " + ex.Message);
            }

            return false;
        }
        private void handleRequest(IAsyncResult result)
        {
            if (!listener.IsListening) return;

            if (!tryHandleRequest(result))
            {
                RespondHTML(
                    "h1", "Invalid request"
                );
            }

            receive();
        }
        public static Func<object, string?> UnitFormatter { get; } = o => o?.ToString();
    }
}
