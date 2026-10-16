using UnityEngine;
using UnityEngine.UI;

namespace Steven.MP2A
{
    /// <summary>
    /// One member of the brigade (a unit of the "Line Cooks" population Resource). Idles in the staff area until
    /// called; then plans a route along the Walkway Mats to the station and walks into it (collision), where it is
    /// Destroyed and the station's Crew stat rises.
    /// </summary>
    public class CookAgent : MonoBehaviour
    {
        public GridAgent agent;
        public StatBlock stats;
        public KitchenEntity entity;
        public Text bubble;

        public CraftingStation TargetStation { get; private set; }
        public bool Dispatched => TargetStation != null;

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
        }

        /// <summary>Plans a walkway-only path to the station. Returns false if no route exists.</summary>
        public bool TryDispatch(CraftingStation station, GridBuilding stationBuilding)
        {
            if (agent == null || station == null || stationBuilding == null)
                return false;
            if (!agent.PlanTo(stationBuilding.cell, stationBuilding.transform.position))
                return false;
            TargetStation = station;
            Say("Yes, Chef!");
            if (stats != null)
                stats.SetRate(StatId.Energy, -2f);
            return true;
        }

        void OnArrived()
        {
            // Normally the station's CrewIntake consumes us on collision before we get here.
            if (TargetStation != null && TargetStation.isActiveAndEnabled)
            {
                Join(TargetStation);
            }
            else
            {
                ReturnToLocker();
            }
        }

        void OnPathLost()
        {
            Say("Walkway's gone, Chef!");
            ReturnToLocker();
        }

        /// <summary>Called by CrewIntake when this cook bumps into its station.</summary>
        public void Join(CraftingStation station)
        {
            if (entity != null && entity.Consumed)
                return;
            if (!station.ReceiveCrew())
            {
                ReturnToLocker();
                return;
            }
            KitchenGame.Blast(transform.position + Vector3.up * 0.8f, new Color(0.6f, 0.8f, 1f), 0.4f);
            if (entity != null)
                entity.Consume();
            else
                Destroy(gameObject);
        }

        void ReturnToLocker()
        {
            // The cook goes back to the locker room: give the Line Cook back to the population resource.
            if (KitchenGame.Res != null)
                KitchenGame.Res.Add(ResourceId.LineCooks, 1f);
            TargetStation = null;
            if (entity != null)
                entity.Consume(0.05f);
            else
                Destroy(gameObject);
        }

        public void Say(string text)
        {
            if (bubble != null)
                bubble.text = text;
        }
    }
}
