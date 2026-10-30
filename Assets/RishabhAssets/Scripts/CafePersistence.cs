using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Preserves the entire Cafe hierarchy (stats, wallet, inventory, grid placements, customers, and loose items
// like the broom, cups, bean packs, syrup, souffles, plants, and violas) across scene transitions.
[DefaultExecutionOrder(-1000)]
public class CafePersistence : MonoBehaviour
{
    public static CafePersistence Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            var targetScene = gameObject.scene;
            gameObject.SetActive(false);
            Destroy(gameObject);
            Instance.RestoreToScene(targetScene);
            return;
        }

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static void UnloadCurrentCafe()
    {
        if (Instance == null)
            Instance = FindFirstObjectByType<CafePersistence>(FindObjectsInactive.Include);

        if (Instance != null)
            Instance.PrepareForUnload();
    }

    void PrepareForUnload()
    {
        ParentLooseCafeObjects();
        ReleaseAllHeldInteractables();
        DontDestroyOnLoad(gameObject);
        gameObject.SetActive(false);
    }

    void RestoreToScene(Scene targetScene)
    {
        if (targetScene.IsValid() && targetScene.isLoaded)
            SceneManager.MoveGameObjectToScene(gameObject, targetScene);

        gameObject.SetActive(true);
        RebindInteractables();

        var receiver = GetComponentInChildren<TableReciever>(true);
        if (receiver != null)
            receiver.ProcessPendingDeliveries();
    }

    void ParentLooseCafeObjects()
    {
        ParentLooseOfType<Broom>();
        ParentLooseOfType<BeanPack>();
        ParentLooseOfType<CoffeeCup>();
        ParentLooseOfType<SyrupPump>();
        ParentLooseOfType<StevenChocolateSouffle>();
        ParentLooseOfType<RebeccaCoffeePlant>();
        ParentLooseOfType<BryceViola>();
        ParentLooseOfType<GridOccupant>();
        ParentLooseOfType<GrabbableItem>();
        ParentLooseOfType<Splat>();
    }

    void ParentLooseOfType<T>() where T : Component
    {
        foreach (var comp in FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!comp) continue;
            var t = comp.transform;
            if (t.IsChildOf(transform)) continue;
            // Only adopt root objects or objects in the same scene that belong to the Cafe.
            if (t.parent == null)
                t.SetParent(transform, true);
        }
    }

    void ReleaseAllHeldInteractables()
    {
        foreach (var interactable in GetComponentsInChildren<XRBaseInteractable>(true))
        {
            if (!interactable || !interactable.isSelected || interactable.interactionManager == null) continue;
            for (int i = interactable.interactorsSelecting.Count - 1; i >= 0; i--)
            {
                var interactor = interactable.interactorsSelecting[i];
                interactable.interactionManager.SelectExit(interactor, interactable);
            }
        }
    }

    void RebindInteractables()
    {
        var manager = FindFirstObjectByType<XRInteractionManager>();
        if (!manager) return;

        foreach (var interactable in GetComponentsInChildren<XRBaseInteractable>(true))
        {
            if (interactable)
                interactable.interactionManager = manager;
        }
    }
}
