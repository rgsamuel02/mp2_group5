using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Steven.MP2A
{
    /// <summary>
    /// The Pantry Board (chalkboard ledger) listing every player Resource. Rows are set up in the Inspector;
    /// at runtime each row updates and flashes green/red when its amount changes.
    /// Also used for the wrist/heads-up HUD with a subset of rows.
    /// </summary>
    public class ResourceBoard : MonoBehaviour
    {
        [Serializable]
        public class Row
        {
            public ResourceId id;
            public Text value;
            public Image flash;
        }

        public PlayerResources resources;
        public List<Row> rows = new List<Row>();
        public float flashDuration = 0.8f;

        readonly Dictionary<ResourceId, Row> m_Lookup = new Dictionary<ResourceId, Row>();
        readonly Dictionary<ResourceId, float> m_FlashTimers = new Dictionary<ResourceId, float>();
        readonly Dictionary<ResourceId, Color> m_FlashColors = new Dictionary<ResourceId, Color>();
        readonly List<ResourceId> m_Keys = new List<ResourceId>();

        PlayerResources Res => resources != null ? resources : KitchenGame.Res;

        void OnEnable()
        {
            m_Lookup.Clear();
            foreach (var r in rows)
                m_Lookup[r.id] = r;
            if (Res != null)
                Res.Changed += OnChanged;
            RefreshAll();
        }

        void OnDisable()
        {
            if (Res != null)
                Res.Changed -= OnChanged;
        }

        void Start() => RefreshAll();

        void OnChanged(ResourceId id, float oldValue, float newValue)
        {
            if (!m_Lookup.TryGetValue(id, out var row))
                return;
            SetValue(row, newValue);
            if (Mathf.FloorToInt(oldValue) == Mathf.FloorToInt(newValue))
                return;
            m_FlashTimers[id] = flashDuration;
            m_FlashColors[id] = newValue > oldValue ? new Color(0.3f, 1f, 0.3f, 0.6f) : new Color(1f, 0.25f, 0.2f, 0.6f);
        }

        void SetValue(Row row, float v)
        {
            if (row.value == null)
                return;
            var text = Mathf.FloorToInt(v + 0.0001f).ToString();
            if (row.value.text != text)
                row.value.text = text;
        }

        public void RefreshAll()
        {
            var res = Res;
            if (res == null)
                return;
            foreach (var r in rows)
                SetValue(r, res.Get(r.id));
        }

        void Update()
        {
            if (m_FlashTimers.Count == 0)
                return;
            m_Keys.Clear();
            m_Keys.AddRange(m_FlashTimers.Keys);
            foreach (var id in m_Keys)
            {
                var t = m_FlashTimers[id] - Time.deltaTime;
                var row = m_Lookup.TryGetValue(id, out var rr) ? rr : null;
                if (row != null && row.flash != null)
                {
                    var c = m_FlashColors[id];
                    c.a *= Mathf.Clamp01(t / flashDuration);
                    row.flash.color = c;
                }
                if (t <= 0f)
                    m_FlashTimers.Remove(id);
                else
                    m_FlashTimers[id] = t;
            }
        }
    }
}
