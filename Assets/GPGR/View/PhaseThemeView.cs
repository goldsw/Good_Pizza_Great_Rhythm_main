using System.Collections;
using GPGR.Charting;
using GPGR.Runtime;
using UnityEngine;

namespace GPGR.View
{
    /// <summary>
    /// 배경은 밝음/어두움 두 장. 페이즈 필드로만 고른다.
    /// </summary>
    public sealed class PhaseThemeView : MonoBehaviour
    {
        [SerializeField] SpriteRenderer bright;
        [SerializeField] SpriteRenderer dark;
        [SerializeField] WheelView wheelView;
        [SerializeField] Camera viewCamera;
        [Tooltip("잠정 0.3초.")]
        [SerializeField] float crossfadeSeconds = 0.3f;
        [Tooltip("오븐 배경이 완전히 나타났을 때의 색. 잠정 (0.55, 0.48, 0.42).")]
        [SerializeField] Color ovenTint = new Color(0.55f, 0.48f, 0.42f, 1f);
        [SerializeField] Color flatColor = new Color(0f, 1f, 0f, 1f);
        [SerializeField] Color sceneBackground = new Color(0.9019608f, 0.9019608f, 0.9019608f, 1f);
        [Tooltip("밝은 주방과 어두운 배경이 같은 자리, 같은 크기다.")]
        [SerializeField] Vector3 backgroundPosition = Vector3.zero;
        [SerializeField] Vector3 backgroundScale = new Vector3(0.926f, 0.926f, 0.926f);

        TimelineDirector director;
        Coroutine fade;
        bool subscribed;
        bool flat;
        bool showingDark;
        bool gotPhase;
        float darkAlpha;

        public void Bind(TimelineDirector timeline, WheelView wheel)
        {
            Unsubscribe();
            director = timeline;
            if (wheel != null) wheelView = wheel;
            Subscribe();
        }

        public void SetFlat(bool enabled)
        {
            flat = enabled;
            ApplyFlat();
        }

        public void ToggleFlat()
        {
            SetFlat(!flat);
        }

        void OnEnable()
        {
            Subscribe();
        }

        void OnDisable()
        {
            Unsubscribe();
        }

        void LateUpdate()
        {
            FitBackgrounds();
        }

        void Awake()
        {
            darkAlpha = 0f;
            ApplyOvenTint(0f);
            KeepBrightWhite();
        }

        void Start()
        {
            FitBackgrounds();
            if (!gotPhase)
            {
                if (bright != null) bright.sortingOrder = -99;
                if (dark != null) dark.sortingOrder = -100;
            }
            ApplyFlat();
        }

        void Subscribe()
        {
            if (subscribed || director == null) return;
            director.PhaseEntered += OnPhaseEntered;
            subscribed = true;
        }

        void Unsubscribe()
        {
            if (!subscribed || director == null)
            {
                subscribed = false;
                return;
            }
            director.PhaseEntered -= OnPhaseEntered;
            subscribed = false;
        }

        void OnPhaseEntered(PhaseDefinition phase, int index)
        {
            if (phase == null) return;
            if (phase.overrideWheelTint && wheelView != null)
                wheelView.ApplyTint(phase.wheelTint);

            bool toDark = phase.backdrop == Backdrop.Dark;
            if (gotPhase && toDark == showingDark) return;
            gotPhase = true;
            if (toDark == showingDark) return;

            showingDark = toDark;
            if (fade != null) StopCoroutine(fade);
            fade = StartCoroutine(Crossfade(toDark));
        }

        IEnumerator Crossfade(bool toDark)
        {
            if (bright == null || dark == null) yield break;

            bright.sortingOrder = -100;
            dark.sortingOrder = -99;
            KeepBrightWhite();
            if (!flat)
            {
                bright.enabled = true;
                dark.enabled = true;
            }

            float duration = Mathf.Max(0.0001f, crossfadeSeconds);
            float from = darkAlpha;
            float to = toDark ? 1f : 0f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                darkAlpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
                ApplyOvenTint(darkAlpha);
                yield return null;
            }

            darkAlpha = to;
            ApplyOvenTint(darkAlpha);
            fade = null;
        }

        void KeepBrightWhite()
        {
            if (bright == null) return;
            Color color = Color.white;
            color.a = 1f;
            bright.color = color;
        }

        void ApplyOvenTint(float alpha)
        {
            if (dark != null)
            {
                Color color = ovenTint;
                color.a = alpha;
                dark.color = color;
            }

            if (wheelView == null) return;
            Color stand = Color.Lerp(Color.white, ovenTint, alpha);
            stand.a = 1f;
            wheelView.ApplyTint(stand);
        }

        void ApplyFlat()
        {
            if (viewCamera != null)
            {
                viewCamera.clearFlags = CameraClearFlags.SolidColor;
                viewCamera.backgroundColor = flat ? flatColor : sceneBackground;
            }
            if (bright != null) bright.enabled = !flat;
            if (dark != null) dark.enabled = !flat;
        }

        void FitBackgrounds()
        {
            Place(bright);
            Place(dark);
        }

        void Place(SpriteRenderer sprite)
        {
            if (sprite == null) return;
            sprite.transform.localPosition = backgroundPosition;
            sprite.transform.localScale = backgroundScale;
        }

    }
}
