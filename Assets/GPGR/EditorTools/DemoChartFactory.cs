using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GPGR.Charting;

namespace GPGR.EditorTools
{
    public static class DemoChartFactory
    {
        public const string AssetPath = "Assets/GPGR/Charts/DemoChart.asset";

        readonly struct PhaseRecipe
        {
            public readonly string PhaseName;
            public readonly double[] HitBeats;
            public readonly Backdrop Backdrop;
            public readonly PizzaState Pizza;
            public readonly double HoldBeats;

            public PhaseRecipe(string phaseName, double[] hitBeats, Backdrop backdrop, PizzaState pizza, double holdBeats)
            {
                PhaseName = phaseName;
                HitBeats = hitBeats;
                Backdrop = backdrop;
                Pizza = pizza;
                HoldBeats = holdBeats;
            }
        }

        // ??? ??6 ???? ?. ???? ????? NotePresetLibrary ??? ?????? ????.
        // ?????? ??????? ????? ????.
        static readonly PhaseRecipe[] Recipes =
        {
            new PhaseRecipe("????", new[] { 2.0 }, Backdrop.Bright, PizzaState.None, 0.0),
            new PhaseRecipe("???? ???", new[] { 2.0, 4.0 }, Backdrop.Bright, PizzaState.None, 0.0),
            new PhaseRecipe("??? ?????", new[] { 2.0 }, Backdrop.Bright, PizzaState.None, 0.0),
            new PhaseRecipe("??????", new[] { 1.0, 2.0, 3.0, 4.0 }, Backdrop.Bright, PizzaState.None, 0.0),
            new PhaseRecipe("????", new[] { 2.0 }, Backdrop.Dark, PizzaState.IntoOven, 0.0),
            new PhaseRecipe("???", System.Array.Empty<double>(), Backdrop.Bright, PizzaState.OutOfOven, 8.0),
        };

        [MenuItem("GPGR/Create Demo Chart Asset")]
        public static void Create()
        {
            bool exists = AssetDatabase.LoadAssetAtPath<Object>(AssetPath) != null;
            if (exists && !EditorUtility.DisplayDialog(
                    "???? ???",
                    "Assets/GPGR/Charts/DemoChart.asset ?? ??? ??????. ????????",
                    "??????",
                    "???"))
                return;

            SongChart chart = BuildDemoChart();
            List<string> problems = ChartLayout.Validate(chart);
            string shape = ShapeError(chart);
            if (!string.IsNullOrEmpty(shape))
                problems.Insert(0, "????: " + shape);

            LogProblems(chart, problems);

            if (HasError(problems))
            {
                Object.DestroyImmediate(chart);
                Debug.LogError("[GPGR] ?????? ??? ???? ????? ???????? ??????.");
                return;
            }

            var existing = AssetDatabase.LoadAssetAtPath<SongChart>(AssetPath);
            if (existing != null)
            {
                EditorUtility.CopySerialized(chart, existing);
                existing.name = "DemoChart";
                EditorUtility.SetDirty(existing);
                Object.DestroyImmediate(chart);
            }
            else
            {
                if (exists)
                    AssetDatabase.DeleteAsset(AssetPath);
                EnsureFolder();
                AssetDatabase.CreateAsset(chart, AssetPath);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[GPGR] ???? ????? ??????????: " + AssetPath);
        }

        public static SongChart BuildDemoChart()
        {
            var chart = ScriptableObject.CreateInstance<SongChart>();
            chart.name = "DemoChart";
            chart.bpm = 120f;
            chart.offsetSeconds = 0.0;
            chart.defaultApproachBeats = 2.0;
            chart.defaultSpawnAngleDeg = 0f;
            chart.defaultHitAngleDeg = 270f;
            chart.clip = null;
            chart.judgement = JudgementWindows.Default;
            chart.playback = PlaybackMode.AutoPlay;
            chart.autoGoodRatio = 0.25f;
            chart.autoSeed = 12345;
            chart.phases = new List<PhaseDefinition>(Recipes.Length);

            IReadOnlyList<NotePreset> presets = NotePresetLibrary.All;
            int notePhases = 0;
            for (int i = 0; i < Recipes.Length; i++)
                if (Recipes[i].HitBeats.Length > 0) notePhases++;

            if (presets.Count < notePhases)
            {
                Debug.LogError("[GPGR] ??? ???????? ???????? ???????.");
                return chart;
            }

            int presetIndex = 0;
            for (int i = 0; i < Recipes.Length; i++)
            {
                PhaseRecipe recipe = Recipes[i];
                var phase = new PhaseDefinition
                {
                    phaseName = recipe.PhaseName,
                    backdrop = recipe.Backdrop,
                    pizza = recipe.Pizza,
                    overrideWheelTint = false,
                    wheelTint = Color.white,
                    holdBeats = recipe.HoldBeats,
                    rows = new List<NoteRow>(recipe.HitBeats.Length),
                };

                if (recipe.HitBeats.Length > 0)
                {
                    NotePreset preset = presets[presetIndex++];
                    for (int h = 0; h < recipe.HitBeats.Length; h++)
                        phase.rows.Add(preset.Create(chart, recipe.HitBeats[h]));
                }

                chart.phases.Add(phase);
            }

            return chart;
        }

        static string ShapeError(SongChart chart)
        {
            if (chart?.phases == null || chart.phases.Count != 6)
                return "?????? 6???? ????.";

            int[] rowCounts = { 1, 2, 1, 4, 1, 0 };
            for (int i = 0; i < rowCounts.Length; i++)
            {
                PhaseDefinition phase = chart.phases[i];
                if (phase == null || phase.rows == null || phase.rows.Count != rowCounts[i])
                    return $"?????? {i}?? ?? ???? {rowCounts[i]}?? ????.";
                if (phase.overrideWheelTint)
                    return $"?????? {i}?? ???? ???? ??? ???? ???.";
            }

            NoteRow dough = chart.phases[0].rows[0];
            if (dough.kind != NoteKind.Long || dough.tailBeats != 2.0 || dough.hitBeats.Count != 1)
                return "?????? 0?? ???? 2?? ???? ??????? ???.";

            NoteRow cheese = chart.phases[2].rows[0];
            if (cheese.kind != NoteKind.Single || cheese.hitBeats.Count != 2 || cheese.path == null || cheese.path.Count != 4)
                return "?????? 2?? ??? 2??, ??? ? 4?????? ???.";
            if (System.Math.Abs(cheese.hitBeats[1] - cheese.hitBeats[0] - 2.0) > 1e-6)
                return "?????? 2?? ?? ??? ????? ? ??? + 2???? ????.";
            float bounce = NotePresetLibrary.BounceLeft(chart.defaultHitAngleDeg);
            if (System.Math.Abs(cheese.path[2].angleDeg - bounce) > 0.01f)
                return "?????? 2?? ??? ?????? ??? ?? - 60??? ????.";

            for (int r = 0; r < chart.phases[3].rows.Count; r++)
            {
                NoteRow row = chart.phases[3].rows[r];
                if (row.hitBeats == null || row.hitBeats.Count != 1 || row.path == null || row.path.Count < 2)
                    return "?????? 3 ???? ??? ??? ???? ????.";
                if (System.Math.Abs(row.hitBeats[0] - row.path[0].beat - 1.0) > 1e-6)
                    return "?????? 3?? ???? ???? 1?? ????.";
            }

            NoteRow oven = chart.phases[4].rows[0];
            if (oven.kind != NoteKind.Long || oven.tailBeats != 4.0)
                return "?????? 4?? ???? 4?? ???????? ???.";
            if (chart.phases[4].backdrop != Backdrop.Dark || chart.phases[4].pizza != PizzaState.IntoOven)
                return "?????? 4?? ???? ??? ???????? ????? ???.";

            PhaseDefinition done = chart.phases[5];
            if (done.holdBeats != 8.0 || done.backdrop != Backdrop.Bright || done.pizza != PizzaState.OutOfOven)
                return "?????? 5?? ???? 8??, ???? ???, ?????? ???????? ???.";

            return null;
        }

        static void LogProblems(SongChart chart, List<string> problems)
        {
            if (problems.Count == 0)
                Debug.Log("[GPGR] ???? ??? ?????? ?????????.");
            else
            {
                for (int i = 0; i < problems.Count; i++)
                {
                    if (problems[i].StartsWith("???"))
                        Debug.LogWarning(problems[i]);
                    else
                        Debug.LogError(problems[i]);
                }
            }

            List<PhaseWindow> windows = ChartLayout.Build(chart);
            for (int i = 0; i < windows.Count && i < chart.phases.Count; i++)
            {
                PhaseWindow w = windows[i];
                string name = chart.phases[i] != null ? chart.phases[i].phaseName : "";
                Debug.Log($"[GPGR] ?????? {i} {name}: ???? {w.OriginBeat:0.000}, ? ???? {w.EarliestSpawnBeat:0.000}, ??????? ?? {w.ClearedBeat:0.000}");
            }
        }

        static bool HasError(List<string> problems)
        {
            for (int i = 0; i < problems.Count; i++)
            {
                if (!problems[i].StartsWith("???"))
                    return true;
            }
            return false;
        }

        static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/GPGR/Charts"))
                AssetDatabase.CreateFolder("Assets/GPGR", "Charts");
        }
    }
}
