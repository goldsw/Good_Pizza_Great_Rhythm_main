using System;
using UnityEngine;

namespace GPGR.Charting
{
    public enum Judgement { Perfect, Good, Bad, Miss }

    [Serializable]
    public struct JudgementWindows
    {
        [Tooltip("ms")] public double perfectMs;
        [Tooltip("ms")] public double goodMs;
        [Tooltip("ms")] public double badMs;
        [Tooltip("%p")] public float perfectPenalty;
        [Tooltip("%p")] public float goodPenalty;
        [Tooltip("%p")] public float badPenalty;
        [Tooltip("%p")] public float missPenalty;

        public static JudgementWindows Default => new JudgementWindows
        {
            perfectMs = 50.0,
            goodMs = 100.0,
            badMs = 160.0,
            perfectPenalty = 0f,
            goodPenalty = 2f,
            badPenalty = 5f,
            missPenalty = 10f,
        };

        /// <summary>
        /// 윈도우가 서로 겹치므로 위에서부터 먼저 맞는 줄을 쓴다.
        /// </summary>
        public Judgement Classify(double absErrorMs)
        {
            if (absErrorMs <= perfectMs) return Judgement.Perfect;
            if (absErrorMs <= goodMs) return Judgement.Good;
            if (absErrorMs <= badMs) return Judgement.Bad;
            return Judgement.Miss;
        }

        public float PenaltyOf(Judgement j)
        {
            switch (j)
            {
                case Judgement.Perfect: return perfectPenalty;
                case Judgement.Good: return goodPenalty;
                case Judgement.Bad: return badPenalty;
                default: return missPenalty;
            }
        }
    }
}
