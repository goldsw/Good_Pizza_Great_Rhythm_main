using GPGR.Charting;

namespace GPGR.Runtime
{
    /// <summary>
    /// 노트 동작. 구현은 노트를 저장하지 않는다. 상태는 <see cref="NoteInstance"/>에 있다.
    /// </summary>
    public interface INotePattern
    {
        void Advance(NoteInstance note, double songBeat);
        bool CanAcceptInput(NoteInstance note, double songBeat);
        double AbsErrorMs(NoteInstance note, double songBeat);
        Judgement Press(NoteInstance note, double songBeat);
        void Release(NoteInstance note, double songBeat);
        double ClearBeat(NoteInstance note);
    }
}
