using UnityEngine;
using UnityEngine.InputSystem;

namespace Steven.MP2A
{
    /// <summary>
    /// Demo / video helper panel in the Chef's Office. Its cards (and F1-F8 keyboard shortcuts in the Editor) trigger
    /// events on demand so every rubric feature can be shown quickly in a short walkthrough video.
    /// </summary>
    public class ChefOfficePanel : MonoBehaviour
    {
        public bool enableKeyboardShortcuts = true;

        // -------------------------------------------------- card targets (persistent listeners)

        public void AddGold() => Res(ResourceId.Gold, 100f, "+100 Gold");

        public void StockPantry()
        {
            var res = KitchenGame.Res;
            var db = KitchenGame.Db;
            if (res == null || db == null)
                return;
            foreach (var r in db.resources)
                if (r.category == ResourceCategory.Raw)
                    res.Add(r.id, 5f);
            Say("+5 of every raw ingredient");
        }

        public void SpawnCustomer()
        {
            var spawner = KitchenGame.Instance != null ? KitchenGame.Instance.customers : null;
            Say(spawner != null && spawner.SpawnArrival() ? "A party arrives!" : "No free set table");
        }

        public void SpawnRat() => SpawnChannel("Rats", "A rat sneaks in...");

        public void SpawnDelivery() => SpawnChannel("Deliveries", "Delivery at the back door!");

        public void SpawnMushrooms() => SpawnChannel("Mushrooms", "Mushrooms sprouted");

        public void IgniteFire()
        {
            FireHazard best = null;
            var bestD = float.MaxValue;
            var head = KitchenGame.Instance != null ? KitchenGame.Instance.PlayerHeadPosition : Vector3.zero;
            foreach (var h in FindObjectsByType<FireHazard>())
            {
                if (h == null || h.IsBurning)
                    continue;
                var d = (h.transform.position - head).sqrMagnitude;
                if (d < bestD)
                {
                    bestD = d;
                    best = h;
                }
            }
            if (best != null)
                best.Ignite();
            else
                Say("Nothing to set on fire");
        }

        public void ResetTutorials()
        {
            var tm = KitchenGame.Instance != null ? KitchenGame.Instance.tutorials : null;
            if (tm != null)
                tm.ResetAll();
        }

        public void ToggleTutorials()
        {
            var tm = KitchenGame.Instance != null ? KitchenGame.Instance.tutorials : null;
            if (tm != null)
                tm.ToggleEnabled();
        }

        public void HireCook() => Res(ResourceId.LineCooks, 1f, "+1 Line Cook");

        // -------------------------------------------------- helpers

        void SpawnChannel(string channel, string message)
        {
            var spawner = KitchenGame.Instance != null ? KitchenGame.Instance.wildSpawner : null;
            Say(spawner != null && spawner.Spawn(channel) != null ? message : "Couldn't spawn " + channel);
        }

        void Res(ResourceId id, float amount, string message)
        {
            if (KitchenGame.Res != null)
                KitchenGame.Res.Add(id, amount);
            Say(message);
        }

        void Say(string text)
        {
            KitchenGame.Float(transform.position + Vector3.up * 0.4f, text, new Color(1f, 0.9f, 0.6f));
        }

        void Update()
        {
            if (!enableKeyboardShortcuts || !Application.isEditor)
                return;
            var kb = Keyboard.current;
            if (kb == null)
                return;
            if (kb.f1Key.wasPressedThisFrame) AddGold();
            if (kb.f2Key.wasPressedThisFrame) StockPantry();
            if (kb.f3Key.wasPressedThisFrame) SpawnCustomer();
            if (kb.f4Key.wasPressedThisFrame) SpawnRat();
            if (kb.f5Key.wasPressedThisFrame) SpawnDelivery();
            if (kb.f6Key.wasPressedThisFrame) IgniteFire();
            if (kb.f7Key.wasPressedThisFrame) HireCook();
            if (kb.f8Key.wasPressedThisFrame) ResetTutorials();
        }
    }
}
