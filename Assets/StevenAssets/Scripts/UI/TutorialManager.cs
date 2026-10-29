using System;
using System.Collections.Generic;
using UnityEngine;

namespace Steven.MP2A
{
    /// <summary>
    /// Pop-up Tutorials: the first time the player meets an affordance or spatial dynamic, a "kitchen ticket"
    /// card pops up right next to the object explaining how it works. Each tutorial id shows once per session
    /// (use the Reset Tutorials card on the debug board before recording a video).
    /// </summary>
    public class TutorialManager : MonoBehaviour
    {
        public KitchenDatabase database;
        public TutorialPopup popupPrefab;
        public bool tutorialsEnabled = true;
        public int maxVisible = 3;
        public float duration = 22f;
        [Tooltip("Shown in front of the player when play starts.")]
        public string welcomeId = "welcome";
        public float minDistanceFromHead = 0.7f;
        [Tooltip("Events further than this from the player are ignored (not marked as seen) so the popup appears next time it happens nearby.")]
        public float noticeRadius = 4f;
        [Tooltip("Only the welcome tip may appear during the first seconds of play (avoids a pile-up at start).")]
        public float startupQuietSeconds = 4f;

        readonly HashSet<string> m_Shown = new HashSet<string>();
        readonly List<TutorialPopup> m_Active = new List<TutorialPopup>();

        public event Action<string> Shown;

        public int ShownCount => m_Shown.Count;

        KitchenDatabase Db => database != null ? database : KitchenGame.Db;

        void Start()
        {
            if (!string.IsNullOrEmpty(welcomeId))
                StartCoroutine(ShowWelcome());
        }

        System.Collections.IEnumerator ShowWelcome()
        {
            // Wait a moment for the XR rig to start tracking so "in front of the player" is meaningful.
            yield return new WaitForSeconds(1.5f);
            var game = KitchenGame.Instance;
            var head = game != null ? game.PlayerHead : null;
            var pos = head != null ? head.position + Flat(head.forward) * 1.3f : new Vector3(0f, 1.5f, 0.3f);
            Notify(welcomeId, pos);
        }

        static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 0.0001f ? v.normalized : Vector3.forward;
        }

        public bool HasShown(string id) => m_Shown.Contains(id);

        readonly HashSet<string> m_Suppressed = new HashSet<string>();

        /// <summary>This tip never pops up, even after a reset (for mechanics left for the player to discover).</summary>
        public void Suppress(string id)
        {
            if (!string.IsNullOrEmpty(id))
                m_Suppressed.Add(id);
        }

        public void Notify(string id, Vector3 position) => Notify(id, position, noticeRadius);

        /// <summary>Shows the tip for <paramref name="id"/> next to <paramref name="position"/> the first time it happens within <paramref name="radius"/> of the player.</summary>
        public void Notify(string id, Vector3 position, float radius)
        {
            if (!tutorialsEnabled || string.IsNullOrEmpty(id) || m_Shown.Contains(id) || m_Suppressed.Contains(id) || popupPrefab == null)
                return;
            if (id != welcomeId && Time.timeSinceLevelLoad < startupQuietSeconds)
                return;
            var info = Db != null ? Db.GetTutorial(id) : null;
            if (info == null)
                return;
            var game = KitchenGame.Instance;
            if (game != null && game.PlayerHead != null && (game.PlayerHeadPosition - position).sqrMagnitude > radius * radius)
                return;
            m_Shown.Add(id);

            m_Active.RemoveAll(p => p == null);
            while (m_Active.Count >= maxVisible)
            {
                var oldest = m_Active[0];
                m_Active.RemoveAt(0);
                if (oldest != null)
                    oldest.Dismiss();
            }

            position = KeepComfortable(position);
            foreach (var other in m_Active)
                if (other != null && (other.transform.position - position).sqrMagnitude < 0.5f * 0.5f)
                    position += Vector3.up * 0.45f;

            var popup = Instantiate(popupPrefab, position, Quaternion.identity, transform);
            popup.Show(this, info.title, info.body, duration, m_Shown.Count);
            m_Active.Add(popup);
            Shown?.Invoke(id);
        }

        Vector3 KeepComfortable(Vector3 p)
        {
            var game = KitchenGame.Instance;
            if (game == null)
                return p;
            var head = game.PlayerHeadPosition;
            p.y = Mathf.Clamp(p.y, 0.9f, 2.2f);
            var flat = p - head;
            flat.y = 0f;
            if (flat.magnitude < minDistanceFromHead)
            {
                var dir = flat.sqrMagnitude > 0.0001f ? flat.normalized : Flat(game.PlayerHead != null ? game.PlayerHead.forward : Vector3.forward);
                p = new Vector3(head.x, p.y, head.z) + dir * minDistanceFromHead;
            }
            return p;
        }

        internal void Closed(TutorialPopup popup) => m_Active.Remove(popup);

        /// <summary>Clears the "already seen" list so every popup can show again (for recording the video).</summary>
        public void ResetAll()
        {
            m_Shown.Clear();
            foreach (var p in m_Active)
                if (p != null)
                    p.Dismiss();
            m_Active.Clear();
            var game = KitchenGame.Instance;
            if (game != null && game.PlayerHead != null)
                KitchenGame.Float(game.PlayerHead.position + Flat(game.PlayerHead.forward), "Tutorials reset", Color.white);
        }

        public void SetEnabled(bool on) => tutorialsEnabled = on;

        public void ToggleEnabled()
        {
            tutorialsEnabled = !tutorialsEnabled;
            var game = KitchenGame.Instance;
            if (game != null && game.PlayerHead != null)
                KitchenGame.Float(game.PlayerHead.position + Flat(game.PlayerHead.forward), tutorialsEnabled ? "Tutorials ON" : "Tutorials OFF", Color.white);
        }
    }
}
