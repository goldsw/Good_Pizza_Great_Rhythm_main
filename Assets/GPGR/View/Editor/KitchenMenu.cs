using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GPGR.View.EditorTools
{
    /// <summary>
    /// 주방 배경과 궤도 파티클을 트레일러 씬에 꽂는다.
    /// </summary>
    public static class KitchenMenu
    {
        const string ScenePath = "Assets/GPGR/Scenes/Trailer.unity";
        const string BrightPath = "Assets/GPGR/Art/BgBright.png";
        const float PixelsPerUnit = 100f;

        [MenuItem("GPGR/View/Apply Background And Effects")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[GPGR] 플레이 중에는 배경을 꽂지 않는다.");
                return;
            }

            if (SceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            ConfigureBright();
            AssignBright();
            SetBackgroundPose();

            GameObject wheel = GameObject.Find("Wheel");
            GameObject track = Ensure("OrbitTrack", wheel != null ? wheel.transform : null);
            AddView(track, "OrbitTrackView");

            GameObject fx = Ensure("NoteFx", null);
            AddView(fx, "NoteFxView");

            WireBootstrap();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("[GPGR] 주방 배경과 파티클을 꽂았다. 검은 길 정렬 7 (피자 5보다 앞, 노트 20보다 뒤).");
        }

        static void ConfigureBright()
        {
            var importer = AssetImporter.GetAtPath(BrightPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError("[GPGR] 임포터 없음: " + BrightPath);
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Bilinear;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.textureType = TextureImporterType.Sprite;
            settings.spriteMode = (int)SpriteImportMode.Single;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spritePivot = new Vector2(0.5f, 0.5f);
            settings.spritePixelsPerUnit = PixelsPerUnit;
            settings.alphaIsTransparency = true;
            settings.mipmapEnabled = false;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        static void AssignBright()
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BrightPath);
            GameObject bright = GameObject.Find("Bright");
            if (sprite == null || bright == null) return;
            SpriteRenderer renderer = bright.GetComponent<SpriteRenderer>();
            if (renderer == null) return;
            renderer.sprite = sprite;
            renderer.color = Color.white;
            EditorUtility.SetDirty(renderer);
        }

        static void SetBackgroundPose()
        {
            GameObject backgrounds = GameObject.Find("Backgrounds");
            if (backgrounds == null) return;
            foreach (MonoBehaviour behaviour in backgrounds.GetComponents<MonoBehaviour>())
            {
                if (behaviour == null || behaviour.GetType().Name != "PhaseThemeView") continue;
                SerializedObject so = new SerializedObject(behaviour);
                SerializedProperty position = so.FindProperty("backgroundPosition");
                SerializedProperty scale = so.FindProperty("backgroundScale");
                if (position != null) position.vector3Value = Vector3.zero;
                if (scale != null) scale.vector3Value = new Vector3(0.926f, 0.926f, 0.926f);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static void WireBootstrap()
        {
            GameObject host = GameObject.Find("TrailerBootstrap");
            if (host == null) return;
            MonoBehaviour bootstrap = null;
            foreach (MonoBehaviour behaviour in host.GetComponents<MonoBehaviour>())
            {
                if (behaviour != null && behaviour.GetType().Name == "TrailerBootstrap")
                    bootstrap = behaviour;
            }
            if (bootstrap == null) return;

            SerializedObject so = new SerializedObject(bootstrap);
            AssignRef(so, "orbitTrack", "OrbitTrackView");
            AssignRef(so, "noteFx", "NoteFxView");
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AssignRef(SerializedObject so, string propertyName, string className)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property == null) return;
            foreach (MonoBehaviour behaviour in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
            {
                if (behaviour != null && behaviour.GetType().Name == className)
                {
                    property.objectReferenceValue = behaviour;
                    return;
                }
            }
        }

        static GameObject Ensure(string name, Transform parent)
        {
            GameObject found = GameObject.Find(name);
            if (found == null)
            {
                found = new GameObject(name);
                if (parent != null) found.transform.SetParent(parent, false);
            }
            found.transform.localPosition = Vector3.zero;
            found.transform.localRotation = Quaternion.identity;
            found.transform.localScale = Vector3.one;
            return found;
        }

        static void AddView(GameObject host, string className)
        {
            Type type = FindView(className);
            if (type == null || host.GetComponent(type) != null) return;
            host.AddComponent(type);
        }

        static Type FindView(string className)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name != "GPGR.View") continue;
                Type type = assembly.GetType("GPGR.View." + className);
                if (type != null) return type;
            }
            return null;
        }
    }
}
