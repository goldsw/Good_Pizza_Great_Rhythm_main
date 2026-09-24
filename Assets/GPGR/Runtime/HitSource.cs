using System;
using UnityEngine;

namespace GPGR.Runtime
{
    /// <summary>
    /// 누르기와 떼기. 원판은 누가 보냈는지 모른다.
    /// 폴링은 <see cref="TimelineDirector"/>가 프레임 순서를 지키기 위해 호출한다.
    /// </summary>
    public abstract class HitSource : MonoBehaviour
    {
        public event Action<double> Pressed;
        public event Action<double> Released;

        protected void RaisePressed(double songBeat)
        {
            Pressed?.Invoke(songBeat);
        }

        protected void RaiseReleased(double songBeat)
        {
            Released?.Invoke(songBeat);
        }

        internal abstract void Poll(double songBeat);
    }
}
