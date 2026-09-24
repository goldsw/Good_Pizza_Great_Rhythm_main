using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GPGR.Charting;

namespace GPGR.EditorTools
{
    /// <summary>
    /// 노트 행 템플릿. 에디터 전용이고 런타임 에셋에 저장하지 않는다.
    /// 재료를 더할 때는 이 목록에 프리셋 하나를 등록한다.
    /// </summary>
    public sealed class NotePreset
    {
        public string Name { get; }
        public string SpritePath { get; }
        public Func<SongChart, double, NoteRow> Create { get; }

        public NotePreset(string name, string spritePath, Func<SongChart, double, NoteRow> create)
        {
            Name = name;
            SpritePath = spritePath;
            Create = create;
        }
    }

    public static class NotePresetLibrary
    {
        // 계약의 잠정값. 프리셋이 행을 만들 때만 쓰고, 만들어진 값은 행에 저장된다.
        const double DoughTailBeats = 2.0;
        const double OvenTailBeats = 4.0;
        const double PepperoniApproachBeats = 1.0;
        const float CheeseBounceDegrees = 90f;
        const double CheeseBounceBeats = 1.0;
        const double CheeseReturnBeats = 2.0;

        static readonly List<NotePreset> presets = new List<NotePreset>();

        /// <summary>
        /// 등록 순서가 데모 채보의 노트 페이즈 순서다. 중간에 끼워 넣지 않는다.
        /// </summary>
        static NotePresetLibrary()
        {
            presets.Add(new NotePreset("반죽", "Assets/GPGR/Art/Notes/Dough.png", (chart, hit) =>
                Row("반죽", NoteKind.Long, Hex("#E8D4A8"), DoughTailBeats,
                    new[] { hit },
                    Arc(chart, hit, Approach(chart)),
                    "Assets/GPGR/Art/Notes/Dough.png")));

            presets.Add(new NotePreset("토마토 소스", "Assets/GPGR/Art/Notes/Sauce.png", (chart, hit) =>
                Row("토마토 소스", NoteKind.Single, Hex("#C0392B"), 0.0,
                    new[] { hit },
                    Arc(chart, hit, Approach(chart)),
                    "Assets/GPGR/Art/Notes/Sauce.png")));

            presets.Add(new NotePreset("치즈 뿌리기", "Assets/GPGR/Art/Notes/Cheese.png", (chart, hit) =>
            {
                float hitAngle = chart != null ? chart.defaultHitAngleDeg : 270f;
                List<MotionKey> keys = Arc(chart, hit, Approach(chart));
                keys.Add(new MotionKey(hit + CheeseBounceBeats, BounceLeft(hitAngle)));
                keys.Add(new MotionKey(hit + CheeseReturnBeats, hitAngle));
                return Row("치즈 뿌리기", NoteKind.Single, Hex("#F1C40F"), 0.0,
                    new[] { hit, hit + CheeseReturnBeats }, keys,
                    "Assets/GPGR/Art/Notes/Cheese.png");
            }));

            presets.Add(new NotePreset("페퍼로니", "Assets/GPGR/Art/Notes/Pepperoni.png", (chart, hit) =>
                Row("페퍼로니", NoteKind.Single, Hex("#8E2B20"), 0.0,
                    new[] { hit },
                    Arc(chart, hit, PepperoniApproachBeats),
                    "Assets/GPGR/Art/Notes/Pepperoni.png")));

            presets.Add(new NotePreset("오븐", null, (chart, hit) =>
                Row("오븐", NoteKind.Long, Hex("#E67E22"), OvenTailBeats,
                    new[] { hit },
                    Arc(chart, hit, Approach(chart)),
                    null)));
        }

        public static IReadOnlyList<NotePreset> All => presets;

        /// <summary>
        /// 확정된 재료 그림을 행에 넣는다. 페이즈 이름 또는 행 이름이 프리셋 이름과 같을 때만.
        /// 경로와 히트 박은 건드리지 않는다. 런타임은 저장된 icon만 읽는다.
        /// </summary>
        public static int ApplyIcons(SongChart chart)
        {
            if (chart?.phases == null) return 0;

            int changed = 0;
            for (int i = 0; i < chart.phases.Count; i++)
            {
                PhaseDefinition phase = chart.phases[i];
                if (phase?.rows == null) continue;

                for (int r = 0; r < phase.rows.Count; r++)
                {
                    NoteRow row = phase.rows[r];
                    if (row == null) continue;

                    NotePreset preset = Find(phase.phaseName) ?? Find(row.label);
                    if (preset == null) continue;

                    if (string.IsNullOrEmpty(preset.SpritePath))
                    {
                        if (row.icon != null)
                        {
                            row.icon = null;
                            changed++;
                        }
                        continue;
                    }

                    Sprite sprite = LoadSprite(preset.SpritePath);
                    if (sprite == null)
                    {
                        Debug.LogError("[GPGR] 스프라이트가 없다: " + preset.SpritePath);
                        continue;
                    }

                    if (row.icon == sprite) continue;
                    row.icon = sprite;
                    changed++;
                }
            }

            return changed;
        }

        [MenuItem("GPGR/Apply Ingredient Icons")]
        public static void ApplyIconsToDemoChart()
        {
            const string path = "Assets/GPGR/Charts/DemoChart.asset";
            var chart = AssetDatabase.LoadAssetAtPath<SongChart>(path);
            if (chart == null)
            {
                Debug.LogError("[GPGR] DemoChart가 없다.");
                return;
            }

            Undo.RecordObject(chart, "Apply Ingredient Icons");
            chart.clip = null;
            int changed = ApplyIcons(chart);
            EditorUtility.SetDirty(chart);
            AssetDatabase.SaveAssets();

            var problems = ChartLayout.Validate(chart);
            int errors = 0;
            for (int i = 0; i < problems.Count; i++)
            {
                if (!problems[i].StartsWith("경고")) errors++;
            }

            Debug.Log("[GPGR] DemoChart.clip은 비웠다. BPM " + chart.bpm);
            if (chart.phases != null)
            {
                for (int i = 0; i < chart.phases.Count; i++)
                {
                    PhaseDefinition phase = chart.phases[i];
                    int rows = phase?.rows?.Count ?? 0;
                    int withIcon = 0;
                    if (phase?.rows != null)
                    {
                        for (int r = 0; r < phase.rows.Count; r++)
                            if (phase.rows[r]?.icon != null) withIcon++;
                    }
                    Debug.Log($"[GPGR] 페이즈 {i} \"{phase?.phaseName}\" 행 {rows}개, 아이콘 {withIcon}개");
                }
            }
            Debug.Log("[GPGR] 아이콘을 바꾼 행 " + changed + ". 검증 오류 " + errors + "개.");
        }

        static NotePreset Find(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            for (int i = 0; i < presets.Count; i++)
            {
                if (presets[i].Name == name) return presets[i];
            }
            return null;
        }

        static double Approach(SongChart chart)
        {
            return chart != null ? chart.defaultApproachBeats : 2.0;
        }

        /// <summary>
        /// 히트 각에서 왼쪽으로 90°. 런타임 보간은 이 값을 쓰지 않고, 행을 만들 때만 쓴다.
        /// </summary>
        public static float BounceLeft(float hitAngleDeg)
        {
            return hitAngleDeg - CheeseBounceDegrees;
        }

        static List<MotionKey> Arc(SongChart chart, double hitBeat, double approachBeats)
        {
            float spawn = chart != null ? chart.defaultSpawnAngleDeg : 180f;
            float hit = chart != null ? chart.defaultHitAngleDeg : 270f;
            return new List<MotionKey>
            {
                new MotionKey(hitBeat - approachBeats, spawn),
                new MotionKey(hitBeat, hit),
            };
        }

        static NoteRow Row(string label, NoteKind kind, Color color, double tailBeats, double[] hits, List<MotionKey> path, string spritePath)
        {
            return new NoteRow
            {
                label = label,
                kind = kind,
                color = color,
                tailBeats = tailBeats,
                onMiss = MissBehaviour.DespawnImmediately,
                hitBeats = new List<double>(hits),
                path = path,
                icon = LoadSprite(spritePath),
            };
        }

        static Sprite LoadSprite(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static Color Hex(string html)
        {
            return ColorUtility.TryParseHtmlString(html, out Color color) ? color : Color.white;
        }
    }
}
