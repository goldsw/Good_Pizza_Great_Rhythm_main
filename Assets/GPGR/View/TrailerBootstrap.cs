using GPGR.Charting;
using GPGR.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GPGR.View
{
    /// <summary>
    /// 트레일러 씬을 켠다. R 다시 재생, H HUD, F 단색 배경.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class TrailerBootstrap : MonoBehaviour
    {
        [Tooltip("A의 DemoChart가 오기 전에는 손으로 만든 임시 차트를 넣는다.")]
        [SerializeField] SongChart chart;
        [SerializeField] TimelineDirector director;
        [SerializeField] ChartClock clock;
        [SerializeField] NoteWheel wheel;
        [SerializeField] AutoPlayHitSource autoPlay;
        [SerializeField] PlayerHitSource player;
        [SerializeField] WheelView wheelView;
        [SerializeField] NoteViewPool pool;
        [SerializeField] PhaseThemeView theme;
        [SerializeField] PizzaOvenView oven;
        [SerializeField] HudView hud;
        [SerializeField] OrbitTrackView orbitTrack;
        [SerializeField] NoteFxView noteFx;
        [SerializeField] bool hudVisible = true;
        [SerializeField] bool flatBackground;
        [SerializeField] int captureWidth = 1920;
        [SerializeField] int captureHeight = 1080;

        bool missingLogged;

        void Start()
        {
            Screen.SetResolution(captureWidth, captureHeight, FullScreenMode.Windowed);
            BeginChart();
        }

        void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.rKey.wasPressedThisFrame) BeginChart();
            if (keyboard.hKey.wasPressedThisFrame)
            {
                hudVisible = !hudVisible;
                if (hud != null) hud.SetVisible(hudVisible);
            }
            if (keyboard.fKey.wasPressedThisFrame)
            {
                flatBackground = !flatBackground;
                if (theme != null) theme.SetFlat(flatBackground);
            }
        }

        void OnValidate()
        {
            if (!Application.isPlaying) return;
            if (hud != null) hud.SetVisible(hudVisible);
            if (theme != null) theme.SetFlat(flatBackground);
        }

        public void BeginChart()
        {
            Resolve();
            if (hud != null) hud.SetVisible(hudVisible);
            if (theme != null) theme.SetFlat(flatBackground);

            if (chart == null || director == null)
            {
                if (!missingLogged)
                {
                    missingLogged = true;
                    Debug.LogWarning("TrailerBootstrap: SongChart 또는 TimelineDirector가 없다. B가 런타임을 채운 뒤 트레일러 씬을 다시 열면 컴포넌트가 붙는다.");
                }
                return;
            }

            if (autoPlay != null) autoPlay.enabled = chart.playback == PlaybackMode.AutoPlay;
            if (player != null) player.enabled = chart.playback == PlaybackMode.Human;

            NoteWheel boundWheel = wheel != null ? wheel : director.Wheel;
            ChartClock boundClock = clock != null ? clock : director.Clock;

            if (pool != null) pool.RecallAll();
            if (wheelView != null) wheelView.Bind(boundWheel);
            if (pool != null) pool.Bind(boundWheel, boundClock, chart.SecondsPerBeat);
            if (theme != null) theme.Bind(director, wheelView);
            if (oven != null) oven.Bind(director);
            if (orbitTrack != null) orbitTrack.Bind(boundWheel);
            if (noteFx != null) noteFx.Bind(boundWheel);
            if (hud != null)
            {
                hud.ClearPopup();
                hud.Bind(director);
            }

            director.Begin(chart);

            boundWheel = wheel != null ? wheel : director.Wheel;
            boundClock = clock != null ? clock : director.Clock;
            if (wheelView != null) wheelView.Bind(boundWheel);
            if (pool != null) pool.Bind(boundWheel, boundClock, chart.SecondsPerBeat);
            if (orbitTrack != null) orbitTrack.Bind(boundWheel);
            if (noteFx != null) noteFx.Bind(boundWheel);
            if (hud != null) hud.Bind(director);
        }

        void Resolve()
        {
            if (director == null) director = FindAnyObjectByType<TimelineDirector>();
            if (clock == null) clock = director != null && director.Clock != null ? director.Clock : FindAnyObjectByType<ChartClock>();
            if (wheel == null) wheel = director != null && director.Wheel != null ? director.Wheel : FindAnyObjectByType<NoteWheel>();
            if (autoPlay == null) autoPlay = FindAnyObjectByType<AutoPlayHitSource>();
            if (player == null) player = FindAnyObjectByType<PlayerHitSource>();
            if (wheelView == null) wheelView = FindAnyObjectByType<WheelView>();
            if (pool == null) pool = FindAnyObjectByType<NoteViewPool>();
            if (theme == null) theme = FindAnyObjectByType<PhaseThemeView>();
            if (oven == null) oven = FindAnyObjectByType<PizzaOvenView>();
            if (orbitTrack == null) orbitTrack = FindAnyObjectByType<OrbitTrackView>();
            if (noteFx == null) noteFx = FindAnyObjectByType<NoteFxView>();
            if (hud == null) hud = FindAnyObjectByType<HudView>();
        }
    }
}
