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
            callback("");
            return true;
        };
        private void callback(string config)
        {
            Server.UpdateThread.Post(_ =>
            {
                // LocalConfig.Load(config);
                Server.OsuConfigManager.Load();
                // Server.FrameworkConfigManager.Load("");
                Server.FrameworkConfigManager.Load();
                Respond(
                    "h1", "Received loadConfig request",
                    "p", $"Config: {config}",
                    "ul",
                        config.Split("\n"),
                        Formatters.UnitFormatter
                );
            }, null);
        }
    }
}
