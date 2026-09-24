using System;
using System.Collections.Generic;
using GPGR.Charting;
using UnityEngine;

namespace GPGR.Runtime
{
    /// <summary>
    /// 레이아웃을 소비해 페이즈를 넘기고 노트를 스폰한다.
    /// 한 프레임 순서: 끝난 노트 제거 → 스폰 → 입력 → 시간 초과·각도.
    /// 페이즈 이름이나 재료 이름으로는 분기하지 않는다.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class TimelineDirector : MonoBehaviour
    {
        const double BeatSlack = 1e-8;

        [SerializeField] ChartClock clock;
        [SerializeField] NoteWheel wheel;
        [SerializeField] PlayerHitSource playerHits;
        [SerializeField] AutoPlayHitSource autoHits;

        readonly List<SpawnPlan> _spawns = new List<SpawnPlan>();

        List<PhaseWindow> _windows = new List<PhaseWindow>();
        bool[] _entered = Array.Empty<bool>();
        bool _running;
        bool _finished;
        NoteWheel _wheelHooked;
        HitSource _sourceHooked;

        public SongChart Chart { get; private set; }
        public ChartClock Clock => clock;
        public NoteWheel Wheel => wheel;
        public AccuracyGauge Accuracy { get; } = new AccuracyGauge();
        public int CurrentPhaseIndex { get; private set; } = -1;

        public event Action<PhaseDefinition, int> PhaseEntered;
        public event Action ChartFinished;

        public void Begin(SongChart chart)
        {
            if (chart == null)
                throw new ArgumentNullException(nameof(chart));

            ResolveComponents();
            Unhook();

            Chart = chart;
            Accuracy.Reset();
            CurrentPhaseIndex = -1;
            _finished = false;
            _windows = ChartLayout.Build(chart);
            _entered = new bool[_windows.Count];
            RebuildSpawns(chart);
            Wheel.ClearAll();

            clock.Bind(chart);
            if (chart.playback == PlaybackMode.AutoPlay)
            {
                autoHits.Arm(chart);
                autoHits.enabled = true;
                playerHits.enabled = false;
                HookSource(autoHits);
            }
            else
            {
                playerHits.enabled = true;
                autoHits.enabled = false;
                HookSource(playerHits);
            }

            HookWheel();
            clock.Play();
            _running = true;

            int phaseCount = chart.phases != null ? chart.phases.Count : 0;
            Debug.Log($"[GPGR] 시작 bpm={chart.bpm} offset={chart.offsetSeconds:0.###}s phases={phaseCount} playback={chart.playback}");
            Step();
        }

        void Update()
        {
            Step();
        }

        internal void Step()
        {
            if (!_running || Chart == null || clock == null || wheel == null)
                return;

            double beat = clock.Beat;
            wheel.ClearFinished(beat);
            EnterAndSpawn(beat);
            _sourceHooked?.Poll(beat);
            wheel.ResolveFrame(beat);
            TryFinish(beat);
        }

        void OnDestroy()
        {
            Unhook();
        }

        void ResolveComponents()
        {
            if (clock == null)
                clock = GetComponent<ChartClock>();
            if (clock == null)
                clock = gameObject.AddComponent<ChartClock>();

            if (wheel == null)
                wheel = GetComponent<NoteWheel>();
            if (wheel == null)
            {
                wheel = gameObject.AddComponent<NoteWheel>();
                Debug.Log("[GPGR] NoteWheel 참조가 없어 같은 오브젝트에 만들었다. 중심을 나누려면 인스펙터에서 연결한다.");
            }

            if (playerHits == null)
                playerHits = GetComponent<PlayerHitSource>();
            if (playerHits == null)
                playerHits = gameObject.AddComponent<PlayerHitSource>();

            if (autoHits == null)
                autoHits = GetComponent<AutoPlayHitSource>();
            if (autoHits == null)
                autoHits = gameObject.AddComponent<AutoPlayHitSource>();
        }

        void RebuildSpawns(SongChart chart)
        {
            _spawns.Clear();
            if (chart.phases == null)
                return;

            for (int i = 0; i < _windows.Count; i++)
            {
                PhaseWindow window = _windows[i];
                if (window.PhaseIndex < 0 || window.PhaseIndex >= chart.phases.Count)
                    continue;

                PhaseDefinition phase = chart.phases[window.PhaseIndex];
                if (phase?.rows == null)
                    continue;

                for (int r = 0; r < phase.rows.Count; r++)
                {
                    NoteRow row = phase.rows[r];
                    if (row == null)
                        continue;

                    double local = NoteRowMath.SpawnBeat(row);
                    _spawns.Add(new SpawnPlan
                    {
                        PhaseIndex = window.PhaseIndex,
                        Origin = window.OriginBeat,
                        AbsBeat = window.OriginBeat + local,
                        Row = row,
                    });
                }
            }

            _spawns.Sort((a, b) => a.AbsBeat.CompareTo(b.AbsBeat));
        }

        void EnterAndSpawn(double beat)
        {
            for (int i = 0; i < _windows.Count; i++)
            {
                if (_entered[i])
                    continue;
                if (beat + BeatSlack < _windows[i].EarliestSpawnBeat)
                    break;

                _entered[i] = true;
                CurrentPhaseIndex = i;

                PhaseWindow window = _windows[i];
                PhaseDefinition phase = null;
                if (Chart.phases != null && window.PhaseIndex >= 0 && window.PhaseIndex < Chart.phases.Count)
                    phase = Chart.phases[window.PhaseIndex];
                if (phase == null)
                    continue;

                Debug.Log($"[GPGR] 페이즈 {i} 진입 \"{phase.phaseName}\" beat={window.EarliestSpawnBeat:0.###}");
                PhaseEntered?.Invoke(phase, i);
            }

            for (int s = 0; s < _spawns.Count; s++)
            {
                SpawnPlan plan = _spawns[s];
                if (plan.Done)
                    continue;
                if (beat + BeatSlack < plan.AbsBeat)
                    break;

                plan.Done = true;
                var note = new NoteInstance(plan.Row, plan.PhaseIndex, plan.Origin, Chart);
                wheel.Spawn(note, beat);
                Debug.Log($"[GPGR] 스폰 phase={plan.PhaseIndex} \"{plan.Row.label}\" {plan.Row.kind} beat={plan.AbsBeat:0.###}");
            }
        }

        void TryFinish(double beat)
        {
            if (_finished || _windows == null || _windows.Count == 0)
                return;

            double end = _windows[_windows.Count - 1].ClearedBeat;
            if (beat + BeatSlack < end)
                return;

            _finished = true;
            Debug.Log($"[GPGR] 채보 끝 beat={end:0.###}");
            ChartFinished?.Invoke();
        }

        void HookWheel()
        {
            _wheelHooked = wheel;
            if (_wheelHooked != null)
                _wheelHooked.Judged += OnJudged;
        }

        void HookSource(HitSource source)
        {
            _sourceHooked = source;
            if (_sourceHooked == null)
                return;

            _sourceHooked.Pressed += OnPressed;
            _sourceHooked.Released += OnReleased;
        }

        void Unhook()
        {
            if (_wheelHooked != null)
            {
                _wheelHooked.Judged -= OnJudged;
                _wheelHooked = null;
            }

            if (_sourceHooked != null)
            {
                _sourceHooked.Pressed -= OnPressed;
                _sourceHooked.Released -= OnReleased;
                _sourceHooked = null;
            }
        }

        void OnJudged(NoteInstance note, Judgement judgement, double signedMs)
        {
            if (Chart != null)
            {
                JudgementWindows windows = Chart.judgement;
                Accuracy.Apply(judgement, windows);
            }

            string label = note != null && note.Row != null ? note.Row.label : "";
            int phase = note != null ? note.PhaseIndex : -1;
            Debug.Log($"[GPGR] 판정 {judgement} {signedMs:0.#}ms phase={phase} \"{label}\"");
        }

        void OnPressed(double songBeat)
        {
            if (wheel != null)
                wheel.Press(songBeat);
        }

        void OnReleased(double songBeat)
        {
            if (wheel != null)
                wheel.Release(songBeat);
        }

        sealed class SpawnPlan
        {
            public int PhaseIndex;
            public double Origin;
            public double AbsBeat;
            public NoteRow Row;
            public bool Done;
        }
    }
}
