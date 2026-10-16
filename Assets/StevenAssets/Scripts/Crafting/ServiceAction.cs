using System.Collections.Generic;
using UnityEngine;

namespace Steven.MP2A
{
    /// <summary>
    /// Caretaking Affordance on a building via a pressable card: spend Resources to boost one of the Instance's
    /// stats ("Change the fryer oil", "Re-bait the mouse trap", "Empty the bin", "Feed the planter").
    /// </summary>
    public class ServiceAction : MonoBehaviour
    {
        public string actionName = "Service";
        public StatBlock stats;
        public StatId stat;
        [Tooltip("Added to the stat. Use a large value with setToMax to refill.")]
        public float amount = 50f;
        public bool setToMax;
        [Tooltip("If true the stat is set to its minimum instead (e.g. emptying the trash).")]
        public bool setToMin;
        [Tooltip("Optional second stat that is also reset to its minimum (e.g. Stench when emptying the bin).")]
        public bool alsoResetStat;
        public StatId alsoResetStatId;
        public List<ResourceAmount> cost = new List<ResourceAmount>();
        public List<ResourceAmount> reward = new List<ResourceAmount>();
        public PressableCard card;
        public string tutorialId;

        float m_Timer;

        /// <summary>Persistent listener target for the card.</summary>
        public void Perform()
        {
            if (stats == null || !stats.Has(stat))
                return;
            var pos = transform.position + Vector3.up * 1.1f;
            var res = KitchenGame.Res;
            if (res != null && !res.Has(cost))
            {
                KitchenGame.Float(pos, res.DescribeMissing(cost), new Color(1f, 0.4f, 0.35f));
                return;
            }
            var before = stats.Get(stat);
            if (setToMin && before <= stats.Find(stat).min + 0.01f)
            {
                KitchenGame.Float(pos, "Nothing to do", Color.white);
                return;
            }
            if (!setToMin && before >= stats.GetMax(stat) - 0.01f)
            {
                KitchenGame.Float(pos, actionName + ": already full", Color.white);
                return;
            }
            if (res != null)
            {
                res.TrySpend(cost);
                res.Add(reward);
            }
            if (setToMin)
                stats.Set(stat, stats.Find(stat).min);
            else if (setToMax)
                stats.Set(stat, stats.GetMax(stat));
            else
                stats.Add(stat, amount);
            if (alsoResetStat && stats.Has(alsoResetStatId))
                stats.Set(alsoResetStatId, stats.Find(alsoResetStatId).min);

            KitchenGame.Float(pos, actionName + "!", new Color(0.5f, 0.9f, 1f));
            KitchenGame.Blast(pos - Vector3.up * 0.3f, new Color(0.4f, 0.8f, 1f), 0.4f);
            if (!string.IsNullOrEmpty(tutorialId))
                KitchenGame.Tutorial(tutorialId, pos);
            KitchenGame.Tutorial("caretaking", pos);
            RefreshCard();
        }

        void Update()
        {
            m_Timer += Time.deltaTime;
            if (m_Timer < 0.3f)
                return;
            m_Timer = 0f;
            RefreshCard();
        }

        void RefreshCard()
        {
            if (card == null || stats == null || KitchenGame.Db == null)
                return;
            var costText = cost.Count > 0 ? KitchenGame.Db.Describe(cost) : "free";
            var ok = KitchenGame.Res == null || KitchenGame.Res.Has(cost);
            card.SetState(actionName.ToUpperInvariant(), $"{stat}: {stats.Get(stat):0}\nCost: {costText}",
                ok ? new Color(0.08f, 0.3f, 0.4f, 0.95f) : new Color(0.25f, 0.2f, 0.2f, 0.95f));
        }
    }
}
