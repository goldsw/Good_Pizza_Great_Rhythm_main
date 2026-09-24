using System.Collections;
using GPGR.Charting;
using GPGR.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace GPGR.View
{
    /// <summary>
    /// 페이즈 이름, 정확도, 판정 팝업만 띄운다.
    /// </summary>
    public sealed class HudView : MonoBehaviour
    {
        [SerializeField] Text phaseLabel;
        [SerializeField] Text accuracyLabel;
        [SerializeField] Text judgementLabel;
        [SerializeField] Canvas hudRoot;
        [Tooltip("판정 글자가 떠 있는 시간.")]
        [SerializeField] float popupSeconds = 0.55f;

        TimelineDirector director;
        NoteWheel wheel;
        AccuracyGauge gauge;
        Coroutine popup;
        bool subscribed;

        public void Bind(TimelineDirector timeline)
        {
            Unsubscribe();
            director = timeline;
            wheel = timeline != null ? timeline.Wheel : null;
            gauge = timeline != null ? timeline.Accuracy : null;
            Subscribe();
            if (gauge != null) ShowAccuracy(gauge.Percent);
        }

        public void SetVisible(bool visible)
        {
            Canvas canvas = hudRoot != null ? hudRoot : GetComponent<Canvas>();
            if (canvas != null) canvas.enabled = visible;
        }

        public void ClearPopup()
        {
            if (popup != null) StopCoroutine(popup);
            popup = null;
            if (judgementLabel != null) judgementLabel.text = "";
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
            if (subscribed) return;
            if (director != null) director.PhaseEntered += OnPhaseEntered;
            if (wheel != null) wheel.Judged += OnJudged;
            if (gauge != null) gauge.Changed += ShowAccuracy;
            subscribed = director != null || wheel != null || gauge != null;
        }

        void Unsubscribe()
        {
            if (!subscribed)
            {
                subscribed = false;
                return;
            }
            if (director != null) director.PhaseEntered -= OnPhaseEntered;
            if (wheel != null) wheel.Judged -= OnJudged;
            if (gauge != null) gauge.Changed -= ShowAccuracy;
            subscribed = false;
        }

        void OnPhaseEntered(PhaseDefinition phase, int index)
        {
            if (phaseLabel == null || phase == null) return;
            phaseLabel.text = phase.phaseName;
        }

        void OnJudged(NoteInstance note, Judgement judgement, double errorMs)
        {
            if (judgementLabel == null) return;
            judgementLabel.text = LabelOf(judgement);
            if (popup != null) StopCoroutine(popup);
            popup = StartCoroutine(HoldPopup());
        }

        void ShowAccuracy(float percent)
        {
            if (accuracyLabel == null) return;
            int shown = Mathf.RoundToInt(Mathf.Clamp(percent, 0f, 100f));
            accuracyLabel.text = shown + "%";
        }

        IEnumerator HoldPopup()
        {
            yield return new WaitForSeconds(Mathf.Max(0.05f, popupSeconds));
            if (judgementLabel != null) judgementLabel.text = "";
            popup = null;
        }

        static string LabelOf(Judgement judgement)
        {
            switch (judgement)
            {
                case Judgement.Perfect: return "PERFECT";
                case Judgement.Good: return "GOOD";
                case Judgement.Bad: return "BAD";
                default: return "MISS";
            }
        }
    }
}
