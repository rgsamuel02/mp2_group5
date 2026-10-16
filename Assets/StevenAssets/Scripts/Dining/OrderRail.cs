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

        readonly List<DiningTable> m_Tickets = new List<DiningTable>();
        readonly List<DiningTable> m_Queue = new List<DiningTable>();
        float m_Timer;

        public void Post(DiningTable table)
        {
            if (table == null || m_Tickets.Contains(table) || m_Queue.Contains(table))
                return;
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
                    card.SetState("-", "(empty clip)", new Color(0.15f, 0.15f, 0.15f, 0.9f));
                    continue;
                }
                var have = res != null ? res.GetInt(t.Order) : 0;
                var patience = t.stats != null ? t.stats.Get(StatId.Patience) : 0f;
                string title = $"TABLE {t.tableNumber}";
                string body = $"{KitchenGame.ResourceName(t.Order)}\nPatience {patience:0}\nIn pantry: {have}";
                Color bg;
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
                card.SetState(title, body, bg);
            }
        }
    }
}
