using GPGR.Charting;

namespace GPGR.Runtime
{
    /// <summary>
    /// 머리 하나만 판정한다. 맞은 뒤 tailBeats 동안 남아 있고, 놓치면 그 박에 사라진다.
    /// </summary>
    public sealed class LongNotePattern : NotePattern
    {
        protected override bool SettlesOnFinalHit => false;

        public override void Release(NoteInstance note, double songBeat)
        {
            // 손을 떼는 시각은 판정에 쓰지 않는다.
        }

        public override double ClearBeat(NoteInstance note)
        {
            if (note == null)
                return 0;

            if (note.Missed)
                return note.DiedBeatAbs;

            double head = NoteRowMath.FirstHitBeat(note.Row);
            double tail = note.Row != null ? note.Row.tailBeats : 0;
            return note.OriginBeat + head + tail;
        }
    }
}
