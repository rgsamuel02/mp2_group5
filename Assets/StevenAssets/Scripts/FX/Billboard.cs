using UnityEngine;

namespace Steven.MP2A
{
    /// <summary>Yaw-only billboard toward the player's head (for labels and bars).</summary>
    public class Billboard : MonoBehaviour
    {
        public bool yawOnly = true;

        void LateUpdate()
        {
            var game = KitchenGame.Instance;
            if (game == null)
                return;
            var dir = transform.position - game.PlayerHeadPosition;
            if (yawOnly)
                dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }
    }
}
