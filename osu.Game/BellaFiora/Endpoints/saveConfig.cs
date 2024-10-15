#pragma warning disable IDE0073

using System;
using System.Net;

namespace osu.Game.BellaFiora.Endpoints
{
    public class saveConfigEndpoint : Endpoint<BellaFioraServer>
    {
        public saveConfigEndpoint(BellaFioraServer server) : base(server) { }
        public override Func<HttpListenerRequest, bool> GetHandler() => handler;
        private bool handler(HttpListenerRequest request)
        {
            callback();
            return true;
        }
        private void callback()
        {
            Server.UpdateThread.Post(_ =>
            {
                bool saved = Server.LocalConfig.Save();
                Respond(
                    "h1", "Received saveConfig request",
                    "p", $"Saved: {saved}"
                );
            }, null);
        }
    }
}
