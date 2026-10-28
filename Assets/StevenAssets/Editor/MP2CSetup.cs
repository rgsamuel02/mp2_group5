using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Steven.MP2A.EditorTools
{
    /// <summary>
    /// Tools > Steven > Setup MP2C Learning Curve  (open StevenScene first; the MP2B setup must already be in it)
    ///
    /// Adds the Kitchen Simplifier and rewires the Progression goals into the MP2C learning curve:
    /// start (charred veg + produce box) -> fry station -> steak & herbs -> equipment catalog -> brigade ->
    /// teammate deliveries -> finale. Safe to run again. Delete this script afterwards if you like.
    /// </summary>
    public static class MP2CSetup
    {
        static readonly List<string> s_Log = new List<string>();

        [MenuItem("Tools/Steven/Setup MP2C Learning Curve")]
        public static void Setup()
        {
            var scene = SceneManager.GetActiveScene();
            var progression = FindFirst<KitchenProgression>(scene);
            if (progression == null)
            {
                EditorUtility.DisplayDialog("MP2C Setup", "No Kitchen Progression found. Open StevenScene (with the MP2B setup in it) first.", "OK");
                return;
            }
            s_Log.Clear();

            // Keep the teammate-delivery objects from the MP2B goals before replacing the goals.
            var partnerObjects = new List<GameObject>();
            foreach (var g in progression.goals)
                if (g.name == "Partner Deliveries")
                    partnerObjects.AddRange(g.unlocks);
            partnerObjects.RemoveAll(o => o == null);

            progression.LoadLearningCurveDefaults();

            var simplifier = progression.GetComponent<KitchenSimplifier>();
            if (simplifier == null)
                simplifier = progression.gameObject.AddComponent<KitchenSimplifier>();
            Log("Kitchen Simplifier on " + progression.name);
            var save = progression.GetComponent<KitchenSave>();
            if (save == null)
                save = progression.gameObject.AddComponent<KitchenSave>();
            Log("Kitchen Save on " + progression.name);

            var stations = FindAll<CraftingStation>(scene);

            // Goal 1: fry station (cut potatoes, then fry).
            UnlockRecipe(progression, 0, stations, "cutfries");
            UnlockRecipe(progression, 0, stations, "fries");
            // Goal 2: butcher box + steak, and the shears to harvest herbs for it.
            UnlockRecipe(progression, 1, stations, "buy_butcher");
            UnlockRecipe(progression, 1, stations, "steak");
            Unlock(progression, 1, Find(scene, "KitchenShears"));
            // Goal 3: the equipment catalog.
            Unlock(progression, 2, Find(scene, "Equipment Catalog"));
            // Goal 4: the brigade (staff board + CALL A COOK on the stations in use).
            Unlock(progression, 3, Find(scene, "Staff Board"));
            UnlockRecipe(progression, 3, stations, "hire_cook");
            var used = new HashSet<string> { "charveg", "cutfries", "fries", "steak" };
            foreach (var st in stations)
            {
                if (st.callCookCard == null)
                    continue;
                foreach (var id in st.recipeIds)
                {
                    if (used.Contains(id))
                    {
                        Unlock(progression, 3, st.callCookCard.gameObject);
                        break;
                    }
                }
            }
            // Goal 5: teammate deliveries (the MP2B interconnections).
            foreach (var o in partnerObjects)
                Unlock(progression, 4, o);
            if (partnerObjects.Count == 0)
                Log("WARNING: no teammate-delivery objects found (run the MP2B setup first, or add Partner Props to goal 5 by hand)");

            EditorUtility.SetDirty(progression);
            EditorUtility.SetDirty(simplifier);
            EditorUtility.SetDirty(save);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = progression.gameObject;
            var report = string.Join("\n", s_Log);
            Debug.Log("[MP2C] Setup complete:\n" + report);
            EditorUtility.DisplayDialog("MP2C Setup", "Done. Scene saved.\n\n" + report, "OK");
        }

        static void UnlockRecipe(KitchenProgression p, int goal, List<CraftingStation> stations, string recipeId)
        {
            var found = false;
            foreach (var st in stations)
            {
                for (var i = 0; i < st.recipeIds.Count && i < st.recipeCards.Count; i++)
                {
                    if (st.recipeIds[i] == recipeId && st.recipeCards[i] != null)
                    {
                        Unlock(p, goal, st.recipeCards[i].gameObject, st.name + " / " + recipeId);
                        found = true;
                    }
                }
            }
            if (!found)
                Log("WARNING: no ticket for recipe '" + recipeId + "'");
        }

        static void Unlock(KitchenProgression p, int goal, Transform t)
        {
            if (t == null)
            {
                Log("WARNING: object for goal " + (goal + 1) + " not found");
                return;
            }
            Unlock(p, goal, t.gameObject);
        }

        static void Unlock(KitchenProgression p, int goal, GameObject go, string label = null)
        {
            if (go == null || goal >= p.goals.Count || p.goals[goal].unlocks.Contains(go))
                return;
            p.goals[goal].unlocks.Add(go);
            Log("Goal " + (goal + 1) + " (" + p.goals[goal].name + ") unlocks " + (label ?? go.name));
        }

        static T FindFirst<T>(Scene scene) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var c = root.GetComponentInChildren<T>(true);
                if (c != null)
                    return c;
            }
            return null;
        }

        static List<T> FindAll<T>(Scene scene) where T : Component
        {
            var list = new List<T>();
            foreach (var root in scene.GetRootGameObjects())
                list.AddRange(root.GetComponentsInChildren<T>(true));
            return list;
        }

        static Transform Find(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == name)
                        return t;
            return null;
        }

        static void Log(string s) => s_Log.Add(s);
    }
}
