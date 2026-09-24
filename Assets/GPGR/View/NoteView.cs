using GPGR.Charting;
using GPGR.Runtime;
using UnityEngine;

namespace GPGR.View
{
    /// <summary>
    /// 노트 아이콘. 궤도를 따라 이동하고 세로는 유지한다.
    /// </summary>
    public sealed class NoteView : MonoBehaviour
    {
        [SerializeField] SpriteRenderer iconRenderer;
        [SerializeField] TrailView trail;
        [SerializeField] TailView tail;
        [Tooltip("행에 아이콘이 없을 때. 폴백만 NoteRow.color를 곱한다. 재료 그림은 원본 색 그대로다.")]
        [SerializeField] Sprite fallbackIcon;

        public NoteInstance Note { get; private set; }

        public void Show(NoteInstance note, NoteWheel wheel, ChartClock clock, double secondsPerBeat, Transform parent)
        {
            Note = note;
            transform.SetParent(parent, false);
            ApplyIcon(note.Row);
            Snap(wheel);
            if (trail != null) trail.Arm(note.Row.color, DrawnRadius(wheel), secondsPerBeat);

            bool longNote = note.Row != null && note.Row.kind == NoteKind.Long;
            if (tail != null)
            {
                tail.gameObject.SetActive(longNote);
                if (longNote) tail.Show(note, wheel, clock);
            }
        }

        public void Tick(NoteWheel wheel)
        {
            if (Note == null || wheel == null) return;
            Snap(wheel);
            if (tail != null && tail.gameObject.activeSelf) tail.Tick();
        }

        public void Hide()
        {
            if (trail != null) trail.Disarm();
            if (tail != null) tail.Hide();
            Note = null;
            gameObject.SetActive(false);
        }

        void Snap(NoteWheel wheel)
        {
            Vector2 p = MotionPath.Position(Note.AngleDeg, wheel.Center, DrawnRadius(wheel));
            transform.position = new Vector3(p.x, p.y, 0f);
            transform.rotation = Quaternion.identity;
        }

        void ApplyIcon(NoteRow row)
        {
            if (iconRenderer == null) return;
            bool hasArt = row != null && row.icon != null;
            iconRenderer.sprite = hasArt ? row.icon : fallbackIcon;
            iconRenderer.color = hasArt || row == null ? Color.white : row.color;
        }

        static float DrawnRadius(NoteWheel wheel)
        {
            return OrbitTrackView.RadiusOr(wheel != null ? wheel.radius : 0f);
        }
    }
}
