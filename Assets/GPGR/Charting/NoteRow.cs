using System;
using System.Collections.Generic;
using UnityEngine;

namespace GPGR.Charting
{
    public enum NoteKind { Single, Long }

    /// <summary>지금은 값이 하나뿐이다. 새 규칙이 생기면 여기에 추가한다.</summary>
    public enum MissBehaviour { DespawnImmediately }

    [Serializable]
    public struct MotionKey
    {
        [Tooltip("페이즈 원점 기준 박")] public double beat;
        [Tooltip("오른쪽 0°, 반시계 +. 왼쪽 180°, 하단 270°")] public float angleDeg;

        public MotionKey(double beat, float angleDeg)
        {
            this.beat = beat;
            this.angleDeg = angleDeg;
        }
    }

    [Serializable]
    public sealed class NoteRow
    {
        [Tooltip("사람이 보기 위한 이름. 코드가 이 값으로 분기하면 안 된다.")]
        public string label = "";

        public NoteKind kind = NoteKind.Single;

        [Tooltip("페이즈 기준 박. 오름차순. 롱노트는 머리 1개.")]
        public List<double> hitBeats = new List<double>();

        [Tooltip("시각 오름차순, 최소 2개. 스폰 박은 첫 키의 시각이다.")]
        public List<MotionKey> path = new List<MotionKey>();

        public MissBehaviour onMiss = MissBehaviour.DespawnImmediately;

        [Tooltip("롱노트만. 머리 도착 뒤 노트가 남아 있는 박. 꼬리가 덮는 원호도 이 값에서 나온다.")]
        public double tailBeats;

        public Sprite icon;
        public Color color = Color.white;
    }
}
