#pragma warning disable IDE0073

using System;
using System.Net;

namespace osu.Game.BellaFiora.Endpoints
{
    public class loadConfigEndpoint : Endpoint<BellaFioraServer>
    {
        public loadConfigEndpoint(BellaFioraServer server) : base(server) { }
        public override Func<HttpListenerRequest, bool> GetHandler() => handler;
        private bool handler(HttpListenerRequest request)
        {
            callback("");
            return true;
        }
        private void callback(string config)
        {
            Server.UpdateThread.Post(_ =>
            {
                // LocalConfig.Load(config);
                Server.LocalConfig.Load();
                Respond(
                    "h1", "Received loadConfig request",
                    "p", $"Config: {config}",
                    "ul",
                        config.Split("\n"),
                        (Func<string, string>)(e => e)
                );
            }, null);
        }
    }
}
