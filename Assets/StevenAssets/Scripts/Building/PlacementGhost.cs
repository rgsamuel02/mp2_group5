using UnityEngine;
using UnityEngine.UI;

namespace Steven.MP2A
{
    /// <summary>
    /// The "chalk outline" preview of a building at its snapped grid cell: green when it can be installed,
    /// red (with the reason) when a foundation, space or money is missing.
    /// </summary>
    public class PlacementGhost : MonoBehaviour
    {
        public Renderer[] renderers;
        public Text label;
        public Color validColor = new Color(0.3f, 1f, 0.45f, 0.45f);
        public Color invalidColor = new Color(1f, 0.25f, 0.2f, 0.45f);

        static readonly int s_BaseColor = Shader.PropertyToID("_BaseColor");
        MaterialPropertyBlock m_Block;
        bool? m_LastValid;

        public void Show(bool valid, string text)
        {
            if (label != null)
            {
                label.text = text;
                label.color = valid ? new Color(0.7f, 1f, 0.75f) : new Color(1f, 0.6f, 0.55f);
            }
            if (m_LastValid.HasValue && m_LastValid.Value == valid)
                return;
            m_LastValid = valid;
            if (renderers == null)
                return;
            if (m_Block == null)
                m_Block = new MaterialPropertyBlock();
            var c = valid ? validColor : invalidColor;
            foreach (var r in renderers)
            {
                if (r == null)
                    continue;
                r.GetPropertyBlock(m_Block);
                m_Block.SetColor(s_BaseColor, c);
                r.SetPropertyBlock(m_Block);
            }
        }

        void LateUpdate()
        {
            if (label == null)
                return;
            var game = KitchenGame.Instance;
            if (game == null)
                return;
            var t = label.canvas != null ? label.canvas.transform : label.transform;
            var dir = t.position - game.PlayerHeadPosition;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
                t.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }
    }
}
