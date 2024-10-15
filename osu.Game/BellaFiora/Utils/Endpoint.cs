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
        protected void Respond(params object[] args) => Server.Respond(args);
        public abstract Func<HttpListenerRequest, bool> GetHandler();
    }
}
