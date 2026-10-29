using UnityEngine;

namespace Steven.MP2A
{
    /// <summary>
    /// For teammates: buy from / sell to the kitchen from any scene, whether or not StevenScene is loaded.
    /// While the kitchen is open it changes the live Pantry Ledger; otherwise it changes the kitchen's saved state
    /// (KitchenSave) and the player sees a delivery message the next time they enter the kitchen.
    ///
    /// Example (garden selling 5 tomatoes for 5 Gold):
    ///     if (!KitchenLedger.TryBuy(ResourceId.Tomato, 5, 5f, "Garden")) ShowPopup(KitchenLedger.LastError);
    /// </summary>
    public static class KitchenLedger
    {
        /// <summary>Why the last call failed, worded for a player-facing popup.</summary>
        public static string LastError { get; private set; } = "";

        /// <summary>True while StevenScene is loaded.</summary>
        public static bool IsOpen => KitchenGame.Res != null;

        /// <summary>False until the player has visited the kitchen once (there is no kitchen to trade with yet).</summary>
        public static bool HasOpened => IsOpen || KitchenSave.HasSave;

        public static float Gold => Get(ResourceId.Gold);

        public static float Get(ResourceId id)
        {
            if (IsOpen)
                return KitchenGame.Res.Get(id);
            return KitchenSave.TryGetSaved(id, out var v) ? v : 0f;
        }

        /// <summary>
        /// The kitchen buys <paramref name="quantity"/> of <paramref name="item"/> for <paramref name="goldCost"/> Gold in
        /// total. Returns false (and nothing changes) if the kitchen hasn't opened yet or can't afford it.
        /// </summary>
        public static bool TryBuy(ResourceId item, int quantity, float goldCost, string partner = "Partner")
        {
            LastError = "";
            if (quantity <= 0)
            {
                LastError = "Nothing to sell.";
                return false;
            }
            if (!HasOpened)
            {
                LastError = "The restaurant hasn't opened yet. Visit the Kitchen first.";
                return false;
            }
            if (Gold + 0.0001f < goldCost)
            {
                LastError = $"The restaurant cannot afford this ({goldCost:0.##} Gold needed, it has {Gold:0.##}).";
                return false;
            }

            var text = $"[{partner}] +{quantity} {KitchenGame.ResourceName(item)}  -{goldCost:0.##} Gold";
            if (IsOpen)
            {
                var res = KitchenGame.Res;
                res.Add(ResourceId.Gold, -goldCost);
                res.Add(item, quantity);
                var game = KitchenGame.Instance;
                var head = game != null ? game.PlayerHeadPosition : Vector3.up * 1.6f;
                KitchenGame.Float(head + Vector3.forward * 0.8f, text, new Color(0.6f, 1f, 0.6f), 1.1f);
            }
            else
            {
                KitchenSave.AddSaved(ResourceId.Gold, -goldCost);
                KitchenSave.AddSaved(item, quantity);
                KitchenSave.AddNotice("Delivery while you were out: " + text);
            }
            return true;
        }
    }
}
