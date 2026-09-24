using GPGR.Runtime;
using UnityEngine;

namespace GPGR.View
{
    /// <summary>
    /// 노트 궤도의 검은 고리.
    /// 작은 원이 스텐실을 찍고, 큰 원은 그 자리만 그리지 않는다. 다른 스프라이트는 스텐실을 읽지 않는다.
    /// </summary>
    public sealed class OrbitTrackView : MonoBehaviour
    {
        [Tooltip("피자(5)보다 앞, 노트 아이콘(20)보다 뒤.")]
        [SerializeField] int sortingOrder = 7;
        [Tooltip("1이면 불투명. 0.8이면 검정 레일이 20% 비친다.")]
        [SerializeField] float railAlpha = 0.8f;

        SpriteRenderer disc;
        SpriteRenderer hole;
        Sprite circle;
        float spawnRadius;
        static OrbitTrackView current;
        static Sprite sharedCircle;
        static Material discMaterial;
        static Material holeMaterial;

        public float SpawnRadius => spawnRadius;

        /// <summary>레일 두께의 가운데. 레일이 없으면 fallback.</summary>
        public static float RadiusOr(float fallback)
        {
            if (current == null || current.spawnRadius <= 0f) return fallback;
            return current.spawnRadius;
        }

        public void Bind(NoteWheel noteWheel)
        {
            if (noteWheel == null) return;
            Apply();
        }

        void Awake()
        {
            current = this;
            StopParticles();
            EnsureVisuals();
            Apply();
        }

        void OnEnable()
        {
            current = this;
        }

        void OnDisable()
        {
            if (current == this) current = null;
        }

        void LateUpdate()
        {
            Apply();
        }

        void StopParticles()
        {
            ParticleSystem system = GetComponent<ParticleSystem>();
            if (system == null) return;
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = false;
            ParticleSystemRenderer renderer = GetComponent<ParticleSystemRenderer>();
            if (renderer != null) renderer.enabled = false;
        }

        void EnsureVisuals()
        {
            circle = SharedCircle();

            if (disc == null)
            {
                Transform found = transform.Find("RailDisc");
                if (found != null) disc = found.GetComponent<SpriteRenderer>();
            }
            if (disc == null)
            {
                var child = new GameObject("RailDisc");
                child.transform.SetParent(transform, false);
                disc = child.AddComponent<SpriteRenderer>();
            }

            disc.sprite = circle;
            disc.sharedMaterial = DiscMaterial();
            PaintDisc();
            disc.sortingOrder = sortingOrder;
            disc.maskInteraction = SpriteMaskInteraction.None;

            Transform holeTransform = transform.Find("RailHole");
            if (holeTransform != null)
            {
                SpriteMask retired = holeTransform.GetComponent<SpriteMask>();
                if (retired != null) Destroy(retired);
                hole = holeTransform.GetComponent<SpriteRenderer>();
                if (hole == null) hole = holeTransform.gameObject.AddComponent<SpriteRenderer>();
            }
            else
            {
                var child = new GameObject("RailHole");
                child.transform.SetParent(transform, false);
                hole = child.AddComponent<SpriteRenderer>();
            }

            hole.sprite = circle;
            hole.color = Color.white;
            hole.sharedMaterial = HoleMaterial();
            hole.sortingOrder = sortingOrder - 1;
            hole.maskInteraction = SpriteMaskInteraction.None;
        }

        void Apply()
        {
            if (disc == null || hole == null || circle == null) return;

            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            disc.transform.localPosition = Vector3.zero;
            disc.transform.localRotation = Quaternion.identity;
            disc.transform.localScale = new Vector3(7f, 7f, 1.230848f);

            hole.transform.localPosition = Vector3.zero;
            hole.transform.localRotation = Quaternion.identity;
            hole.transform.localScale = new Vector3(5.7f, 5.7f, 1.0872f);

            disc.sprite = circle;
            PaintDisc();
            disc.sortingOrder = sortingOrder;
            hole.sprite = circle;
            hole.sortingOrder = sortingOrder - 1;

            float outer = CircleRadius(disc);
            float inner = CircleRadius(hole);
            if (outer > 0f && inner > 0f)
                spawnRadius = inner + (outer - inner) * 0.5f;
        }

        void PaintDisc()
        {
            if (disc == null) return;
            disc.color = Color.white;
            Material material = disc.sharedMaterial;
            if (material != null) material.SetColor("_Color", RailColor());
        }

        Color RailColor()
        {
            return new Color(0f, 0f, 0f, Mathf.Clamp01(railAlpha));
        }

        static float CircleRadius(SpriteRenderer renderer)
        {
            if (renderer == null || renderer.sprite == null) return 0f;
            float half = renderer.sprite.bounds.extents.x;
            float scale = Mathf.Abs(renderer.transform.lossyScale.x);
            return half * scale;
        }

        static Sprite SharedCircle()
        {
            if (sharedCircle != null) return sharedCircle;

            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "RailDisc";
            texture.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color32[size * size];
            float reach = (size - 1) * 0.5f;
            var center = new Vector2(reach, reach);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    byte alpha = (byte)Mathf.Clamp(Mathf.RoundToInt((reach - distance + 0.5f) * 255f), 0, 255);
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            sharedCircle = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                size,
                0,
                SpriteMeshType.FullRect);
            sharedCircle.name = "RailDisc";
            return sharedCircle;
        }

        static Material DiscMaterial()
        {
            if (discMaterial != null) return discMaterial;
            discMaterial = Create("GPGR/RailDisc");
            return discMaterial;
        }

        static Material HoleMaterial()
        {
            if (holeMaterial != null) return holeMaterial;
            holeMaterial = Create("GPGR/RailHole");
            return holeMaterial;
        }

        static Material Create(string shaderName)
        {
            Shader shader = Shader.Find(shaderName);
            return shader != null ? new Material(shader) : null;
        }
    }
}
