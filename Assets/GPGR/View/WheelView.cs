using GPGR.Runtime;
using UnityEngine;

namespace GPGR.View
{
    /// <summary>
    /// 피자 그림과 분리된 고정 원판. 링과 하단 히트 표시는 움직이지 않는다.
    /// </summary>
    public sealed class WheelView : MonoBehaviour
    {
        [SerializeField] SpriteRenderer ring;
        [SerializeField] SpriteRenderer hitMarker;
        [Tooltip("NoteWheel이 아직 없을 때 쓰는 반지름.")]
        [SerializeField] float fallbackRadius = 2.85f;
        [SerializeField] Color tint = Color.white;

        NoteWheel wheel;

        public void Bind(NoteWheel noteWheel)
        {
            wheel = noteWheel;
        }

        public void ApplyTint(Color color)
        {
            tint = color;
            if (ring != null) ring.color = tint;
        }

        void Awake()
        {
            if (wheel == null) wheel = GetComponent<NoteWheel>();
            if (ring != null) ring.color = tint;
        }

        void LateUpdate()
        {
            float chartRadius = wheel != null ? wheel.radius : fallbackRadius;
            float radius = OrbitTrackView.RadiusOr(chartRadius);
            Vector2 center = wheel != null ? wheel.Center : (Vector2)transform.position;
            PlaceHitMarker(center, radius);
        }

        void PlaceHitMarker(Vector2 center, float radius)
        {
            if (hitMarker == null) return;
            Vector2 p = MotionPath.Position(270f, center, Mathf.Max(0.05f, radius));
            hitMarker.transform.position = new Vector3(p.x, p.y, 0f);
            hitMarker.transform.rotation = Quaternion.identity;
        }
    }
}
