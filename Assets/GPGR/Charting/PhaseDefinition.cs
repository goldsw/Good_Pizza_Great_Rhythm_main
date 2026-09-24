using System;
using System.Collections.Generic;
using UnityEngine;

namespace GPGR.Charting
{
    public enum Backdrop { Bright, Dark }
    public enum PizzaState { None, IntoOven, OutOfOven }

    [Serializable]
    public sealed class PhaseDefinition
    {
        [Tooltip("HUD에 그대로 나간다. 연출 분기에는 쓰지 않는다.")]
        public string phaseName = "";

        public List<NoteRow> rows = new List<NoteRow>();

        public Backdrop backdrop = Backdrop.Bright;
        public PizzaState pizza = PizzaState.None;

        [Tooltip("끄면 직전 틴트를 유지한다.")] public bool overrideWheelTint;
        public Color wheelTint = Color.white;

        [Tooltip("노트가 없는 페이즈(완성)가 연출로 버티는 박. 노트가 있으면 지워지는 박과 비교해 더 긴 쪽을 쓴다.")]
        public double holdBeats;
    }
}
