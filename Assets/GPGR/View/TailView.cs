using GPGR.Runtime;
using UnityEngine;

namespace GPGR.View
{
    /// <summary>
    /// 롱노트 몸통. 잔상이 아니라 tailBeats 동안 지나온 원호다.
    /// </summary>
    public sealed class TailView : MonoBehaviour
    {
        [Tooltip("몸통 두께. 유닛.")]
        [SerializeField] float width = 0.42f;
        [Tooltip("원호를 쪼개는 각도.")]
        [SerializeField] float degreesPerStep = 4f;
        [SerializeField] int sortingOrder = 8;
        [SerializeField] Material lineMaterial;

        LineRenderer line;
        NoteInstance note;
        NoteWheel wheel;
        ChartClock clock;

        public void Show(NoteInstance noteInstance, NoteWheel noteWheel, ChartClock chartClock)
        {
            note = noteInstance;
            wheel = noteWheel;
            clock = chartClock;
            EnsureLine();
            line.enabled = true;
            if (note != null && note.Row != null) line.startColor = line.endColor = note.Row.color;
            Tick();
        }

        public void Hide()
        {
            note = null;
            if (line != null)
            {
                line.positionCount = 0;
                line.enabled = false;
            }
        }

        public void Tick()
        {
            if (note == null || note.Row == null || wheel == null || clock == null)
            {
                Hide();
                return;
            }

            EnsureLine();
            double local = note.LocalBeat(clock.Beat);
            float back = MotionPath.AngleAt(note.Row.path, local - note.Row.tailBeats);
            float front = note.AngleDeg;
            float span = front - back;

            if (Mathf.Abs(span) < 0.5f)
            {
                line.positionCount = 0;
                return;
            }

            int steps = Mathf.Clamp(Mathf.CeilToInt(Mathf.Abs(span) / Mathf.Max(0.5f, degreesPerStep)), 1, 180);
            line.positionCount = steps + 1;
            line.startWidth = line.endWidth = width;
            line.startColor = line.endColor = note.Row.color;

            Vector2 center = wheel.Center;
            float radius = OrbitTrackView.RadiusOr(wheel.radius);
            for (int i = 0; i <= steps; i++)
            {
                float angle = back + span * (i / (float)steps);
                Vector2 p = MotionPath.Position(angle, center, radius);
                line.SetPosition(i, new Vector3(p.x, p.y, 0f));
            }
        }

        void EnsureLine()
        {
            if (line != null) return;
            line = GetComponent<LineRenderer>();
            if (line == null) line = gameObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = false;
            line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Stretch;
            line.numCapVertices = 6;
            line.numCornerVertices = 4;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.sortingOrder = sortingOrder;
            line.positionCount = 0;
            line.sharedMaterial = lineMaterial != null ? lineMaterial : FallbackMaterial();
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
