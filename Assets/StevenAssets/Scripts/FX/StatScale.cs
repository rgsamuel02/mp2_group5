using UnityEngine;

namespace Steven.MP2A
{
    /// <summary>Maps a Stat onto a transform's scale (e.g. herb Growth, ice cube Size, fire Intensity, bin Fill).</summary>
    public class StatScale : MonoBehaviour
    {
        public StatBlock stats;
        public StatId stat;
        public Transform target;
        public Vector3 minScale = Vector3.one * 0.2f;
        public Vector3 maxScale = Vector3.one;
        public float smoothing = 8f;

        void Update()
        {
            if (stats == null || target == null)
                return;
            var goal = Vector3.Lerp(minScale, maxScale, stats.GetNormalized(stat));
            target.localScale = Vector3.Lerp(target.localScale, goal, 1f - Mathf.Exp(-smoothing * Time.deltaTime));
        }
    }
}
