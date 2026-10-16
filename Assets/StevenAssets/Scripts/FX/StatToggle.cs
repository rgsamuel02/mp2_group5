using UnityEngine;

namespace Steven.MP2A
{
    /// <summary>Turns a child object on while a stat is above a threshold (e.g. the "ready to harvest" sparkle).</summary>
    public class StatToggle : MonoBehaviour
    {
        public StatBlock stats;
        public StatId stat;
        public GameObject target;
        public float threshold = 50f;
        public bool activeWhenAbove = true;

        void Update()
        {
            if (stats == null || target == null)
                return;
            var above = stats.Get(stat) >= threshold;
            var want = activeWhenAbove ? above : !above;
            if (target.activeSelf != want)
                target.SetActive(want);
        }
    }
}
