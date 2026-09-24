using GPGR.Charting;

namespace GPGR.Runtime
{
    /// <summary>
    /// 노트 한 개의 상태. 패턴 객체는 이걸 저장하지 않는다.
    /// </summary>
    public sealed class NoteInstance
    {
        public NoteRow Row { get; }
        public int PhaseIndex { get; }
        public double OriginBeat { get; }
        public int HitCount { get; private set; }
        public Judgement LastJudgement { get; private set; }
        public bool HasJudged { get; private set; }
        public float AngleDeg { get; internal set; }
        public bool Cleared { get; private set; }

        internal SongChart Chart { get; }
        internal INotePattern Pattern { get; set; }
        internal bool Missed { get; private set; }
        internal double DiedBeatAbs { get; private set; }
        internal double ResolvedBeatAbs { get; private set; }
        internal double LastSignedErrorMs { get; private set; }
        internal int JudgementSerial { get; private set; }

        internal NoteInstance(NoteRow row, int phaseIndex, double originBeat, SongChart chart)
        {
            Row = row;
            PhaseIndex = phaseIndex;
            OriginBeat = originBeat;
            Chart = chart;
        }

        public double LocalBeat(double songBeat)
        {
            return songBeat - OriginBeat;
        }

        internal void RecordHit(Judgement judgement, double signedErrorMs, double songBeat, bool settleNow)
        {
            HitCount++;
            LastJudgement = judgement;
            HasJudged = true;
            LastSignedErrorMs = signedErrorMs;
            JudgementSerial++;
            if (settleNow)
                ResolvedBeatAbs = songBeat;
        }

        internal void RecordMiss(double diedBeatAbs, double signedErrorMs)
        {
            if (Missed)
                return;

            Missed = true;
            DiedBeatAbs = diedBeatAbs;
            LastJudgement = Judgement.Miss;
            HasJudged = true;
            LastSignedErrorMs = signedErrorMs;
            JudgementSerial++;
        }

        internal void MarkCleared()
        {
            Cleared = true;
        }
    }
}
