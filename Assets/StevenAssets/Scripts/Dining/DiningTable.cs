using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Steven.MP2A
{
    /// <summary>
    /// A dining table building. Stats: Guests, Patience, Appetite.
    /// Collisions: a Customer that walks into its reserved table is Destroyed (seated -> Guests up);
    /// a Plate that touches a table with an open order is Destroyed (served -> Appetite down, gold & reputation up).
    /// Needs a Place Setting built on top (foundation) before customers will be seated here.
    /// </summary>
    public class DiningTable : MonoBehaviour
    {
        public GridBuilding building;
        public StatBlock stats;
        public GameObject[] seatedGuests;
        public Text orderLabel;
        public int tableNumber;
        [Tooltip("Patience lost per second while waiting for food.")]
        public float patienceDrain = 0.9f;
        public float eatDuration = 7f;

        static readonly List<DiningTable> s_All = new List<DiningTable>();
        static int s_NextNumber = 1;
        public static IReadOnlyList<DiningTable> All => s_All;

        CustomerAgent m_Incoming;
        bool m_Waiting;
        bool m_Eating;
        float m_EatTimer;
        int m_Party;

        public ResourceId Order { get; private set; }
        public bool HasOrder => m_Waiting;
        public bool PlateOut { get; set; }

        public bool HasPlaceSetting
        {
            get
            {
                var above = building != null ? building.Above : null;
                return above != null && above.buildingId == BuildingId.PlaceSetting;
            }
        }

        public bool IsAvailable => isActiveAndEnabled && m_Incoming == null && !m_Waiting && !m_Eating &&
                                   stats != null && stats.Get(StatId.Guests) < 0.5f && HasPlaceSetting;

        void OnEnable()
        {
            if (!s_All.Contains(this))
                s_All.Add(this);
            if (tableNumber <= 0)
                tableNumber = 100 + s_NextNumber++;
        }

        void OnDisable()
        {
            s_All.Remove(this);
            var rail = KitchenGame.Instance != null ? KitchenGame.Instance.orderRail : null;
            if (rail != null)
                rail.Remove(this);
        }

        public void Reserve(CustomerAgent c) => m_Incoming = c;

        public void ReleaseReservation(CustomerAgent c)
        {
            if (m_Incoming == c)
                m_Incoming = null;
        }

        void OnTriggerEnter(Collider other) => HandleContact(other);

        void OnCollisionEnter(Collision collision) => HandleContact(collision.collider);

        void HandleContact(Collider other)
        {
            var e = KitchenEntity.FromCollider(other);
            if (e == null || e.Consumed)
                return;
            if (e.kind == EntityKind.Customer)
            {
                var c = e.GetComponent<CustomerAgent>();
                if (c != null && !c.Leaving && c.TargetTable == this)
                    Seat(c);
            }
            else if (e.kind == EntityKind.Plate && m_Waiting)
            {
                var plate = e.GetComponent<Plate>();
                if (plate != null)
                    Serve(plate);
            }
        }

        void Seat(CustomerAgent c)
        {
            m_Incoming = null;
            m_Party = c.PartySize;
            c.entity.Consume();
            stats.Set(StatId.Guests, m_Party);
            stats.Set(StatId.Patience, stats.GetMax(StatId.Patience));
            stats.Set(StatId.Appetite, stats.GetMax(StatId.Appetite));
            stats.SetRate(StatId.Patience, -patienceDrain);
            ShowGuests(m_Party);

            var db = KitchenGame.Db;
            var dish = db != null ? db.PickRandomDish() : null;
            Order = dish != null ? dish.dish : ResourceId.Fries;
            m_Waiting = true;
            PlateOut = false;
            SetLabel($"T{tableNumber}: {KitchenGame.ResourceName(Order)}");

            var rail = KitchenGame.Instance != null ? KitchenGame.Instance.orderRail : null;
            if (rail != null)
                rail.Post(this);
            KitchenGame.Float(transform.position + Vector3.up * 1.4f, $"Order: {KitchenGame.ResourceName(Order)}", new Color(1f, 0.9f, 0.5f));
            KitchenGame.Tutorial("table", transform.position + Vector3.up * 1.4f);
            KitchenGame.Tutorial("collision", transform.position + Vector3.up * 1.2f);
        }

        void Serve(Plate plate)
        {
            var correct = plate.dish == Order;
            var warmth = plate.stats != null ? plate.stats.GetNormalized(StatId.Warmth) : 1f;
            var db = KitchenGame.Db;
            var info = db != null ? db.GetDish(plate.dish) : null;
            var price = info != null ? info.price : 15;
            var pay = Mathf.RoundToInt(price * (0.5f + 0.5f * warmth) * (correct ? 1f : 0.6f)) * Mathf.Max(1, m_Party);
            var rep = (correct ? 2 : 0) + (warmth > 0.5f ? 1 : 0);

            plate.entity.Consume();
            var res = KitchenGame.Res;
            if (res != null)
            {
                res.Add(ResourceId.Gold, pay);
                res.Add(ResourceId.Reputation, rep);
                res.Add(ResourceId.CoversServed, m_Party);
                res.Add(ResourceId.Temper, correct ? -6f : 4f);
            }

            stats.SetRate(StatId.Patience, 0f);
            m_Waiting = false;
            m_Eating = true;
            m_EatTimer = 0f;
            stats.SetRate(StatId.Appetite, -stats.GetMax(StatId.Appetite) / Mathf.Max(0.5f, eatDuration));
            var rail = KitchenGame.Instance != null ? KitchenGame.Instance.orderRail : null;
            if (rail != null)
                rail.Remove(this);

            var msg = correct ? $"+{pay} Gold  +{rep} Rep" : $"Wrong dish! +{pay} Gold";
            KitchenGame.Float(transform.position + Vector3.up * 1.3f, msg, correct ? new Color(1f, 0.85f, 0.3f) : new Color(1f, 0.5f, 0.4f));
            KitchenGame.Blast(transform.position + Vector3.up * 0.9f, new Color(1f, 0.8f, 0.2f), 0.9f);
            SetLabel(correct ? "Delicious!" : "That's not what we ordered...");
        }

        void Update()
        {
            if (stats == null)
                return;
            if (m_Waiting && stats.Get(StatId.Patience) <= 0.01f)
            {
                Walkout();
                return;
            }
            if (m_Eating)
            {
                m_EatTimer += Time.deltaTime;
                if (m_EatTimer >= eatDuration)
                    Leave(true);
            }
        }

        void Walkout()
        {
            var res = KitchenGame.Res;
            if (res != null)
            {
                res.Add(ResourceId.Reputation, -3f);
                res.Add(ResourceId.Temper, 15f);
            }
            KitchenGame.Float(transform.position + Vector3.up * 1.4f, "WALKOUT! -3 Reputation", new Color(1f, 0.3f, 0.2f));
            Leave(false);
        }

        void Leave(bool happy)
        {
            var party = m_Party;
            m_Waiting = false;
            m_Eating = false;
            PlateOut = false;
            stats.SetRate(StatId.Patience, 0f);
            stats.SetRate(StatId.Appetite, 0f);
            stats.Set(StatId.Guests, 0f);
            ShowGuests(0);
            SetLabel("");
            var rail = KitchenGame.Instance != null ? KitchenGame.Instance.orderRail : null;
            if (rail != null)
                rail.Remove(this);
            var spawner = FindSpawner();
            if (spawner != null && building != null)
                spawner.SpawnLeaving(building.cell, happy, party);
        }

        static CustomerSpawner FindSpawner()
        {
            return KitchenGame.Instance != null ? KitchenGame.Instance.customers : null;
        }

        /// <summary>A plate for this table was dropped or lost: the ticket can be fired again.</summary>
        public void PlateLost()
        {
            PlateOut = false;
        }

        void ShowGuests(int n)
        {
            if (seatedGuests == null)
                return;
            for (var i = 0; i < seatedGuests.Length; i++)
                if (seatedGuests[i] != null)
                    seatedGuests[i].SetActive(i < n);
        }

        void SetLabel(string text)
        {
            if (orderLabel != null)
                orderLabel.text = text;
        }
    }
}
