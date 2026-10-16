using System.Collections.Generic;
using UnityEngine;

namespace Steven.MP2A
{
    /// <summary>
    /// Path Following: the pass conveyor carries plated dishes from the kitchen plating window out to the
    /// dining-room expo along a designer-defined curved path (stored as a polyline). The belt's arrow texture is a
    /// "Solar Coaster" UV scroll whose speed matches the plates.
    /// </summary>
    public class PassConveyor : MonoBehaviour
    {
        [Tooltip("Designer-defined path, sampled from a waypoint spline (world space).")]
        public List<Vector3> pathPoints = new List<Vector3>();
        public Plate platePrefab;
        public float speed = 0.3f;
        public float spacing = 0.3f;
        public float surfaceOffset = 0.03f;

        readonly List<Plate> m_Riding = new List<Plate>();
        readonly List<float> m_Distance = new List<float>();
        float[] m_Cumulative;
        float m_Length;

        public float Length
        {
            get
            {
                EnsureCumulative();
                return m_Length;
            }
        }

        void EnsureCumulative()
        {
            if (m_Cumulative != null && m_Cumulative.Length == pathPoints.Count)
                return;
            m_Cumulative = new float[pathPoints.Count];
            m_Length = 0f;
            for (var i = 1; i < pathPoints.Count; i++)
            {
                m_Length += Vector3.Distance(pathPoints[i - 1], pathPoints[i]);
                m_Cumulative[i] = m_Length;
            }
        }

        public Vector3 Evaluate(float s, out Vector3 tangent)
        {
            EnsureCumulative();
            tangent = Vector3.forward;
            if (pathPoints.Count == 0)
                return transform.position;
            if (pathPoints.Count == 1)
                return pathPoints[0];
            s = Mathf.Clamp(s, 0f, m_Length);
            for (var i = 1; i < pathPoints.Count; i++)
            {
                if (s <= m_Cumulative[i] || i == pathPoints.Count - 1)
                {
                    var segLen = m_Cumulative[i] - m_Cumulative[i - 1];
                    var t = segLen > 0.0001f ? (s - m_Cumulative[i - 1]) / segLen : 0f;
                    tangent = (pathPoints[i] - pathPoints[i - 1]).normalized;
                    return Vector3.Lerp(pathPoints[i - 1], pathPoints[i], Mathf.Clamp01(t));
                }
            }
            return pathPoints[pathPoints.Count - 1];
        }

        public Plate SpawnPlate(ResourceId dish, DiningTable table)
        {
            if (platePrefab == null || pathPoints.Count < 2)
                return null;
            var start = Evaluate(0f, out var tangent);
            var plate = Instantiate(platePrefab, start + Vector3.up * surfaceOffset, Quaternion.LookRotation(tangent, Vector3.up));
            plate.Setup(dish, table, this);
            m_Riding.Add(plate);
            m_Distance.Add(0f);
            KitchenGame.Tutorial("conveyor", start + Vector3.up * 0.4f);
            return plate;
        }

        public void Release(Plate plate)
        {
            var i = m_Riding.IndexOf(plate);
            if (i < 0)
                return;
            m_Riding.RemoveAt(i);
            m_Distance.RemoveAt(i);
            if (plate != null)
                plate.DetachFromConveyor();
        }

        void Update()
        {
            EnsureCumulative();
            var dt = Time.deltaTime;
            // Index 0 is the oldest plate (furthest along); each plate keeps 'spacing' behind the one ahead.
            var limit = m_Length;
            for (var i = 0; i < m_Riding.Count; i++)
            {
                var plate = m_Riding[i];
                if (plate == null)
                {
                    m_Riding.RemoveAt(i);
                    m_Distance.RemoveAt(i);
                    i--;
                    continue;
                }
                var s = Mathf.Min(m_Distance[i] + speed * dt, limit);
                m_Distance[i] = s;
                limit = Mathf.Max(0f, s - spacing);
                var p = Evaluate(s, out var tangent);
                plate.transform.SetPositionAndRotation(p + Vector3.up * surfaceOffset, Quaternion.LookRotation(tangent, Vector3.up));
            }
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.7f, 0.1f);
            for (var i = 1; i < pathPoints.Count; i++)
                Gizmos.DrawLine(pathPoints[i - 1], pathPoints[i]);
        }
    }
}
