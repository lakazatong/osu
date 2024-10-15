#pragma warning disable IDE0073

using System;
using System.Net;
using osu.Game.BellaFiora.Utils;

namespace osu.Game.BellaFiora.Endpoints
{
    public class stopMapEndpoint : Endpoint<BellaFioraServer>
    {
        public stopMapEndpoint(BellaFioraServer server) : base(server) { }
        public override Func<HttpListenerRequest, bool> Handler => request =>
        {
            callback();
            return true;
        };
        private void callback()
        {
            Server.UpdateThread.Post(_ =>
            {
                Server.HotkeyExitOverlay?.Action.Invoke();
                Respond(
                    "h1", "Received stopMap request",
                    "p", "Map stopped"
                );
            }, null);
        }
    }
}
