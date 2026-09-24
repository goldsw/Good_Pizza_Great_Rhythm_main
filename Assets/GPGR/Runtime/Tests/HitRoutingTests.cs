using System.Collections.Generic;
using GPGR.Charting;
using NUnit.Framework;
using UnityEngine;

namespace GPGR.Runtime.Tests
{
    public sealed class HitRoutingTests
    {
        GameObject _go;
        SongChart _chart;
        NoteWheel _wheel;
        AccuracyGauge _gauge;
        int _judged;

        [SetUp]
        public void SetUp()
        {
            _chart = ScriptableObject.CreateInstance<SongChart>();
            _chart.bpm = 120f;
            _chart.judgement = JudgementWindows.Default;

            _go = new GameObject("wheel");
            _wheel = _go.AddComponent<NoteWheel>();
            _gauge = new AccuracyGauge();
            _judged = 0;
            _wheel.Judged += (note, judgement, error) =>
            {
                _judged++;
                _gauge.Apply(judgement, _chart.judgement);
            };
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
                Object.DestroyImmediate(_go);
            if (_chart != null)
                Object.DestroyImmediate(_chart);
        }

        [Test]
        public void Press_GoesToTheSmallerError()
        {
            NoteInstance far = Make(10.25);
            NoteInstance close = Make(10.0);
            _wheel.Spawn(far, 0);
            _wheel.Spawn(close, 0);

            _wheel.Press(10.05);

            Assert.AreEqual(1, close.HitCount);
            Assert.AreEqual(0, far.HitCount);
            Assert.AreEqual(1, _judged);
            Assert.AreEqual(Judgement.Perfect, close.LastJudgement);
        }

        [Test]
        public void Press_EqualError_GoesToTheEarlierHit()
        {
            NoteInstance later = Make(10.25);
            NoteInstance earlier = Make(10.0);
            _wheel.Spawn(later, 0);
            _wheel.Spawn(earlier, 0);

            _wheel.Press(10.125);

            Assert.AreEqual(1, earlier.HitCount);
            Assert.AreEqual(0, later.HitCount);
            Assert.AreEqual(Judgement.Good, earlier.LastJudgement);
        }

        [Test]
        public void Press_WithNobodyInWindow_DoesNotTouchAccuracy()
        {
            _wheel.Press(0);
            Assert.AreEqual(0, _judged);
            Assert.AreEqual(100f, _gauge.Percent);

            NoteInstance note = Make(2.0);
            _wheel.Spawn(note, 0);
            _wheel.Press(0);

            Assert.AreEqual(0, _judged);
            Assert.AreEqual(100f, _gauge.Percent);
            Assert.AreEqual(0, note.HitCount);
            Assert.AreEqual(1, _wheel.Active.Count);
        }

        [Test]
        public void Single_PastTheWindow_MissesAndLeaves()
        {
            NoteInstance note = Make(2.0, 4.0);
            _wheel.Spawn(note, 0);

            double deadline = 2.0 + _chart.BeatsFromMs(_chart.judgement.badMs);
            _wheel.ResolveFrame(deadline);

            Assert.AreEqual(Judgement.Miss, note.LastJudgement);
            Assert.AreEqual(0, note.HitCount);
            Assert.AreEqual(0, _wheel.Active.Count);
            Assert.IsTrue(note.Cleared);
            Assert.AreEqual(1, _judged);
            Assert.AreEqual(100f - _chart.judgement.missPenalty, _gauge.Percent, 0.001f);
        }

        [Test]
        public void Single_FirstHitKeepsTheNote_SecondHitClearsIt()
        {
            NoteInstance note = Make(2.0, 4.0);
            _wheel.Spawn(note, 0);

            _wheel.Press(2.0);
            _wheel.ResolveFrame(2.0);

            Assert.AreEqual(1, note.HitCount);
            Assert.AreEqual(Judgement.Perfect, note.LastJudgement);
            Assert.AreEqual(1, _wheel.Active.Count);

            _wheel.Press(4.0);
            _wheel.ResolveFrame(4.0);

            Assert.AreEqual(2, note.HitCount);
            Assert.AreEqual(0, _wheel.Active.Count);
        }

        [Test]
        public void Long_StaysForTheTail_AndReleaseDoesNotJudge()
        {
            var row = new NoteRow
            {
                kind = NoteKind.Long,
                hitBeats = new List<double> { 2.0 },
                tailBeats = 2.0,
                path = new List<MotionKey>
                {
                    new MotionKey(0, 180f),
                    new MotionKey(2, 270f),
                },
            };
            var note = new NoteInstance(row, 0, 0, _chart);
            _wheel.Spawn(note, 0);

            _wheel.Press(2.0);
            _wheel.ResolveFrame(2.0);
            Assert.AreEqual(1, _wheel.Active.Count);
            Assert.AreEqual(Judgement.Perfect, note.LastJudgement);

            int serial = note.JudgementSerial;
            _wheel.Release(3.0);
            Assert.AreEqual(serial, note.JudgementSerial);
            Assert.AreEqual(Judgement.Perfect, note.LastJudgement);
            Assert.AreEqual(1, _judged);

            _wheel.ResolveFrame(3.9);
            Assert.AreEqual(1, _wheel.Active.Count);

            _wheel.ResolveFrame(4.0);
            Assert.AreEqual(0, _wheel.Active.Count);
            Assert.AreEqual(Judgement.Perfect, note.LastJudgement);
        }

        NoteInstance Make(params double[] hits)
        {
            double last = hits[hits.Length - 1];
            var row = new NoteRow
            {
                kind = NoteKind.Single,
                hitBeats = new List<double>(hits),
                path = new List<MotionKey>
                {
                    new MotionKey(0, 180f),
                    new MotionKey(last, 270f),
                },
            };
            return new NoteInstance(row, 0, 0, _chart);
        }
    }
}
