#pragma warning disable IDE0073

using System;
using System.Net;

namespace osu.Game.BellaFiora.Utils
{
    public abstract class Endpoint<T>
        where T : BaseServer
    {
        protected virtual T Server { get; private set; }
        public Endpoint(T server)
        {
            Server = server;
        }
        public abstract Func<HttpListenerRequest, bool> Handler { get; }
    }
}
