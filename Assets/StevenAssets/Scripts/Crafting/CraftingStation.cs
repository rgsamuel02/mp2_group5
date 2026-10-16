using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Steven.MP2A
{
    /// <summary>
    /// Crafting Affordance ("ticket rail" metaphor): each recipe is a kitchen ticket clipped above the station.
    /// Poking/grabbing a ticket fires the recipe: input Resources are spent, the station's Progress stat advances
    /// over time (Euler integration, faster with Heat and Crew), and the output Resources are added on completion.
    /// Boards (supplier, staff) use the same component with instant (duration 0) exchanges.
    /// </summary>
    public class CraftingStation : MonoBehaviour
    {
        public string stationKey;
        public string stationName = "Station";
        public StatBlock stats;

        [Tooltip("Recipe ids from the KitchenDatabase, aligned with recipeCards.")]
        public List<string> recipeIds = new List<string>();
        public List<PressableCard> recipeCards = new List<PressableCard>();
        public PressableCard callCookCard;
        public Text statusLabel;

        [Header("Rules")]
        public bool usesHeat;
        public bool requiresCleanliness = true;
        public float minCleanliness = 15f;
        public float cleanlinessCostPerRecipe = 8f;
        public float minIntegrity = 10f;
        public float oilCostPerRecipe;
        public float minOilQuality = 10f;
        public float crewSpeedBonus = 0.5f;
        public int maxCrew = 3;
        public bool autoRepeatWithCrew = true;

        [Header("Output")]
        public Transform outputPoint;
        public GameObject scrapsPrefab;
        [Range(0f, 1f)] public float scrapsChance = 0.6f;

        RecipeInfo m_Active;
        RecipeInfo m_Last;
        float m_CardTimer;
        bool m_Paused;
        string m_LastMessage;
        float m_MessageTimer;

        public bool IsBusy => m_Active != null;
        public RecipeInfo ActiveRecipe => m_Active;

        KitchenDatabase Db => KitchenGame.Db;

        float Crew => stats != null ? stats.Get(StatId.Crew) : 0f;

        Vector3 OutputPosition => outputPoint != null ? outputPoint.position : transform.position + Vector3.up;

        void Start()
        {
            RefreshCards();
        }

        // -------------------------------------------------------------- persistent listener targets

        /// <summary>Wired from each recipe card's onPressed with its index as the argument.</summary>
        public void StartRecipeAt(int index)
        {
            if (Db == null || index < 0 || index >= recipeIds.Count)
                return;
            var recipe = Db.GetRecipe(recipeIds[index]);
            if (recipe == null)
                return;
            if (!TryStart(recipe, false))
                return;
            m_Last = recipe;
        }

        /// <summary>Wired from the "Call a Line Cook" card.</summary>
        public void CallCook()
        {
            var pop = KitchenGame.Instance != null ? KitchenGame.Instance.population : null;
            if (pop == null)
                return;
            if (Crew >= maxCrew)
            {
                Message($"Crew is full ({maxCrew})");
                return;
            }
            var result = pop.Dispatch(this);
            Message(result);
        }

        // -------------------------------------------------------------- crafting

        public string CanStart(RecipeInfo recipe)
        {
            if (recipe == null)
                return "Unknown recipe";
            if (m_Active != null)
                return "Busy: " + m_Active.displayName;
            if (stats != null)
            {
                if (stats.Has(StatId.Integrity) && stats.Get(StatId.Integrity) < minIntegrity)
                    return "Broken! Repair with the wrench";
                if (requiresCleanliness && stats.Has(StatId.Cleanliness) && stats.Get(StatId.Cleanliness) < minCleanliness)
                    return "Health code! Spray it clean";
                if (oilCostPerRecipe > 0f && stats.Has(StatId.OilQuality) && stats.Get(StatId.OilQuality) < minOilQuality)
                    return "Oil is spent: change the oil";
                if (usesHeat && recipe.minHeat > 0f && stats.Get(StatId.Heat) < recipe.minHeat)
                    return $"Too cold: needs Heat {recipe.minHeat:0}";
            }
            var res = KitchenGame.Res;
            if (res != null && !res.Has(recipe.inputs))
                return res.DescribeMissing(recipe.inputs) ?? "Missing ingredients";
            return null;
        }

        public bool TryStart(RecipeInfo recipe, bool fromCrew)
        {
            var problem = CanStart(recipe);
            if (problem != null)
            {
                if (!fromCrew)
                {
                    Message(problem);
                    KitchenGame.Float(OutputPosition + Vector3.up * 0.2f, problem, new Color(1f, 0.4f, 0.35f));
                }
                return false;
            }

            var res = KitchenGame.Res;
            if (res != null && !res.TrySpend(recipe.inputs))
                return false;

            if (recipe.duration <= 0.01f)
            {
                Complete(recipe);
                return true;
            }

            m_Active = recipe;
            m_Paused = false;
            if (stats != null)
                stats.Set(StatId.Progress, 0f);
            KitchenGame.Float(OutputPosition, (fromCrew ? "Crew fires " : "Firing ") + recipe.displayName + "!", new Color(1f, 0.75f, 0.3f));
            KitchenGame.Tutorial("crafting", OutputPosition + Vector3.up * 0.3f);
            RefreshCards();
            return true;
        }

        void Update()
        {
            if (m_MessageTimer > 0f)
                m_MessageTimer -= Time.deltaTime;

            if (m_Active != null)
                AdvanceRecipe(Time.deltaTime);

            m_CardTimer += Time.deltaTime;
            if (m_CardTimer >= 0.25f)
            {
                m_CardTimer = 0f;
                RefreshCards();
                if (m_Active == null && autoRepeatWithCrew && m_Last != null && Crew >= 1f && CanStart(m_Last) == null)
                    TryStart(m_Last, true);
            }
        }

        void AdvanceRecipe(float dt)
        {
            var rate = 100f / Mathf.Max(0.1f, m_Active.duration);
            rate *= 1f + crewSpeedBonus * Crew;

            if (stats != null)
            {
                if (usesHeat && m_Active.minHeat > 0f)
                {
                    var heat = stats.Get(StatId.Heat);
                    if (heat < m_Active.minHeat * 0.6f)
                    {
                        if (!m_Paused)
                            Message("Heat dropped! Cooking paused");
                        m_Paused = true;
                        return;
                    }
                    rate *= Mathf.Lerp(0.6f, 1.3f, Mathf.Clamp01(heat / 100f));
                }
                if (stats.Has(StatId.Flavor))
                    rate *= 1f + stats.Get(StatId.Flavor) / 200f;
            }
            m_Paused = false;

            if (stats != null && stats.Has(StatId.Progress))
            {
                // Euler integration of the Progress stat.
                stats.Add(StatId.Progress, rate * dt);
                if (stats.Get(StatId.Progress) >= stats.GetMax(StatId.Progress) - 0.001f)
                {
                    var done = m_Active;
                    m_Active = null;
                    stats.Set(StatId.Progress, 0f);
                    Complete(done);
                }
            }
            else
            {
                var done = m_Active;
                m_Active = null;
                Complete(done);
            }
        }

        void Complete(RecipeInfo recipe)
        {
            var res = KitchenGame.Res;
            if (res != null)
                res.Add(recipe.outputs);

            if (stats != null)
            {
                if (stats.Has(StatId.Cleanliness))
                    stats.Add(StatId.Cleanliness, -cleanlinessCostPerRecipe);
                if (oilCostPerRecipe > 0f && stats.Has(StatId.OilQuality))
                    stats.Add(StatId.OilQuality, -oilCostPerRecipe);
            }

            var db = Db;
            var text = "+" + (db != null ? db.Describe(recipe.outputs) : recipe.displayName);
            KitchenGame.Float(OutputPosition + Vector3.up * 0.15f, text, new Color(0.55f, 1f, 0.5f));
            KitchenGame.Blast(OutputPosition, new Color(1f, 0.7f, 0.25f), 0.45f);

            if (recipe.makesScraps && scrapsPrefab != null && Random.value < scrapsChance)
            {
                var offset = new Vector3(Random.Range(-0.15f, 0.15f), 0.1f, Random.Range(-0.15f, 0.15f));
                Instantiate(scrapsPrefab, OutputPosition + offset, Random.rotation);
                KitchenGame.Tutorial("trash", OutputPosition);
            }
            RefreshCards();
        }

        /// <summary>A Line Cook bumped into this station (collision): add crew.</summary>
        public bool ReceiveCrew()
        {
            if (stats == null || !stats.Has(StatId.Crew) || Crew >= maxCrew)
                return false;
            stats.Add(StatId.Crew, 1f);
            KitchenGame.Float(OutputPosition + Vector3.up * 0.2f, "Yes, Chef!  Crew " + Mathf.RoundToInt(Crew), Color.white);
            KitchenGame.Tutorial("crew", OutputPosition + Vector3.up * 0.3f);
            if (m_Last == null && recipeIds.Count > 0 && Db != null)
                m_Last = Db.GetRecipe(recipeIds[0]);
            return true;
        }

        void Message(string text)
        {
            m_LastMessage = text;
            m_MessageTimer = 3f;
            UpdateStatus();
        }

        // -------------------------------------------------------------- card/label refresh

        void RefreshCards()
        {
            var db = Db;
            if (db == null)
                return;
            for (var i = 0; i < recipeCards.Count && i < recipeIds.Count; i++)
            {
                var card = recipeCards[i];
                var recipe = db.GetRecipe(recipeIds[i]);
                if (card == null || recipe == null)
                    continue;
                string title;
                Color bg;
                if (m_Active == recipe)
                {
                    var pct = stats != null ? stats.Get(StatId.Progress) : 0f;
                    title = $"{recipe.displayName}\n<color=#ffcc66>{(m_Paused ? "PAUSED" : "COOKING")} {pct:0}%</color>";
                    bg = new Color(0.55f, 0.28f, 0.05f, 0.95f);
                }
                else
                {
                    var problem = CanStart(recipe);
                    title = recipe.displayName;
                    bg = problem == null ? new Color(0.12f, 0.38f, 0.16f, 0.95f) : new Color(0.32f, 0.08f, 0.07f, 0.95f);
                }
                var body = db.Describe(recipe.inputs) + "\n-> " + db.Describe(recipe.outputs);
                if (recipe.minHeat > 0f)
                    body += $"\nHeat >= {recipe.minHeat:0}";
                card.SetState(title, body, bg);
            }

            if (callCookCard != null)
            {
                var cooks = KitchenGame.Res != null ? KitchenGame.Res.GetInt(ResourceId.LineCooks) : 0;
                var ok = cooks > 0 && Crew < maxCrew;
                callCookCard.SetState("CALL A COOK", $"Crew {Mathf.RoundToInt(Crew)}/{maxCrew}\nLine Cooks: {cooks}",
                    ok ? new Color(0.1f, 0.22f, 0.45f, 0.95f) : new Color(0.2f, 0.2f, 0.25f, 0.95f));
            }
            UpdateStatus();
        }

        void UpdateStatus()
        {
            if (statusLabel == null)
                return;
            string text;
            if (m_MessageTimer > 0f && !string.IsNullOrEmpty(m_LastMessage))
                text = m_LastMessage;
            else if (m_Active != null)
                text = $"{m_Active.displayName} {(stats != null ? stats.Get(StatId.Progress) : 0f):0}%";
            else
                text = stationName;
            if (statusLabel.text != text)
                statusLabel.text = text;
        }
    }
}
