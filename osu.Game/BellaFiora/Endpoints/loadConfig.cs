#pragma warning disable IDE0073

using System;
using System.Net;
using osu.Game.BellaFiora.Utils;

namespace osu.Game.BellaFiora.Endpoints
{
    public class loadConfigEndpoint : Endpoint<Server>
    {
        public loadConfigEndpoint(Server server) : base(server) { }
        public override Func<HttpListenerRequest, bool> Handler => request =>
        {
            callback("", "");
            return true;
        };
        private void callback(string osuConfig, string frameworkConfig)
        {
            Server.UpdateThread.Post(_ =>
            {
                Server.OsuConfigManager.Load("a");
                Server.FrameworkConfigManager.Load("b");
                Server.RespondHTML(
                    "h1", "Received loadConfig request",
                    "p", $"OsuConfig: {osuConfig}",
                    "ul",
                        osuConfig.Split("\n"),
                        BaseServer.UnitFormatter,
                    "p", $"FrameworkConfig: {frameworkConfig}",
                    "ul",
                        frameworkConfig.Split("\n"),
                        BaseServer.UnitFormatter
                );
            }, null);
        }
    }
}
