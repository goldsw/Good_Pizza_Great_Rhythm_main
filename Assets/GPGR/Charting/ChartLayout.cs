using System.Collections.Generic;
using UnityEngine;

namespace GPGR.Charting
{
    /// <summary>한 페이즈가 곡 박자에서 차지하는 구간. 모두 절대 박이다.</summary>
    public readonly struct PhaseWindow
    {
        public readonly int PhaseIndex;
        public readonly double OriginBeat;
        public readonly double EarliestSpawnBeat;
        public readonly double ClearedBeat;

        public PhaseWindow(int phaseIndex, double originBeat, double earliestSpawnBeat, double clearedBeat)
        {
            PhaseIndex = phaseIndex;
            OriginBeat = originBeat;
            EarliestSpawnBeat = earliestSpawnBeat;
            ClearedBeat = clearedBeat;
        }

        public double LengthBeats => ClearedBeat - EarliestSpawnBeat;
    }

    public static class ChartLayout
    {
        /// <summary>
        /// 페이즈를 곡 박자에 펼친다. 판정 결과와 무관한 순수 함수다.
        /// 미스로 노트가 예정보다 일찍 사라져도 이 값을 다시 계산하지 않는다.
        /// </summary>
        public static List<PhaseWindow> Build(SongChart chart)
        {
            var windows = new List<PhaseWindow>();
            if (chart?.phases == null) return windows;

            double cursor = 0.0;

            for (int i = 0; i < chart.phases.Count; i++)
            {
                PhaseDefinition phase = chart.phases[i];
                if (phase == null)
                {
                    windows.Add(new PhaseWindow(i, cursor, cursor, cursor));
                    continue;
                }

                double earliestLocal = 0.0;
                double clearedLocal = 0.0;
                bool any = false;

                if (phase.rows != null)
                {
                    for (int r = 0; r < phase.rows.Count; r++)
                    {
                        NoteRow row = phase.rows[r];
                        if (row == null) continue;

                        double spawn = NoteRowMath.SpawnBeat(row);
                        double cleared = NoteRowMath.LayoutEndBeat(row);

                        if (!any)
                        {
                            earliestLocal = spawn;
                            clearedLocal = cleared;
                            any = true;
                        }
                        else
                        {
                            // 히트가 가장 이른 행이 아니라 첫 키가 가장 이른 행을 쓴다.
                            // 접근 박이 행마다 다를 수 있기 때문이다.
                            if (spawn < earliestLocal) earliestLocal = spawn;
                            if (cleared > clearedLocal) clearedLocal = cleared;
                        }
                    }
                }

                // 노트가 없는 페이즈(완성)는 유지 박으로 길이를 얻는다.
                if (phase.holdBeats > clearedLocal) clearedLocal = phase.holdBeats;

                double origin = cursor - earliestLocal;
                double clearedAbs = origin + clearedLocal;

                windows.Add(new PhaseWindow(i, origin, cursor, clearedAbs));
                cursor = clearedAbs;
            }

            return windows;
        }

        /// <summary>사람이 읽을 문제 목록. 비어 있으면 통과.</summary>
        public static List<string> Validate(SongChart chart)
        {
            var problems = new List<string>();

            if (chart == null)
            {
                problems.Add("오류: 차트가 비어 있다.");
                return problems;
            }

            if (chart.bpm <= 0f) problems.Add("오류: BPM이 0 이하다.");

            if (chart.phases == null || chart.phases.Count == 0)
            {
                problems.Add("오류: 페이즈가 없다.");
                return problems;
            }

            for (int i = 0; i < chart.phases.Count; i++)
            {
                PhaseDefinition phase = chart.phases[i];
                if (phase == null)
                {
                    problems.Add($"오류: 페이즈 {i}가 비어 있다.");
                    continue;
                }

                string head = $"페이즈 {i} \"{phase.phaseName}\"";
                int rowCount = phase.rows?.Count ?? 0;

                if (rowCount == 0 && phase.holdBeats <= 0.0)
                    problems.Add($"오류: {head}는 노트가 없는데 유지 박이 0이다. 연출이 한 프레임에 끝난다.");

                for (int r = 0; r < rowCount; r++)
                {
                    NoteRow row = phase.rows[r];
                    string rh = $"{head} 행 {r} \"{row?.label}\"";

                    if (row == null)
                    {
                        problems.Add($"오류: {rh}가 비어 있다.");
                        continue;
                    }

                    if (row.path == null || row.path.Count < 2)
                    {
                        problems.Add($"오류: {rh}의 경로 키가 2개보다 적다.");
                        continue;
                    }

                    for (int k = 1; k < row.path.Count; k++)
                    {
                        if (row.path[k].beat < row.path[k - 1].beat)
                            problems.Add($"오류: {rh}의 경로 키 {k}가 시각 오름차순이 아니다.");
                    }

                    if (row.hitBeats == null || row.hitBeats.Count == 0)
                    {
                        problems.Add($"오류: {rh}에 히트 박이 없다.");
                        continue;
                    }

                    for (int h = 1; h < row.hitBeats.Count; h++)
                    {
                        if (row.hitBeats[h] <= row.hitBeats[h - 1])
                            problems.Add($"오류: {rh}의 히트 박 {h}가 오름차순이 아니다.");
                    }

                    if (row.kind == NoteKind.Long)
                    {
                        if (row.hitBeats.Count != 1)
                            problems.Add($"오류: {rh}는 롱노트인데 히트 박이 {row.hitBeats.Count}개다. 머리 1개여야 한다.");
                        if (row.tailBeats <= 0.0)
                            problems.Add($"오류: {rh}는 롱노트인데 꼬리 박이 0이다.");
                    }

                    double first = row.path[0].beat;
                    double last = row.path[row.path.Count - 1].beat;

                    for (int h = 0; h < row.hitBeats.Count; h++)
                    {
                        double hb = row.hitBeats[h];

                        if (hb < first || hb > last)
                        {
                            problems.Add($"오류: {rh}의 히트 박 {hb}이 경로 키 범위({first}~{last}) 밖이다.");
                            continue;
                        }

                        float angle = MotionPathMath.AngleAt(row.path, hb);
                        if (Mathf.Abs(angle - chart.defaultHitAngleDeg) > 1f)
                        {
                            problems.Add($"경고: {rh}의 히트 박 {hb}에서 각도가 {angle:0.#}°다. " +
                                         $"히트 각 {chart.defaultHitAngleDeg:0.#}°와 달라 하단에 맞물리지 않는다.");
                        }
                    }
                }
            }

            return problems;
        }
    }
}
