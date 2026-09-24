using System.Collections.Generic;
using GPGR.Charting;
using UnityEngine;

namespace GPGR.Runtime
{
    /// <summary>
    /// 히트마다 Good 또는 Perfect가 나오는 시각에 누름을 보낸다.
    /// 보내는 박은 예정 시각이다. 프레임이 늦어도 그 박으로 판정한다.
    /// </summary>
    public sealed class AutoPlayHitSource : HitSource
    {
        readonly List<PlannedHit> _plan = new List<PlannedHit>();
        int _cursor;

        internal void Arm(SongChart chart)
        {
            _plan.Clear();
            _cursor = 0;
            if (chart == null)
                return;

            var windows = ChartLayout.Build(chart);
            var rng = new System.Random(chart.autoSeed);
            int order = 0;

            for (int i = 0; i < windows.Count; i++)
            {
                PhaseWindow window = windows[i];
                if (chart.phases == null || window.PhaseIndex < 0 || window.PhaseIndex >= chart.phases.Count)
                    continue;

                PhaseDefinition phase = chart.phases[window.PhaseIndex];
                if (phase?.rows == null)
                    continue;

                for (int r = 0; r < phase.rows.Count; r++)
                {
                    NoteRow row = phase.rows[r];
                    if (row?.hitBeats == null)
                        continue;

                    for (int h = 0; h < row.hitBeats.Count; h++)
                    {
                        double abs = window.OriginBeat + row.hitBeats[h];
                        double signedMs = ChooseSignedMs(chart, rng);
                        double fire = abs + chart.BeatsFromMs(signedMs);
                        _plan.Add(new PlannedHit(fire, false, order++));

                        if (row.kind == NoteKind.Long && h == 0)
                            _plan.Add(new PlannedHit(abs + row.tailBeats, true, order++));
                    }
                }
            }

            _plan.Sort((a, b) =>
            {
                int byBeat = a.Beat.CompareTo(b.Beat);
                if (byBeat != 0)
                    return byBeat;
                int byRelease = a.Release.CompareTo(b.Release);
                if (byRelease != 0)
                    return byRelease;
                return a.Order.CompareTo(b.Order);
            });
        }

        internal override void Poll(double songBeat)
        {
            while (_cursor < _plan.Count && _plan[_cursor].Beat <= songBeat + 1e-8)
            {
                PlannedHit ev = _plan[_cursor++];
                if (ev.Release)
                    RaiseReleased(ev.Beat);
                else
                    RaisePressed(ev.Beat);
            }
        }

        static double ChooseSignedMs(SongChart chart, System.Random rng)
        {
            JudgementWindows windows = chart.judgement;
            if (rng.NextDouble() >= chart.autoGoodRatio)
                return 0;

            double perfect = windows.perfectMs;
            double good = windows.goodMs;
            double span = good - perfect;
            double mag = span > 0
                ? perfect + span * (0.2 + 0.6 * rng.NextDouble())
                : good;

            if (windows.Classify(mag) != Judgement.Good && span > 0)
                mag = perfect + span * 0.5;

            if (rng.Next(0, 2) == 0)
                mag = -mag;
            return mag;
        }

        readonly struct PlannedHit
        {
            public readonly double Beat;
            public readonly bool Release;
            public readonly int Order;

            public PlannedHit(double beat, bool release, int order)
            {
                Beat = beat;
                Release = release;
                Order = order;
            }
        }
    }
}
