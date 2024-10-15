#pragma warning disable IDE0073

using System;

namespace osu.Game.BellaFiora.Utils
{
    public static class Formatters
    {
        public static Func<object, string?> UnitFormatter { get; } = e => e?.ToString();
    }
}
