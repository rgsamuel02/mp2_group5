using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Steven.MP2A
{
    /// <summary>
    /// "First Shift": a Plants-vs-Zombies style opening tutorial for the core loop
    /// (guest orders -> cook the dish -> poke the order ticket -> carry the plate to the table).
    ///
    /// Round 1: a gold arrow points at the one thing to do and a single line says what to do; each step done gets a
    /// "Yes, Chef!". Round 2: arrows only with short lines. Round 3: on your own (the arrow only comes back if the
    /// player is stuck for a while). Then the normal goals start.
    ///
    /// No popup cards: while it runs, the one-time tip cards and the goal cards are held back, and only one guest at a
    /// time is seated (with patience frozen in the guided rounds, so nobody walks out while you learn).
    /// The step shown is worked out from the game state every frame, so mistakes (a dropped plate, running out of
    /// vegetables) simply send the arrow back to the right place.
    /// </summary>
    [DefaultExecutionOrder(-800)]
    public class FirstShiftCoach : MonoBehaviour
    {
        public bool runTutorial = true;
        public KitchenProgression progression;
        [Tooltip("Gold arrow material (created by the setup menu). Empty = a default material.")]
        public Material arrowMaterial;
        public Font font;
        public int rounds = 3;
        [Tooltip("Round 3: seconds stuck on a step before the arrow comes back as a hint.")]
        public float hintDelay = 12f;
        [Tooltip("Tip cards marked as seen when the tutorial ends (it already taught them).")]
        public List<string> taughtTips = new List<string>
            { "welcome", "customer", "table", "collision", "crafting", "heat", "conveyor", "plate_delivery", "order", "orders", "order_rail" };

        enum Phase { WaitGuest, Supply, Cook, Cooking, Plate, PickUp, Deliver, Served }

        static bool s_Done;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => s_Done = false;

        CustomerSpawner m_Spawner;
        TutorialManager m_Tips;
        bool m_TipsWereOn;
        DiningTable m_Table;
        float m_TableDrain = -1f;
        int m_Round;
        Phase m_Phase;
        float m_PhaseTime;
        float m_CoversAtStart;
        bool m_Running;

        Transform m_Arrow;
        Transform m_Strip;
        Text m_StripText;
        Vector3 m_ArrowTarget;
        bool m_ArrowOn;

        public static bool Active { get; private set; }

        void Awake()
        {
            if (!runTutorial || s_Done || KitchenSave.HasSave)
            {
                s_Done = true;
                enabled = false;
                return;
            }
            if (progression == null)
                progression = FindAnyObjectByType<KitchenProgression>();
            if (progression != null)
                progression.paused = true;
            Active = true;
        }

        void Start()
        {
            if (!enabled)
                return;
            var game = KitchenGame.Instance;
            m_Spawner = game != null ? game.customers : null;
            m_Tips = game != null ? game.tutorials : null;
            if (m_Spawner != null)
                m_Spawner.spawningEnabled = false;
            if (m_Tips != null)
            {
                m_TipsWereOn = m_Tips.tutorialsEnabled;
                m_Tips.tutorialsEnabled = false;
            }
            BuildVisuals();
            StartCoroutine(Run());
        }

        void OnDestroy()
        {
            // Leaving the kitchen mid-tutorial: don't make the player sit through it again.
            if (m_Running && m_Round >= 2)
                s_Done = true;
            Active = false;
        }

        // ------------------------------------------------------------------ flow

        IEnumerator Run()
        {
            m_Running = true;
            yield return new WaitForSeconds(1.5f);
            Say("FIRST SHIFT - let's serve your first guest.", false);
            yield return new WaitForSeconds(3.5f);

            for (m_Round = 1; m_Round <= rounds; m_Round++)
            {
                m_Table = null;
                m_CoversAtStart = Res != null ? Res.Get(ResourceId.CoversServed) : 0f;
                if (m_Round == rounds)
                {
                    Say("Your turn: serve the next guest on your own.", false);
                    yield return new WaitForSeconds(3f);
                }

                // Seat exactly one guest.
                var tries = 0;
                while (m_Spawner != null && !m_Spawner.SpawnArrival())
                {
                    if (++tries > 15)
                    {
                        Finish("No free table with a place setting - skipping the tutorial.");
                        yield break;
                    }
                    yield return new WaitForSeconds(2f);
                }

                SetPhase(Phase.WaitGuest);
                var wait = new WaitForSeconds(0.15f);   // the arrow animates every frame; the state is checked ~7x a second
                while (m_Phase != Phase.Served)
                {
                    Step();
                    yield return wait;
                }
                RestorePatience();
                Praise(m_Table != null ? m_Table.transform.position + Vector3.up * 1.2f : m_ArrowTarget,
                    m_Round == 1 ? "FIRST COVER SERVED!" : m_Round == rounds ? "Perfect service!" : "Yes, Chef! Served!", true);
                ArrowOff();
                Say(m_Round == 1 ? "That's the whole job. Once more - follow the arrows." : m_Round < rounds ? "Great. Next one's yours." : "", false);
                yield return new WaitForSeconds(m_Round < rounds ? 3.5f : 1.5f);
            }
            Finish("Kitchen's open! Keep serving to reach your goals.");
        }

        void Finish(string line)
        {
            m_Running = false;
            s_Done = true;
            Active = false;
            ArrowOff();
            RestorePatience();
            Say(line, false);
            if (m_Spawner != null)
                m_Spawner.spawningEnabled = true;
            if (m_Tips != null)
            {
                foreach (var id in taughtTips)
                    m_Tips.Suppress(id);
                m_Tips.tutorialsEnabled = m_TipsWereOn;
            }
            if (progression != null)
                progression.Resume();
            StartCoroutine(HideStripLater(4f));
        }

        IEnumerator HideStripLater(float t)
        {
            yield return new WaitForSeconds(t);
            if (m_Strip != null)
                m_Strip.gameObject.SetActive(false);
        }

        /// <summary>Works out what the player should do next from the game state, and shows it.</summary>
        void Step()
        {
            var res = Res;
            if (m_Table == null)
            {
                foreach (var t in DiningTable.All)
                    if (t != null && t.HasOrder)
                    {
                        m_Table = t;
                        break;
                    }
                if (m_Table == null)
                {
                    Show(Phase.WaitGuest, null, "A guest is on the way to a table...", "A guest is coming...");
                    return;
                }
                if (m_Round < rounds)
                {
                    m_TableDrain = m_Table.patienceDrain;
                    m_Table.patienceDrain = 0f;
                }
            }

            // The guest walked out before being served: seat a new one and carry on.
            var served = res != null && res.Get(ResourceId.CoversServed) > m_CoversAtStart + 0.5f;
            if (!served && !m_Table.HasOrder)
            {
                RestorePatience();
                m_Table = null;
                if (m_Spawner != null)
                    m_Spawner.SpawnArrival();
                SetPhase(Phase.WaitGuest);
                return;
            }

            var dish = m_Table.Order;
            var dishName = KitchenGame.ResourceName(dish);
            FindRecipe(dish, out var station, out var cardIndex, out var recipe);
            var plate = FindPlate(m_Table);

            Phase next;
            if (served)
                next = Phase.Served;
            else if (plate != null && plate.grab != null && plate.grab.isSelected)
                next = Phase.Deliver;
            else if (plate != null || m_Table.PlateOut)
                next = Phase.PickUp;
            else if (res != null && res.GetInt(dish) > 0)
                next = Phase.Plate;
            else if (station != null && station.IsBusy && station.ActiveRecipe == recipe)
                next = Phase.Cooking;
            else if (recipe != null && res != null && !res.Has(recipe.inputs))
                next = Phase.Supply;
            else
                next = Phase.Cook;

            // Praise each step forward (not the waits).
            if (next > m_Phase && m_Phase != Phase.WaitGuest && m_Phase != Phase.Cooking && next != Phase.Served && m_Round < rounds)
                Praise(m_ArrowTarget, m_Phase == Phase.Plate ? "Order up!" : "Yes, Chef!", false);

            var tableNo = m_Table.tableNumber;
            switch (next)
            {
                case Phase.Supply:
                {
                    FindSupply(recipe, out var board, out var supplyCard);
                    Show(next, supplyCard != null ? supplyCard.transform : board != null ? board.transform : null,
                        "Out of vegetables! Press PRODUCE BOX on the Supplier Board.", "Restock at the Supplier Board.");
                    break;
                }
                case Phase.Cook:
                    Show(next, CardTransform(station, cardIndex),
                        $"Table {tableNo} wants {dishName}. Press the {dishName.ToUpperInvariant()} ticket on the grill.",
                        $"Cook the {dishName}.");
                    break;
                case Phase.Cooking:
                    Show(next, CardTransform(station, cardIndex), "It's cooking - wait for it...", "Cooking...");
                    break;
                case Phase.Plate:
                {
                    var rail = KitchenGame.Instance != null ? KitchenGame.Instance.orderRail : null;
                    var slot = rail != null ? rail.SlotFor(m_Table) : null;
                    Show(next, slot != null ? slot.transform : rail != null ? rail.transform : null,
                        $"Done! Poke Table {tableNo}'s ticket on the rail to plate it.", "Poke the ticket to plate it.");
                    break;
                }
                case Phase.PickUp:
                    Show(next, plate != null ? plate.transform : null,
                        "Grab the plate when it reaches the end of the pass.", "Grab the plate.");
                    break;
                case Phase.Deliver:
                    Show(next, m_Table.transform, $"Set it down on Table {tableNo}.", $"To Table {tableNo}.");
                    break;
                case Phase.Served:
                    SetPhase(next);
                    break;
            }
        }

        void Show(Phase phase, Transform target, string fullLine, string shortLine)
        {
            if (phase != m_Phase)
                SetPhase(phase);
            var guided = m_Round < rounds;
            var stuck = !guided && Time.time - m_PhaseTime > hintDelay;
            if (guided || stuck)
            {
                Say(m_Round == 1 ? fullLine : shortLine, true);
                if (target != null)
                    ArrowAt(target);
                else
                    ArrowOff();
            }
            else
            {
                ArrowOff();
                if (m_StripText != null && m_StripText.text.Length > 0 && Time.time - m_PhaseTime > 3f)
                    Say("", false);
            }
        }

        void SetPhase(Phase p)
        {
            m_Phase = p;
            m_PhaseTime = Time.time;
        }

        void RestorePatience()
        {
            if (m_Table != null && m_TableDrain >= 0f)
                m_Table.patienceDrain = m_TableDrain;
            m_TableDrain = -1f;
        }

        // ------------------------------------------------------------------ lookups

        static PlayerResources Res => KitchenGame.Res;

        static void FindRecipe(ResourceId dish, out CraftingStation station, out int cardIndex, out RecipeInfo recipe)
        {
            station = null;
            cardIndex = -1;
            recipe = null;
            var db = KitchenGame.Db;
            if (db == null)
                return;
            foreach (var st in FindObjectsByType<CraftingStation>())
            {
                for (var i = 0; i < st.recipeIds.Count && i < st.recipeCards.Count; i++)
                {
                    var r = db.GetRecipe(st.recipeIds[i]);
                    if (r == null || st.recipeCards[i] == null || !st.recipeCards[i].gameObject.activeInHierarchy)
                        continue;
                    foreach (var o in r.outputs)
                    {
                        if (o.id != dish)
                            continue;
                        station = st;
                        cardIndex = i;
                        recipe = r;
                        return;
                    }
                }
            }
        }

        static void FindSupply(RecipeInfo needs, out CraftingStation board, out PressableCard card)
        {
            board = null;
            card = null;
            var db = KitchenGame.Db;
            var res = Res;
            if (db == null || needs == null || res == null)
                return;
            foreach (var st in FindObjectsByType<CraftingStation>())
            {
                for (var i = 0; i < st.recipeIds.Count && i < st.recipeCards.Count; i++)
                {
                    var r = db.GetRecipe(st.recipeIds[i]);
                    if (r == null || r.duration > 0f || st.recipeCards[i] == null || !st.recipeCards[i].gameObject.activeInHierarchy)
                        continue;
                    foreach (var o in r.outputs)
                        foreach (var need in needs.inputs)
                            if (o.id == need.id && !res.Has(need.id, need.amount))
                            {
                                board = st;
                                card = st.recipeCards[i];
                                return;
                            }
                }
            }
        }

        static Transform CardTransform(CraftingStation st, int index)
        {
            if (st == null)
                return null;
            if (index >= 0 && index < st.recipeCards.Count && st.recipeCards[index] != null)
                return st.recipeCards[index].transform;
            return st.transform;
        }

        static Plate FindPlate(DiningTable table)
        {
            foreach (var p in FindObjectsByType<Plate>())
                if (p != null && p.table == table)
                    return p;
            return null;
        }

        // ------------------------------------------------------------------ visuals

        void Praise(Vector3 at, string text, bool big)
        {
            KitchenGame.Float(at + Vector3.up * 0.15f, text, new Color(1f, 0.85f, 0.3f), big ? 1.6f : 1.1f);
            KitchenGame.Blast(at, new Color(1f, 0.8f, 0.3f), big ? 1.2f : 0.5f);
        }

        void Say(string line, bool keep)
        {
            if (m_StripText == null)
                return;
            if (m_StripText.text != line)
                m_StripText.text = line;
            var show = !string.IsNullOrEmpty(line);
            if (m_Strip.gameObject.activeSelf != show)
                m_Strip.gameObject.SetActive(show);
        }

        void ArrowAt(Transform target)
        {
            var size = 0.25f;
            var r = target.GetComponentInChildren<Renderer>();
            if (r != null)
                size = Mathf.Clamp(r.bounds.extents.y, 0.05f, 0.6f);
            m_ArrowTarget = target.position + Vector3.up * (size + 0.12f);
            m_ArrowOn = true;
            if (!m_Arrow.gameObject.activeSelf)
            {
                m_Arrow.gameObject.SetActive(true);
                m_Arrow.position = m_ArrowTarget;
            }
        }

        void ArrowOff()
        {
            m_ArrowOn = false;
            if (m_Arrow != null && m_Arrow.gameObject.activeSelf)
                m_Arrow.gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            if (m_Arrow != null && m_ArrowOn)
            {
                // Glide to the target, bob up and down and spin slowly so it reads from any side.
                var bob = Mathf.Sin(Time.time * 5f) * 0.035f;
                m_Arrow.position = Vector3.Lerp(m_Arrow.position, m_ArrowTarget + Vector3.up * bob, 1f - Mathf.Exp(-10f * Time.deltaTime));
                m_Arrow.rotation = Quaternion.Euler(0f, Time.time * 90f, 0f);
            }

            var game = KitchenGame.Instance;
            var head = game != null ? game.PlayerHead : null;
            if (m_Strip != null && m_Strip.gameObject.activeSelf && head != null)
            {
                // A single subtitle-style line that lazily follows the view (low and in front, never a card).
                var fwd = head.forward;
                fwd.y = 0f;
                fwd = fwd.sqrMagnitude > 0.001f ? fwd.normalized : Vector3.forward;
                var goal = head.position + fwd * 1.15f + Vector3.down * 0.32f;
                m_Strip.position = Vector3.Lerp(m_Strip.position, goal, 1f - Mathf.Exp(-4f * Time.deltaTime));
                var look = m_Strip.position - head.position;
                look.y = 0f;
                if (look.sqrMagnitude > 0.001f)
                    m_Strip.rotation = Quaternion.LookRotation(look.normalized, Vector3.up);
            }

#if UNITY_EDITOR
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (m_Running && kb != null && kb.f8Key.wasPressedThisFrame)
            {
                StopAllCoroutines();
                Finish("Tutorial skipped.");
            }
#endif
        }

        void BuildVisuals()
        {
            // Arrow: a low-poly gold arrow pointing down (shaft + head), built in code.
            var arrow = new GameObject("First Shift Arrow");
            arrow.transform.SetParent(transform, false);
            var mf = arrow.AddComponent<MeshFilter>();
            mf.sharedMesh = ArrowMesh();
            var mr = arrow.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (arrowMaterial != null)
            {
                mr.sharedMaterial = arrowMaterial;
            }
            else
            {
                var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                mr.sharedMaterial = tmp.GetComponent<MeshRenderer>().sharedMaterial;
                Destroy(tmp);
                var block = new MaterialPropertyBlock();
                block.SetColor("_BaseColor", new Color(1f, 0.8f, 0.2f));
                block.SetColor("_Color", new Color(1f, 0.8f, 0.2f));
                mr.SetPropertyBlock(block);
            }
            m_Arrow = arrow.transform;
            arrow.SetActive(false);

            // Subtitle strip: one line of text on a dark band.
            var strip = new GameObject("First Shift Line", typeof(RectTransform), typeof(Canvas));
            strip.transform.SetParent(transform, false);
            var rt = (RectTransform)strip.transform;
            rt.sizeDelta = new Vector2(1400f, 150f);
            rt.localScale = Vector3.one * 0.0005f;
            strip.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            strip.GetComponent<Canvas>().sortingOrder = 50;

            var band = new GameObject("Band", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            band.transform.SetParent(strip.transform, false);
            Stretch((RectTransform)band.transform);
            band.GetComponent<Image>().color = new Color(0.08f, 0.06f, 0.05f, 0.82f);
            band.GetComponent<Image>().raycastTarget = false;

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textGo.transform.SetParent(strip.transform, false);
            var trt = (RectTransform)textGo.transform;
            Stretch(trt);
            trt.offsetMin = new Vector2(30f, 10f);
            trt.offsetMax = new Vector2(-30f, -10f);
            m_StripText = textGo.GetComponent<Text>();
            m_StripText.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            m_StripText.fontSize = 64;
            m_StripText.resizeTextForBestFit = true;
            m_StripText.resizeTextMinSize = 30;
            m_StripText.resizeTextMaxSize = 64;
            m_StripText.alignment = TextAnchor.MiddleCenter;
            m_StripText.color = new Color(1f, 0.93f, 0.75f);
            m_StripText.raycastTarget = false;
            m_StripText.text = "";
            m_Strip = strip.transform;
            strip.SetActive(false);

            var head = KitchenGame.Instance != null ? KitchenGame.Instance.PlayerHead : null;
            if (head != null)
                m_Strip.position = head.position + head.forward * 1.15f + Vector3.down * 0.32f;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>8-sided shaft and cone tip, tip at the origin pointing down, about 0.22 m tall.</summary>
        static Mesh ArrowMesh()
        {
            const int sides = 8;
            var verts = new List<Vector3>();
            var tris = new List<int>();
            void Ring(float y, float r)
            {
                for (var i = 0; i < sides; i++)
                {
                    var a = i * Mathf.PI * 2f / sides;
                    verts.Add(new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r));
                }
            }
            // rings: 0 head base (wide), 1 head base (shaft radius), 2 shaft top
            Ring(0.09f, 0.06f);
            Ring(0.09f, 0.022f);
            Ring(0.22f, 0.022f);
            var tip = verts.Count;
            verts.Add(Vector3.zero);
            var top = verts.Count;
            verts.Add(new Vector3(0f, 0.22f, 0f));
            // Each triangle is wound so its face points along the given outward direction (Unity: clockwise = front).
            void Tri(int a, int b, int c, Vector3 outward)
            {
                var n = Vector3.Cross(verts[b] - verts[a], verts[c] - verts[a]);
                if (Vector3.Dot(n, outward) < 0f)
                {
                    var t = b;
                    b = c;
                    c = t;
                }
                tris.Add(a); tris.Add(b); tris.Add(c);
            }
            Vector3 Radial(int a, int b, int c, float y)
            {
                var m = (verts[a] + verts[b] + verts[c]) / 3f;
                return new Vector3(m.x, y, m.z);
            }
            for (var i = 0; i < sides; i++)
            {
                var j = (i + 1) % sides;
                Tri(tip, i, j, Radial(tip, i, j, -0.05f));                               // cone
                Tri(i, j, sides + j, Vector3.up);                                         // back of the arrow head
                Tri(i, sides + j, sides + i, Vector3.up);
                Tri(sides + i, sides + j, 2 * sides + j, Radial(sides + i, sides + j, 2 * sides + j, 0f));   // shaft
                Tri(sides + i, 2 * sides + j, 2 * sides + i, Radial(sides + i, 2 * sides + j, 2 * sides + i, 0f));
                Tri(top, 2 * sides + i, 2 * sides + j, Vector3.up);                       // cap
            }
            var mesh = new Mesh { name = "FirstShiftArrow" };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
