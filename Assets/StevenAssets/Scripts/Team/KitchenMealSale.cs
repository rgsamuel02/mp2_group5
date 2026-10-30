using Steven.MP2A;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// No-code way for teammates to buy meals from Steven's kitchen, from their own scene.
///
/// Setup (Inspector only): add this component to any object in your scene and call Buy() from a Button's On Click
/// (or any other event). For the Orchestra, the two ticked options already add the meal to GameState and show
/// failures in the orchestra menu (they go through GameState.Instance, so they survive scene reloads).
/// On Bought / On Failed are for anything extra.
/// Works while the kitchen scene isn't loaded; the kitchen gets the Gold and loses a cooked dish.
/// </summary>
public class KitchenMealSale : MonoBehaviour
{
    [Tooltip("Meals bought per Buy().")]
    public int count = 1;
    [Tooltip("Gold the kitchen earns per meal.")]
    public float goldEach = 10f;
    [Tooltip("Your sim's name, shown in the kitchen's message.")]
    public string partner = "Orchestra";

    [Header("Orchestra (Bryce)")]
    [Tooltip("Call GameState.Instance.AddMeal() for each meal bought.")]
    public bool addOrchestraMeal = true;
    [Tooltip("Show why it failed in the orchestra menu (GameState.Instance.menu).")]
    public bool showFailureInOrchestraMenu = true;

    [Header("Extra")]
    [Tooltip("Runs once per meal bought.")]
    public UnityEvent onBought = new UnityEvent();
    [Tooltip("Runs with a player-facing reason when nothing could be bought.")]
    public UnityEvent<string> onFailed = new UnityEvent<string>();

    /// <summary>Meals the kitchen has ready right now.</summary>
    public int MealsReady => KitchenLedger.MealsReady;

    public bool Buy()
    {
        if (KitchenLedger.TrySellMeals(count, goldEach, partner))
        {
            for (var i = 0; i < count; i++)
            {
                if (addOrchestraMeal && GameState.Instance != null)
                    GameState.Instance.AddMeal();
                onBought?.Invoke();
            }
            return true;
        }
        else
        {
            var why = KitchenLedger.LastError;
            if (showFailureInOrchestraMenu && GameState.Instance != null && GameState.Instance.menu != null)
                GameState.Instance.menu.LoadMenu(why + "\n\nPress B to close this menu.");
            onFailed?.Invoke(why);
        }
        return false;
    }
}
