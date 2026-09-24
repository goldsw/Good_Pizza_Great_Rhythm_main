using System;
using System.Collections.Generic;
using GPGR.Charting;
using NUnit.Framework;
using UnityEngine;

namespace GPGR.Runtime.Tests
{
    public sealed class AutoPlayTests
    {
        [Test]
        public void DemoChart_AutoPlay_OnlyPerfectOrGood_AndAccuracyMatches()
        {
            SongChart chart = CreateDemo();
            try
            {
                RunResult run = Simulate(chart);
                AssertDemo(chart, run);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(chart);
            }
        }

        [Test]
        public void SameSeed_ProducesTheSameTake()
        {
            SongChart chart = CreateDemo();
            try
            {
                RunResult first = Simulate(chart);
                RunResult second = Simulate(chart);

                Assert.AreEqual(first.Judgements.Count, second.Judgements.Count);
                for (int i = 0; i < first.Judgements.Count; i++)
                {
                    Assert.AreEqual(first.Judgements[i], second.Judgements[i]);
                    Assert.AreEqual(first.Errors[i], second.Errors[i], 1e-6);
                }

                Assert.AreEqual(first.Accuracy, second.Accuracy, 0.001f);
                CollectionAssert.AreEqual(first.Phases, second.Phases);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(chart);
            }
        }

        [Test]
        public void Clock_WithoutClip_FollowsBpm_AndCachesTheFrame()
        {
            var chart = ScriptableObject.CreateInstance<SongChart>();
            chart.bpm = 120f;
            chart.offsetSeconds = 0.25;
            chart.clip = null;

            double now = 0;
            int frame = 0;
            var go = new GameObject("clock");
            try
            {
                var clock = go.AddComponent<ChartClock>();
                clock.UnscaledTime = () => now;
                clock.FrameIndex = () => frame;
                clock.Bind(chart);
                clock.Play();

                Assert.IsTrue(clock.IsRunning);
                Assert.AreEqual(0.25, clock.SongSeconds, 1e-9);
                Assert.AreEqual(0.5, clock.Beat, 1e-9);

                now = 1;
                Assert.AreEqual(0.25, clock.SongSeconds, 1e-9, "같은 프레임에서는 시각을 다시 계산하지 않는다.");

                frame = 1;
                Assert.AreEqual(1.25, clock.SongSeconds, 1e-9);
                Assert.AreEqual(2.5, clock.Beat, 1e-9);

                clock.Stop();
                frame = 2;
                now = 10;
                Assert.IsFalse(clock.IsRunning);
                Assert.AreEqual(1.25, clock.SongSeconds, 1e-9);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
                UnityEngine.Object.DestroyImmediate(chart);
            }
        }

        static void AssertDemo(SongChart chart, RunResult run)
        {
            int hits = 0;
            int rows = 0;
            foreach (PhaseDefinition phase in chart.phases)
            {
                rows += phase.rows.Count;
                foreach (NoteRow row in phase.rows)
                    hits += row.hitBeats.Count;
            }

            Assert.AreEqual(9, rows);
            Assert.AreEqual(10, hits);
            Assert.AreEqual(hits, run.Judgements.Count);
            Assert.AreEqual(rows, run.SpawnCount);
            Assert.IsTrue(run.Finished);
            CollectionAssert.AreEqual(
                new[] { "반죽", "토마토 소스", "치즈 뿌리기", "페퍼로니", "오븐", "완성" },
                run.Phases);

            int goods = 0;
            Assert.AreEqual(2f, chart.judgement.goodPenalty);
            for (int i = 0; i < run.Judgements.Count; i++)
            {
                Judgement judgement = run.Judgements[i];
                Assert.IsTrue(judgement == Judgement.Perfect || judgement == Judgement.Good, judgement.ToString());
                double abs = Math.Abs(run.Errors[i]);
                if (judgement == Judgement.Perfect)
                {
                    Assert.AreEqual(0, run.Errors[i], 1e-4);
                }
                else
                {
                    goods++;
                    Assert.Greater(abs, chart.judgement.perfectMs);
                    Assert.LessOrEqual(abs, chart.judgement.goodMs);
                }
            }

            float expected = Mathf.Max(0f, 100f - 2f * goods);
            Assert.AreEqual(expected, run.Accuracy, 0.001f);
        }

        static RunResult Simulate(SongChart chart)
        {
            foreach (string problem in ChartLayout.Validate(chart))
                Assert.IsFalse(problem.StartsWith("오류"), problem);

            var result = new RunResult();
            double now = 0;
            int frame = 0;
            var go = new GameObject("sim");
            try
            {
                var clock = go.AddComponent<ChartClock>();
                clock.UnscaledTime = () => now;
                clock.FrameIndex = () => frame;
                var wheel = go.AddComponent<NoteWheel>();
                go.AddComponent<PlayerHitSource>();
                go.AddComponent<AutoPlayHitSource>();
                var director = go.AddComponent<TimelineDirector>();

                wheel.Judged += (note, judgement, error) =>
                {
                    result.Judgements.Add(judgement);
                    result.Errors.Add(error);
                };
                wheel.Spawned += note => result.SpawnCount++;
                director.PhaseEntered += (phase, index) => result.Phases.Add(phase.phaseName);
                director.ChartFinished += () => result.Finished = true;

                director.Begin(chart);
                AssertOnePhase(wheel);

                double end = ChartLayout.Build(chart)[chart.phases.Count - 1].ClearedBeat;
                const double step = 0.05;
                for (double beat = step; beat <= end + step; beat += step)
                {
                    now = beat * chart.SecondsPerBeat - chart.offsetSeconds;
                    frame++;
                    director.Step();
                    AssertOnePhase(wheel);
                }

                Assert.AreEqual(0, wheel.Active.Count);
                result.Accuracy = director.Accuracy.Percent;
                return result;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static void AssertOnePhase(NoteWheel wheel)
        {
            int seen = int.MinValue;
            for (int i = 0; i < wheel.Active.Count; i++)
            {
                int phase = wheel.Active[i].PhaseIndex;
                if (seen == int.MinValue)
                    seen = phase;
                else
                    Assert.AreEqual(seen, phase, "한 프레임에 두 페이즈의 노트가 같이 있다.");
            }
        }

        static SongChart CreateDemo()
        {
            Color dough = Hex("#E8D4A8");
            Color sauce = Hex("#C0392B");
            Color cheese = Hex("#F1C40F");
            Color pepperoni = Hex("#8E2B20");
            Color oven = Hex("#E67E22");

            var chart = ScriptableObject.CreateInstance<SongChart>();
            chart.bpm = 120f;
            chart.offsetSeconds = 0;
            chart.playback = PlaybackMode.AutoPlay;
            chart.autoGoodRatio = 0.25f;
            chart.autoSeed = 12345;
            chart.judgement = JudgementWindows.Default;
            chart.defaultApproachBeats = 2;
            chart.defaultSpawnAngleDeg = 0f;
            chart.defaultHitAngleDeg = 270f;
            chart.phases = new List<PhaseDefinition>
            {
                Phase("반죽", Backdrop.Bright, PizzaState.None, 0, Long("반죽", 2, 2, dough)),
                Phase("토마토 소스", Backdrop.Bright, PizzaState.None, 0,
                    Single("토마토 소스", new[] { 2.0 }, new[] { (0.0, 180f), (2.0, 270f) }, sauce),
                    Single("토마토 소스", new[] { 4.0 }, new[] { (2.0, 180f), (4.0, 270f) }, sauce)),
                Phase("치즈 뿌리기", Backdrop.Bright, PizzaState.None, 0,
                    Single("치즈 뿌리기", new[] { 2.0, 4.0 }, new[]
                    {
                        (0.0, 180f), (2.0, 270f), (3.0, 210f), (4.0, 270f),
                    }, cheese)),
                Phase("페퍼로니", Backdrop.Bright, PizzaState.None, 0,
                    Single("페퍼로니", new[] { 1.0 }, new[] { (0.0, 180f), (1.0, 270f) }, pepperoni),
                    Single("페퍼로니", new[] { 2.0 }, new[] { (1.0, 180f), (2.0, 270f) }, pepperoni),
                    Single("페퍼로니", new[] { 3.0 }, new[] { (2.0, 180f), (3.0, 270f) }, pepperoni),
                    Single("페퍼로니", new[] { 4.0 }, new[] { (3.0, 180f), (4.0, 270f) }, pepperoni)),
                Phase("오븐", Backdrop.Dark, PizzaState.IntoOven, 0, Long("오븐", 2, 4, oven)),
                Phase("완성", Backdrop.Bright, PizzaState.OutOfOven, 8),
            };
            return chart;
        }

        static PhaseDefinition Phase(string name, Backdrop backdrop, PizzaState pizza, double hold, params NoteRow[] rows)
        {
            return new PhaseDefinition
            {
                phaseName = name,
                backdrop = backdrop,
                pizza = pizza,
                holdBeats = hold,
                rows = new List<NoteRow>(rows),
            };
        }

        static NoteRow Long(string label, double hit, double tail, Color color)
        {
            return new NoteRow
            {
                label = label,
                kind = NoteKind.Long,
                hitBeats = new List<double> { hit },
                tailBeats = tail,
                color = color,
                path = new List<MotionKey>
                {
                    new MotionKey(0, 180f),
                    new MotionKey(hit, 270f),
                },
            };
        }

        static NoteRow Single(string label, double[] hits, (double beat, float angle)[] keys, Color color)
        {
            var path = new List<MotionKey>();
            for (int i = 0; i < keys.Length; i++)
                path.Add(new MotionKey(keys[i].beat, keys[i].angle));

            return new NoteRow
            {
                label = label,
                kind = NoteKind.Single,
                hitBeats = new List<double>(hits),
                color = color,
                path = path,
            };
        }

        static Color Hex(string html)
        {
            ColorUtility.TryParseHtmlString(html, out Color color);
            return color;
        }

        sealed class RunResult
        {
            public readonly List<Judgement> Judgements = new List<Judgement>();
            public readonly List<double> Errors = new List<double>();
            public readonly List<string> Phases = new List<string>();
            public float Accuracy;
            public bool Finished;
            public int SpawnCount;
        }
    }
}
