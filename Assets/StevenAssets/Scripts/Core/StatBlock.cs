using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Steven.MP2A
{
    [Serializable]
    public class Stat
    {
        public StatId id;
        public float value = 100f;
        public float min;
        public float max = 100f;

        [Tooltip("Passive change per second, integrated every frame with Euler integration (value += rate * dt).")]
        public float passiveRate;

        [Tooltip("When this stat drops to its minimum the Instance is Destroyed (Death).")]
        public bool lethal;

        [Tooltip("Shown as a bar on the floating stat display.")]
        public bool showInDisplay = true;

        public Color color = Color.white;

        public float Normalized => max > min ? Mathf.Clamp01((value - min) / (max - min)) : 0f;
    }

    [Serializable]
    public class StatChangedEvent : UnityEvent<StatId, float> { }

    /// <summary>
    /// Holds an Instance's Stats. Stats are advanced passively with Euler integration in Update and modified
    /// by interactions (tools, collisions, neighbors, crafting). A lethal stat reaching zero Destroys the Instance.
    /// </summary>
    public class StatBlock : MonoBehaviour
    {
        public string displayName = "Instance";
        public List<Stat> stats = new List<Stat>();

        [Tooltip("Seconds between the death trigger and the GameObject being destroyed (lets FX play).")]
        public float destroyDelay = 0.05f;

        [Tooltip("Invoked (statId, newValue) whenever a stat changes through Add/Set.")]
        public StatChangedEvent onStatChanged = new StatChangedEvent();

        [Tooltip("Invoked once when a lethal stat reaches zero, right before the Instance is destroyed.")]
        public UnityEvent onDied = new UnityEvent();

        [Tooltip("If false the death event fires but the GameObject is kept (another script handles removal).")]
        public bool destroyOnDeath = true;

        public bool IsDead { get; private set; }

        /// <summary>The stat that killed this Instance (valid once IsDead).</summary>
        public StatId CauseOfDeath { get; private set; }

        /// <summary>Raised from C# for runtime listeners (stat, oldValue, newValue).</summary>
        public event Action<StatBlock, StatId, float, float> Changed;

        public Stat Find(StatId id)
        {
            for (var i = 0; i < stats.Count; i++)
                if (stats[i].id == id)
                    return stats[i];
            return null;
        }

        public bool Has(StatId id) => Find(id) != null;

        public float Get(StatId id, float fallback = 0f)
        {
            var s = Find(id);
            return s != null ? s.value : fallback;
        }

        public float GetNormalized(StatId id)
        {
            var s = Find(id);
            return s != null ? s.Normalized : 0f;
        }

        public float GetMax(StatId id)
        {
            var s = Find(id);
            return s != null ? s.max : 0f;
        }

        /// <summary>Adds delta (clamped) and returns the change actually applied.</summary>
        public float Add(StatId id, float delta)
        {
            var s = Find(id);
            if (s == null || IsDead || Mathf.Approximately(delta, 0f))
                return 0f;
            var old = s.value;
            s.value = Mathf.Clamp(s.value + delta, s.min, s.max);
            var applied = s.value - old;
            if (!Mathf.Approximately(applied, 0f))
                Notify(s, old);
            return applied;
        }

        public void Set(StatId id, float value)
        {
            var s = Find(id);
            if (s == null || IsDead)
                return;
            var old = s.value;
            s.value = Mathf.Clamp(value, s.min, s.max);
            if (!Mathf.Approximately(old, s.value))
                Notify(s, old);
        }

        /// <summary>Changes the passive (Euler) rate of a stat at runtime.</summary>
        public void SetRate(StatId id, float perSecond)
        {
            var s = Find(id);
            if (s != null)
                s.passiveRate = perSecond;
        }

        public float GetRate(StatId id)
        {
            var s = Find(id);
            return s != null ? s.passiveRate : 0f;
        }

        void Update()
        {
            if (IsDead)
                return;
            var dt = Time.deltaTime;
            for (var i = 0; i < stats.Count; i++)
            {
                var s = stats[i];
                if (Mathf.Approximately(s.passiveRate, 0f))
                    continue;
                // Euler integration: x(t + dt) = x(t) + x'(t) * dt
                var old = s.value;
                s.value = Mathf.Clamp(s.value + s.passiveRate * dt, s.min, s.max);
                if (!Mathf.Approximately(old, s.value))
                    Notify(s, old, false);
                if (IsDead)
                    return;
            }
        }

        void Notify(Stat s, float old, bool fireUnityEvent = true)
        {
            Changed?.Invoke(this, s.id, old, s.value);
            if (fireUnityEvent)
                onStatChanged?.Invoke(s.id, s.value);
            if (s.lethal && s.value <= s.min + 0.0001f && old > s.min + 0.0001f)
                Die(s.id);
        }

        /// <summary>Kills this Instance (also used by scripts, e.g. demolition).</summary>
        public void Die(StatId cause)
        {
            if (IsDead)
                return;
            IsDead = true;
            CauseOfDeath = cause;
            onDied?.Invoke();
            KitchenGame.RaiseEntityDied(this, cause);
            KitchenGame.Tutorial("death", transform.position + Vector3.up * 0.5f);
            if (destroyOnDeath)
                Destroy(gameObject, destroyDelay);
        }
    }
}
