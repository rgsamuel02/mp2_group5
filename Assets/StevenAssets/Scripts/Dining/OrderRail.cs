using System.Collections.Generic;
using UnityEngine;

namespace Steven.MP2A
{
    /// <summary>
    /// The ticket rail at the pass. Every seated table clips an order ticket here. Poke a ticket when the dish is
    /// in the pantry to plate it: the dish Resource is spent and a Plate rides the conveyor to the dining room.
    /// </summary>
    public class OrderRail : MonoBehaviour
    {
        public List<PressableCard> slots = new List<PressableCard>();

        [Tooltip("Paper tickets (restyled): dark ink text and a status stamp instead of colored cards.")]
        public bool paperTickets;
        [Tooltip("Kitchen clock shown on tickets starts at this hour.")]
        public int serviceStartHour = 17;

        readonly List<DiningTable> m_Tickets = new List<DiningTable>();
        readonly Dictionary<DiningTable, int> m_OrderNumber = new Dictionary<DiningTable, int>();
        readonly Dictionary<DiningTable, float> m_OrderTime = new Dictionary<DiningTable, float>();
        int m_NextOrder = 41;
        readonly List<DiningTable> m_Queue = new List<DiningTable>();
        float m_Timer;

        /// <summary>The ticket clip currently holding this table's order (null if none or still queued).</summary>
        public PressableCard SlotFor(DiningTable table)
        {
            var i = table != null ? m_Tickets.IndexOf(table) : -1;
            return i >= 0 && i < slots.Count ? slots[i] : null;
        }

        public void Post(DiningTable table)
        {
            if (table == null || m_Tickets.Contains(table) || m_Queue.Contains(table))
                return;
            m_OrderNumber[table] = ++m_NextOrder;
            m_OrderTime[table] = Time.time;
            var free = FreeSlot();
            if (free >= 0)
                Assign(free, table);
            else
                m_Queue.Add(table);
            Refresh();
        }

        public void Remove(DiningTable table)
        {
            m_Queue.Remove(table);
            m_OrderNumber.Remove(table);
            m_OrderTime.Remove(table);
            var i = m_Tickets.IndexOf(table);
            if (i >= 0)
            {
                m_Tickets[i] = null;
                if (m_Queue.Count > 0)
                {
                    var next = m_Queue[0];
                    m_Queue.RemoveAt(0);
                    Assign(i, next);
                }
            }
            Refresh();
        }

        int FreeSlot()
        {
            while (m_Tickets.Count < slots.Count)
                m_Tickets.Add(null);
            for (var i = 0; i < slots.Count; i++)
                if (m_Tickets[i] == null)
                    return i;
            return -1;
        }

        void Assign(int slot, DiningTable table)
        {
            while (m_Tickets.Count < slots.Count)
                m_Tickets.Add(null);
            m_Tickets[slot] = table;
        }

        /// <summary>Wired from each ticket card's onPressed with its slot index.</summary>
        public void FireSlot(int index)
        {
            while (m_Tickets.Count < slots.Count)
                m_Tickets.Add(null);
            if (index < 0 || index >= slots.Count)
                return;
            var table = m_Tickets[index];
            var pos = slots[index] != null ? slots[index].transform.position + Vector3.up * 0.2f : transform.position;
            if (table == null)
            {
                KitchenGame.Float(pos, "No order on this clip", Color.white);
                return;
            }
            if (table.PlateOut)
            {
                KitchenGame.Float(pos, "Already on its way!", Color.white);
                return;
            }
            var res = KitchenGame.Res;
            if (res == null || !res.TrySpend(table.Order, 1f))
            {
                KitchenGame.Float(pos, $"Cook a {KitchenGame.ResourceName(table.Order)} first!", new Color(1f, 0.45f, 0.35f));
                return;
            }
            var conveyor = KitchenGame.Instance != null ? KitchenGame.Instance.conveyor : null;
            if (conveyor == null || conveyor.SpawnPlate(table.Order, table) == null)
            {
                res.Add(table.Order, 1f);
                return;
            }
            table.PlateOut = true;
            KitchenGame.Float(pos, "SERVICE! Plate on the pass", new Color(1f, 0.85f, 0.3f));
            Refresh();
        }

        void Update()
        {
            m_Timer += Time.deltaTime;
            if (m_Timer < 0.3f)
                return;
            m_Timer = 0f;
            Refresh();
        }

        void Refresh()
        {
            while (m_Tickets.Count < slots.Count)
                m_Tickets.Add(null);
            var res = KitchenGame.Res;
            for (var i = 0; i < slots.Count; i++)
            {
                var card = slots[i];
                if (card == null)
                    continue;
                var t = m_Tickets[i];
                if (t == null)
                {
                    if (paperTickets)
                    {
                        card.SetState("", "\n\n(empty clip)", new Color(0.75f, 0.75f, 0.72f));
                        card.SetMeter(-1f, Color.clear);
                    }
                    else
                    {
                        card.SetState("-", "(empty clip)", new Color(0.15f, 0.15f, 0.15f, 0.9f));
                    }
                    continue;
                }
                var have = res != null ? res.GetInt(t.Order) : 0;
                var patience = t.stats != null ? t.stats.Get(StatId.Patience) : 0f;
                var patienceMax = t.stats != null ? Mathf.Max(1f, t.stats.GetMax(StatId.Patience)) : 100f;
                string title;
                string body;
                Color bg;
                if (paperTickets)
                {
                    title = $"TABLE {t.tableNumber}";
                    m_OrderNumber.TryGetValue(t, out var no);
                    m_OrderTime.TryGetValue(t, out var at);
                    var minutes = Mathf.FloorToInt(at / 60f * 4f);   // kitchen clock runs 4x
                    var clock = $"{serviceStartHour + minutes / 60:00}:{minutes % 60:00}";
                    string stamp;
                    if (t.PlateOut)
                    {
                        stamp = "<color=#2a4fa0>ON THE PASS</color>";
                        bg = new Color(0.16f, 0.31f, 0.63f);
                    }
                    else if (have > 0)
                    {
                        stamp = "<color=#1f7a2e>READY - POKE TO PLATE</color>";
                        bg = new Color(0.12f, 0.48f, 0.18f);
                    }
                    else if (patience < 30f)
                    {
                        stamp = "<color=#b3261e>HURRY!</color>";
                        bg = new Color(0.7f, 0.1f, 0.08f);
                    }
                    else
                    {
                        stamp = "<color=#7a4a10>COOK IT</color>";
                        bg = new Color(0.62f, 0.38f, 0.08f);
                    }
                    body = $"#{no:0000}      {clock}\n- - - - - - - - - -\n<size=30><b>1 x {KitchenGame.ResourceName(t.Order).ToUpperInvariant()}</b></size>\n- - - - - - - - - -\nIN PANTRY: {have}\n<b>{stamp}</b>";
                    card.SetMeter(patience / patienceMax, Color.Lerp(new Color(0.75f, 0.12f, 0.1f), new Color(0.2f, 0.55f, 0.25f), patience / patienceMax));
                }
                else
                {
                    title = $"TABLE {t.tableNumber}";
                    body = $"{KitchenGame.ResourceName(t.Order)}\nPatience {patience:0}\nIn pantry: {have}";
                    if (t.PlateOut)
                    {
                        body += "\n(plated)";
                        bg = new Color(0.2f, 0.25f, 0.45f, 0.95f);
                    }
                    else if (have > 0)
                    {
                        body += "\nPOKE TO PLATE";
                        bg = new Color(0.12f, 0.4f, 0.15f, 0.95f);
                    }
                    else
                    {
                        bg = patience < 30f ? new Color(0.5f, 0.05f, 0.05f, 0.95f) : new Color(0.35f, 0.18f, 0.05f, 0.95f);
                    }
                }
                card.SetState(title, body, bg);
            }
        }
    }
}
