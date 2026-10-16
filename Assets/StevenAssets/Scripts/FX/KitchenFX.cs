using System.Collections.Generic;
using UnityEngine;

namespace Steven.MP2A
{
    /// <summary>
    /// Pools and plays the small feedback effects: floating text, "Solar Blast" bursts and neighbor link pulses.
    /// Effect prefabs are assigned in the Inspector.
    /// </summary>
    public class KitchenFX : MonoBehaviour
    {
        public FloatingText floatingTextPrefab;
        public BlastEffect blastPrefab;
        public NeighborPulse pulsePrefab;

        [Tooltip("Floating texts further than this from the player are skipped (keeps Quest frame time down).")]
        public float maxTextDistance = 9f;

        readonly Stack<FloatingText> m_TextPool = new Stack<FloatingText>();
        readonly Stack<BlastEffect> m_BlastPool = new Stack<BlastEffect>();
        readonly Stack<NeighborPulse> m_PulsePool = new Stack<NeighborPulse>();

        public void FloatText(Vector3 position, string text, Color color, float scale = 1f)
        {
            if (floatingTextPrefab == null || string.IsNullOrEmpty(text))
                return;
            var game = KitchenGame.Instance;
            if (game != null && (game.PlayerHeadPosition - position).sqrMagnitude > maxTextDistance * maxTextDistance)
                return;
            var ft = m_TextPool.Count > 0 ? m_TextPool.Pop() : Instantiate(floatingTextPrefab, transform);
            if (ft == null)
                ft = Instantiate(floatingTextPrefab, transform);
            ft.transform.position = position;
            ft.Play(this, text, color, scale);
        }

        public void Blast(Vector3 position, Color color, float size = 0.5f)
        {
            if (blastPrefab == null)
                return;
            var b = m_BlastPool.Count > 0 ? m_BlastPool.Pop() : Instantiate(blastPrefab, transform);
            if (b == null)
                b = Instantiate(blastPrefab, transform);
            b.transform.position = position;
            b.Play(this, color, size);
        }

        public void NeighborPulse(Vector3 from, Vector3 to, Color color)
        {
            if (pulsePrefab == null)
                return;
            var p = m_PulsePool.Count > 0 ? m_PulsePool.Pop() : Instantiate(pulsePrefab, transform);
            if (p == null)
                p = Instantiate(pulsePrefab, transform);
            p.Play(this, from, to, color);
        }

        internal void Release(FloatingText t)
        {
            t.gameObject.SetActive(false);
            m_TextPool.Push(t);
        }

        internal void Release(BlastEffect b)
        {
            b.gameObject.SetActive(false);
            m_BlastPool.Push(b);
        }

        internal void Release(NeighborPulse p)
        {
            p.gameObject.SetActive(false);
            m_PulsePool.Push(p);
        }
    }
}
