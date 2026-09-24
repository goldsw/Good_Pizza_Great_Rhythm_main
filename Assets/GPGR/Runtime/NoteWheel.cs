using System;
using System.Collections.Generic;
using GPGR.Charting;
using UnityEngine;

namespace GPGR.Runtime
{
    /// <summary>
    /// 살아있는 노트를 들고 입력을 가장 가까운 한 노트에만 넘긴다.
    /// 맞았는지는 곡 박자로만 정한다.
    /// </summary>
    public sealed class NoteWheel : MonoBehaviour
    {
        const double BeatSlack = 1e-8;
        const double ErrorTieMs = 1e-4;

        readonly List<NoteInstance> _active = new List<NoteInstance>();
        readonly SingleNotePattern _single = new SingleNotePattern();
        readonly LongNotePattern _long = new LongNotePattern();

        [Tooltip("유닛. 뷰가 노트 위치를 구할 때 쓴다.")]
        public float radius = 3f;

        public Vector2 Center => transform.position;

        public IReadOnlyList<NoteInstance> Active => _active;

        public event Action<NoteInstance> Spawned;
        public event Action<NoteInstance, Judgement, double> Judged;
        public event Action<NoteInstance> Cleared;

        public void Press(double songBeat)
        {
            NoteInstance best = null;
            double bestAbs = double.PositiveInfinity;
            double bestHit = double.PositiveInfinity;

            for (int i = 0; i < _active.Count; i++)
            {
                NoteInstance note = _active[i];
                INotePattern pattern = note.Pattern;
                if (pattern == null || !pattern.CanAcceptInput(note, songBeat))
                    continue;

                double abs = pattern.AbsErrorMs(note, songBeat);
                double hit = TargetAbs(note);
                bool closer = abs < bestAbs - ErrorTieMs;
                bool tieEarlier = Math.Abs(abs - bestAbs) <= ErrorTieMs && hit < bestHit;
                if (best == null || closer || tieEarlier)
                {
                    best = note;
                    bestAbs = abs;
                    bestHit = hit;
                }
            }

            if (best == null)
                return;

            int serial = best.JudgementSerial;
            Judgement judgement = best.Pattern.Press(best, songBeat);
            if (best.JudgementSerial != serial)
                Judged?.Invoke(best, judgement, best.LastSignedErrorMs);
        }

        public void Release(double songBeat)
        {
            for (int i = 0; i < _active.Count; i++)
                _active[i].Pattern?.Release(_active[i], songBeat);
        }

        internal void Spawn(NoteInstance note, double songBeat)
        {
            if (note == null)
                return;

            if (note.Pattern == null)
                note.Pattern = note.Row != null && note.Row.kind == NoteKind.Long ? _long : _single;

            int serial = note.JudgementSerial;
            note.Pattern.Advance(note, songBeat);
            _active.Add(note);
            Spawned?.Invoke(note);
            if (note.JudgementSerial != serial)
                Judged?.Invoke(note, note.LastJudgement, note.LastSignedErrorMs);
        }

        internal void ResolveFrame(double songBeat)
        {
            for (int i = 0; i < _active.Count; i++)
            {
                NoteInstance note = _active[i];
                if (note.Pattern == null)
                    continue;

                int serial = note.JudgementSerial;
                note.Pattern.Advance(note, songBeat);
                if (note.JudgementSerial != serial)
                    Judged?.Invoke(note, note.LastJudgement, note.LastSignedErrorMs);
            }

            ClearFinished(songBeat);
        }

        internal void ClearFinished(double songBeat)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (ReadyToLeave(_active[i], songBeat))
                    RemoveAt(i);
            }
        }

        internal void ClearAll()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
                RemoveAt(i);
        }

        static bool ReadyToLeave(NoteInstance note, double songBeat)
        {
            if (note?.Pattern == null)
                return false;

            if (!note.Missed && !IsSettled(note))
                return false;

            return songBeat + BeatSlack >= note.Pattern.ClearBeat(note);
        }

        static bool IsSettled(NoteInstance note)
        {
            if (!note.HasJudged)
                return false;

            if (note.Row != null && note.Row.kind == NoteKind.Long)
                return note.HitCount >= 1;

            int total = note.Row?.hitBeats?.Count ?? 0;
            return total > 0 && note.HitCount >= total;
        }

        static double TargetAbs(NoteInstance note)
        {
            var hits = note.Row?.hitBeats;
            if (hits == null || hits.Count == 0)
                return note.OriginBeat;

            int index = note.HitCount;
            if (index < 0)
                index = 0;
            if (index >= hits.Count)
                index = hits.Count - 1;
            return note.OriginBeat + hits[index];
        }

        void RemoveAt(int index)
        {
            NoteInstance note = _active[index];
            _active.RemoveAt(index);
            note.MarkCleared();
            Cleared?.Invoke(note);
        }
    }
}
