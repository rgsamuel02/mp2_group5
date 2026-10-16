using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Steven.MP2A
{
    /// <summary>
    /// Floating stat bars above an Instance (the "station gauge" signifier). Rows are set up in the Inspector;
    /// at runtime this only refreshes values. Hidden when the player is far away to keep the kitchen readable.
    /// </summary>
    public class StatDisplay : MonoBehaviour
    {
        [Serializable]
        public class Row
        {
            public StatId stat;
            public RectTransform fill;
            public Text valueLabel;
        }

        public StatBlock stats;
        public Canvas canvas;
        public Text title;
        public List<Row> rows = new List<Row>();

        [Tooltip("Only visible when the player's head is closer than this (meters).")]
        public float showDistance = 3.2f;
        public float updateInterval = 0.12f;
        public bool billboard = true;

        float m_Timer;

        void LateUpdate()
        {
            m_Timer += Time.deltaTime;
            if (m_Timer < updateInterval)
                return;
            m_Timer = 0f;

            var game = KitchenGame.Instance;
            if (game == null || stats == null)
                return;
            var head = game.PlayerHeadPosition;
            var visible = (head - transform.position).sqrMagnitude < showDistance * showDistance;
            if (canvas != null && canvas.enabled != visible)
                canvas.enabled = visible;
            if (!visible)
                return;

            if (billboard)
            {
                var dir = transform.position - head;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.0001f)
                    transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            }

            foreach (var row in rows)
            {
                var s = stats.Find(row.stat);
                if (s == null)
                    continue;
                if (row.fill != null)
                {
                    var max = row.fill.anchorMax;
                    max.x = Mathf.Max(0.001f, s.Normalized);
                    row.fill.anchorMax = max;
                }
                if (row.valueLabel != null)
                    row.valueLabel.text = Mathf.RoundToInt(s.value).ToString();
            }
        }
    }
}
