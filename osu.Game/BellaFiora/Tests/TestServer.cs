#pragma warning disable IDE0073

using osu.Game.BellaFiora.Utils;

namespace osu.Game.BellaFiora.Tests
{
    public class TestServer : BaseServer
    {
        public TestServer() : base()
        {
            AddGET("/test", new testEndpoint(this).Handler);
        }
    }
}
