using System;
using GPGR.Charting;
using UnityEngine;

namespace GPGR.Runtime
{
    /// <summary>
    /// 100에서 시작한다. MonoBehaviour가 아니다. 디렉터가 들고 있는다.
    /// </summary>
    public sealed class AccuracyGauge
    {
        public float Percent { get; private set; } = 100f;

        public event Action<float> Changed;

        public void Reset()
        {
            Percent = 100f;
            Changed?.Invoke(Percent);
        }

        public void Apply(Judgement judgement, in JudgementWindows windows)
        {
            float next = Mathf.Clamp(Percent - windows.PenaltyOf(judgement), 0f, 100f);
            if (Mathf.Approximately(next, Percent))
                return;

            Percent = next;
            Changed?.Invoke(Percent);
        }
    }
}
