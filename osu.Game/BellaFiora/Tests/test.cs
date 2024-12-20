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
            Server.RespondHTML(
                "h1", "Received test request"
            );
        }
    }
}
