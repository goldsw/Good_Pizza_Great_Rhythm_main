namespace GPGR.Charting
{
    public static class NoteRowMath
    {
        /// <summary>스폰 박은 첫 경로 키의 시각이다. 접근 박은 행에 저장하지 않는다.</summary>
        public static double SpawnBeat(NoteRow row)
        {
            if (row?.path == null || row.path.Count == 0) return 0.0;
            return row.path[0].beat;
        }

        public static double FirstHitBeat(NoteRow row)
        {
            if (row?.hitBeats == null || row.hitBeats.Count == 0) return SpawnBeat(row);
            return row.hitBeats[0];
        }

        public static double LastHitBeat(NoteRow row)
        {
            if (row?.hitBeats == null || row.hitBeats.Count == 0) return SpawnBeat(row);
            return row.hitBeats[row.hitBeats.Count - 1];
        }

        /// <summary>
        /// 페이즈를 곡 박자에 이을 때 쓰는 끝 박. Bad 창을 넣지 않는다.
        /// 롱노트는 머리 히트 박 + 꼬리 박. 단일은 마지막 히트 박.
        /// </summary>
        public static double LayoutEndBeat(NoteRow row)
        {
            if (row == null) return 0.0;
            if (row.kind == NoteKind.Long)
                return FirstHitBeat(row) + row.tailBeats;
            return LastHitBeat(row);
        }

        /// <summary>
        /// 예정 지워지는 박. 판정 결과와 무관하다.
        /// 롱노트는 머리 히트 박 + 꼬리 박.
        /// 단일은 마지막 히트 박 + Bad 창(그 박을 지나면 Miss로 바로 사라진다).
        /// 다음 페이즈 원점은 이 값이 아니라 <see cref="LayoutEndBeat"/>를 쓴다.
        /// </summary>
        public static double ClearBeat(NoteRow row, SongChart chart)
        {
            if (row == null) return 0.0;

            if (row.kind == NoteKind.Long)
                return FirstHitBeat(row) + row.tailBeats;

            double badBeats = chart != null ? chart.BeatsFromMs(chart.judgement.badMs) : 0.0;
            return LastHitBeat(row) + badBeats;
        }
    }
}
