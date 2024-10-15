#pragma warning disable IDE0073

namespace osu.Game.BellaFiora.Tests
{
    public class TestServer : BaseServer
    {
        public TestServer() : base()
        {
            AddGET("/test", new testEndpoint(this).GetHandler());
        }
    }
}
