using System.Collections;
using GPGR.Charting;
using GPGR.Runtime;
using UnityEngine;

namespace GPGR.View
{
    /// <summary>
    /// 피자 그림만 오븐으로 들어갔다 나온다. 원판과 히트 라인은 여기 없다.
    /// 이동과 페이드는 겹치지 않는다. 한 단계가 끝난 뒤에만 다음 단계가 시작한다.
    /// </summary>
    public sealed class PizzaOvenView : MonoBehaviour
    {
        enum Place
        {
            Home,
            Entering,
            Inside,
            Exiting,
        }

        [SerializeField] Transform pizza;
        [Tooltip("아트가 오면 불꽃을 여기 붙인다.")]
        [SerializeField] Transform flameAnchor;
        [Tooltip("피자가 들어가는 자리. 원판의 자식이 되면 안 된다.")]
        [SerializeField] Transform ovenPoint;
        [Tooltip("집 크기 대비. 잠정 0.35.")]
        [SerializeField] float ovenScale = 0.35f;
        [Tooltip("0이면 집, 1이면 오븐 점. 입구 오브젝트는 만들지 않는다. 잠정 0.65.")]
        [SerializeField] float gateRatio = 0.65f;
        [SerializeField] float toGateSeconds = 0.45f;
        [SerializeField] float fadeOutSeconds = 0.2f;
        [SerializeField] float toInsideSeconds = 0.2f;
        [SerializeField] float toGateFromInsideSeconds = 0.2f;
        [SerializeField] float fadeInSeconds = 0.2f;
        [SerializeField] float toHomeSeconds = 0.45f;
        [SerializeField] Sprite doughSprite;
        [SerializeField] Sprite doneSprite;
        [SerializeField] SpriteRenderer lidOpen;
        [SerializeField] SpriteRenderer lidClosed;
        [Tooltip("뚜껑이 열리고 닫히는 시간. 잠정 0.35초.")]
        [SerializeField] float lidFadeSeconds = 0.35f;

        Vector3 homePosition;
        Vector3 homeScale;
        SpriteRenderer pizzaRenderer;
        TimelineDirector director;
        Coroutine routine;
        Place place;
        bool subscribed;
        bool exitAfterEnter;
        bool flameWarned;

        void Awake()
        {
            if (pizza != null)
            {
                homePosition = pizza.position;
                homeScale = pizza.localScale;
                pizzaRenderer = pizza.GetComponent<SpriteRenderer>();
                UseSprite(doughSprite);
            }
            SetLids(1f, 0f);
            if (flameAnchor == null && !flameWarned)
            {
                flameWarned = true;
                Debug.LogWarning("PizzaOvenView: 화덕 불꽃 자리가 없다.", this);
            }
        }

        public void Bind(TimelineDirector timeline)
        {
            Unsubscribe();
            director = timeline;
            Subscribe();
        }

        void OnEnable()
        {
            Subscribe();
        }

        void OnDisable()
        {
            Unsubscribe();
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
            if (phase == null || pizza == null) return;

            if (phase.pizza == PizzaState.IntoOven)
            {
                if (place == Place.Entering || place == Place.Inside) return;
                exitAfterEnter = false;
                routine = StartCoroutine(Enter());
                return;
            }

            if (phase.pizza == PizzaState.OutOfOven)
            {
                if (place == Place.Exiting || place == Place.Home) return;
                if (place == Place.Entering)
                {
                    exitAfterEnter = true;
                    return;
                }
                routine = StartCoroutine(Exit());
                return;
            }

            SnapHome();
        }

        IEnumerator Enter()
        {
            place = Place.Entering;
            Gate(out Vector3 gatePosition, out Vector3 gateScale);
            Vector3 insidePosition = InsidePosition();
            Vector3 insideScale = homeScale * ovenScale;

            UseSprite(doughSprite);
            yield return Move(gatePosition, gateScale, 1f, toGateSeconds);
            yield return Fade(0f, fadeOutSeconds);
            yield return Move(insidePosition, insideScale, 0f, toInsideSeconds);
            yield return CrossfadeLids(0f, 1f);

            place = Place.Inside;
            routine = null;
            if (!exitAfterEnter) yield break;
            exitAfterEnter = false;
            routine = StartCoroutine(Exit());
        }

        IEnumerator Exit()
        {
            place = Place.Exiting;
            UseSprite(doneSprite);
            yield return CrossfadeLids(1f, 0f);
            Gate(out Vector3 gatePosition, out Vector3 gateScale);

            yield return Move(gatePosition, gateScale, 0f, toGateFromInsideSeconds);
            yield return Fade(1f, fadeInSeconds);
            yield return Move(homePosition, homeScale, 1f, toHomeSeconds);

            place = Place.Home;
            routine = null;
        }

        IEnumerator Move(Vector3 targetPosition, Vector3 targetScale, float holdAlpha, float seconds)
        {
            SetAlpha(pizzaRenderer, holdAlpha);
            Vector3 fromPosition = pizza.position;
            Vector3 fromScale = pizza.localScale;
            float duration = Mathf.Max(0.0001f, seconds);
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
                pizza.position = Vector3.Lerp(fromPosition, targetPosition, u);
                pizza.localScale = Vector3.Lerp(fromScale, targetScale, u);
                yield return null;
            }
            pizza.position = targetPosition;
            pizza.localScale = targetScale;
            SetAlpha(pizzaRenderer, holdAlpha);
        }

        IEnumerator Fade(float targetAlpha, float seconds)
        {
            float fromAlpha = pizzaRenderer != null ? pizzaRenderer.color.a : targetAlpha;
            float duration = Mathf.Max(0.0001f, seconds);
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                SetAlpha(pizzaRenderer, Mathf.Lerp(fromAlpha, targetAlpha, Mathf.Clamp01(t / duration)));
                yield return null;
            }
            SetAlpha(pizzaRenderer, targetAlpha);
        }

        IEnumerator CrossfadeLids(float openAlpha, float closedAlpha)
        {
            if (lidOpen == null && lidClosed == null) yield break;
            float fromOpen = Alpha(lidOpen);
            float fromClosed = Alpha(lidClosed);
            float duration = Mathf.Max(0.0001f, lidFadeSeconds);
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
                SetAlpha(lidOpen, Mathf.Lerp(fromOpen, openAlpha, u));
                SetAlpha(lidClosed, Mathf.Lerp(fromClosed, closedAlpha, u));
                yield return null;
            }
            SetLids(openAlpha, closedAlpha);
        }

        void SnapHome()
        {
            if (routine != null) StopCoroutine(routine);
            routine = null;
            exitAfterEnter = false;
            place = Place.Home;
            if (pizza == null) return;
            pizza.position = homePosition;
            pizza.localScale = homeScale;
            UseSprite(doughSprite);
            SetAlpha(pizzaRenderer, 1f);
            SetLids(1f, 0f);
        }

        void UseSprite(Sprite sprite)
        {
            if (pizzaRenderer == null || sprite == null) return;
            float alpha = pizzaRenderer.color.a;
            pizzaRenderer.sprite = sprite;
            SetAlpha(pizzaRenderer, alpha);
        }

        void SetLids(float openAlpha, float closedAlpha)
        {
            SetAlpha(lidOpen, openAlpha);
            SetAlpha(lidClosed, closedAlpha);
        }

        static float Alpha(SpriteRenderer renderer)
        {
            return renderer != null ? renderer.color.a : 0f;
        }

        void Gate(out Vector3 position, out Vector3 scale)
        {
            float ratio = Mathf.Clamp01(gateRatio);
            position = Vector3.Lerp(homePosition, InsidePosition(), ratio);
            scale = Vector3.Lerp(homeScale, homeScale * ovenScale, ratio);
        }

        Vector3 InsidePosition()
        {
            return ovenPoint != null ? ovenPoint.position : homePosition;
        }

        static void SetAlpha(SpriteRenderer renderer, float alpha)
        {
            if (renderer == null) return;
            Color color = renderer.color;
            color.a = alpha;
            renderer.color = color;
        }
    }
}
