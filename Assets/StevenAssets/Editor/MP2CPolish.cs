using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Steven.MP2A.EditorTools
{
    /// <summary>
    /// MP2C polish for StevenScene (open it first). Each menu is safe to run again; "Run All" does all four and saves.
    ///   Tools > Steven > Polish > Add Teleport Menu       Bryce's scene-switch menu (Assets/Prefabs/Canvas.prefab) on the right controller
    ///   Tools > Steven > Polish > Restyle Order Tickets   the pass rail tickets become printed kitchen order chits
    ///   Tools > Steven > Polish > Cleaver To Crowbar      the crate-opening tool becomes a red crowbar
    ///   Tools > Steven > Polish > Add First Shift Tutorial  the guided opening tutorial (FirstShiftCoach)
    /// </summary>
    public static class MP2CPolish
    {
        const string TeleportPrefab = "Assets/Prefabs/Canvas.prefab";
        const string CleaverPrefab = "Assets/StevenAssets/Prefabs/Tools/Cleaver.prefab";
        const string CrowbarPrefab = "Assets/StevenAssets/Prefabs/Tools/Crowbar.prefab";
        const string MatFolder = "Assets/StevenAssets/Materials";
        const string FontPath = "Assets/StevenAssets/Fonts/CourierPrime-Bold.ttf";
        const string TicketPaper = "Assets/StevenAssets/Textures/OrderTicket_Paper.png";

        static readonly List<string> s_Log = new List<string>();

        [MenuItem("Tools/Steven/Polish/Run All")]
        public static void RunAll()
        {
            s_Log.Clear();
            var scene = SceneManager.GetActiveScene();
            AddTeleport(scene);
            RestyleTickets(scene);
            Crowbar(scene);
            AddCoach(scene);
            WireToGo(scene);
            Finish(scene, "MP2C Polish");
        }

        [MenuItem("Tools/Steven/Polish/Add Teleport Menu")]
        public static void AddTeleportMenu() { s_Log.Clear(); var s = SceneManager.GetActiveScene(); AddTeleport(s); Finish(s, "Teleport Menu"); }

        [MenuItem("Tools/Steven/Polish/Restyle Order Tickets")]
        public static void RestyleOrderTickets() { s_Log.Clear(); var s = SceneManager.GetActiveScene(); RestyleTickets(s); Finish(s, "Order Tickets"); }

        [MenuItem("Tools/Steven/Polish/Cleaver To Crowbar")]
        public static void CleaverToCrowbar() { s_Log.Clear(); var s = SceneManager.GetActiveScene(); Crowbar(s); Finish(s, "Crowbar"); }

        [MenuItem("Tools/Steven/Polish/Add First Shift Tutorial")]
        public static void AddFirstShift() { s_Log.Clear(); var s = SceneManager.GetActiveScene(); AddCoach(s); Finish(s, "First Shift"); }

        [MenuItem("Tools/Steven/Polish/Wire Orchestra Meal Crate")]
        public static void WireOrchestraCrate() { s_Log.Clear(); var s = SceneManager.GetActiveScene(); WireToGo(s); Finish(s, "Orchestra Meal Crate"); }

        /// <summary>The purple TO ORCHESTRA crate boxes plates "to go" as clean Dish_ prefabs.</summary>
        static void WireToGo(Scene scene)
        {
            TransportTunnel tunnel = null;
            foreach (var t in FindAll<TransportTunnel>(scene))
                if (t.receiverId == "Orchestra")
                    tunnel = t;
            if (tunnel == null)
            {
                Log("Meal crate: no sender with Receiver Id 'Orchestra' (run Place All Delivery Crates first).");
                return;
            }
            var toGo = tunnel.GetComponent<PlateToGo>();
            if (toGo == null)
                toGo = tunnel.gameObject.AddComponent<PlateToGo>();
            toGo.dishPrefabs.Clear();
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab Dish_", new[] { "Assets/StevenAssets/Prefabs/Dishes" }))
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (go != null && go.GetComponent<DishItem>() != null)
                    toGo.dishPrefabs.Add(go);
            }
            EditorUtility.SetDirty(toGo);
            Log("Meal crate: plates dropped in the TO ORCHESTRA crate are sent as Dish_ meals (" + toGo.dishPrefabs.Count + " dish prefabs), +" + toGo.goldPerMeal + " Gold each.");
        }

        static void Finish(Scene scene, string title)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            var report = string.Join("\n", s_Log);
            Debug.Log("[MP2C Polish] " + title + ":\n" + report);
            EditorUtility.DisplayDialog(title, report + "\n\nScene saved.", "OK");
        }

        static void Log(string s) => s_Log.Add(s);

        // ------------------------------------------------------------------ 1. teleport menu

        static void AddTeleport(Scene scene)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TeleportPrefab);
            if (prefab == null)
            {
                Log("Teleport: " + TeleportPrefab + " not found (pull main first).");
                return;
            }
            var right = Find(scene, "Right Controller");
            if (right == null)
            {
                Log("Teleport: no 'Right Controller' under the XR Origin.");
                return;
            }

            // A stray TeleportationManager with empty fields on the shared controller model switches the model off at
            // start; remove it from this scene's copy (a prefab override, the shared prefab is left alone).
            foreach (var tm in right.GetComponentsInChildren<TeleportationManager>(true))
            {
                if (tm.StickPressed != null)
                    continue;
                var where = tm.gameObject.name;
                try
                {
                    Object.DestroyImmediate(tm, false);
                    Log("Teleport: removed an empty TeleportationManager from '" + where + "' (it hid the controller model).");
                }
                catch (System.Exception e)
                {
                    tm.enabled = false;
                    Log("Teleport: disabled an empty TeleportationManager on '" + where + "' (" + e.Message + ").");
                }
            }

            foreach (var tm in right.GetComponentsInChildren<TeleportationManager>(true))
            {
                if (tm.StickPressed != null)
                {
                    Log("Teleport: menu already on the right controller.");
                    return;
                }
            }

            var menu = (GameObject)PrefabUtility.InstantiatePrefab(prefab, right);
            menu.transform.localPosition = new Vector3(0f, 0.2f, 0.2f);   // same as the other scenes
            menu.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
            Log("Teleport: added Bryce's menu to Right Controller (click the right stick, push toward a scene, confirm).");
        }

        // ------------------------------------------------------------------ 2. order tickets

        static void RestyleTickets(Scene scene)
        {
            var rail = FindFirst<OrderRail>(scene);
            if (rail == null)
            {
                Log("Tickets: no Order Rail found.");
                return;
            }
            var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            var paper = LoadSprite(TicketPaper);
            var steel = AssetDatabase.LoadAssetAtPath<Material>(MatFolder + "/Steel.mat");
            if (font == null || paper == null)
            {
                Log("Tickets: font or ticket texture missing (extract the zip into Assets/ first).");
                return;
            }

            var count = 0;
            foreach (var card in rail.slots)
            {
                if (card == null)
                    continue;
                var visual = card.visual != null ? card.visual : card.transform;

                var backing = visual.Find("Backing");
                if (backing != null && backing.GetComponent<MeshRenderer>() != null)
                    backing.GetComponent<MeshRenderer>().enabled = false;   // the paper is the ticket now

                if (card.background != null)
                {
                    card.background.sprite = paper;
                    card.background.type = Image.Type.Simple;
                    card.background.color = Color.white;
                    EditorUtility.SetDirty(card.background);
                }

                // Colored flag at the top shows the state (cook it / ready / on the pass / hurry).
                var stripe = card.canvas != null ? card.canvas.transform.Find("TopStripe") : null;
                if (stripe != null && stripe.GetComponent<Image>() != null)
                {
                    var rt = (RectTransform)stripe;
                    Anchor(rt, 0.04f, 0.935f, 0.96f, 0.985f);
                    card.accent = stripe.GetComponent<Image>();
                }

                if (card.title != null)
                {
                    Ink(card.title, font, 44, TextAnchor.MiddleCenter);
                    Anchor((RectTransform)card.title.transform, 0.04f, 0.77f, 0.96f, 0.9f);
                }
                if (card.body != null)
                {
                    Ink(card.body, font, 24, TextAnchor.UpperCenter);
                    card.body.supportRichText = true;
                    Anchor((RectTransform)card.body.transform, 0.05f, 0.16f, 0.95f, 0.77f);
                }

                // Patience bar near the bottom.
                if (card.canvas != null)
                {
                    var canvasRt = (RectTransform)card.canvas.transform;
                    var old = canvasRt.Find("Patience Track");
                    if (old != null)
                        Object.DestroyImmediate(old.gameObject);
                    var track = NewImage(canvasRt, "Patience Track", new Color(0.82f, 0.8f, 0.74f));
                    Anchor(track, 0.1f, 0.075f, 0.9f, 0.11f);
                    var fill = NewImage(track, "Fill", new Color(0.2f, 0.55f, 0.25f));
                    Anchor(fill, 0f, 0f, 1f, 1f);
                    card.meter = fill;
                    var labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                    labelGo.transform.SetParent(track, false);
                    var label = labelGo.GetComponent<Text>();
                    Ink(label, font, 18, TextAnchor.LowerLeft);
                    label.text = "GUEST PATIENCE";
                    label.raycastTarget = false;
                    Anchor((RectTransform)labelGo.transform, 0f, 1.15f, 1f, 2.6f);
                    track.gameObject.SetActive(false);
                }

                // A steel clip at the top, holding the ticket on the rail.
                var clip = visual.Find("Clip");
                if (clip == null && steel != null)
                {
                    var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.name = "Clip";
                    Object.DestroyImmediate(go.GetComponent<Collider>());
                    go.transform.SetParent(visual, false);
                    go.transform.localPosition = new Vector3(0f, 0.118f, 0.002f);
                    go.transform.localScale = new Vector3(0.04f, 0.03f, 0.012f);
                    go.GetComponent<MeshRenderer>().sharedMaterial = steel;
                }
                EditorUtility.SetDirty(card);
                count++;
            }
            rail.paperTickets = true;
            EditorUtility.SetDirty(rail);
            Log("Tickets: restyled " + count + " order tickets as printed kitchen chits.");
        }

        static void Ink(Text t, Font font, int size, TextAnchor anchor)
        {
            t.font = font;
            t.fontSize = size;
            t.fontStyle = FontStyle.Normal;
            t.color = new Color(0.1f, 0.1f, 0.12f);
            t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = Mathf.Max(10, size / 2);
            t.resizeTextMaxSize = size;
            EditorUtility.SetDirty(t);
        }

        // ------------------------------------------------------------------ 3. crowbar

        static void Crowbar(Scene scene)
        {
            var path = AssetDatabase.LoadAssetAtPath<GameObject>(CrowbarPrefab) != null ? CrowbarPrefab : CleaverPrefab;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                Log("Crowbar: tool prefab not found.");
                return;
            }

            var red = PaintMat("Crowbar_Red", new Color(0.72f, 0.12f, 0.09f), 0.45f);
            var rubber = PaintMat("Crowbar_Grip", new Color(0.08f, 0.08f, 0.08f), 0.1f);
            var steel = AssetDatabase.LoadAssetAtPath<Material>(MatFolder + "/Steel.mat");
            if (steel == null)
                steel = red;

            var root = PrefabUtility.LoadPrefabContents(path);
            root.name = "Crowbar";
            var visual = root.transform.Find("Visual");
            if (visual == null)
            {
                visual = new GameObject("Visual").transform;
                visual.SetParent(root.transform, false);
            }
            for (var i = visual.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(visual.GetChild(i).gameObject);

            // Low-poly crowbar along +Z: red bar, goose-neck hook with claw at the back, flat chisel at the front.
            Part(visual, "Bar", new Vector3(0f, 0f, 0.02f), new Vector3(0.02f, 0.02f, 0.40f), 0f, red);
            Part(visual, "Neck", new Vector3(0f, 0.02f, -0.2f), new Vector3(0.02f, 0.02f, 0.07f), 40f, red);
            Part(visual, "Hook", new Vector3(0f, 0.07f, -0.235f), new Vector3(0.02f, 0.02f, 0.06f), 80f, red);
            Part(visual, "Claw", new Vector3(0f, 0.097f, -0.21f), new Vector3(0.034f, 0.008f, 0.055f), 20f, steel);
            Part(visual, "Chisel", new Vector3(0f, -0.006f, 0.245f), new Vector3(0.03f, 0.007f, 0.06f), 12f, steel);
            Part(visual, "Grip", new Vector3(0f, 0f, -0.1f), new Vector3(0.026f, 0.026f, 0.12f), 0f, rubber);

            foreach (var c in root.GetComponents<BoxCollider>())
            {
                if (c.isTrigger)
                    continue;
                c.center = new Vector3(0f, 0f, 0.02f);
                c.size = new Vector3(0.03f, 0.03f, 0.5f);
            }
            var grip = root.transform.Find("Grip");
            if (grip != null)
                grip.localPosition = new Vector3(0f, 0f, -0.1f);
            var tip = root.transform.Find("Tip");
            if (tip != null)
            {
                tip.localPosition = new Vector3(0f, -0.008f, 0.26f);
                var tc = tip.GetComponent<BoxCollider>();
                if (tc != null)
                    tc.size = new Vector3(0.06f, 0.06f, 0.08f);
            }
            var entity = root.GetComponent<KitchenEntity>();
            if (entity != null)
                entity.displayName = "Crowbar";

            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
            if (path == CleaverPrefab)
            {
                var err = AssetDatabase.RenameAsset(CleaverPrefab, "Crowbar");
                Log(string.IsNullOrEmpty(err) ? "Crowbar: rebuilt the Cleaver prefab as Crowbar.prefab." : "Crowbar: rebuilt the prefab (rename failed: " + err + ").");
            }
            else
            {
                Log("Crowbar: rebuilt Crowbar.prefab.");
            }

            // Scene copy, simplifier list, tip texts.
            foreach (var t in FindAllTransforms(scene))
            {
                if (t != null && t.name == "Cleaver" && t.GetComponent<KitchenEntity>() != null)
                {
                    t.name = "Crowbar";
                    Log("Crowbar: renamed the scene tool.");
                }
            }
            var simplifier = FindFirst<KitchenSimplifier>(scene);
            if (simplifier != null)
            {
                var i = simplifier.tools.IndexOf("Cleaver");
                if (i >= 0)
                    simplifier.tools[i] = "Crowbar";
                else if (!simplifier.tools.Contains("Crowbar"))
                    simplifier.tools.Add("Crowbar");
                EditorUtility.SetDirty(simplifier);
            }
            var db = KitchenGame.Db != null ? KitchenGame.Db : FindFirst<KitchenGame>(scene)?.database;
            if (db != null)
            {
                foreach (var tip2 in db.tutorials)
                {
                    if (tip2.id == "cleaver")
                    {
                        tip2.title = "CROWBAR";
                        tip2.body = "Jam the flat end into a supply crate to pry it open.";
                    }
                    else if (tip2.id == "crate")
                    {
                        tip2.body = "Deliveries pile up at the back door and lose Freshness over time. Pry one open with the crowbar to get loose ingredients.";
                    }
                    else if (!string.IsNullOrEmpty(tip2.body) && tip2.body.Contains("cleaver"))
                    {
                        tip2.body = tip2.body.Replace("cleaver", "crowbar");
                    }
                }
                EditorUtility.SetDirty(db);
                Log("Crowbar: updated the tip texts.");
            }
        }

        static void Part(Transform parent, string name, Vector3 center, Vector3 size, float rotX, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localRotation = Quaternion.Euler(rotX, 0f, 0f);
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        static Material PaintMat(string name, Color color, float smoothness)
        {
            var path = MatFolder + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var src = AssetDatabase.LoadAssetAtPath<Material>(MatFolder + "/White.mat");
                mat = src != null ? new Material(src) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);
            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ------------------------------------------------------------------ 4. first shift tutorial

        static void AddCoach(Scene scene)
        {
            var progression = FindFirst<KitchenProgression>(scene);
            if (progression == null)
            {
                Log("First Shift: no Kitchen Progression (run the MP2C setup first).");
                return;
            }
            var coach = progression.GetComponent<FirstShiftCoach>();
            if (coach == null)
                coach = progression.gameObject.AddComponent<FirstShiftCoach>();
            coach.progression = progression;
            var gold = PaintMat("FirstShift_Arrow", new Color(1f, 0.78f, 0.15f), 0.6f);
            if (gold.HasProperty("_EmissionColor"))
            {
                gold.EnableKeyword("_EMISSION");
                gold.SetColor("_EmissionColor", new Color(0.9f, 0.6f, 0.1f));
            }
            coach.arrowMaterial = gold;
            EditorUtility.SetDirty(coach);

            // The tutorial teaches the core loop, so the first goal needs at least one table ready for guests.
            var tables = 0;
            foreach (var t in FindAll<DiningTable>(scene))
                if (t.gameObject.activeInHierarchy && t.HasPlaceSetting)
                    tables++;
            Log("First Shift: tutorial added to " + progression.name + " (" + tables + " set tables found" +
                (tables == 0 ? " - it needs at least one table with a place setting!" : "") + "). Press F8 in Play mode to skip it.");
        }

        // ------------------------------------------------------------------ helpers

        static Sprite LoadSprite(string path)
        {
            AssetDatabase.ImportAsset(path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return null;
            if (importer.textureType != TextureImporterType.Sprite || !importer.alphaIsTransparency)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = true;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static RectTransform NewImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return (RectTransform)go.transform;
        }

        static void Anchor(RectTransform rt, float xMin, float yMin, float xMax, float yMax)
        {
            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
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

        static List<Transform> FindAllTransforms(Scene scene) => FindAll<Transform>(scene);

        static Transform Find(Scene scene, string name)
        {
            foreach (var t in FindAllTransforms(scene))
                if (t.name == name)
                    return t;
            return null;
        }
    }
}
