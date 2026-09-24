using System.Collections.Generic;
using UnityEngine;

namespace GPGR.Charting
{
    public enum PlaybackMode { AutoPlay, Human }

    [CreateAssetMenu(fileName = "SongChart", menuName = "GPGR/Song Chart")]
    public sealed class SongChart : ScriptableObject
    {
        public float bpm = 120f;

        [Tooltip("Ŭ���� ����� �ð��� �� ���� ���Ѵ�.")]
        public double offsetSeconds;

        [Tooltip("�������� ù ��� Ű�� �ð��� ���� �� ����. �࿡�� �������� �ʴ´�.")]
        public double defaultApproachBeats = 2.0;

        public float defaultSpawnAngleDeg = 0f;
        public float defaultHitAngleDeg = 270f;

        [Tooltip("��� �־ �ȴ�. �׶��� BPM������ ����.")]
        public AudioClip clip;

        public JudgementWindows judgement = JudgementWindows.Default;

        public PlaybackMode playback = PlaybackMode.AutoPlay;

        [Tooltip("�ڵ� ������� Good�� ���� ����. �������� Perfect.")]
        [Range(0f, 1f)] public float autoGoodRatio = 0.25f;

        [Tooltip("���� ���̸� ���� ����ũ�� ���´�.")]
        public int autoSeed = 12345;

        public List<PhaseDefinition> phases = new List<PhaseDefinition>();

        public double SecondsPerBeat => 60.0 / Mathf.Max(1f, bpm);
        public double BeatsFromMs(double ms) => ms / 1000.0 / SecondsPerBeat;
        public double MsFromBeats(double beats) => beats * SecondsPerBeat * 1000.0;
    }
}
