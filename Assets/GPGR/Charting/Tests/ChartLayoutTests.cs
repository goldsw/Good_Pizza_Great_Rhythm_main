using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace GPGR.Charting.Tests
{
    public class ChartLayoutTests
    {
        SongChart chart;

        [SetUp]
        public void SetUp()
        {
            chart = ScriptableObject.CreateInstance<SongChart>();
            chart.bpm = 120f;
            chart.judgement = JudgementWindows.Default;
            chart.defaultSpawnAngleDeg = 0f;
            chart.defaultHitAngleDeg = 270f;
            chart.phases = new List<PhaseDefinition>();
        }

        [TearDown]
        public void TearDown()
        {
            if (chart != null)
                Object.DestroyImmediate(chart);
        }

        [Test]
        public void SinglePhase_EarliestSpawnBeat_IsZero()
        {
            // ???? ?????? 0?? ???? ? ???????? ???? ? ?????? 0???.
            NoteRow row = ArcSingle(hit: 3.0, approach: 2.0);
            Assert.AreEqual(1.0, NoteRowMath.SpawnBeat(row), 1e-9);
            chart.phases.Add(Phase(row));

            PhaseWindow window = ChartLayout.Build(chart)[0];
            Assert.AreEqual(0.0, window.EarliestSpawnBeat, 1e-9);
        }

        [Test]
        public void SecondPhase_EarliestSpawn_EqualsFirstCleared()
        {
            chart.phases.Add(Phase(ArcSingle(hit: 2.0, approach: 2.0)));
            chart.phases.Add(Phase(ArcSingle(hit: 3.0, approach: 2.0)));

            List<PhaseWindow> windows = ChartLayout.Build(chart);
            Assert.AreEqual(windows[0].ClearedBeat, windows[1].EarliestSpawnBeat, 1e-9);
            Assert.Greater(windows[0].ClearedBeat, 0.0);
        }

        [Test]
        public void MixedApproach_DoesNotOverlapPreviousPhase()
        {
            // ???? 2?? ???? ????? ?? ?????, ???? 1?? ???? ????? ?? ???.
            // ???? ??? ??? ??(2)?? ???? ??? ???? ??(0)?? ?????.
            NoteRow slow = ArcSingle(hit: 2.0, approach: 2.0);
            NoteRow fast = ArcSingle(hit: 5.0, approach: 1.0);
            Assert.Greater(NoteRowMath.FirstHitBeat(fast), NoteRowMath.FirstHitBeat(slow));
            Assert.AreEqual(0.0, NoteRowMath.SpawnBeat(slow), 1e-9);
            Assert.AreEqual(4.0, NoteRowMath.SpawnBeat(fast), 1e-9);
            Assert.AreEqual(2.0, NoteRowMath.FirstHitBeat(slow) - NoteRowMath.SpawnBeat(slow), 1e-9);
            Assert.AreEqual(1.0, NoteRowMath.FirstHitBeat(fast) - NoteRowMath.SpawnBeat(fast), 1e-9);

            chart.phases.Add(Phase(ArcSingle(hit: 2.0, approach: 2.0)));
            chart.phases.Add(Phase(slow, fast));

            List<PhaseWindow> windows = ChartLayout.Build(chart);
            double previousCleared = windows[0].ClearedBeat;
            Assert.AreEqual(2.0, previousCleared, 1e-9);

            // ?????? 0?? ???? ??????? ?????? ???? ?????? ??????? ??? ????.
            // ??? ????(2)???? ?????? ???? ?? ???? ?????? ?? ??????? ?????.
            Assert.AreEqual(previousCleared, windows[1].OriginBeat, 1e-9);
            Assert.AreEqual(previousCleared, windows[1].EarliestSpawnBeat, 1e-9);

            double origin = windows[1].OriginBeat;
            for (int i = 0; i < chart.phases[1].rows.Count; i++)
            {
                double absoluteSpawn = origin + NoteRowMath.SpawnBeat(chart.phases[1].rows[i]);
                Assert.GreaterOrEqual(absoluteSpawn + 1e-9, previousCleared);
            }
        }

        [Test]
        public void EmptyPhase_HoldBeatsEight_LastsEightBeats()
        {
            chart.phases.Add(new PhaseDefinition
            {
                phaseName = "???",
                rows = new List<NoteRow>(),
                holdBeats = 8.0,
            });

            PhaseWindow window = ChartLayout.Build(chart)[0];
            Assert.AreEqual(0.0, window.EarliestSpawnBeat, 1e-9);
            Assert.AreEqual(8.0, window.LengthBeats, 1e-9);
            Assert.AreEqual(8.0, window.ClearedBeat - window.EarliestSpawnBeat, 1e-9);
        }

        [Test]
        public void Single_ClearBeat_IsLastHitPlusBadWindow()
        {
            var row = new NoteRow
            {
                kind = NoteKind.Single,
                hitBeats = new List<double> { 2.0, 4.0 },
                path = new List<MotionKey>
                {
                    new MotionKey(0.0, 180f),
                    new MotionKey(2.0, 270f),
                    new MotionKey(4.0, 270f),
                },
            };
            chart.phases.Add(Phase(row));

            double expected = 4.0 + chart.BeatsFromMs(chart.judgement.badMs);
            Assert.AreEqual(expected, NoteRowMath.ClearBeat(row, chart), 1e-9);
            Assert.AreEqual(4.0, NoteRowMath.LayoutEndBeat(row), 1e-9);
            Assert.AreEqual(4.0, ChartLayout.Build(chart)[0].ClearedBeat, 1e-9);
        }

        [Test]
        public void NextPhase_Origin_IgnoresBadWindow()
        {
            chart.phases.Add(Phase(ArcSingle(hit: 2.0, approach: 2.0)));
            chart.phases.Add(Phase(ArcSingle(hit: 2.0, approach: 2.0)));

            List<PhaseWindow> windows = ChartLayout.Build(chart);
            Assert.AreEqual(2.0, windows[0].ClearedBeat, 1e-9);
            Assert.AreEqual(2.0, windows[1].OriginBeat, 1e-9);
            Assert.AreEqual(4.0, windows[1].OriginBeat + 2.0, 1e-9);
        }

        [Test]
        public void Long_ClearBeat_IsHeadHitPlusTail()
        {
            var row = new NoteRow
            {
                kind = NoteKind.Long,
                hitBeats = new List<double> { 2.0 },
                tailBeats = 4.0,
                path = new List<MotionKey>
                {
                    new MotionKey(0.0, 180f),
                    new MotionKey(2.0, 270f),
                },
            };
            chart.phases.Add(Phase(row));

            Assert.AreEqual(6.0, NoteRowMath.ClearBeat(row, chart), 1e-9);
            Assert.AreEqual(6.0, ChartLayout.Build(chart)[0].ClearedBeat, 1e-9);
        }

        [Test]
        public void Validate_FlagsReversedPathKeys()
        {
            var row = new NoteRow
            {
                kind = NoteKind.Single,
                hitBeats = new List<double> { 2.0 },
                path = new List<MotionKey>
                {
                    new MotionKey(2.0, 270f),
                    new MotionKey(0.0, 180f),
                },
            };
            chart.phases.Add(Phase(row));

            List<string> problems = ChartLayout.Validate(chart);
            Assert.IsTrue(problems.Exists(p => p.StartsWith("????") && p.Contains("????????")));
        }

        static PhaseDefinition Phase(params NoteRow[] rows)
        {
            return new PhaseDefinition
            {
                rows = new List<NoteRow>(rows),
                holdBeats = 0.0,
            };
        }

        static NoteRow ArcSingle(double hit, double approach)
        {
            return new NoteRow
            {
                kind = NoteKind.Single,
                hitBeats = new List<double> { hit },
                path = new List<MotionKey>
                {
                    new MotionKey(hit - approach, 180f),
                    new MotionKey(hit, 270f),
                },
            };
        }
    }
}
