#pragma warning disable IDE0073

using System;

namespace osu.Game.BellaFiora.Tests
{
    public static class Program
    {
        private static bool running = true;
        [STAThread]
        public static void Main()
        {
            Console.CancelKeyPress += (sender, e) => { e.Cancel = true; running = false; };
            TestServer server = new TestServer();
            server.Start();
            while (running) { }
            server.Stop();
        }
    }
}
