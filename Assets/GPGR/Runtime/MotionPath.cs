using System.Collections.Generic;
using GPGR.Charting;
using UnityEngine;

namespace GPGR.Runtime
{
    /// <summary>
    /// 각도 보간은 <see cref="MotionPathMath.AngleAt"/>을 그대로 부른다.
    /// 위치만 여기서 구한다. 오른쪽 0°, 반시계가 양수.
    /// </summary>
    public static class MotionPath
    {
        public static float AngleAt(IReadOnlyList<MotionKey> keys, double localBeat)
        {
            return MotionPathMath.AngleAt(keys, localBeat);
        }

        public static Vector2 Position(float angleDeg, Vector2 center, float radius)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            return new Vector2(
                center.x + radius * Mathf.Cos(rad),
                center.y + radius * Mathf.Sin(rad));
        }
    }
}
