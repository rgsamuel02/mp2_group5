using UnityEngine;

namespace Steven.MP2A
{
    /// <summary>Moves a transform between two local positions by a Stat (e.g. sink water surface, fryer oil level).</summary>
    public class StatSlide : MonoBehaviour
    {
        public StatBlock stats;
        public StatId stat;
        public Transform target;
        public Vector3 minLocalPosition;
        public Vector3 maxLocalPosition;

        void Update()
        {
            if (stats == null || target == null)
                return;
            target.localPosition = Vector3.Lerp(minLocalPosition, maxLocalPosition, stats.GetNormalized(stat));
        }
    }
}
