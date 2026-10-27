using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Utilities;

public class CoffeeTutorial : MonoBehaviour
{
    public static bool isCompleted = false;
    public XROrigin xro;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public static CoffeeTutorial Instance { get; private set; }

    private void Awake()
    {
        // 2. Enforce the Singleton pattern (destroy duplicates)
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); 
            return; // Exit early so no further code in Awake runs
        }

        Instance = this;

        // 3. Optional: Persist the manager across scene loads
        DontDestroyOnLoad(gameObject);
        if (isCompleted) Complete();
        GameState.Instance.menu.LoadMenu("Some of your customers have been falling asleep during your concerts! That's probably not great... In fact, from now on you will lose money if your customers fall asleep, but don't worry, you can wake them up with coffee! Try pressing the button and giving the coffee to the customer.\n\nPress B to close this menu.");
    }

    public static void Complete()
    {
        Instance.xro.transform.position = new (0, 0, 0);
        Destroy(Instance.gameObject);
    }
}
