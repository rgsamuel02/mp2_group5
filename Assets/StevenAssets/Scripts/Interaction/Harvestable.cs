using System.Collections.Generic;
using UnityEngine;

namespace Steven.MP2A
{
    /// <summary>
    /// Harvesting Affordance target. Either the whole Instance is gathered (Destroyed -> +Resource, e.g. an onion
    /// dropped in the basket) or one of its Stats is reduced (e.g. snipping 50 Growth off an herb plant -> +Herbs).
    /// </summary>
    public class Harvestable : MonoBehaviour
    {
        public KitchenEntity entity;
        public StatBlock stats;
        public List<ResourceAmount> yield = new List<ResourceAmount>();

        [Header("Stat harvesting (instead of destroying)")]
        public bool usesStat;
        public StatId stat = StatId.Growth;
        public float statCost = 50f;
        public float minStat = 50f;

        [Header("Ripeness for destroy-harvests (optional)")]
        public bool requiresRipeness;
        public StatId ripenessStat = StatId.Growth;
        public float ripenessNeeded = 40f;

        public string tutorialId;

        float m_LastFail;

        public bool IsReady
        {
            get
            {
                if (stats == null)
                    return true;
                if (usesStat)
                    return stats.Get(stat) >= minStat;
                if (requiresRipeness)
                    return stats.Get(ripenessStat) >= ripenessNeeded;
                return true;
            }
        }

        public bool TryHarvest()
        {
            if (entity != null && entity.Consumed)
                return false;
            if (stats != null && stats.IsDead)
                return false;
            var pos = transform.position + Vector3.up * 0.3f;
            if (!IsReady)
            {
                if (Time.time - m_LastFail > 1.5f)
                {
                    m_LastFail = Time.time;
                    KitchenGame.Float(pos, "Not ready yet", new Color(1f, 0.85f, 0.5f));
                }
                return false;
            }

            var res = KitchenGame.Res;
            if (res != null)
                res.Add(yield);

            var db = KitchenGame.Db;
            KitchenGame.Float(pos, "+" + (db != null ? db.Describe(yield) : "harvest"), new Color(0.55f, 1f, 0.5f));
            KitchenGame.Blast(pos, new Color(0.5f, 1f, 0.4f), 0.35f);
            KitchenGame.Tutorial("harvest", pos);
            if (!string.IsNullOrEmpty(tutorialId))
                KitchenGame.Tutorial(tutorialId, pos);

            if (usesStat && stats != null)
                stats.Add(stat, -statCost);
            else if (entity != null)
                entity.Consume();
            else
                Destroy(gameObject);
            return true;
        }
    }
}
