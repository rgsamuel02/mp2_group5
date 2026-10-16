using UnityEngine;

namespace Steven.MP2A
{
    /// <summary>
    /// The more the bin is filled with scraps, the faster its Stench rises (Euler). A full, stinking bin drags
    /// the kitchen's Hygiene down until someone empties it (ServiceAction on the bin's lid card).
    /// </summary>
    public class TrashBin : MonoBehaviour
    {
        public StatBlock stats;
        public float stenchAtFull = 1.5f;
        public float stenchDecay = 0.3f;
        public float hygieneDrainWhenStinking = 0.08f;
        public float stinkThreshold = 70f;

        void Update()
        {
            if (stats == null)
                return;
            var fill = stats.GetNormalized(StatId.Fill);
            stats.SetRate(StatId.Stench, fill > 0.05f ? stenchAtFull * fill : -stenchDecay);
            if (stats.Get(StatId.Stench) >= stinkThreshold && KitchenGame.Res != null)
            {
                KitchenGame.Res.Add(ResourceId.Hygiene, -hygieneDrainWhenStinking * Time.deltaTime);
                KitchenGame.Tutorial("trash", transform.position + Vector3.up * 1.2f);
            }
        }
    }
}
