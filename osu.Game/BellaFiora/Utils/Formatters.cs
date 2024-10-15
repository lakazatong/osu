#pragma warning disable IDE0073

using System;
using osu.Game.Overlays.Mods;

namespace osu.Game.BellaFiora.Utils
{
    public static class Formatters
    {
        public static Func<object, string?> UnitFormatter { get; } = o => o?.ToString();
        public static Func<object, string?> FormatPanel { get; } = o =>
        {
            var p = (ModPanel)o;
            return $"{p.Mod.Acronym}: {p.Mod.Name}";
        };
    }
}
