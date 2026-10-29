using System;
using System.Collections.Generic;
using UnityEngine;

namespace Steven.MP2A
{
    /// <summary>
    /// Shatter Affordance: prying a supply crate with the crowbar replaces it with loose, harvestable ingredient
    /// Instances (one per remaining Stock).
    /// </summary>
    public class Shatterable : MonoBehaviour
    {
        [Serializable]
        public class Content
        {
            public GameObject prefab;
            public float weight = 1f;
        }

        public KitchenEntity entity;
        public StatBlock stats;
        public List<Content> contents = new List<Content>();
        [Tooltip("Used when the Instance has no Stock stat.")]
        public int fallbackCount = 4;
        public float scatterRadius = 0.25f;
        public float scatterImpulse = 1.2f;

        public bool TryShatter()
        {
            if (entity != null && entity.Consumed)
                return false;
            if (contents.Count == 0)
                return false;
            var count = stats != null && stats.Has(StatId.Stock) ? Mathf.RoundToInt(stats.Get(StatId.Stock)) : fallbackCount;
            var origin = transform.position + Vector3.up * 0.25f;
            // Remove the crate first so the loose items don't spawn inside its collider.
            if (entity != null)
                entity.Consume();
            else
                Destroy(gameObject);
            for (var i = 0; i < count; i++)
            {
                var prefab = Pick();
                if (prefab == null)
                    continue;
                var offset = UnityEngine.Random.insideUnitSphere * scatterRadius;
                offset.y = Mathf.Abs(offset.y) * 0.5f;
                var go = Instantiate(prefab, origin + offset, UnityEngine.Random.rotation);
                var rb = go.GetComponent<Rigidbody>();
                if (rb != null && !rb.isKinematic)
                    rb.AddForce((offset.normalized + Vector3.up) * scatterImpulse, ForceMode.VelocityChange);
            }
            KitchenGame.Float(origin + Vector3.up * 0.3f, $"Crate pried open! {count} items", new Color(1f, 0.85f, 0.45f));
            KitchenGame.Blast(origin, new Color(0.8f, 0.55f, 0.3f), 0.8f);
            KitchenGame.Tutorial("ingredients", origin + Vector3.up * 0.4f);
            return true;
        }

        GameObject Pick()
        {
            float total = 0f;
            foreach (var c in contents)
                total += Mathf.Max(0f, c.weight);
            var roll = UnityEngine.Random.value * total;
            foreach (var c in contents)
            {
                roll -= Mathf.Max(0f, c.weight);
                if (roll <= 0f)
                    return c.prefab;
            }
            return contents[contents.Count - 1].prefab;
        }
    }
}
