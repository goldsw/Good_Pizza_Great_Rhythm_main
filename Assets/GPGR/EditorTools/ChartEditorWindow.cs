using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GPGR.Charting;

namespace GPGR.EditorTools
{
    public sealed class ChartEditorWindow : EditorWindow
    {
        const string SessionKey = "GPGR.ChartEditor.Target";

        SongChart chart;
        Vector2 scroll;
        Vector2 validationScroll;
        bool songOpen = true;
        readonly Dictionary<int, bool> phaseOpen = new Dictionary<int, bool>();
        int presetIndex;
        double presetHitBeat = 2.0;

        [MenuItem("GPGR/Chart Editor")]
        public static void Open()
        {
            GetWindow<ChartEditorWindow>("Chart Editor");
        }

        void OnEnable()
        {
            minSize = new Vector2(460f, 520f);
            Undo.undoRedoPerformed += Repaint;
            string path = SessionState.GetString(SessionKey, "");
            if (!string.IsNullOrEmpty(path))
                chart = AssetDatabase.LoadAssetAtPath<SongChart>(path);
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= Repaint;
        }

        void OnGUI()
        {
            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 170f;
            try
            {
                DrawPicker();
                if (chart == null)
                {
                    EditorGUILayout.HelpBox("Song Chart 에셋을 고르세요.", MessageType.Info);
                    return;
                }

                scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.ExpandHeight(true));
                DrawSong();
                DrawPhases();
                EditorGUILayout.EndScrollView();
                DrawValidation();
            }
            finally
            {
                EditorGUIUtility.labelWidth = labelWidth;
            }
        }

        void DrawPicker()
        {
            EditorGUILayout.Space(4f);
            var next = (SongChart)EditorGUILayout.ObjectField("Song Chart", chart, typeof(SongChart), false);
            if (next != chart)
            {
                chart = next;
                SessionState.SetString(SessionKey, chart != null ? AssetDatabase.GetAssetPath(chart) : "");
            }
        }

        void DrawSong()
        {
            songOpen = EditorGUILayout.Foldout(songOpen, "곡", true);
            if (!songOpen) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUI.BeginChangeCheck();

            float bpm = EditorGUILayout.FloatField("BPM", chart.bpm);
            double offset = EditorGUILayout.DoubleField("오프셋 (초)", chart.offsetSeconds);
            double approach = EditorGUILayout.DoubleField("기본 접근 박", chart.defaultApproachBeats);
            float spawnAngle = EditorGUILayout.FloatField("기본 스폰 각", chart.defaultSpawnAngleDeg);
            float hitAngle = EditorGUILayout.FloatField("기본 히트 각", chart.defaultHitAngleDeg);
            var clip = (AudioClip)EditorGUILayout.ObjectField("오디오 클립", chart.clip, typeof(AudioClip), false);
            var playback = (PlaybackMode)EditorGUILayout.EnumPopup("재생", chart.playback);
            float goodRatio = EditorGUILayout.Slider("자동 Good 비율", chart.autoGoodRatio, 0f, 1f);
            int seed = EditorGUILayout.IntField("시드", chart.autoSeed);

            JudgementWindows j = chart.judgement;
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("판정 창", EditorStyles.boldLabel);
            j.perfectMs = EditorGUILayout.DoubleField("Perfect (ms)", j.perfectMs);
            j.goodMs = EditorGUILayout.DoubleField("Good (ms)", j.goodMs);
            j.badMs = EditorGUILayout.DoubleField("Bad (ms)", j.badMs);
            j.perfectPenalty = EditorGUILayout.FloatField("Perfect 감점", j.perfectPenalty);
            j.goodPenalty = EditorGUILayout.FloatField("Good 감점", j.goodPenalty);
            j.badPenalty = EditorGUILayout.FloatField("Bad 감점", j.badPenalty);
            j.missPenalty = EditorGUILayout.FloatField("Miss 감점", j.missPenalty);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(chart, "곡 설정");
                chart.bpm = bpm;
                chart.offsetSeconds = offset;
                chart.defaultApproachBeats = approach;
                chart.defaultSpawnAngleDeg = spawnAngle;
                chart.defaultHitAngleDeg = hitAngle;
                chart.clip = clip;
                chart.playback = playback;
                chart.autoGoodRatio = goodRatio;
                chart.autoSeed = seed;
                chart.judgement = j;
                EditorUtility.SetDirty(chart);
            }

            EditorGUILayout.EndVertical();
        }

        void DrawPhases()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("페이즈", EditorStyles.boldLabel);

            List<PhaseWindow> windows = ChartLayout.Build(chart);
            int phaseCount = chart.phases != null ? chart.phases.Count : 0;

            for (int i = 0; i < phaseCount; i++)
            {
                bool hasWindow = i < windows.Count;
                DrawPhase(i, hasWindow ? windows[i] : default, hasWindow);
            }

            if (GUILayout.Button("페이즈 추가"))
            {
                Commit("페이즈 추가", () =>
                {
                    if (chart.phases == null)
                        chart.phases = new List<PhaseDefinition>();
                    chart.phases.Add(NewPhase());
                });
            }
        }

        void DrawPhase(int index, PhaseWindow window, bool hasWindow)
        {
            PhaseDefinition phase = chart.phases[index];
            if (phase == null)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"페이즈 {index} (비어 있음)");
                if (GUILayout.Button("비어 있는 페이즈 지우기"))
                {
                    int remove = index;
                    Commit("페이즈 삭제", () => chart.phases.RemoveAt(remove));
                }
                EditorGUILayout.EndVertical();
                return;
            }

            if (!phaseOpen.TryGetValue(index, out bool open))
                open = true;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            string title = string.IsNullOrEmpty(phase.phaseName) ? "(이름 없음)" : phase.phaseName;
            phaseOpen[index] = EditorGUILayout.Foldout(open, $"페이즈 {index}  {title}", true);
            EditorGUI.BeginDisabledGroup(index == 0);
            bool moveUp = GUILayout.Button("위로", GUILayout.Width(48f));
            EditorGUI.EndDisabledGroup();
            EditorGUI.BeginDisabledGroup(index >= chart.phases.Count - 1);
            bool moveDown = GUILayout.Button("아래로", GUILayout.Width(56f));
            EditorGUI.EndDisabledGroup();
            bool removePhase = GUILayout.Button("삭제", GUILayout.Width(48f));
            EditorGUILayout.EndHorizontal();

            if (moveUp)
            {
                int from = index;
                Commit("페이즈 순서", () => Swap(chart.phases, from, from - 1));
            }
            if (moveDown)
            {
                int from = index;
                Commit("페이즈 순서", () => Swap(chart.phases, from, from + 1));
            }
            if (removePhase)
            {
                int remove = index;
                Commit("페이즈 삭제", () => chart.phases.RemoveAt(remove));
            }

            if (hasWindow)
                DrawWindow(window);

            if (!phaseOpen[index])
            {
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUI.BeginChangeCheck();
            string phaseName = EditorGUILayout.TextField("이름", phase.phaseName);
            var backdrop = (Backdrop)EditorGUILayout.EnumPopup("배경", phase.backdrop);
            var pizza = (PizzaState)EditorGUILayout.EnumPopup("피자", phase.pizza);
            bool tint = EditorGUILayout.Toggle("원판 틴트 덮어쓰기", phase.overrideWheelTint);
            Color tintColor = EditorGUILayout.ColorField("틴트", phase.wheelTint);
            double hold = EditorGUILayout.DoubleField("유지 박", phase.holdBeats);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(chart, "페이즈");
                phase.phaseName = phaseName;
                phase.backdrop = backdrop;
                phase.pizza = pizza;
                phase.overrideWheelTint = tint;
                phase.wheelTint = tintColor;
                phase.holdBeats = hold;
                EditorUtility.SetDirty(chart);
            }

            DrawRows(phase);
            EditorGUILayout.EndVertical();
        }

        void DrawRows(PhaseDefinition phase)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("노트 행", EditorStyles.boldLabel);

            IReadOnlyList<NotePreset> presets = NotePresetLibrary.All;
            if (presets.Count > 0)
            {
                if (presetIndex >= presets.Count) presetIndex = 0;
                var names = new string[presets.Count];
                for (int i = 0; i < presets.Count; i++)
                    names[i] = presets[i].Name;

                presetIndex = EditorGUILayout.Popup("프리셋", presetIndex, names);
                presetHitBeat = EditorGUILayout.DoubleField("프리셋 히트 박", presetHitBeat);
                if (GUILayout.Button("프리셋으로 행 추가"))
                {
                    int chosen = presetIndex;
                    double beat = presetHitBeat;
                    Commit("프리셋으로 행 추가", () =>
                    {
                        if (phase.rows == null) phase.rows = new List<NoteRow>();
                        phase.rows.Add(NotePresetLibrary.All[chosen].Create(chart, beat));
                    });
                }
            }

            int rowCount = phase.rows != null ? phase.rows.Count : 0;
            for (int i = 0; i < rowCount; i++)
                DrawRow(phase, i);

            if (GUILayout.Button("빈 행 추가"))
            {
                Commit("행 추가", () =>
                {
                    if (phase.rows == null) phase.rows = new List<NoteRow>();
                    phase.rows.Add(new NoteRow());
                });
            }
        }

        void DrawRow(PhaseDefinition phase, int index)
        {
            NoteRow row = phase.rows[index];
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (row == null)
            {
                EditorGUILayout.LabelField($"행 {index} (비어 있음)");
                if (GUILayout.Button("행 삭제"))
                {
                    int remove = index;
                    Commit("행 삭제", () => phase.rows.RemoveAt(remove));
                }
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"행 {index}", EditorStyles.boldLabel, GUILayout.Width(48f));
            EditorGUI.BeginDisabledGroup(index == 0);
            bool moveUp = GUILayout.Button("위로", GUILayout.Width(48f));
            EditorGUI.EndDisabledGroup();
            EditorGUI.BeginDisabledGroup(index >= phase.rows.Count - 1);
            bool moveDown = GUILayout.Button("아래로", GUILayout.Width(56f));
            EditorGUI.EndDisabledGroup();
            bool removeRow = GUILayout.Button("삭제", GUILayout.Width(48f));
            EditorGUILayout.EndHorizontal();

            if (moveUp)
            {
                int from = index;
                Commit("행 순서", () => Swap(phase.rows, from, from - 1));
            }
            if (moveDown)
            {
                int from = index;
                Commit("행 순서", () => Swap(phase.rows, from, from + 1));
            }
            if (removeRow)
            {
                int remove = index;
                Commit("행 삭제", () => phase.rows.RemoveAt(remove));
            }

            if (NotePresetLibrary.All.Count > 0 && GUILayout.Button("이 프리셋으로 다시 채우기"))
            {
                int chosen = Mathf.Clamp(presetIndex, 0, NotePresetLibrary.All.Count - 1);
                double beat = row.hitBeats != null && row.hitBeats.Count > 0 ? row.hitBeats[0] : presetHitBeat;
                int replace = index;
                Commit("프리셋으로 채우기", () =>
                {
                    phase.rows[replace] = NotePresetLibrary.All[chosen].Create(chart, beat);
                });
            }

            EditorGUI.BeginChangeCheck();
            string label = EditorGUILayout.TextField("이름", row.label);
            var kind = (NoteKind)EditorGUILayout.EnumPopup("종류", row.kind);
            var onMiss = (MissBehaviour)EditorGUILayout.EnumPopup("놓치면", row.onMiss);
            double tail = EditorGUILayout.DoubleField("꼬리 박", row.tailBeats);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("아이콘", row.icon, typeof(Sprite), false);
            EditorGUI.EndDisabledGroup();
            Color color = EditorGUILayout.ColorField("색", row.color);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(chart, "노트 행");
                row.label = label;
                row.kind = kind;
                row.onMiss = onMiss;
                row.tailBeats = tail;
                row.color = color;
                EditorUtility.SetDirty(chart);
            }

            DrawHitBeats(row);
            DrawPath(row);
            EditorGUILayout.EndVertical();
        }

        void DrawHitBeats(NoteRow row)
        {
            EditorGUILayout.LabelField("히트 박");
            int count = row.hitBeats != null ? row.hitBeats.Count : 0;

            for (int i = 0; i < count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                double beat = EditorGUILayout.DoubleField(row.hitBeats[i]);
                bool changed = EditorGUI.EndChangeCheck();
                bool remove = GUILayout.Button("-", GUILayout.Width(24f));
                EditorGUILayout.EndHorizontal();

                if (changed)
                {
                    int at = i;
                    Undo.RecordObject(chart, "히트 박");
                    row.hitBeats[at] = beat;
                    EditorUtility.SetDirty(chart);
                }
                if (remove)
                {
                    int at = i;
                    Commit("히트 박 삭제", () => row.hitBeats.RemoveAt(at));
                }
            }

            if (GUILayout.Button("히트 박 추가"))
            {
                Commit("히트 박 추가", () =>
                {
                    if (row.hitBeats == null) row.hitBeats = new List<double>();
                    double next = row.hitBeats.Count == 0 ? 0.0 : row.hitBeats[row.hitBeats.Count - 1] + 1.0;
                    row.hitBeats.Add(next);
                });
            }
        }

        void DrawPath(NoteRow row)
        {
            EditorGUILayout.LabelField("경로 키 (시각, 각도)");
            int count = row.path != null ? row.path.Count : 0;

            for (int i = 0; i < count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                double beat = EditorGUILayout.DoubleField(row.path[i].beat);
                float angle = EditorGUILayout.FloatField(row.path[i].angleDeg);
                bool changed = EditorGUI.EndChangeCheck();
                bool remove = GUILayout.Button("-", GUILayout.Width(24f));
                EditorGUILayout.EndHorizontal();

                if (changed)
                {
                    int at = i;
                    Undo.RecordObject(chart, "경로 키");
                    row.path[at] = new MotionKey(beat, angle);
                    EditorUtility.SetDirty(chart);
                }
                if (remove)
                {
                    int at = i;
                    Commit("경로 키 삭제", () => row.path.RemoveAt(at));
                }
            }

            if (GUILayout.Button("경로 키 추가"))
            {
                Commit("경로 키 추가", () =>
                {
                    if (row.path == null) row.path = new List<MotionKey>();
                    double nextBeat = row.path.Count == 0 ? 0.0 : row.path[row.path.Count - 1].beat + 1.0;
                    float angle = chart != null ? chart.defaultHitAngleDeg : 270f;
                    row.path.Add(new MotionKey(nextBeat, angle));
                });
            }
        }

        static void DrawWindow(PhaseWindow window)
        {
            EditorGUILayout.SelectableLabel(
                $"절대 박    원점 {window.OriginBeat:0.000}    첫 스폰 {window.EarliestSpawnBeat:0.000}    비워지는 박 {window.ClearedBeat:0.000}",
                EditorStyles.textField,
                GUILayout.Height(EditorGUIUtility.singleLineHeight));
        }

        void DrawValidation()
        {
            EditorGUILayout.Space(4f);
            List<string> problems = ChartLayout.Validate(chart);
            EditorGUILayout.LabelField($"검증 ({problems.Count})", EditorStyles.boldLabel);
            validationScroll = EditorGUILayout.BeginScrollView(validationScroll, GUILayout.MinHeight(72f), GUILayout.MaxHeight(180f));
            if (problems.Count == 0)
                EditorGUILayout.HelpBox("문제 없음.", MessageType.Info);
            else
            {
                for (int i = 0; i < problems.Count; i++)
                {
                    MessageType type = problems[i].StartsWith("경고") ? MessageType.Warning : MessageType.Error;
                    EditorGUILayout.HelpBox(problems[i], type);
                }
            }
            EditorGUILayout.EndScrollView();
        }

        void Commit(string undo, Action mutate)
        {
            Undo.RecordObject(chart, undo);
            mutate();
            EditorUtility.SetDirty(chart);
            GUIUtility.ExitGUI();
        }

        static PhaseDefinition NewPhase()
        {
            return new PhaseDefinition
            {
                rows = new List<NoteRow>(),
                backdrop = Backdrop.Bright,
                pizza = PizzaState.None,
                wheelTint = Color.white,
            };
        }

        static void Swap<T>(List<T> list, int a, int b)
        {
            T item = list[a];
            list[a] = list[b];
            list[b] = item;
        }
    }
}
