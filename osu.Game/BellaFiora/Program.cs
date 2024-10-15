#pragma warning disable IDE0073

using System;
using osu.Game.BellaFiora.Tests;

namespace osu.Game.BellaFiora
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
