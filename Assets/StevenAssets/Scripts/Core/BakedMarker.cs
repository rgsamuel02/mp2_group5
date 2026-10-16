using UnityEngine;

namespace Steven.MP2A
{
    /// <summary>
    /// Marks each top-level object the MP2A Editor bake placed in the scene, so re-running the bake can delete
    /// and rebuild them instead of duplicating. Has no runtime behaviour.
    /// </summary>
    [DisallowMultipleComponent]
    public class BakedMarker : MonoBehaviour
    {
        public string bakedBy = "Tools > Steven > Bake MP2A";
        public string bakedAt;
        [TextArea(3, 10)] public string notes;
    }
}
