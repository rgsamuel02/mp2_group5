using UnityEngine;

namespace Steven.MP2A
{
    /// <summary>
    /// The ice machine makes Ice (passive Euler gain while it has water) and drops a grabbable ice cube into its
    /// tray every time a batch is ready. Throw cubes into the fryer/grill to cool them or into the fridge to chill it.
    /// </summary>
    public class IceMachine : MonoBehaviour
    {
        public StatBlock stats;
        public GameObject iceCubePrefab;
        public Transform tray;
        public float icePerSecond = 4f;
        public float icePerCube = 25f;
        public float waterPerCube = 6f;
        public int maxCubesInTray = 4;
        public float trayRadius = 0.25f;

        void Update()
        {
            if (stats == null)
                return;
            var hasWater = !stats.Has(StatId.WaterLevel) || stats.Get(StatId.WaterLevel) >= waterPerCube;
            stats.SetRate(StatId.Ice, hasWater ? icePerSecond : 0f);
            if (stats.Get(StatId.Ice) < icePerCube || iceCubePrefab == null || tray == null)
                return;
            if (CountCubesInTray() >= maxCubesInTray)
                return;
            stats.Add(StatId.Ice, -icePerCube);
            if (stats.Has(StatId.WaterLevel))
                stats.Add(StatId.WaterLevel, -waterPerCube);
            var offset = new Vector3(Random.Range(-0.08f, 0.08f), 0.05f, Random.Range(-0.08f, 0.08f));
            Instantiate(iceCubePrefab, tray.position + offset, Random.rotation);
            KitchenGame.Tutorial("ice", tray.position + Vector3.up * 0.4f);
        }

        int CountCubesInTray()
        {
            var n = 0;
            var all = KitchenEntity.All;
            for (var i = 0; i < all.Count; i++)
            {
                var e = all[i];
                if (e != null && e.kind == EntityKind.IceCube && (e.transform.position - tray.position).sqrMagnitude < trayRadius * trayRadius)
                    n++;
            }
            return n;
        }
    }
}
