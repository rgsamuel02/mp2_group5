using UnityEngine;

namespace Steven.MP2A
{
    /// <summary>
    /// Collision script on crafting stations: a Line Cook that walks into the station it was sent to is
    /// Destroyed (absorbed into the brigade at that station) and the station's Crew stat increases.
    /// Must live on the same GameObject as the station's collider.
    /// </summary>
    public class CrewIntake : MonoBehaviour
    {
        public CraftingStation station;

        void OnTriggerEnter(Collider other)
        {
            if (station == null)
                return;
            var e = KitchenEntity.FromCollider(other);
            if (e == null || e.Consumed || e.kind != EntityKind.LineCook)
                return;
            var cook = e.GetComponent<CookAgent>();
            if (cook == null || cook.TargetStation != station)
                return;
            cook.Join(station);
            KitchenGame.Tutorial("collision", transform.position + Vector3.up * 1.2f);
        }
    }
}
