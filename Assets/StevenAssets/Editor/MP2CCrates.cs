using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Steven.MP2A.EditorTools
{
    /// <summary>
    /// Tools > Steven > Create Delivery Crates
    ///   Builds a low-poly, color-coded wooden delivery crate (meshes, materials, Sender + Receiver prefabs) and places
    ///   one Sender crate in the open scene (StevenScene), set to deliver to Rishabh's cafe.
    /// Tools > Steven > Place Kitchen Receiver Crate
    ///   Puts a Receiver crate (id "Kitchen") next to it, so teammates can send things to the restaurant.
    /// Tools > Steven > Place All Delivery Crates
    ///   Rebuilds the prefabs (with a route sign) and lines up the Kitchen receiver plus four senders along the office's
    ///   south wall: to Cafe, Garden, Orchestra, and a test sender to this kitchen's own receiver.
    /// Tools > Steven > Remove Hidden Kitchen Content
    ///   Deletes from StevenScene everything the MP2C Kitchen Simplifier was hiding.
    /// Safe to run again. Delete this script afterwards if you like; the crates are ordinary assets.
    /// </summary>
    public static class MP2CCrates
    {
        const string ModelFolder = "Assets/StevenAssets/Models";
        const string MatFolder = "Assets/StevenAssets/Materials";
        const string PrefabFolder = "Assets/StevenAssets/Prefabs/Delivery";
        const string SenderPath = PrefabFolder + "/DeliveryCrate_Sender.prefab";
        const string ReceiverPath = PrefabFolder + "/DeliveryCrate_Receiver.prefab";

        // Route colors: a crate is painted in the color of the place it delivers to.
        public static readonly Color KitchenRed = new Color(0.85f, 0.18f, 0.15f);
        public static readonly Color CafeOrange = new Color(0.95f, 0.5f, 0.15f);
        public static readonly Color GardenGreen = new Color(0.3f, 0.75f, 0.25f);
        public static readonly Color OrchestraPurple = new Color(0.55f, 0.35f, 0.9f);

        const float W = 0.62f, D = 0.52f, H = 0.5f;

        struct Box
        {
            public string part;
            public Vector3 center, size;
            public float rotZ;
        }

        static Box B(string part, float cx, float cy, float cz, float sx, float sy, float sz, float rz) =>
            new Box { part = part, center = new Vector3(cx, cy, cz), size = new Vector3(sx, sy, sz), rotZ = rz };

        // Generated from the crate design (41 boxes, front faces +Z).
        static readonly Box[] s_Boxes =
        {
            B("Dark", 0.0000f, 0.0200f, -0.1700f, 0.6200f, 0.0400f, 0.0600f, 0.00f),
            B("Dark", 0.0000f, 0.0200f, 0.1700f, 0.6200f, 0.0400f, 0.0600f, 0.00f),
            B("Wood", 0.0000f, 0.0475f, 0.0000f, 0.5800f, 0.0150f, 0.4800f, 0.00f),
            B("Dark", -0.2850f, 0.2700f, -0.2350f, 0.0500f, 0.4600f, 0.0500f, 0.00f),
            B("Dark", -0.2850f, 0.2700f, 0.2350f, 0.0500f, 0.4600f, 0.0500f, 0.00f),
            B("Dark", 0.2850f, 0.2700f, -0.2350f, 0.0500f, 0.4600f, 0.0500f, 0.00f),
            B("Dark", 0.2850f, 0.2700f, 0.2350f, 0.0500f, 0.4600f, 0.0500f, 0.00f),
            B("Wood", 0.0000f, 0.1100f, -0.2440f, 0.5200f, 0.1100f, 0.0220f, 0.00f),
            B("Wood", 0.0000f, 0.1100f, 0.2440f, 0.5200f, 0.1100f, 0.0220f, 0.00f),
            B("Wood", -0.2940f, 0.1100f, 0.0000f, 0.0220f, 0.1100f, 0.4200f, 0.00f),
            B("Wood", 0.2940f, 0.1100f, 0.0000f, 0.0220f, 0.1100f, 0.4200f, 0.00f),
            B("Wood", 0.0000f, 0.2500f, -0.2440f, 0.5200f, 0.1200f, 0.0220f, 0.00f),
            B("Wood", 0.0000f, 0.2500f, 0.2440f, 0.5200f, 0.1200f, 0.0220f, 0.00f),
            B("Wood", -0.2940f, 0.2500f, 0.0000f, 0.0220f, 0.1200f, 0.4200f, 0.00f),
            B("Wood", 0.2940f, 0.2500f, 0.0000f, 0.0220f, 0.1200f, 0.4200f, 0.00f),
            B("Wood", 0.0000f, 0.3950f, -0.2440f, 0.5200f, 0.1200f, 0.0220f, 0.00f),
            B("Wood", 0.0000f, 0.3950f, 0.2440f, 0.5200f, 0.1200f, 0.0220f, 0.00f),
            B("Wood", -0.2940f, 0.3950f, 0.0000f, 0.0220f, 0.1200f, 0.4200f, 0.00f),
            B("Wood", 0.2940f, 0.3950f, 0.0000f, 0.0220f, 0.1200f, 0.4200f, 0.00f),
            B("Dark", 0.0000f, 0.4775f, -0.2450f, 0.6200f, 0.0450f, 0.0300f, 0.00f),
            B("Dark", 0.0000f, 0.4775f, 0.2450f, 0.6200f, 0.0450f, 0.0300f, 0.00f),
            B("Dark", -0.2950f, 0.4775f, 0.0000f, 0.0300f, 0.0450f, 0.4600f, 0.00f),
            B("Dark", 0.2950f, 0.4775f, 0.0000f, 0.0300f, 0.0450f, 0.4600f, 0.00f),
            B("Paint", 0.0000f, 0.2500f, -0.2570f, 0.5100f, 0.0700f, 0.0040f, 0.00f),
            B("Paint", 0.0000f, 0.2500f, 0.2570f, 0.5100f, 0.0700f, 0.0040f, 0.00f),
            B("Paint", -0.3070f, 0.2500f, 0.0000f, 0.0040f, 0.0700f, 0.4100f, 0.00f),
            B("Paint", 0.3070f, 0.2500f, 0.0000f, 0.0040f, 0.0700f, 0.4100f, 0.00f),
            B("Dark", 0.0000f, 0.2550f, -0.2680f, 0.6360f, 0.0600f, 0.0180f, -37.57f),
            B("Dark", 0.0000f, 0.2550f, 0.2680f, 0.6360f, 0.0600f, 0.0180f, 37.57f),
            B("Metal", -0.2800f, 0.5040f, -0.2300f, 0.0700f, 0.0100f, 0.0700f, 0.00f),
            B("Metal", -0.3040f, 0.0700f, -0.2540f, 0.0160f, 0.0600f, 0.0160f, 0.00f),
            B("Metal", -0.2800f, 0.5040f, 0.2300f, 0.0700f, 0.0100f, 0.0700f, 0.00f),
            B("Metal", -0.3040f, 0.0700f, 0.2540f, 0.0160f, 0.0600f, 0.0160f, 0.00f),
            B("Metal", 0.2800f, 0.5040f, -0.2300f, 0.0700f, 0.0100f, 0.0700f, 0.00f),
            B("Metal", 0.3040f, 0.0700f, -0.2540f, 0.0160f, 0.0600f, 0.0160f, 0.00f),
            B("Metal", 0.2800f, 0.5040f, 0.2300f, 0.0700f, 0.0100f, 0.0700f, 0.00f),
            B("Metal", 0.3040f, 0.0700f, 0.2540f, 0.0160f, 0.0600f, 0.0160f, 0.00f),
            B("Placard", 0.1600f, 0.3950f, 0.2590f, 0.1500f, 0.0950f, 0.0040f, 0.00f),
            B("Paint", 0.1600f, 0.4090f, 0.2620f, 0.0160f, 0.0400f, 0.0030f, 0.00f),
            B("Paint", 0.1485f, 0.3820f, 0.2620f, 0.0120f, 0.0360f, 0.0030f, 45.00f),
            B("Paint", 0.1715f, 0.3820f, 0.2620f, 0.0120f, 0.0360f, 0.0030f, -45.00f),
        };

        // ------------------------------------------------------------------ menu: crates

        [MenuItem("Tools/Steven/Create Delivery Crates")]
        public static void CreateCrates()
        {
            var scene = SceneManager.GetActiveScene();
            EnsureFolder(PrefabFolder);
            BuildPrefabs();

            Remove(scene, "Delivery Crate - to Cafe");
            var sender = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(SenderPath));
            sender.name = "Delivery Crate - to Cafe";
            SceneManager.MoveGameObjectToScene(sender, scene);
            sender.transform.SetPositionAndRotation(new Vector3(3.4f, 0f, -2.95f), Quaternion.identity); // office, south-east corner, front facing the room
            var tunnel = sender.GetComponentInChildren<TransportTunnel>();
            tunnel.destinationScene = "Rishabh Scene";
            tunnel.receiverId = "Cafe";
            tunnel.tunnelColor = CafeOrange;
            EditorUtility.SetDirty(tunnel);

            Save(scene);
            Selection.activeGameObject = sender;
            EditorUtility.DisplayDialog("Delivery Crates",
                "Created " + SenderPath + " and " + ReceiverPath + ".\n\nPlaced an orange 'Delivery Crate - to Cafe' in the office (south-east corner). " +
                "It sends anything dropped into it to the Receiver with id 'Cafe' in 'Rishabh Scene'.", "OK");
        }

        [MenuItem("Tools/Steven/Place All Delivery Crates")]
        public static void PlaceAllCrates()
        {
            var scene = SceneManager.GetActiveScene();
            EnsureFolder(PrefabFolder);
            BuildPrefabs();
            foreach (var root in scene.GetRootGameObjects())
                if (root.name.StartsWith("Delivery Crate - "))
                    Object.DestroyImmediate(root);

            // Office, along the south wall (under the trophy shelf and office panel), fronts facing the room.
            const float z = -2.95f;
            PlaceReceiver(scene, "Delivery Crate - Kitchen Receiving", new Vector3(2.6f, 0f, z), "Kitchen", KitchenRed);
            PlaceSender(scene, "Delivery Crate - to Cafe", new Vector3(3.4f, 0f, z), "Rishabh Scene", "Cafe", CafeOrange, "");
            PlaceSender(scene, "Delivery Crate - TEST to Kitchen", new Vector3(1.8f, 0f, z), "StevenScene", "Kitchen", KitchenRed, "TEST: TO KITCHEN");
            PlaceSender(scene, "Delivery Crate - to Garden", new Vector3(1.0f, 0f, z), "SampleScene", "Garden", GardenGreen, "");
            var last = PlaceSender(scene, "Delivery Crate - to Orchestra", new Vector3(0.2f, 0f, z), "BryceSceneLvl", "Orchestra", OrchestraPurple, "");

            Save(scene);
            Selection.activeGameObject = last;
            EditorUtility.DisplayDialog("Delivery Crates",
                "Placed along the office's south wall (west to east): to Orchestra, to Garden, TEST to Kitchen, Kitchen Receiving, to Cafe.\n\n" +
                "Drop something in TEST to Kitchen and it should appear in Kitchen Receiving right away. Scene saved.", "OK");
        }

        static GameObject PlaceSender(Scene scene, string name, Vector3 pos, string destination, string id, Color color, string labelText)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(SenderPath));
            go.name = name;
            SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.SetPositionAndRotation(pos, Quaternion.identity);
            var tunnel = go.GetComponentInChildren<TransportTunnel>();
            tunnel.destinationScene = destination;
            tunnel.receiverId = id;
            tunnel.tunnelColor = color;
            tunnel.labelText = labelText;
            tunnel.Refresh();   // sign and color (OnValidate doesn't run for values set from code)
            EditorUtility.SetDirty(tunnel);
            if (tunnel.label != null)
            {
                EditorUtility.SetDirty(tunnel.label);
                PrefabUtility.RecordPrefabInstancePropertyModifications(tunnel.label);
            }
            PrefabUtility.RecordPrefabInstancePropertyModifications(tunnel);
            return go;
        }

        static GameObject PlaceReceiver(Scene scene, string name, Vector3 pos, string id, Color color)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ReceiverPath));
            go.name = name;
            SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.SetPositionAndRotation(pos, Quaternion.identity);
            var r = go.GetComponent<TransportReceiver>();
            r.receiverId = id;
            r.color = color;
            r.Refresh();
            EditorUtility.SetDirty(r);
            if (r.label != null)
            {
                EditorUtility.SetDirty(r.label);
                PrefabUtility.RecordPrefabInstancePropertyModifications(r.label);
            }
            PrefabUtility.RecordPrefabInstancePropertyModifications(r);
            return go;
        }

        [MenuItem("Tools/Steven/Place Kitchen Receiver Crate")]
        public static void PlaceKitchenReceiver()
        {
            var scene = SceneManager.GetActiveScene();
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ReceiverPath) == null)
                BuildPrefabs();
            Remove(scene, "Delivery Crate - Kitchen Receiving");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ReceiverPath));
            go.name = "Delivery Crate - Kitchen Receiving";
            SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.SetPositionAndRotation(new Vector3(2.6f, 0f, -2.95f), Quaternion.identity);
            var r = go.GetComponent<TransportReceiver>();
            r.receiverId = "Kitchen";
            r.color = KitchenRed;
            EditorUtility.SetDirty(r);
            Save(scene);
            Selection.activeGameObject = go;
        }

        static void BuildPrefabs()
        {
            EnsureFolder(ModelFolder);
            EnsureFolder(MatFolder);

            var wood = CopyMat("Wood", "DeliveryCrate_Wood", new Color(0.92f, 0.78f, 0.6f));
            var dark = CopyMat("Wood", "DeliveryCrate_DarkWood", new Color(0.55f, 0.38f, 0.25f));
            var paint = CopyMat("White", "DeliveryCrate_Paint", Color.white);
            var metal = AssetDatabase.LoadAssetAtPath<Material>(MatFolder + "/Steel.mat");
            if (metal == null)
                metal = dark;
            var placard = AssetDatabase.LoadAssetAtPath<Material>(MatFolder + "/Paper.mat");
            if (placard == null)
                placard = paint;
            var glowIn = CopyMat("Blast", "DeliveryCrate_GlowIn", Color.white, new Vector4(0f, 1.2f, 0f, 0f));
            var glowOut = CopyMat("Blast", "DeliveryCrate_GlowOut", Color.white, new Vector4(0f, -1.2f, 0f, 0f));
            var disc = AssetDatabase.LoadAssetAtPath<Mesh>(ModelFolder + "/M_PolarDisc.asset");

            var meshes = new Dictionary<string, Mesh>();
            foreach (var part in new[] { "Wood", "Dark", "Metal", "Paint", "Placard" })
                meshes[part] = SaveMesh(BuildMesh(part), ModelFolder + "/DeliveryCrate_" + part + ".asset");

            // Sender: anything dropped in the open top is delivered; the glow rings spiral inward.
            var sender = BuildCrate("DeliveryCrate_Sender", meshes, wood, dark, metal, placard, paint, glowIn, disc, out var senderTint, out var senderLabel);
            var intake = new GameObject("Intake");
            intake.transform.SetParent(sender.transform, false);
            intake.transform.localPosition = new Vector3(0f, 0.27f, 0f);
            var trigger = intake.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(W - 0.12f, 0.34f, D - 0.12f);
            var tunnel = intake.AddComponent<TransportTunnel>();
            tunnel.colorRenderers = senderTint;
            tunnel.label = senderLabel;
            tunnel.tunnelColor = CafeOrange;
            tunnel.destinationScene = "Rishabh Scene";
            tunnel.receiverId = "Cafe";
            SavePrefab(sender, SenderPath);

            // Receiver: deliveries drop into the crate; the glow rings spread outward.
            var receiver = BuildCrate("DeliveryCrate_Receiver", meshes, wood, dark, metal, placard, paint, glowOut, disc, out var receiverTint, out var receiverLabel);
            var rec = receiver.AddComponent<TransportReceiver>();
            AddStraw(receiver);
            rec.dropHeight = 0.55f;
            rec.colorRenderers = receiverTint;
            rec.label = receiverLabel;
            rec.color = KitchenRed;
            SavePrefab(receiver, ReceiverPath);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Tools/Steven/Add Packing Straw To Receiver Crates")]
        public static void AddStrawToReceiverPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(ReceiverPath);
            AddStraw(root);
            var rec = root.GetComponent<TransportReceiver>();
            if (rec != null)
                rec.dropHeight = 0.55f;
            PrefabUtility.SaveAsPrefabAsset(root, ReceiverPath);
            PrefabUtility.UnloadPrefabContents(root);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Receiver Crates", "Receiver crates now have packing straw near the top, so small items land within easy reach. Every receiver crate in every scene uses the updated prefab.", "OK");
        }

        /// <summary>A layer of packing straw near the rim: deliveries rest on it instead of the deep crate floor.</summary>
        static void AddStraw(GameObject crate)
        {
            var old = crate.transform.Find("Packing Straw");
            if (old != null)
                Object.DestroyImmediate(old.gameObject);
            var straw = CopyMat("White", "DeliveryCrate_Straw", new Color(0.86f, 0.73f, 0.4f));
            var bed = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bed.name = "Packing Straw";
            bed.transform.SetParent(crate.transform, false);
            bed.transform.localPosition = new Vector3(0f, 0.31f, 0f);
            bed.transform.localScale = new Vector3(W - 0.07f, 0.06f, D - 0.07f);
            bed.GetComponent<MeshRenderer>().sharedMaterial = straw;   // keeps its box collider: items rest on it
            var rng = new System.Random(7);
            for (var i = 0; i < 9; i++)
            {
                var tuft = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tuft.name = "Tuft";
                Object.DestroyImmediate(tuft.GetComponent<Collider>());
                tuft.transform.SetParent(bed.transform, false);
                tuft.transform.localPosition = new Vector3((float)rng.NextDouble() - 0.5f, 0.5f, (float)rng.NextDouble() - 0.5f) * 0.9f;
                tuft.transform.localRotation = Quaternion.Euler((float)rng.NextDouble() * 40f - 20f, (float)rng.NextDouble() * 180f, (float)rng.NextDouble() * 40f - 20f);
                tuft.transform.localScale = new Vector3(0.16f, 0.5f, 0.04f);
                tuft.GetComponent<MeshRenderer>().sharedMaterial = straw;
            }
        }

        static GameObject BuildCrate(string name, Dictionary<string, Mesh> meshes, Material wood, Material dark, Material metal,
            Material placard, Material paint, Material glow, Mesh disc, out Renderer[] tinted, out UnityEngine.UI.Text label)
        {
            var root = new GameObject(name);
            Part(root, "Wood", meshes["Wood"], wood);
            Part(root, "Frame", meshes["Dark"], dark);
            Part(root, "Metal", meshes["Metal"], metal);
            Part(root, "Placard", meshes["Placard"], placard);
            var paintR = Part(root, "Route Paint", meshes["Paint"], paint);
            var list = new List<Renderer> { paintR };
            if (disc != null)
            {
                var g = new GameObject("Route Glow");
                g.transform.SetParent(root.transform, false);
                g.transform.localPosition = new Vector3(0f, 0.058f, 0f);
                g.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                g.transform.localScale = new Vector3(0.42f, 0.42f, 1f);
                g.AddComponent<MeshFilter>().sharedMesh = disc;
                var r = g.AddComponent<MeshRenderer>();
                r.sharedMaterial = glow;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                list.Add(r);
            }
            tinted = list.ToArray();

            // Solid walls and floor so the crate stands and holds what lands in it (the top stays open).
            Wall(root, new Vector3(0f, 0.04f, 0f), new Vector3(W, 0.08f, D));
            Wall(root, new Vector3(0f, H / 2f, D / 2f - 0.015f), new Vector3(W, H, 0.03f));
            Wall(root, new Vector3(0f, H / 2f, -D / 2f + 0.015f), new Vector3(W, H, 0.03f));
            Wall(root, new Vector3(W / 2f - 0.015f, H / 2f, 0f), new Vector3(0.03f, H, D));
            Wall(root, new Vector3(-W / 2f + 0.015f, H / 2f, 0f), new Vector3(0.03f, H, D));

            label = BuildSign(root, dark, placard);
            return root;
        }

        /// <summary>A small route sign on two posts at the back of the crate, readable from the front (+Z).</summary>
        static UnityEngine.UI.Text BuildSign(GameObject root, Material post, Material board)
        {
            var z = -D / 2f + 0.03f;
            var boardY = H + 0.13f;
            Block(root, "Sign Post L", new Vector3(-0.22f, H + 0.05f, z), new Vector3(0.025f, 0.2f, 0.025f), post);
            Block(root, "Sign Post R", new Vector3(0.22f, H + 0.05f, z), new Vector3(0.025f, 0.2f, 0.025f), post);
            Block(root, "Sign Board", new Vector3(0f, boardY, z), new Vector3(0.52f, 0.13f, 0.02f), board);

            var canvasGo = new GameObject("Sign Text", typeof(RectTransform), typeof(Canvas));
            canvasGo.transform.SetParent(root.transform, false);
            var rect = canvasGo.GetComponent<RectTransform>();
            rect.localPosition = new Vector3(0f, boardY, z + 0.0115f);
            rect.localRotation = Quaternion.Euler(0f, 180f, 0f);   // UI faces -Z; turn it to read from the front
            rect.localScale = Vector3.one * 0.001f;
            rect.sizeDelta = new Vector2(500f, 120f);
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(UnityEngine.UI.Text));
            textGo.transform.SetParent(canvasGo.transform, false);
            var tr = textGo.GetComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = new Vector2(10f, 6f);
            tr.offsetMax = new Vector2(-10f, -6f);
            var text = textGo.GetComponent<UnityEngine.UI.Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 18;
            text.resizeTextMaxSize = 64;
            text.text = "DELIVERY";
            text.color = new Color(0.15f, 0.15f, 0.15f);
            return text;
        }

        static void Block(GameObject root, string name, Vector3 center, Vector3 size, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = center;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        static Renderer Part(GameObject root, string name, Mesh mesh, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            return r;
        }

        static void Wall(GameObject root, Vector3 center, Vector3 size)
        {
            var c = root.AddComponent<BoxCollider>();
            c.center = center;
            c.size = size;
        }

        static Mesh BuildMesh(string part)
        {
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            foreach (var b in s_Boxes)
            {
                if (b.part != part)
                    continue;
                var rot = Quaternion.Euler(0f, 0f, b.rotZ);
                var h = b.size * 0.5f;
                AddFace(verts, normals, uvs, tris, b.center, rot, Vector3.right, Vector3.up, Vector3.forward, h.x, h.y, h.z, b.size.x, b.size.y);   // +z
                AddFace(verts, normals, uvs, tris, b.center, rot, Vector3.left, Vector3.up, Vector3.back, h.x, h.y, h.z, b.size.x, b.size.y);     // -z
                AddFace(verts, normals, uvs, tris, b.center, rot, Vector3.back, Vector3.up, Vector3.right, h.z, h.y, h.x, b.size.z, b.size.y);    // +x
                AddFace(verts, normals, uvs, tris, b.center, rot, Vector3.forward, Vector3.up, Vector3.left, h.z, h.y, h.x, b.size.z, b.size.y);  // -x
                AddFace(verts, normals, uvs, tris, b.center, rot, Vector3.right, Vector3.back, Vector3.up, h.x, h.z, h.y, b.size.x, b.size.z);    // +y
                AddFace(verts, normals, uvs, tris, b.center, rot, Vector3.right, Vector3.forward, Vector3.down, h.x, h.z, h.y, b.size.x, b.size.z); // -y
            }
            var mesh = new Mesh { name = "DeliveryCrate_" + part };
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }

        // One quad: u/v axes span the face, n is its outward normal; half sizes hu/hv, offset hn along n.
        // UVs are in meters (x2) so the wood grain is the same scale on every plank.
        static void AddFace(List<Vector3> verts, List<Vector3> normals, List<Vector2> uvs, List<int> tris, Vector3 c, Quaternion rot,
            Vector3 u, Vector3 v, Vector3 n, float hu, float hv, float hn, float su, float sv)
        {
            var i = verts.Count;
            var nn = rot * n;
            var corners = new[] { -u * hu - v * hv, u * hu - v * hv, u * hu + v * hv, -u * hu + v * hv };
            for (var k = 0; k < 4; k++)
            {
                verts.Add(c + rot * (corners[k] + n * hn));
                normals.Add(nn);
            }
            uvs.Add(new Vector2(0f, 0f));
            uvs.Add(new Vector2(su * 2f, 0f));
            uvs.Add(new Vector2(su * 2f, sv * 2f));
            uvs.Add(new Vector2(0f, sv * 2f));
            // Clockwise as seen from outside (Unity's front faces), given u x v = n for every face.
            tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
            tris.Add(i); tris.Add(i + 2); tris.Add(i + 3);
        }

        static Material CopyMat(string sourceName, string newName, Color color, Vector4? scroll = null)
        {
            var path = MatFolder + "/" + newName + ".mat";
            var src = AssetDatabase.LoadAssetAtPath<Material>(MatFolder + "/" + sourceName + ".mat");
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            Material m;
            if (existing != null)
            {
                m = existing;
                if (src != null)
                    m.CopyPropertiesFromMaterial(src);
            }
            else
            {
                m = src != null ? new Material(src) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, path);
            }
            if (m.HasProperty("_BaseColor"))
                m.SetColor("_BaseColor", color);
            if (scroll.HasValue && m.HasProperty("_ScrollSpeed"))
                m.SetVector("_ScrollSpeed", scroll.Value);
            EditorUtility.SetDirty(m);
            return m;
        }

        static Mesh SaveMesh(Mesh mesh, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(mesh, path);
                return mesh;
            }
            EditorUtility.CopySerialized(mesh, existing);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        static void SavePrefab(GameObject root, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        // ------------------------------------------------------------------ menu: remove hidden content

        [MenuItem("Tools/Steven/Remove Hidden Kitchen Content")]
        public static void RemoveHidden()
        {
            var scene = SceneManager.GetActiveScene();
            var simplifier = FindFirst<KitchenSimplifier>(scene);
            if (simplifier == null)
            {
                EditorUtility.DisplayDialog("Remove Hidden Content", "No Kitchen Simplifier in this scene. Open StevenScene and run the MP2C setup first.", "OK");
                return;
            }
            if (!EditorUtility.DisplayDialog("Remove Hidden Content",
                    "This permanently deletes from this scene everything the Kitchen Simplifier hides:\n" +
                    "unused recipe tickets and CALL A COOK cards, catalog miniatures, tools, rat holes and mouse traps, " +
                    "grease-fire hazards, hidden Pantry Board/HUD rows and the rat/produce/mushroom spawners.\n\n" +
                    "Content that unlocks later in the learning curve is kept. Continue?", "Delete", "Cancel"))
                return;

            var log = new List<string>();
            var progression = FindFirst<KitchenProgression>(scene);
            var keep = new HashSet<GameObject>();
            if (progression != null)
                foreach (var g in progression.goals)
                    foreach (var o in g.unlocks)
                        if (o != null)
                            keep.Add(o);

            // Recipe tickets and CALL A COOK cards nobody can ever use.
            var used = new HashSet<string>(simplifier.startRecipes) { "hire_cook" };
            foreach (var st in FindAll<CraftingStation>(scene))
            {
                var stationUsed = false;
                for (var i = 0; i < st.recipeIds.Count && i < st.recipeCards.Count; i++)
                {
                    var card = st.recipeCards[i];
                    if (card == null)
                        continue;
                    if (used.Contains(st.recipeIds[i]) || keep.Contains(card.gameObject))
                    {
                        stationUsed = true;
                        continue;
                    }
                    Object.DestroyImmediate(card.gameObject);
                    st.recipeCards[i] = null;
                    log.Add("ticket " + st.recipeIds[i] + " (" + st.name + ")");
                }
                if (!stationUsed && st.callCookCard != null && !keep.Contains(st.callCookCard.gameObject))
                {
                    Object.DestroyImmediate(st.callCookCard.gameObject);
                    st.callCookCard = null;
                    log.Add("CALL A COOK (" + st.name + ")");
                }
                EditorUtility.SetDirty(st);
            }

            // Catalog miniatures outside the short catalog (and their shelf slots).
            foreach (var mini in FindAll<BlueprintMini>(scene))
            {
                if (simplifier.catalog.Contains(mini.buildingId))
                    continue;
                if (mini.homeAnchor != null)
                    Object.DestroyImmediate(mini.homeAnchor.gameObject);
                log.Add("catalog " + mini.buildingId);
                Object.DestroyImmediate(mini.gameObject);
            }

            // Tools.
            foreach (var toolName in new[] { "WateringCan", "RollingPin", "Wrench", "DemolitionMallet", "SanitizerSpray", "FireExtinguisher", "KitchenShears", "Cleaver", "Crowbar", "HarvestBasket" })
            {
                if (simplifier.tools.Contains(toolName))
                    continue;
                var t = Find(scene, toolName);
                if (t != null)
                {
                    Object.DestroyImmediate(t.gameObject);
                    log.Add("tool " + toolName);
                }
            }

            // Rats: holes, traps and the spawner channels for rats, loose produce and mushrooms.
            foreach (var t in FindAllTransforms(scene))
            {
                if (t == null)
                    continue;
                if (t.name.StartsWith("Rat Hole") || t.name.StartsWith("Mouse Trap ("))
                {
                    log.Add(t.name);
                    Object.DestroyImmediate(t.gameObject);
                }
            }
            var wild = FindFirst<WildSpawner>(scene);
            if (wild != null)
            {
                var removed = wild.channels.RemoveAll(ch => !simplifier.keepSpawnChannels.Contains(ch.name));
                log.Add(removed + " spawn channel(s)");
                EditorUtility.SetDirty(wild);
            }

            // Grease-fire hazards on stations.
            if (simplifier.noGreaseFires)
                foreach (var f in FindAll<FireHazard>(scene))
                {
                    log.Add("fire hazard (" + f.name + ")");
                    Object.DestroyImmediate(f);
                }

            // Pantry Board and HUD rows that aren't shown.
            foreach (var board in FindAll<ResourceBoard>(scene))
            {
                var kept = new List<ResourceBoard.Row>();
                foreach (var row in board.rows)
                {
                    if (simplifier.shownResources.Contains(row.id))
                    {
                        kept.Add(row);
                        continue;
                    }
                    var rowObject = row.flash != null ? row.flash.transform.parent : (row.value != null ? row.value.transform.parent : null);
                    if (rowObject != null && rowObject != board.transform)
                        Object.DestroyImmediate(rowObject.gameObject);
                    log.Add("board row " + row.id);
                }
                board.rows = kept;
                EditorUtility.SetDirty(board);
            }

            Save(scene);
            Debug.Log("[MP2C] Removed " + log.Count + " hidden items:\n" + string.Join("\n", log));
            EditorUtility.DisplayDialog("Remove Hidden Content", "Removed " + log.Count + " items (details in the Console). Scene saved.", "OK");
        }

        // ------------------------------------------------------------------ helpers

        static void Save(Scene scene)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        static void Remove(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == name)
                    Object.DestroyImmediate(root);
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

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            var parts = path.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
