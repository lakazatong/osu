#pragma warning disable IDE0073

using System;
using System.Net;
using osu.Game.BellaFiora.Utils;

namespace osu.Game.BellaFiora.Endpoints
{
    public class stopMapEndpoint : Endpoint<Server>
    {
        public stopMapEndpoint(Server server) : base(server) { }
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
                Server.RespondHTML(
                    "h1", "Received stopMap request",
                    "p", "Map stopped"
                );
            }, null);
        }
    }
}
