#pragma warning disable IDE0073

using System;
using System.Net;
using osu.Game.BellaFiora.Utils;

namespace osu.Game.BellaFiora.Endpoints
{
    public class saveConfigEndpoint : Endpoint<BellaFioraServer>
    {
        public saveConfigEndpoint(BellaFioraServer server) : base(server) { }
        public override Func<HttpListenerRequest, bool> Handler => request =>
        {
            callback();
            return true;
        };
        private void callback()
        {
            Server.UpdateThread.Post(_ =>
            {
                Respond(
                    "h1", "Received saveConfig request",
                    "p", $"OsuConfigManager Saved: {Server.OsuConfigManager.Save()}",
                    "p", $"FrameworkConfigManager Saved: {Server.FrameworkConfigManager.Save()}"
                );
            }, null);
        }
    }
}
