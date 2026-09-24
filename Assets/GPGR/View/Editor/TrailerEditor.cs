using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GPGR.View.EditorTools
{
    [InitializeOnLoad]
    static class TrailerEditor
    {
        const string ScenePath = "Assets/GPGR/Scenes/Trailer.unity";
        const string ChartPath = "Assets/GPGR/Scenes/TempTrailerChart.asset";

        static bool gameViewWarned;

        static TrailerEditor()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorApplication.delayCall += WireActiveScene;
        }

        static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            EditorApplication.delayCall += WireActiveScene;
        }

        [MenuItem("GPGR/View/Add Runtime Components")]
        static void MenuWire()
        {
            if (SceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            WireActiveScene();
        }

        static void WireActiveScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath) return;

            SetGameView(1920, 1080);

            bool changed = false;
            changed |= PreferDemoChart();
            changed |= Add(GameObject.Find("Wheel"), "NoteWheel");
            GameObject timeline = GameObject.Find("Timeline");
            changed |= Add(timeline, "ChartClock");
            changed |= Add(timeline, "TimelineDirector");
            changed |= Add(timeline, "AutoPlayHitSource");
            changed |= Add(timeline, "PlayerHitSource");
            if (timeline != null && timeline.GetComponent<AudioSource>() == null)
            {
                AudioSource audio = timeline.AddComponent<AudioSource>();
                audio.playOnAwake = false;
                changed = true;
            }

            foreach (MonoBehaviour behaviour in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (behaviour == null) continue;
                string ns = behaviour.GetType().Namespace;
                if (ns != "GPGR.Runtime") continue;
                changed |= FillEmptyRefs(behaviour);
                changed |= EnsureRadius(behaviour);
            }

            if (!changed) return;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        const string DemoPath = "Assets/GPGR/Charts/DemoChart.asset";

        static bool PreferDemoChart()
        {
            UnityEngine.Object demo = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(DemoPath);
            if (demo == null) return false;
            GameObject host = GameObject.Find("TrailerBootstrap");
            if (host == null) return false;

            MonoBehaviour[] behaviours = host.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null || behaviour.GetType().Name != "TrailerBootstrap") continue;
                SerializedObject so = new SerializedObject(behaviour);
                SerializedProperty chart = so.FindProperty("chart");
                if (chart == null || chart.objectReferenceValue == demo) return false;
                string current = chart.objectReferenceValue != null
                    ? AssetDatabase.GetAssetPath(chart.objectReferenceValue)
                    : "";
                if (!string.IsNullOrEmpty(current) && current != ChartPath) return false;
                chart.objectReferenceValue = demo;
                so.ApplyModifiedPropertiesWithoutUndo();
                return true;
            }
            return false;
        }

        static bool Add(GameObject host, string className)
        {
            if (host == null) return false;
            Type type = FindRuntimeType(className);
            if (type == null || !typeof(Component).IsAssignableFrom(type)) return false;
            if (host.GetComponent(type) != null) return false;
            host.AddComponent(type);
            return true;
        }

        static Type FindRuntimeType(string className)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name != "GPGR.Runtime") continue;
                Type type = assembly.GetType("GPGR.Runtime." + className);
                if (type != null) return type;
            }
            return null;
        }

        static bool EnsureRadius(Component component)
        {
            SerializedObject so = new SerializedObject(component);
            SerializedProperty radius = so.FindProperty("radius");
            if (radius == null || radius.propertyType != SerializedPropertyType.Float) return false;
            if (radius.floatValue > 0f) return false;
            radius.floatValue = 3.4075f;
            so.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        static bool FillEmptyRefs(Component host)
        {
            SerializedObject so = new SerializedObject(host);
            SerializedProperty it = so.GetIterator();
            bool enter = true;
            bool changed = false;
            while (it.Next(enter))
            {
                enter = false;
                if (it.propertyType != SerializedPropertyType.ObjectReference) continue;
                if (it.objectReferenceValue != null) continue;
                string typeName = PointerType(it.type);
                if (string.IsNullOrEmpty(typeName) || typeName == "MonoScript") continue;
                UnityEngine.Object found = FindSceneObject(typeName);
                if (found == null) continue;
                it.objectReferenceValue = found;
                changed = true;
            }
            if (changed) so.ApplyModifiedPropertiesWithoutUndo();
            return changed;
        }

        static UnityEngine.Object FindSceneObject(string className)
        {
            if (className == "SongChart")
                return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ChartPath);
            if (className == "AudioSource")
                return UnityEngine.Object.FindAnyObjectByType<AudioSource>();

            foreach (MonoBehaviour behaviour in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (behaviour != null && behaviour.GetType().Name == className)
                    return behaviour;
            }
            return null;
        }

        static string PointerType(string propertyType)
        {
            if (string.IsNullOrEmpty(propertyType)) return null;
            const string dollar = "PPtr<$";
            const string plain = "PPtr<";
            if (propertyType.StartsWith(dollar) && propertyType.EndsWith(">"))
                return propertyType.Substring(dollar.Length, propertyType.Length - dollar.Length - 1);
            if (propertyType.StartsWith(plain) && propertyType.EndsWith(">"))
                return propertyType.Substring(plain.Length, propertyType.Length - plain.Length - 1);
            return null;
        }

        static void SetGameView(int width, int height)
        {
            try
            {
                Assembly editor = typeof(Editor).Assembly;
                Type gameViewType = editor.GetType("UnityEditor.GameView");
                UnityEngine.Object[] windows = Resources.FindObjectsOfTypeAll(gameViewType);
                if (windows == null || windows.Length == 0) return;

                Type sizesType = editor.GetType("UnityEditor.GameViewSizes");
                Type singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
                object sizes = singleton.GetProperty("instance").GetValue(null);
                object group = sizesType.GetProperty("currentGroup").GetValue(sizes);
                Type groupType = group.GetType();
                int count = (int)groupType.GetMethod("GetTotalCount").Invoke(group, null);
                MethodInfo getSize = groupType.GetMethod("GetGameViewSize");
                int index = -1;
                for (int i = 0; i < count; i++)
                {
                    object size = getSize.Invoke(group, new object[] { i });
                    Type sizeType = size.GetType();
                    int w = (int)sizeType.GetProperty("width").GetValue(size);
                    int h = (int)sizeType.GetProperty("height").GetValue(size);
                    if (w == width && h == height)
                    {
                        index = i;
                        break;
                    }
                }

                if (index < 0)
                {
                    Type sizeType = editor.GetType("UnityEditor.GameViewSize");
                    Type enumType = editor.GetType("UnityEditor.GameViewSizeType");
                    object fixedResolution = Enum.Parse(enumType, "FixedResolution");
                    ConstructorInfo ctor = sizeType.GetConstructor(new[] { enumType, typeof(int), typeof(int), typeof(string) });
                    object created = ctor.Invoke(new[] { fixedResolution, width, height, width + "x" + height });
                    groupType.GetMethod("AddCustomSize").Invoke(group, new[] { created });
                    index = (int)groupType.GetMethod("GetTotalCount").Invoke(group, null) - 1;
                }

                PropertyInfo selected = gameViewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                selected.SetValue(windows[0], index);
            }
            catch (Exception ex)
            {
                if (gameViewWarned) return;
                gameViewWarned = true;
                Debug.LogWarning("게임 뷰를 1920x1080으로 맞추지 못했다. 게임 뷰 해상도에서 직접 고른다. " + ex.Message);
            }
        }
    }
}
