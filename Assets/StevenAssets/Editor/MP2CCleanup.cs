using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Steven.MP2A.EditorTools
{
    /// <summary>
    /// Tools > Steven > Polish > Clean Up Scene  (open StevenScene first)
    /// Plain wording for every tip card, goal card and sign, and removes leftovers from the full MP2A kitchen:
    /// equipment with nothing to cook on it, the demo shortcut panel (F-keys still work in the Editor), tips for
    /// removed systems and unused scripts. Undo with git (discard StevenScene) if needed.
    /// </summary>
    public static class MP2CCleanup
    {
        static readonly BuildingId[] s_Unused =
        {
            BuildingId.RangeStove, BuildingId.SautePan, BuildingId.Stockpot, BuildingId.ConvectionOven,
            BuildingId.ReachInFridge, BuildingId.IceMachine, BuildingId.PowerOutlet,
        };

        // Tips for systems the simplified kitchen doesn't have: never shown.
        static readonly string[] s_DeadTips =
        {
            "temper", "hygiene", "rat", "mousetrap", "grease_fire", "extinguisher", "spray", "fridge", "ice", "cookware",
            "mallet", "wrench", "rolling_pin", "office", "mushrooms", "watering_can", "neighbor",
        };

        static readonly Dictionary<string, string[]> s_Tips = new Dictionary<string, string[]>
        {
            { "welcome", new[] { "HEAD CHEF", "Cook what the tables order and get it to them before they leave. Press YES, CHEF! to close a tip." } },
            { "catalog", new[] { "EQUIPMENT CATALOG", "Pick up a miniature and hold it over the floor to see where it goes. Let go to buy and place it. Pull the trigger while holding it to place more than one." } },
            { "grid_snap", new[] { "FLOOR GRID", "Equipment snaps to the floor grid. Turn the miniature to turn the piece; the arrow shows the front." } },
            { "foundation", new[] { "HOOKUPS", "Some equipment needs something under it: tiles under everything, a gas line under grills and fryers, a counter under cutting boards, a table under place settings." } },
            { "pantry_board", new[] { "PANTRY LEDGER", "Everything you have: gold, reputation, cooks, ingredients and cooked dishes. A line flashes green when it goes up and red when it goes down." } },
            { "crafting", new[] { "RECIPE TICKETS", "The tickets above a station are its recipes. Green means you have the ingredients. Press one to start cooking; the dish goes to the pantry when it's done." } },
            { "heat", new[] { "HEAT", "A lit grill or fryer takes a few seconds to heat up. Some recipes won't start until it's hot enough." } },
            { "customer", new[] { "GUESTS", "Guests walk along the carpet runners to a free table with a place setting." } },
            { "table", new[] { "NEW ORDER", "This table has ordered and is waiting. Its ticket is now on the rail by the pass." } },
            { "order_rail", new[] { "ORDER TICKETS", "Each table's order hangs here. Once the dish is cooked, press the ticket to plate it." } },
            { "conveyor", new[] { "THE PASS", "Plates ride the belt to the dining room. Pick the plate up at the end and take it to the table on its label." } },
            { "plate_delivery", new[] { "SERVING", "Touch the plate to the right table. If you drop it, you'll have to plate it again." } },
            { "line_cooks", new[] { "LINE COOKS", "Your idle cooks wait in the staff area. Hire more at the Staff Board." } },
            { "call_cook", new[] { "CALL A COOK", "A cook walks over along the mats and joins the station's crew." } },
            { "walkway", new[] { "WALKWAY MATS", "Cooks and guests only walk on mats and runners, so keep the staff area, stations, door and tables connected." } },
            { "crew", new[] { "CREW", "This station has a cook now. It cooks faster and repeats the last ticket by itself." } },
            { "supplier", new[] { "SUPPLIER", "Spend gold here on boxes of ingredients. Each ticket is an instant purchase." } },
            { "staff_board", new[] { "STAFF BOARD", "Hire a line cook for gold. They wait in the staff area until you call them." } },
            { "pass", new[] { "THE PASS", "Orders come in on the ticket rail and plates go out on the belt." } },
            { "herbs", new[] { "HERB PLANT", "Grows as long as it has water. Snip it with the shears once it's grown to get herbs." } },
            { "shears", new[] { "SHEARS", "Touch a grown herb plant to snip some herbs." } },
            { "crate", new[] { "SUPPLY CRATE", "Deliveries show up at the back door. Pry one open with the crowbar to get the ingredients inside. Don't leave them too long or they go off." } },
            { "cleaver", new[] { "CROWBAR", "Push the flat end into a supply crate to pry it open." } },
            { "ingredients", new[] { "LOOSE INGREDIENTS", "Touch them with the basket or drop them in the pantry chute to add them to the pantry. Don't leave them lying around." } },
            { "basket", new[] { "HARVEST BASKET", "Touch loose ingredients with the basket to collect them." } },
            { "harvest", new[] { "COLLECTED", "That went into your pantry." } },
            { "collision", new[] { "BY TOUCH", "A lot of things here work by touch: plates on tables, cooks on stations, ingredients in the chute." } },
            { "intermittent", new[] { "SOMETHING ARRIVED", "Deliveries and guests turn up every so often. Keep an eye on the back door and the dining room." } },
            { "death", new[] { "GONE", "When something runs out completely, like a guest's patience, it's gone for good." } },
            { "caretaking", new[] { "UPKEEP", "Looking after equipment and plants keeps them working." } },
            { "fryer", new[] { "DEEP FRYER", "Cut potatoes go in here. Wait for the oil to heat up first." } },
            { "trash", new[] { "SCRAPS", "Prep leaves scraps behind. Throw them in the bin." } },
            { "sink", new[] { "SINK", "The sink keeps the herb plants next to it watered." } },
            { "planter", new[] { "PLANTER BOX", "The herbs grow in this. Keep it watered." } },
        };

        static readonly Dictionary<string, string> s_Labels = new Dictionary<string, string>
        {
            { "TOOLS (they return here if left lying around)", "TOOLS" },
            { "SUPPLIER (trade gold for stock)", "SUPPLIER" },
            { "STAFF BOARD - hire line cooks", "STAFF BOARD" },
            { "SUPPLIER - trade gold for stock", "SUPPLIER" },
            { "ORDERS - poke a ticket to plate it", "ORDERS" },
            { "EXPO - grab the plate", "PICK UP" },
        };

        [MenuItem("Tools/Steven/Polish/Clean Up Scene")]
        public static void CleanUp()
        {
            var scene = SceneManager.GetActiveScene();
            var game = FindFirst<KitchenGame>(scene);
            if (game == null)
            {
                EditorUtility.DisplayDialog("Clean Up", "Open StevenScene first.", "OK");
                return;
            }

            var remove = UnusedEquipment(scene);
            if (!EditorUtility.DisplayDialog("Clean Up Scene",
                    "Rewrites the tip cards, goal cards and signs in plain words, and removes:\n" +
                    "- " + remove.Count + " pieces of equipment nothing is cooked on (stoves, pans, oven, fridge, ice machine and their hookups)\n" +
                    "- the demo shortcut panel in the office (F-keys still work in the Editor)\n" +
                    "- tips for systems that were taken out, and two unused scripts.\n\nUndo with git if needed. Continue?", "Clean up", "Cancel"))
                return;

            var log = new List<string>();

            // 1. Tip cards.
            var db = game.database;
            if (db != null)
            {
                var n = 0;
                foreach (var t in db.tutorials)
                {
                    if (t == null || !s_Tips.TryGetValue(t.id, out var text))
                        continue;
                    t.title = text[0];
                    t.body = text[1];
                    n++;
                }
                EditorUtility.SetDirty(db);
                log.Add("Rewrote " + n + " tip cards.");
            }
            var simplifier = FindFirst<KitchenSimplifier>(scene);
            if (simplifier != null)
            {
                foreach (var id in s_DeadTips)
                    if (!simplifier.undiscoveredTips.Contains(id))
                        simplifier.undiscoveredTips.Add(id);
                EditorUtility.SetDirty(simplifier);
                log.Add("Muted " + s_DeadTips.Length + " tips for removed systems.");
            }

            // 2. Goal cards.
            var progression = FindFirst<KitchenProgression>(scene);
            if (progression != null)
            {
                foreach (var g in progression.goals)
                    Goal(g);
                EditorUtility.SetDirty(progression);
                log.Add("Rewrote the goal cards.");
            }

            // 3. Signs and station names.
            var labels = 0;
            foreach (var t in FindAll<Text>(scene))
            {
                if (t != null && s_Labels.TryGetValue(t.text, out var shorter))
                {
                    t.text = shorter;
                    EditorUtility.SetDirty(t);
                    labels++;
                }
            }
            foreach (var st in FindAll<CraftingStation>(scene))
            {
                if (s_Labels.TryGetValue(st.stationName, out var shorter))
                {
                    st.stationName = shorter;
                    EditorUtility.SetDirty(st);
                    labels++;
                }
            }
            log.Add("Shortened " + labels + " signs.");

            // 4. Unused equipment.
            foreach (var b in remove)
            {
                if (b == null)
                    continue;
                try
                {
                    Object.DestroyImmediate(b.gameObject);
                }
                catch (System.Exception)
                {
                    b.gameObject.SetActive(false);   // part of another prefab instance: hide it instead
                }
            }
            log.Add("Removed " + remove.Count + " unused pieces of equipment.");

            // 5. Demo shortcut panel: hide it, keep the Editor F-keys.
            var office = FindFirst<ChefOfficePanel>(scene);
            if (office != null)
            {
                foreach (Transform child in office.transform)
                    child.gameObject.SetActive(false);
                foreach (var trig in office.GetComponents<TutorialTrigger>())
                    Object.DestroyImmediate(trig);
                log.Add("Hid the demo shortcut panel (F1 gold, F2 stock, F3 guests, F7 cook still work in the Editor).");
            }

            // 6. Dev names.
            foreach (var t in FindAll<Transform>(scene))
            {
                if (t != null && t.name.StartsWith("MP2B "))
                    t.name = t.name.Substring(5);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            // 7. Unused scripts (last: deleting scripts recompiles).
            foreach (var path in new[] { "Assets/StevenAssets/Scripts/Core/BakedMarker.cs", "Assets/StevenAssets/Scripts/Debugging/EditorSimulatorActivator.cs" })
                if (AssetDatabase.DeleteAsset(path))
                    log.Add("Deleted " + System.IO.Path.GetFileName(path) + " (unused).");

            Debug.Log("[MP2C] Clean up:\n" + string.Join("\n", log));
            EditorUtility.DisplayDialog("Clean Up Scene", string.Join("\n", log) + "\n\nScene saved.", "OK");
        }

        static List<GridBuilding> UnusedEquipment(Scene scene)
        {
            var all = FindAll<GridBuilding>(scene);
            var list = new List<GridBuilding>();
            var unused = new HashSet<BuildingId>(s_Unused);
            foreach (var b in all)
                if (b != null && unused.Contains(b.buildingId))
                    list.Add(b);
            // Water lines that only fed the ice machine.
            foreach (var b in all)
            {
                if (b == null || b.buildingId != BuildingId.WaterLine)
                    continue;
                var feedsSink = false;
                foreach (var other in all)
                    if (other != null && other.cell == b.cell && other.buildingId == BuildingId.Sink)
                        feedsSink = true;
                if (!feedsSink)
                    list.Add(b);
            }
            return list;
        }

        static void Goal(KitchenProgression.Goal g)
        {
            switch (g.name)
            {
                case "First Service":
                    g.revealTitle = "UNLOCKED: FRY STATION";
                    g.revealBody = "Some dishes use two stations. Cut potatoes on the cutting board, then fry them in the deep fryer.";
                    g.sandboxPrompt = "Try it: cut some potatoes on the cutting board.";
                    g.sandboxSuccess = "Cut! Now fry them.";
                    break;
                case "Lunch Rush":
                    g.revealTitle = "UNLOCKED: STEAK";
                    g.revealBody = "The butcher now sells beef, and the grill has a Grilled Steak ticket. Steak needs herbs: snip them from the herb plants with the shears.";
                    g.sandboxPrompt = "Try it: snip a herb plant with the shears.";
                    g.sandboxSuccess = "Got herbs. Now grill a steak.";
                    break;
                case "Expansion":
                    g.revealTitle = "UNLOCKED: EQUIPMENT CATALOG";
                    g.revealBody = "Buy new equipment from the catalog shelf. Pick up a miniature and let go over the floor to place it. Grills go on a gas line.";
                    break;
                case "Dinner Service":
                    g.revealTitle = "UNLOCKED: LINE COOKS";
                    g.revealBody = "Hire cooks at the Staff Board. Press CALL A COOK on a station and a cook walks over and keeps cooking there.";
                    g.sandboxPrompt = "Try it: hire a cook at the Staff Board.";
                    g.sandboxSuccess = "You hired a cook.";
                    break;
                case "Partner Deliveries":
                    g.revealTitle = "UNLOCKED: DELIVERIES";
                    g.revealBody = "The garden, café and orchestra can send things here now. Garden tomatoes and orchestra coins go in the pantry chute; café coffee and roses go on a dining table.";
                    break;
                case "Grand Opening":
                    g.revealTitle = "RESTAURANT COMPLETE";
                    g.revealBody = "Full tables and a good reputation. The restaurant is ready for the grand opening.";
                    break;
            }
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
    }
}
