using System;
using System.Collections.Generic;
using UnityEngine;

namespace Steven.MP2A
{
    /// <summary>
    /// MP2B progression for the kitchen:
    /// - Mastery Thresholds: goals checked in order; content in a goal's "unlocks" list stays hidden until it clears.
    /// - Unlock Reveal: when a goal clears, an "UNLOCKED" card introduces the new content's metaphor first,
    ///   and the content itself appears a few seconds later.
    /// - Thresholds on the Horizon: a "NEXT GOAL" card that only shows the next target after the previous one clears.
    /// - Sandbox Feedback: after an unlock, a small practice target with its own reinforcing feedback.
    /// - Achievements: conditions checked all the time; meeting one puts a trophy on the shelf.
    /// </summary>
    public class KitchenProgression : MonoBehaviour
    {
        [Serializable]
        public class Goal
        {
            public string name = "Goal";
            [Tooltip("Shown on the NEXT GOAL card, e.g. \"Serve 3 covers\".")]
            public string goalText = "Serve 3 covers";
            public ResourceId resource = ResourceId.CoversServed;
            public float amount = 3f;
            [Tooltip("Optional second condition (both must be met).")]
            public bool useSecondCondition;
            public ResourceId resource2 = ResourceId.Hygiene;
            public float amount2 = 60f;

            [Header("Unlock reveal (shown BEFORE the content appears)")]
            public string revealTitle = "UNLOCKED";
            [TextArea(2, 5)] public string revealBody = "";
            [Tooltip("Objects hidden until this goal clears (boards, catalog miniatures, partner intakes...). Don't put grid buildings here.")]
            public List<GameObject> unlocks = new List<GameObject>();

            [Header("Sandbox (optional practice after the unlock)")]
            public bool hasSandbox;
            [Tooltip("Shown with the unlock, e.g. \"Try it: hire a cook at the Staff Board\".")]
            public string sandboxPrompt = "";
            public ResourceId sandboxResource = ResourceId.LineCooks;
            [Tooltip("How much the resource must rise after the unlock.")]
            public float sandboxGain = 1f;
            public string sandboxSuccess = "";

            [Header("Menu")]
            [Tooltip("Dishes diners start ordering once this goal clears (used with the Kitchen Simplifier's menu).")]
            public List<ResourceId> addToMenu = new List<ResourceId>();

            [NonSerialized] public bool cleared;
            [NonSerialized] public bool sandboxDone;
            [NonSerialized] public float sandboxBaseline;
        }

        [Serializable]
        public class Achievement
        {
            public string name = "Achievement";
            public string description = "";
            public ResourceId resource = ResourceId.Gold;
            public float amount = 500f;
            [Tooltip("Optional trophy object (hidden until earned). If empty, a gold trophy is made on the shelf.")]
            public GameObject trophy;
            [NonSerialized] public bool earned;
        }

        public List<Goal> goals = new List<Goal>();
        public List<Achievement> achievements = new List<Achievement>();

        [Header("Where things appear")]
        [Tooltip("Where the NEXT GOAL card stands (e.g. an empty object above the Pass). Empty = in front of the player.")]
        public Transform horizonAnchor;
        [Tooltip("Where earned trophies line up (e.g. an empty object on a wall shelf). Empty = next to the horizon card.")]
        public Transform trophyShelf;
        [Tooltip("Card prefab. Empty = the tutorial card prefab from the TutorialManager.")]
        public TutorialPopup cardPrefab;
        public float revealDelay = 3f;
        public float checkInterval = 0.25f;

        int m_Current;
        float m_Timer;
        TutorialPopup m_HorizonCard;
        int m_TrophyCount;
        PlayerResources m_Res;

        public int GoalsCleared => m_Current;

        void Reset() => LoadDefaults();

        /// <summary>Fills in the default goals and achievements (also used when the component is first added).</summary>
        /// <summary>
        /// MP2C learning curve: one new idea per goal, each building on the last
        /// (cook -> chain two stations -> harvest + buy -> build -> automate -> teammate deliveries -> finale).
        /// </summary>
        public void LoadLearningCurveDefaults()
        {
            goals = new List<Goal>
            {
                new Goal { name = "First Service", goalText = "Serve 2 covers", resource = ResourceId.CoversServed, amount = 2,
                    revealTitle = "UNLOCKED: THE FRY STATION",
                    revealBody = "Some dishes take two stations. Cut potatoes on the cutting board, then fry them in the deep fryer - like a real prep line.",
                    hasSandbox = true, sandboxPrompt = "Try it: cut potatoes on the cutting board.", sandboxResource = ResourceId.RawFries, sandboxGain = 1,
                    sandboxSuccess = "Prepped! Now fry them.", addToMenu = new List<ResourceId> { ResourceId.Fries } },
                new Goal { name = "Lunch Rush", goalText = "Serve 5 covers", resource = ResourceId.CoversServed, amount = 5,
                    revealTitle = "UNLOCKED: STEAK & HERBS",
                    revealBody = "The butcher now delivers beef, and the Grilled Steak ticket is on the grill. Steak needs fresh herbs: snip them from the herb planters with the kitchen shears.",
                    hasSandbox = true, sandboxPrompt = "Try it: snip an herb plant with the shears.", sandboxResource = ResourceId.Herbs, sandboxGain = 1,
                    sandboxSuccess = "Fresh herbs! Steak is ready to grill.", addToMenu = new List<ResourceId> { ResourceId.GrilledSteak } },
                new Goal { name = "Expansion", goalText = "Bank 400 Gold", resource = ResourceId.Gold, amount = 400,
                    revealTitle = "UNLOCKED: THE EQUIPMENT CATALOG",
                    revealBody = "Shop the restaurant-supply catalog. Grab a miniature and release it on the floor to install it. Equipment needs its hookup first: a grill goes on a gas line." },
                new Goal { name = "Dinner Service", goalText = "Serve 10 covers", resource = ResourceId.CoversServed, amount = 10,
                    revealTitle = "UNLOCKED: THE BRIGADE",
                    revealBody = "Hire line cooks at the Staff Board. Press CALL A COOK on a station: a cook walks over and keeps cooking that ticket for you.",
                    hasSandbox = true, sandboxPrompt = "Try it: hire a cook at the Staff Board.", sandboxResource = ResourceId.LineCooks, sandboxGain = 1,
                    sandboxSuccess = "Yes, Chef! Your brigade grew." },
                new Goal { name = "Partner Deliveries", goalText = "Serve 14 covers", resource = ResourceId.CoversServed, amount = 14,
                    revealTitle = "UNLOCKED: PARTNER DELIVERIES",
                    revealBody = "Your other businesses now supply the kitchen: garden produce and orchestra coins go in the pantry chute, cafe coffee and roses go on a dining table." },
                new Goal { name = "Grand Opening", goalText = "Serve 20 covers with Reputation 25+", resource = ResourceId.CoversServed, amount = 20,
                    useSecondCondition = true, resource2 = ResourceId.Reputation, amount2 = 25,
                    revealTitle = "THE KITCHEN IS A HIT", revealBody = "A full dining room and a strong reputation. The restaurant's part of the Grand Opening is complete!" },
            };
            achievements = new List<Achievement>
            {
                new Achievement { name = "First Service", description = "Served your first cover", resource = ResourceId.CoversServed, amount = 1 },
                new Achievement { name = "Deep Pockets", description = "Hold 500 Gold", resource = ResourceId.Gold, amount = 500 },
                new Achievement { name = "Regulars", description = "Reputation 20 or more", resource = ResourceId.Reputation, amount = 20 },
            };
        }

        public void LoadDefaults()
        {
            goals = new List<Goal>
            {
                new Goal { name = "Lunch Rush", goalText = "Serve 2 covers", resource = ResourceId.CoversServed, amount = 2,
                    revealTitle = "UNLOCKED: THE BRIGADE", revealBody = "The Staff Board is open. Hire line cooks: they walk the mats to your stations and join the crew, like a real kitchen brigade.",
                    hasSandbox = true, sandboxPrompt = "Try it: hire a cook at the Staff Board.", sandboxResource = ResourceId.LineCooks, sandboxGain = 1, sandboxSuccess = "Yes, Chef! Your brigade grew." },
                new Goal { name = "Dinner Service", goalText = "Serve 5 covers", resource = ResourceId.CoversServed, amount = 5,
                    revealTitle = "UNLOCKED: THE HOT LINE", revealBody = "New equipment in the catalog: the Char Grill and Deep Fryer. Hook them up to a gas line like a real hot line.",
                    hasSandbox = true, sandboxPrompt = "Try it: cook any dish.", sandboxResource = ResourceId.CoversServed, sandboxGain = 1, sandboxSuccess = "Order up! The line is getting hot." },
                new Goal { name = "Fine Dining", goalText = "Bank 400 Gold", resource = ResourceId.Gold, amount = 400,
                    revealTitle = "UNLOCKED: THE PASTRY SECTION", revealBody = "The Convection Oven joins the catalog. Bake Beef Wellington and souffles for the big spenders." },
                new Goal { name = "Partner Deliveries", goalText = "Serve 8 covers with Hygiene 60+", resource = ResourceId.CoversServed, amount = 8,
                    useSecondCondition = true, resource2 = ResourceId.Hygiene, amount2 = 60,
                    revealTitle = "UNLOCKED: PARTNER DELIVERIES", revealBody = "Your other businesses now supply the kitchen: garden produce goes in the pantry chute, and cafe coffee and orchestra music keep diners happy.",
                    hasSandbox = true, sandboxPrompt = "Try it: drop a garden tomato in the pantry chute.", sandboxResource = ResourceId.Tomato, sandboxGain = 1, sandboxSuccess = "Fresh from the garden! Partner delivery received." },
                new Goal { name = "Grand Opening", goalText = "Serve 12 covers with Reputation 25+", resource = ResourceId.CoversServed, amount = 12,
                    useSecondCondition = true, resource2 = ResourceId.Reputation, amount2 = 25,
                    revealTitle = "THE KITCHEN IS A HIT", revealBody = "A full dining room and a glowing reputation. The restaurant's part of the Grand Opening is complete!" },
            };
            achievements = new List<Achievement>
            {
                new Achievement { name = "First Service", description = "Served your first cover", resource = ResourceId.CoversServed, amount = 1 },
                new Achievement { name = "Full Brigade", description = "Employ 4 line cooks", resource = ResourceId.LineCooks, amount = 4 },
                new Achievement { name = "Deep Pockets", description = "Hold 500 Gold", resource = ResourceId.Gold, amount = 500 },
                new Achievement { name = "Regulars", description = "Reputation 20 or more", resource = ResourceId.Reputation, amount = 20 },
                new Achievement { name = "Busy Night", description = "Served 10 covers", resource = ResourceId.CoversServed, amount = 10 },
            };
        }

        void Start()
        {
            m_Res = KitchenGame.Res;
            if (cardPrefab == null && KitchenGame.Instance != null && KitchenGame.Instance.tutorials != null)
                cardPrefab = KitchenGame.Instance.tutorials.popupPrefab;

            foreach (var g in goals)
                foreach (var go in g.unlocks)
                    if (go != null)
                        go.SetActive(false);
            foreach (var a in achievements)
                if (a.trophy != null)
                    a.trophy.SetActive(false);

            ShowHorizon();
        }

        void Update()
        {
#if UNITY_EDITOR
            // Demo shortcuts for recording (Editor only): F9 +2 covers, F10 +5 Reputation.
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && KitchenGame.Res != null)
            {
                if (kb.f9Key.wasPressedThisFrame) KitchenGame.Res.Add(ResourceId.CoversServed, 2f);
                if (kb.f10Key.wasPressedThisFrame) KitchenGame.Res.Add(ResourceId.Reputation, 5f);
            }
#endif
            m_Timer -= Time.deltaTime;
            if (m_Timer > 0f)
                return;
            m_Timer = checkInterval;
            if (m_Res == null)
                m_Res = KitchenGame.Res;
            if (m_Res == null)
                return;

            if (m_Current < goals.Count && Met(goals[m_Current]))
                Clear(goals[m_Current]);

            foreach (var g in goals)
            {
                if (!g.cleared || !g.hasSandbox || g.sandboxDone)
                    continue;
                if (m_Res.Get(g.sandboxResource) >= g.sandboxBaseline + g.sandboxGain - 0.0001f)
                {
                    g.sandboxDone = true;
                    var p = CardPosition(0.4f);
                    KitchenGame.Float(p, g.sandboxSuccess, new Color(0.5f, 1f, 0.5f), 1.3f);
                    KitchenGame.Blast(p, new Color(0.5f, 1f, 0.5f), 0.8f);
                }
            }

            foreach (var a in achievements)
                if (!a.earned && m_Res.Get(a.resource) >= a.amount - 0.0001f)
                    Earn(a);
        }

        bool Met(Goal g)
        {
            if (m_Res.Get(g.resource) < g.amount - 0.0001f)
                return false;
            return !g.useSecondCondition || m_Res.Get(g.resource2) >= g.amount2 - 0.0001f;
        }

        void Clear(Goal g)
        {
            g.cleared = true;
            var db = KitchenGame.Db;
            if (db != null && db.orderFilter != null)
                foreach (var dish in g.addToMenu)
                    db.orderFilter.Add(dish);
            m_Current++;
            var pos = CardPosition(0f);
            KitchenGame.Blast(pos, new Color(1f, 0.85f, 0.3f), 1.2f);
            KitchenGame.Float(pos + Vector3.up * 0.5f, "GOAL CLEARED: " + g.name, new Color(1f, 0.85f, 0.3f), 1.4f);

            // Reveal first (introduces the metaphor), content appears after the delay.
            var body = g.revealBody;
            if (g.hasSandbox && !string.IsNullOrEmpty(g.sandboxPrompt))
                body += "\n\n" + g.sandboxPrompt;
            ShowCard(g.revealTitle, body, pos, 14f, "UNLOCKED");
            StartCoroutine(UnlockAfterDelay(g));

            if (m_HorizonCard != null)
                m_HorizonCard.Dismiss();
            m_HorizonCard = null;
            Invoke(nameof(ShowHorizon), revealDelay + 1f);
        }

        System.Collections.IEnumerator UnlockAfterDelay(Goal g)
        {
            yield return new WaitForSeconds(revealDelay);
            foreach (var go in g.unlocks)
            {
                if (go == null)
                    continue;
                go.SetActive(true);
                KitchenGame.Blast(go.transform.position + Vector3.up * 0.3f, new Color(1f, 0.85f, 0.3f), 0.6f);
            }
            if (g.hasSandbox && m_Res != null)
                g.sandboxBaseline = m_Res.Get(g.sandboxResource);
        }

        void ShowHorizon()
        {
            if (m_Current >= goals.Count)
                return;
            var g = goals[m_Current];
            var pos = horizonAnchor != null ? horizonAnchor.position : CardPosition(0f) + Vector3.right * 0.8f;
            m_HorizonCard = ShowCard("NEXT GOAL: " + g.name, g.goalText, pos, 100000f, "GOAL " + (m_Current + 1) + " OF " + goals.Count);
        }

        void Earn(Achievement a)
        {
            var spot = PlaceTrophy(a);
            KitchenGame.Blast(spot + Vector3.up * 0.2f, new Color(1f, 0.8f, 0.2f), 0.7f);
            KitchenGame.Float(spot + Vector3.up * 0.45f, "ACHIEVEMENT: " + a.name + "\n" + a.description, new Color(1f, 0.85f, 0.3f), 1.2f);
        }

        Vector3 PlaceTrophy(Achievement a)
        {
            a.earned = true;
            Vector3 spot;
            if (a.trophy != null)
            {
                a.trophy.SetActive(true);
                spot = a.trophy.transform.position;
            }
            else
            {
                var basePos = trophyShelf != null ? trophyShelf.position
                    : (horizonAnchor != null ? horizonAnchor.position + Vector3.down * 0.6f : CardPosition(0f) + Vector3.left * 0.8f);
                var right = trophyShelf != null ? trophyShelf.right : Vector3.right;
                spot = basePos + right * (0.25f * m_TrophyCount);
                MakeTrophy(spot, a.name);
            }
            m_TrophyCount++;
            return spot;
        }

        /// <summary>
        /// Quietly fast-forwards to a saved point (KitchenSave, when the player comes back to the kitchen): cleared goals
        /// show their unlocks and menu dishes, earned trophies are back on the shelf, and the next goal card is shown.
        /// Call after the resources are restored.
        /// </summary>
        public void RestoreState(int goalsCleared, List<bool> sandboxDone, List<bool> earned)
        {
            if (m_Res == null)
                m_Res = KitchenGame.Res;
            CancelInvoke(nameof(ShowHorizon));
            if (m_HorizonCard != null)
                m_HorizonCard.Dismiss();
            m_HorizonCard = null;

            m_Current = Mathf.Clamp(goalsCleared, 0, goals.Count);
            var db = KitchenGame.Db;
            for (var i = 0; i < m_Current; i++)
            {
                var g = goals[i];
                g.cleared = true;
                g.sandboxDone = sandboxDone != null && i < sandboxDone.Count && sandboxDone[i];
                if (g.hasSandbox && !g.sandboxDone && m_Res != null)
                    g.sandboxBaseline = m_Res.Get(g.sandboxResource);
                if (db != null && db.orderFilter != null)
                    foreach (var dish in g.addToMenu)
                        db.orderFilter.Add(dish);
                foreach (var go in g.unlocks)
                    if (go != null)
                        go.SetActive(true);
            }

            if (earned != null)
                for (var i = 0; i < achievements.Count && i < earned.Count; i++)
                    if (earned[i] && !achievements[i].earned)
                        PlaceTrophy(achievements[i]);

            ShowHorizon();
        }

        static void MakeTrophy(Vector3 pos, string label)
        {
            var root = new GameObject("Trophy - " + label);
            root.transform.position = pos;
            var gold = new Color(1f, 0.78f, 0.2f);
            Piece(root.transform, PrimitiveType.Cube, new Vector3(0f, 0.02f, 0f), new Vector3(0.12f, 0.04f, 0.12f), new Color(0.2f, 0.15f, 0.1f));
            Piece(root.transform, PrimitiveType.Cylinder, new Vector3(0f, 0.08f, 0f), new Vector3(0.03f, 0.04f, 0.03f), gold);
            Piece(root.transform, PrimitiveType.Sphere, new Vector3(0f, 0.17f, 0f), new Vector3(0.11f, 0.1f, 0.11f), gold);
            root.AddComponent<IdleBob>().spinDegreesPerSecond = 40f;
        }

        static void Piece(Transform parent, PrimitiveType type, Vector3 pos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            var col = go.GetComponent<Collider>();
            if (col != null)
                Destroy(col);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                var block = new MaterialPropertyBlock();
                block.SetColor("_BaseColor", color);
                block.SetColor("_Color", color);
                r.SetPropertyBlock(block);
            }
        }

        TutorialPopup ShowCard(string title, string body, Vector3 pos, float lifetime, string counterText)
        {
            if (cardPrefab == null)
            {
                KitchenGame.Float(pos, title + "\n" + body, Color.white, 1.2f);
                return null;
            }
            var card = Instantiate(cardPrefab, pos, Quaternion.identity, transform);
            card.Show(null, title, body, lifetime, 0);
            if (card.counter != null)
                card.counter.text = counterText;
            return card;
        }

        Vector3 CardPosition(float up)
        {
            var game = KitchenGame.Instance;
            var head = game != null ? game.PlayerHead : null;
            if (head == null)
                return new Vector3(0f, 1.6f + up, 0f);
            var f = head.forward;
            f.y = 0f;
            f = f.sqrMagnitude > 0.0001f ? f.normalized : Vector3.forward;
            var p = head.position + f * 1.2f;
            p.y = Mathf.Clamp(head.position.y, 1.2f, 1.9f) + up;
            return p;
        }
    }
}
