using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GPGR.View.EditorTools
{
    /// <summary>
    /// 인게임 그림을 임포트 설정하고 트레일러 씬에 꽂는다.
    /// </summary>
    public static class IngameArtMenu
    {
        const string ScenePath = "Assets/GPGR/Scenes/Trailer.unity";
        const string NotePrefabPath = "Assets/GPGR/Prefabs/Note.prefab";
        const float PixelsPerUnit = 100f;
        const byte OpaqueAlpha = 16;

        const string WheelBoardPath = "Assets/GPGR/Art/WheelBoard.png";
        const string PizzaPath = "Assets/GPGR/Art/Pizza.png";
        const string DarkPath = "Assets/GPGR/Art/BgDark.png";
        const string DoughPath = "Assets/GPGR/Art/Notes/Dough.png";
        const string SaucePath = "Assets/GPGR/Art/Notes/Sauce.png";
        const string CheesePath = "Assets/GPGR/Art/Notes/Cheese.png";
        const string PepperoniPath = "Assets/GPGR/Art/Notes/Pepperoni.png";

        [MenuItem("GPGR/View/Apply Ingame Art")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[GPGR] 플레이 중에는 인게임 아트를 꽂지 않는다.");
                return;
            }

            if (SceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            Configure(DoughPath, new Vector2(0.5f, 0.5f), false, false);
            Configure(SaucePath, new Vector2(0.5f, 0.5f), false, false);
            Configure(CheesePath, new Vector2(0.5f, 0.5f), false, false);
            Configure(PepperoniPath, new Vector2(0.5f, 0.5f), false, false);
            Configure(DarkPath, new Vector2(0.5f, 0.5f), false, false);
            Configure(WheelBoardPath, new Vector2(0.5f, 0.5f), true, true);
            Configure(PizzaPath, new Vector2(0.5f, 0.5f), true, true);

            Vector2 boardPivot = MeasureCircle(WheelBoardPath, out float boardFraction, out _);
            Vector2 pizzaPivot = MeasureCircle(PizzaPath, out _, out int pizzaWidth);
            Configure(WheelBoardPath, boardPivot, false, true);
            Configure(PizzaPath, pizzaPivot, false, true);

            Sprite board = LoadSprite(WheelBoardPath);
            Sprite pizza = LoadSprite(PizzaPath);
            Sprite dark = LoadSprite(DarkPath);
            AssignSprite("Ring", board);
            AssignSprite("Pizza", pizza);
            AssignSprite("Dark", dark);

            float radius = ReadRadius();
            FitBoard(board, boardFraction, radius);
            FitPizza(pizzaWidth, radius);
            FitHitMarker();
            ClearNotePrefabSprite();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();

            Debug.Log("[GPGR] 도마 피벗 " + boardPivot.ToString("0.000")
                + " (가장 넓은 불투명 줄의 중점, 나무 원 중심). 궤도 비율 " + boardFraction.ToString("0.000"));
            Debug.Log("[GPGR] 피자 피벗 " + pizzaPivot.ToString("0.000")
                + " (피자 원의 중심. 텍스처 정중앙이 아님).");
        }

        static void Configure(string path, Vector2 pivot, bool readable, bool customPivot)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError("[GPGR] 임포터 없음: " + path);
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.isReadable = readable;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Bilinear;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.textureType = TextureImporterType.Sprite;
            settings.spriteMode = (int)SpriteImportMode.Single;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)(customPivot ? SpriteAlignment.Custom : SpriteAlignment.Center);
            settings.spritePivot = pivot;
            settings.spritePixelsPerUnit = PixelsPerUnit;
            settings.alphaIsTransparency = true;
            settings.readable = readable;
            settings.mipmapEnabled = false;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        static Vector2 MeasureCircle(string path, out float widthFraction, out int maxRowWidth)
        {
            widthFraction = 1f;
            maxRowWidth = 0;
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null) return new Vector2(0.5f, 0.5f);

            Color32[] pixels = texture.GetPixels32();
            int w = texture.width;
            int h = texture.height;
            int eq = 0;
            int leftEq = 0;
            int rightEq = 0;
            for (int y = 0; y < h; y++)
            {
                int left = -1;
                int right = -1;
                int row = y * w;
                for (int x = 0; x < w; x++)
                {
                    if (pixels[row + x].a <= OpaqueAlpha) continue;
                    if (left < 0) left = x;
                    right = x;
                }
                int rowWidth = left < 0 ? 0 : right - left + 1;
                if (rowWidth <= maxRowWidth) continue;
                maxRowWidth = rowWidth;
                eq = y;
                leftEq = left;
                rightEq = right;
            }

            if (maxRowWidth <= 0) return new Vector2(0.5f, 0.5f);
            float cx = (leftEq + rightEq) * 0.5f;
            widthFraction = maxRowWidth / (float)w;
            return new Vector2((cx + 0.5f) / w, (eq + 0.5f) / h);
        }

        static Sprite LoadSprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null) return sprite;
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite found) return found;
            }
            Debug.LogError("[GPGR] 스프라이트 없음: " + path);
            return null;
        }

        static void AssignSprite(string objectName, Sprite sprite)
        {
            if (sprite == null) return;
            GameObject host = GameObject.Find(objectName);
            if (host == null)
            {
                Debug.LogError("[GPGR] 씬 오브젝트 없음: " + objectName);
                return;
            }
            var renderer = host.GetComponent<SpriteRenderer>();
            if (renderer == null) return;
            renderer.sprite = sprite;
            renderer.color = Color.white;
            EditorUtility.SetDirty(renderer);
        }

        static float ReadRadius()
        {
            GameObject wheel = GameObject.Find("Wheel");
            if (wheel == null) return 3f;
            MonoBehaviour[] behaviours = wheel.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null || behaviour.GetType().Name != "NoteWheel") continue;
                SerializedObject so = new SerializedObject(behaviour);
                SerializedProperty radius = so.FindProperty("radius");
                if (radius != null && radius.floatValue > 0.01f) return radius.floatValue;
            }
            return 3f;
        }

        static void FitBoard(Sprite board, float widthFraction, float radius)
        {
            GameObject ring = GameObject.Find("Ring");
            if (ring == null) return;
            ring.transform.localPosition = Vector3.zero;
            ring.transform.localRotation = Quaternion.identity;
            if (board != null && widthFraction > 0.01f)
            {
                float native = board.bounds.size.x * widthFraction;
                if (native > 0.0001f)
                    ring.transform.localScale = Vector3.one * (radius * 2f / native);
            }

            GameObject wheel = GameObject.Find("Wheel");
            if (wheel == null) return;
            MonoBehaviour[] behaviours = wheel.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] == null || behaviours[i].GetType().Name != "WheelView") continue;
                SerializedObject so = new SerializedObject(behaviours[i]);
                SerializedProperty fraction = so.FindProperty("orbitWidthFraction");
                SerializedProperty tint = so.FindProperty("tint");
                if (fraction != null) fraction.floatValue = widthFraction;
                if (tint != null) tint.colorValue = Color.white;
                so.ApplyModifiedProperties();
            }
        }

        static void FitPizza(int opaqueWidth, float radius)
        {
            GameObject pizza = GameObject.Find("Pizza");
            if (pizza == null || opaqueWidth <= 0) return;
            float desired = radius * 2f * 0.68f;
            float native = opaqueWidth / PixelsPerUnit;
            float scale = native > 0.0001f ? desired / native : 1f;
            pizza.transform.localPosition = Vector3.zero;
            pizza.transform.localRotation = Quaternion.identity;
            pizza.transform.localScale = new Vector3(scale, scale, 1f);
            var renderer = pizza.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = 5;
                EditorUtility.SetDirty(renderer);
            }
            Debug.Log("[GPGR] 피자 스케일 " + scale.ToString("0.000")
                + " (나무 원 지름의 0.68, 테두리가 보이게).");
        }

        static void FitHitMarker()
        {
            GameObject marker = GameObject.Find("HitMarker");
            if (marker == null) return;
            var renderer = marker.GetComponent<SpriteRenderer>();
            float half = renderer != null && renderer.sprite != null
                ? renderer.sprite.bounds.extents.y * marker.transform.localScale.y
                : 0.2f;
            float inset = Mathf.Max(0.05f, half - 0.06f);

            GameObject wheel = GameObject.Find("Wheel");
            if (wheel == null) return;
            MonoBehaviour[] behaviours = wheel.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] == null || behaviours[i].GetType().Name != "WheelView") continue;
                SerializedObject so = new SerializedObject(behaviours[i]);
                SerializedProperty property = so.FindProperty("hitMarkerInset");
                if (property != null) property.floatValue = inset;
                so.ApplyModifiedProperties();
                Debug.Log("[GPGR] 히트 마커 안쪽 당김 " + inset.ToString("0.000")
                    + " (손잡이 시작이 아니라 나무 원 위).");
            }
        }

        static void ClearNotePrefabSprite()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(NotePrefabPath);
            if (root == null) return;
            var renderer = root.GetComponent<SpriteRenderer>();
            if (renderer != null) renderer.sprite = null;
            PrefabUtility.SaveAsPrefabAsset(root, NotePrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
