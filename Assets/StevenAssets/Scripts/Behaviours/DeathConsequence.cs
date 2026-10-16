using System.Collections.Generic;
using UnityEngine;

namespace Steven.MP2A
{
    /// <summary>
    /// What happens to the player's Resources when this Instance dies (wired to StatBlock.onDied in the Inspector):
    /// a rat squashed (+Hygiene), food spoiling (-Hygiene), a station burning down (+Temper), a walkout...
    /// Optionally only applies when a specific stat caused the death.
    /// </summary>
    public class DeathConsequence : MonoBehaviour
    {
        public StatBlock stats;
        public bool onlyForCause;
        public StatId cause;
        public List<ResourceAmount> resourceChanges = new List<ResourceAmount>();
        public string message;
        public Color color = Color.white;
        public Color blastColor = new Color(0.4f, 0.4f, 0.4f);
        public float blastSize = 0.5f;

        /// <summary>Persistent listener target for StatBlock.onDied.</summary>
        public void Apply()
        {
            if (onlyForCause && stats != null && stats.CauseOfDeath != cause)
                return;
            var res = KitchenGame.Res;
            if (res != null)
                foreach (var r in resourceChanges)
                    res.Add(r.id, r.amount);
            var pos = transform.position + Vector3.up * 0.5f;
            if (!string.IsNullOrEmpty(message))
                KitchenGame.Float(pos, message, color);
            KitchenGame.Blast(pos, blastColor, blastSize);
        }
    }
}
