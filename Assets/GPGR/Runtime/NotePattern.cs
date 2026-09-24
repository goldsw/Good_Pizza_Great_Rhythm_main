using GPGR.Charting;

namespace GPGR.Runtime
{
    /// <summary>
    /// 단일·롱이 공유하는 판정. 맞은 횟수와 각도는 노트에만 둔다.
    /// </summary>
    public abstract class NotePattern : INotePattern
    {
        const double BeatSlack = 1e-8;

        protected abstract bool SettlesOnFinalHit { get; }

        public virtual void Advance(NoteInstance note, double songBeat)
        {
            if (note == null)
                return;

            note.AngleDeg = MotionPath.AngleAt(note.Row?.path, note.LocalBeat(songBeat));
            TryTimeout(note, songBeat);
        }

        public virtual bool CanAcceptInput(NoteInstance note, double songBeat)
        {
            if (!HasTarget(note) || note.Chart == null)
                return false;

            return AbsErrorMs(note, songBeat) <= note.Chart.judgement.badMs;
        }

        public virtual double AbsErrorMs(NoteInstance note, double songBeat)
        {
            return System.Math.Abs(SignedErrorMs(note, songBeat));
        }

        public virtual Judgement Press(NoteInstance note, double songBeat)
        {
            if (!CanAcceptInput(note, songBeat))
                return note.LastJudgement;

            double signed = SignedErrorMs(note, songBeat);
            Judgement judgement = note.Chart.judgement.Classify(System.Math.Abs(signed));
            int total = note.Row.hitBeats.Count;
            bool settleNow = SettlesOnFinalHit && note.HitCount + 1 >= total;
            note.RecordHit(judgement, signed, songBeat, settleNow);
            return judgement;
        }

        public virtual void Release(NoteInstance note, double songBeat)
        {
        }

        public abstract double ClearBeat(NoteInstance note);

        static bool HasTarget(NoteInstance note)
        {
            if (note == null || note.Missed || note.Cleared)
                return false;

            var hits = note.Row?.hitBeats;
            return hits != null && note.HitCount < hits.Count;
        }

        static double SignedErrorMs(NoteInstance note, double songBeat)
        {
            if (!HasTarget(note) || note.Chart == null)
                return 0;

            double target = note.OriginBeat + note.Row.hitBeats[note.HitCount];
            return note.Chart.MsFromBeats(songBeat - target);
        }

        static void TryTimeout(NoteInstance note, double songBeat)
        {
            if (!HasTarget(note) || note.Chart == null)
                return;

            double target = note.OriginBeat + note.Row.hitBeats[note.HitCount];
            double deadline = target + note.Chart.BeatsFromMs(note.Chart.judgement.badMs);
            if (songBeat + BeatSlack < deadline)
                return;

            double signed = note.Chart.MsFromBeats(songBeat - target);
            note.RecordMiss(deadline, signed);
        }
    }
}
