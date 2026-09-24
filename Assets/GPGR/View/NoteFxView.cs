using GPGR.Charting;
using GPGR.Runtime;
using UnityEngine;

namespace GPGR.View
{
    /// <summary>
    /// 스폰은 짧은 링, 타격은 한 점의 폭발. Miss와 빈 입력에는 타격이 없다.
    /// 파티클은 월드에 남고 노트를 따라가지 않는다.
    /// </summary>
    public sealed class NoteFxView : MonoBehaviour
    {
        [SerializeField] int spawnCount = 16;
        [SerializeField] float spawnRingRadius = 0.16f;
        [SerializeField] float spawnSpeed = 1.4f;
        [SerializeField] float spawnSize = 0.12f;
        [SerializeField] float spawnLifetime = 0.28f;
        [SerializeField] int hitCount = 22;
        [SerializeField] float hitSpeed = 2.6f;
        [SerializeField] float hitSize = 0.2f;
        [SerializeField] float hitLifetime = 0.22f;
        [SerializeField] int spawnSortingOrder = 16;
        [SerializeField] int hitSortingOrder = 17;

        NoteWheel wheel;
        ParticleSystem spawnBurst;
        ParticleSystem hitBurst;
        bool subscribed;

        public void Bind(NoteWheel noteWheel)
        {
            Unsubscribe();
            wheel = noteWheel;
            Subscribe();
        }

        void Awake()
        {
            spawnBurst = ChildSystem("SpawnBurst", spawnSortingOrder);
            hitBurst = ChildSystem("HitBurst", hitSortingOrder);
        }

        void OnEnable()
        {
            Subscribe();
        }

        void OnDisable()
        {
            Unsubscribe();
        }

        void Subscribe()
        {
            if (subscribed || wheel == null) return;
            wheel.Spawned += OnSpawned;
            wheel.Judged += OnJudged;
            subscribed = true;
        }

        void Unsubscribe()
        {
            if (!subscribed || wheel == null)
            {
                subscribed = false;
                return;
            }
            wheel.Spawned -= OnSpawned;
            wheel.Judged -= OnJudged;
            subscribed = false;
        }

        void OnSpawned(NoteInstance note)
        {
            if (note == null || wheel == null || spawnBurst == null) return;
            Vector3 at = Place(note);
            Color color = RowColor(note);
            for (int i = 0; i < spawnCount; i++)
            {
                float angle = (i + 0.5f) / spawnCount * Mathf.PI * 2f;
                Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                Emit(spawnBurst, at + dir * spawnRingRadius, dir * spawnSpeed, color, spawnSize, spawnLifetime);
            }
        }

        void OnJudged(NoteInstance note, Judgement judgement, double signedErrorMs)
        {
            if (judgement == Judgement.Miss) return;
            if (note == null || wheel == null || hitBurst == null) return;
            Vector3 at = Place(note);
            Color color = RowColor(note);
            for (int i = 0; i < hitCount; i++)
            {
                float angle = (i + 0.5f) / hitCount * Mathf.PI * 2f;
                Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                Emit(hitBurst, at, dir * hitSpeed, color, hitSize, hitLifetime);
            }
        }

        Vector3 Place(NoteInstance note)
        {
            Vector2 point = MotionPath.Position(note.AngleDeg, wheel.Center, OrbitTrackView.RadiusOr(wheel.radius));
            return new Vector3(point.x, point.y, 0f);
        }

        static Color RowColor(NoteInstance note)
        {
            if (note.Row == null) return Color.white;
            return note.Row.color;
        }

        static void Emit(ParticleSystem system, Vector3 position, Vector3 velocity, Color color, float size, float lifetime)
        {
            var emit = new ParticleSystem.EmitParams();
            emit.position = position;
            emit.velocity = velocity;
            emit.startColor = color;
            emit.startSize = size;
            emit.startLifetime = lifetime;
            system.Emit(emit, 1);
        }

        ParticleSystem ChildSystem(string childName, int sortingOrder)
        {
            Transform child = transform.Find(childName);
            GameObject host = child != null ? child.gameObject : new GameObject(childName);
            if (child == null)
            {
                host.transform.SetParent(transform, false);
            }
            return ParticleSheets.Ensure(host, sortingOrder);
        }
    }
}
