#pragma warning disable IDE0073

using System;
using System.Net;
using osu.Game.BellaFiora.Utils;
using System.IO;
using osu.Game.Beatmaps.Formats;
using osu.Game.IO;

using osu.Game.Beatmaps;
using osu.Game.Tests.Beatmaps;
using osu.Game.Rulesets;

namespace osu.Game.BellaFiora.Endpoints
{
    public class loadOsuFileEndpoint : Endpoint<Server>
    {
        public loadOsuFileEndpoint(Server server) : base(server) { }
        public override Func<HttpListenerRequest, bool> Handler => request =>
        {
            var QueryString = request.QueryString;
            string? pathStr = QueryString["path"];
            string? modeStr = QueryString["mode"];

            if (pathStr != null)
            {
                callback(pathStr, modeStr);
                return true;
            }
            return false;
        };
        private void callback(string path, string? mode)
        {
            Server.UpdateThread.Post(_ =>
            {
                using (Stream stream = File.OpenRead(path))
                using (var reader = new LineBufferedReader(stream))
                {
                    var beatmap = Decoder.GetDecoder<Beatmap>(reader).Decode(reader);
                    beatmap.BeatmapInfo.UpdateStatisticsFromBeatmap(beatmap);

                    var wBeatmap = new TestWorkingBeatmap(beatmap);
                    Server.RespondJSON(wBeatmap.GetPlayableBeatmap(mode == null || mode.Length == 0 ? beatmap.BeatmapInfo.Ruleset : new RulesetInfo { ShortName = mode, Available = true }));
                }
            }, null);
        }
    }
}
