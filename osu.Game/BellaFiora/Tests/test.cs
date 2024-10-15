#pragma warning disable IDE0073

using System;
using System.Net;
using osu.Game.BellaFiora.Utils;

namespace osu.Game.BellaFiora.Tests
{
    public class testEndpoint : Endpoint<TestServer>
    {
        public testEndpoint(TestServer server) : base(server) { }
        public override Func<HttpListenerRequest, bool> Handler => request =>
        {
            callback();
            return true;
        };
        private void callback()
        {
            Respond(
                "h1", "Received recordMap request",
                "p", $"Beatmap ID: {0}",
                "p", $"Skin: {0}",
                "p", "Requested Mods:",
                "ul",
                    "RX+DT+HD".Split('+'),
                    Formatters.UnitFormatter,
                "p", "Do not have this beatmap"
            );
        }
    }
}
