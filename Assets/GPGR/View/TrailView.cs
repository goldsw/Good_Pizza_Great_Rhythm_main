using UnityEngine;

namespace GPGR.View
{
    /// <summary>
    /// 노트 뒤를 따르는 잔상. 각도를 다시 계산하지 않고 실제 위치를 기록한다.
    /// </summary>
    public sealed class TrailView : MonoBehaviour
    {
        [Tooltip("잠정 0.5박.")]
        [SerializeField] float lengthBeats = 0.5f;
        [Tooltip("반지름에 곱한다. 잠정 0.06.")]
        [SerializeField] float widthOverRadius = 0.06f;
        [Tooltip("잠정 0.2초. 트레일 끝의 페이드.")]
        [SerializeField] float fadeSeconds = 0.2f;
        [SerializeField] int sortingOrder = 9;
        [SerializeField] Material lineMaterial;

        TrailRenderer trail;

        public void Arm(Color color, float radius, double secondsPerBeat)
        {
            EnsureTrail();
            float lifetime = Mathf.Max(0.01f, lengthBeats * (float)secondsPerBeat);
            trail.time = lifetime;
            trail.widthMultiplier = Mathf.Max(0.001f, radius * widthOverRadius);
            trail.colorGradient = Fade(color, lifetime);
            trail.Clear();
            trail.emitting = true;
        }

        public void Disarm()
        {
            if (trail == null) return;
            trail.emitting = false;
            trail.Clear();
        }

        void EnsureTrail()
        {
            if (trail != null) return;
            trail = GetComponent<TrailRenderer>();
            if (trail == null) trail = gameObject.AddComponent<TrailRenderer>();
            trail.autodestruct = false;
            trail.emitting = false;
            trail.minVertexDistance = 0.02f;
            trail.alignment = LineAlignment.View;
            trail.textureMode = LineTextureMode.Stretch;
            trail.numCapVertices = 4;
            trail.numCornerVertices = 2;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.sortingOrder = sortingOrder;
            trail.sharedMaterial = lineMaterial != null ? lineMaterial : FallbackMaterial();
            trail.Clear();
        }

        Gradient Fade(Color color, float lifetime)
        {
            float fadePortion = Mathf.Clamp01(fadeSeconds / lifetime);
            float solidUntil = 1f - fadePortion;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
                new[]
                {
                    new GradientAlphaKey(color.a, 0f),
                    new GradientAlphaKey(color.a, solidUntil),
                    new GradientAlphaKey(0f, 1f),
                });
            return gradient;
        }

        static Material fallback;

        static Material FallbackMaterial()
        {
            if (fallback != null) return fallback;
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            fallback = shader != null ? new Material(shader) : null;
            return fallback;
        }
    }
}
