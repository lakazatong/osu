#pragma warning disable IDE0073

using System;

namespace osu.Game.BellaFiora.Tests
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            TestServer server = new TestServer();
            server.Listen();
        }
    }
}
