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
using osu.Game.Rulesets.Objects;
using osu.Game.Beatmaps.ControlPoints;
using System.Linq;

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
        private double snapStartTime(double timeMs, double resolutionMs, double previousNoteTime)
        {
            double adjustedTime = timeMs - previousNoteTime;
            return Math.Round(adjustedTime / resolutionMs) * resolutionMs + previousNoteTime;
        }
        private void tryRestoreSnapping(IBeatmap beatmap)
        {
            double previousNoteTime = (double)beatmap.HitObjects[0].StartTime;
            foreach (HitObject o in beatmap.HitObjects.Skip(1).ToList())
            {
                TimingControlPoint cp = beatmap.ControlPointInfo.TimingPointAt(o.StartTime);
                o.StartTime = snapStartTime((double)o.StartTime, cp.BeatLength / cp.TimeSignature.Numerator, previousNoteTime);
                // previousNoteTime = o.StartTime;
            }

        }
        private void callback(string path, string? mode)
        {
            Server.UpdateThread.Post(_ =>
            {
                using (Stream stream = File.OpenRead(path))
                using (var reader = new LineBufferedReader(stream))
                {
                    var beatmap = Decoder.GetDecoder<Beatmap>(reader).Decode(reader);
                    beatmap.BeatmapInfo.UpdateStatisticsFromBeatmap(beatmap);

                    IBeatmap playableBeatmap = new TestWorkingBeatmap(beatmap).GetPlayableBeatmap(mode == null || mode.Length == 0 ? beatmap.BeatmapInfo.Ruleset : new RulesetInfo { ShortName = mode, Available = true });
                    // tryRestoreSnapping(playableBeatmap);

                    Server.RespondJSON(playableBeatmap);
                }
            }, null);
        }
    }
}
