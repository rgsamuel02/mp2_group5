using UnityEngine;

/// <summary>
/// For Bryce's scenes: put it on a DeliveryCrate_Receiver (Receiver Id "Orchestra"). Every meal that arrives from the
/// kitchen (any object with a DishItem) adds one meal to GameState, then the dish is cleared from the crate.
/// Inspector only; it goes through GameState.Instance, so it keeps working across scene reloads.
/// </summary>
[RequireComponent(typeof(TransportReceiver))]
public class OrchestraMealIntake : MonoBehaviour
{
    [Tooltip("Remove the dish from the crate after counting it (your MealManager spawns its own meal objects).")]
    public bool removeDish = true;

    void Awake()
    {
        GetComponent<TransportReceiver>().onArrived.AddListener(OnArrived);
    }

    void OnArrived(GameObject item)
    {
        if (item == null || item.GetComponent<Steven.MP2A.DishItem>() == null)
            return;
        if (GameState.Instance != null)
            GameState.Instance.AddMeal();
        if (removeDish)
            Destroy(item, 1.5f);   // let the player see it land first
    }
}
