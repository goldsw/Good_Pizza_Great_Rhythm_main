using System.Collections.Generic;

namespace GPGR.Charting
{
    /// <summary>
    /// 경로 키를 각도로 바꾸는 순수 계산. 에디터 검증과 런타임이 같은 구현을 써야 하므로
    /// 여기에 둔다. 런타임 쪽 GPGR.Runtime.MotionPath 가 이걸 감싼다.
    /// </summary>
    public static class MotionPathMath
    {
        /// <summary>
        /// 현재 박에서 경로 키를 읽어 보간한다. 직전 프레임에 속도를 더하지 않는다.
        /// 키와 키 사이는 적힌 부호 있는 각도만큼 움직인다. 짧은 쪽으로 자동으로 꺾지 않는다.
        /// </summary>
        public static float AngleAt(IReadOnlyList<MotionKey> keys, double localBeat)
        {
            if (keys == null || keys.Count == 0) return 0f;
            if (keys.Count == 1) return keys[0].angleDeg;

            if (localBeat <= keys[0].beat) return keys[0].angleDeg;

            int last = keys.Count - 1;
            if (localBeat >= keys[last].beat) return keys[last].angleDeg;

            for (int i = 1; i <= last; i++)
            {
                MotionKey b = keys[i];
                if (localBeat > b.beat) continue;

                MotionKey a = keys[i - 1];
                double span = b.beat - a.beat;
                if (span <= 0.0) return b.angleDeg;

                double t = (localBeat - a.beat) / span;
                return (float)(a.angleDeg + (b.angleDeg - a.angleDeg) * t);
            }

            return keys[last].angleDeg;
        }
    }
}
