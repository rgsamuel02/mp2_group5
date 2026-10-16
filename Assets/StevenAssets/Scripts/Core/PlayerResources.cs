using System;
using System.Collections.Generic;
using UnityEngine;

namespace Steven.MP2A
{
    /// <summary>
    /// The player's Resources (the "Pantry Ledger"). Ingredients, dishes, gold, reputation, hygiene, temper,
    /// line cooks... Amounts are initialised from the KitchenDatabase start amounts when play begins.
    /// Some resources drift over time (Euler integration), e.g. the Head Chef's Temper cools down.
    /// </summary>
    [DefaultExecutionOrder(-900)]
    public class PlayerResources : MonoBehaviour
    {
        [Serializable]
        public class ResourceDrift
        {
            public ResourceId id;
            [Tooltip("Units per second. Applied every frame (Euler integration).")]
            public float perSecond;
            [Tooltip("Drift stops once this value is reached.")]
            public float target;
        }

        public KitchenDatabase database;

        [Tooltip("Resources that change on their own over time.")]
        public List<ResourceDrift> drifts = new List<ResourceDrift>();

        [Header("Consequences")]
        [Tooltip("When Temper reaches this, the Head Chef blows up (Reputation penalty).")]
        public float temperMeltdown = 100f;
        public float temperAfterMeltdown = 40f;
        public int meltdownReputationPenalty = 5;
        [Tooltip("Below this Hygiene the health inspector docks Reputation every inspectionInterval seconds.")]
        public float hygieneWarningLevel = 25f;
        public float inspectionInterval = 25f;

        readonly Dictionary<ResourceId, float> m_Amounts = new Dictionary<ResourceId, float>();
        bool m_Initialised;
        float m_InspectionTimer;

        /// <summary>id, oldValue, newValue</summary>
        public event Action<ResourceId, float, float> Changed;

        void Awake()
        {
            EnsureInitialised();
        }

        void EnsureInitialised()
        {
            if (m_Initialised)
                return;
            m_Initialised = true;
            var db = database != null ? database : KitchenGame.Db;
            if (db == null)
                return;
            foreach (var info in db.resources)
                m_Amounts[info.id] = info.startAmount;
        }

        void Update()
        {
            var dt = Time.deltaTime;
            foreach (var drift in drifts)
            {
                var current = Get(drift.id);
                if (Mathf.Approximately(current, drift.target))
                    continue;
                var step = Mathf.Abs(drift.perSecond) * dt;
                var next = Mathf.MoveTowards(current, drift.target, step);
                SetInternal(drift.id, next, false);
            }

            if (Get(ResourceId.Hygiene) < hygieneWarningLevel)
            {
                m_InspectionTimer += dt;
                if (m_InspectionTimer >= inspectionInterval)
                {
                    m_InspectionTimer = 0f;
                    Add(ResourceId.Reputation, -1f);
                    var head = KitchenGame.Instance != null ? KitchenGame.Instance.PlayerHeadPosition : Vector3.up;
                    KitchenGame.Float(head + Vector3.up * 0.3f, "Health inspector: -1 Reputation", new Color(1f, 0.4f, 0.3f));
                    KitchenGame.Tutorial("hygiene", head);
                }
            }
            else
            {
                m_InspectionTimer = 0f;
            }
        }

        public float Get(ResourceId id)
        {
            EnsureInitialised();
            return m_Amounts.TryGetValue(id, out var v) ? v : 0f;
        }

        public int GetInt(ResourceId id) => Mathf.FloorToInt(Get(id) + 0.0001f);

        public bool Has(ResourceId id, float amount) => Get(id) + 0.0001f >= amount;

        public bool Has(List<ResourceAmount> amounts)
        {
            if (amounts == null)
                return true;
            // Sum duplicates so "2 Egg + 1 Egg" is handled.
            for (var i = 0; i < amounts.Count; i++)
            {
                var need = 0;
                for (var j = 0; j < amounts.Count; j++)
                    if (amounts[j].id == amounts[i].id)
                        need += amounts[j].amount;
                if (!Has(amounts[i].id, need))
                    return false;
            }
            return true;
        }

        /// <summary>Returns the first missing resource description or null when everything is available.</summary>
        public string DescribeMissing(List<ResourceAmount> amounts)
        {
            if (amounts == null)
                return null;
            foreach (var a in amounts)
            {
                var have = GetInt(a.id);
                if (have < a.amount)
                    return $"Need {a.amount} {KitchenGame.ResourceName(a.id)} (have {have})";
            }
            return null;
        }

        public bool TrySpend(List<ResourceAmount> amounts)
        {
            if (!Has(amounts))
                return false;
            if (amounts != null)
                foreach (var a in amounts)
                    Add(a.id, -a.amount);
            return true;
        }

        public bool TrySpend(ResourceId id, float amount)
        {
            if (!Has(id, amount))
                return false;
            Add(id, -amount);
            return true;
        }

        public void Add(List<ResourceAmount> amounts, float multiplier = 1f)
        {
            if (amounts == null)
                return;
            foreach (var a in amounts)
                Add(a.id, a.amount * multiplier);
        }

        public void Add(ResourceId id, float delta)
        {
            if (Mathf.Approximately(delta, 0f))
                return;
            SetInternal(id, Get(id) + delta, true);
        }

        public void Set(ResourceId id, float value) => SetInternal(id, value, true);

        void SetInternal(ResourceId id, float value, bool checkConsequences)
        {
            EnsureInitialised();
            var old = Get(id);
            value = Clamp(id, value);
            if (Mathf.Approximately(old, value))
                return;
            m_Amounts[id] = value;
            Changed?.Invoke(id, old, value);

            if (!checkConsequences)
                return;

            if (id == ResourceId.Temper && value > old)
            {
                var head = KitchenGame.Instance != null ? KitchenGame.Instance.PlayerHeadPosition : Vector3.up;
                KitchenGame.Tutorial("temper", head);
                if (value >= temperMeltdown)
                {
                    m_Amounts[id] = temperAfterMeltdown;
                    Changed?.Invoke(id, value, temperAfterMeltdown);
                    Add(ResourceId.Reputation, -meltdownReputationPenalty);
                    KitchenGame.Float(head + Vector3.forward * 0.6f, "CHEF MELTDOWN!  -" + meltdownReputationPenalty + " Reputation", new Color(1f, 0.25f, 0.1f));
                }
            }
            else if (id == ResourceId.Hygiene && value < old)
            {
                var head = KitchenGame.Instance != null ? KitchenGame.Instance.PlayerHeadPosition : Vector3.up;
                KitchenGame.Tutorial("hygiene", head);
            }
        }

        float Clamp(ResourceId id, float value)
        {
            if (value < 0f)
                value = 0f;
            var db = database != null ? database : KitchenGame.Db;
            var info = db != null ? db.GetResource(id) : null;
            if (info != null && info.maxAmount > 0f && value > info.maxAmount)
                value = info.maxAmount;
            return value;
        }

        /// <summary>Debug / video helper: reset every resource to its start amount.</summary>
        public void ResetToStart()
        {
            var db = database != null ? database : KitchenGame.Db;
            if (db == null)
                return;
            foreach (var info in db.resources)
                Set(info.id, info.startAmount);
        }
    }
}
