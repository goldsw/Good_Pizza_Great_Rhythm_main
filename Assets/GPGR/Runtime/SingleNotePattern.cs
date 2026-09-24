using GPGR.Charting;

namespace GPGR.Runtime
{
    /// <summary>
    /// 히트 목록을 순서대로 소비한다. 목표 창을 지나면 Miss이고 남은 히트는 가지 않는다.
    /// 마지막 히트를 맞추면 그 박에 사라진다.
    /// </summary>
    public sealed class SingleNotePattern : NotePattern
    {
        protected override bool SettlesOnFinalHit => true;

        public override double ClearBeat(NoteInstance note)
        {
            if (note == null)
                return 0;

            if (note.Missed)
                return note.DiedBeatAbs;

            var hits = note.Row?.hitBeats;
            int total = hits?.Count ?? 0;
            if (total > 0 && note.HitCount >= total && note.HasJudged)
                return note.ResolvedBeatAbs;

            double last = NoteRowMath.LastHitBeat(note.Row);
            double bad = note.Chart != null ? note.Chart.BeatsFromMs(note.Chart.judgement.badMs) : 0;
            return note.OriginBeat + last + bad;
        }
    }
}
