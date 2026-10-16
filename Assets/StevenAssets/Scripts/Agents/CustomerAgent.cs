using UnityEngine;
using UnityEngine.UI;

namespace Steven.MP2A
{
    /// <summary>
    /// A dinner party that arrives at the front door at a random time and plans its route ONLY along the
    /// carpet-runner Walkway Mats (Planning down Pathways) to a set table. Walking into its table is a collision:
    /// the customer Instance is Destroyed and the table's Guests stat rises. Leaving parties walk back out.
    /// </summary>
    public class CustomerAgent : MonoBehaviour
    {
        public GridAgent agent;
        public StatBlock stats;
        public KitchenEntity entity;
        public GameObject[] partyMembers;
        public Text bubble;

        public DiningTable TargetTable { get; private set; }
        public int PartySize { get; private set; } = 1;
        public bool Leaving { get; private set; }

        Vector2Int m_Door;
        float m_RetryTimer;
        bool m_Waiting;

        void Awake()
        {
            if (agent != null)
            {
                agent.ArrivedAtGoal += OnArrived;
                agent.PathLost += OnPathLost;
            }
        }

        void OnDestroy()
        {
            if (agent != null)
            {
                agent.ArrivedAtGoal -= OnArrived;
                agent.PathLost -= OnPathLost;
            }
            if (TargetTable != null && !Leaving)
                TargetTable.ReleaseReservation(this);
        }

        public void SetPartySize(int size)
        {
            PartySize = Mathf.Max(1, size);
            if (partyMembers != null)
                for (var i = 0; i < partyMembers.Length; i++)
                    if (partyMembers[i] != null)
                        partyMembers[i].SetActive(i < PartySize);
        }

        public void BeginArrival(DiningTable table, Vector2Int door)
        {
            TargetTable = table;
            m_Door = door;
            Leaving = false;
            Say("Table for " + PartySize + "!");
            KitchenGame.Tutorial("customer", transform.position + Vector3.up * 1.8f);
            TryPlanToTable();
        }

        public void BeginLeaving(Vector2Int door, bool happy)
        {
            Leaving = true;
            TargetTable = null;
            m_Door = door;
            if (stats != null)
                stats.SetRate(StatId.Patience, 0f);
            Say(happy ? "Compliments to the chef!" : "Never again!");
            if (agent == null || !agent.PlanTo(door))
                Vanish();
        }

        void TryPlanToTable()
        {
            if (TargetTable == null || TargetTable.building == null || agent == null)
            {
                Vanish();
                return;
            }
            var ok = agent.PlanTo(TargetTable.building.cell, TargetTable.transform.position);
            m_Waiting = !ok;
            if (!ok)
            {
                Say("No path to our table?!");
                KitchenGame.Tutorial("walkway", transform.position + Vector3.up * 1.8f);
            }
            else
            {
                Say("Table for " + PartySize + "!");
            }
        }

        void Update()
        {
            if (Leaving || !m_Waiting)
                return;
            m_RetryTimer += Time.deltaTime;
            if (m_RetryTimer >= 2f)
            {
                m_RetryTimer = 0f;
                if (TargetTable == null || !TargetTable.isActiveAndEnabled)
                {
                    Vanish();
                    return;
                }
                TryPlanToTable();
            }
        }

        void OnArrived()
        {
            if (Leaving)
            {
                Vanish();
                return;
            }
            // Normally the table collision consumes us first; if not, try again shortly.
            m_Waiting = true;
        }

        void OnPathLost()
        {
            if (Leaving)
            {
                Vanish();
                return;
            }
            m_Waiting = true;
            Say("The runner's gone!");
        }

        /// <summary>Wired to StatBlock.onDied (Patience ran out while waiting at the door).</summary>
        public void OnWalkout()
        {
            Say("We're leaving!");
            if (TargetTable != null)
                TargetTable.ReleaseReservation(this);
            TargetTable = null;
        }

        void Say(string text)
        {
            if (bubble != null)
                bubble.text = text;
        }

        void Vanish()
        {
            KitchenGame.Blast(transform.position + Vector3.up * 0.8f, new Color(0.9f, 0.85f, 0.7f), 0.4f);
            if (entity != null)
                entity.Consume();
            else
                Destroy(gameObject);
        }
    }
}
