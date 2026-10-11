
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class Scr_Tutorial : MonoBehaviour
{
    [System.Serializable]
    public class Tutorial
    {
        public string title;

        [TextArea(3, 6)]
        public string message;
    }

    [Header("Tutorial Panel")]
    public GameObject tutorialPanel;
    public TMP_Text tutorialTitle;
    public TMP_Text tutorialText;

    [Header("3D Cube Buttons")]
    public GameObject nextButtonCube;
    public GameObject closeButtonCube;

    [Header("VR Interaction")]
    public Camera playerCamera;
    public float interactionDistance = 5f;
    public InputActionReference interactAction;

    [Header("Tutorial Content")]
    public Tutorial[] tutorials = new Tutorial[]
    {
        new Tutorial { title = "Welcome to the Garden!", message = "Grow produce and flowers to support your garden." },
        new Tutorial { title = "Prepare the Soil", message = "Select a garden plot and place it on an empty grid cell." },
        new Tutorial { title = "Plant a Seed", message = "Select tomatoes, herbs, or flowers, then plant them on a garden plot." },
        new Tutorial { title = "Water Your Plants", message = "Point at a plant and use your watering control to restore moisture." },
        new Tutorial { title = "Watch Your Plants Grow", message = "Plants grow over time. Keep them healthy and monitor their growth." },
        new Tutorial { title = "Harvest Your Crops", message = "When a plant is mature, point at it and use your harvest control." },
        new Tutorial { title = "Manage Your Resources", message = "Check your inventory before using seeds, water, fertilizer, and supplies." },
        new Tutorial { title = "Discover the Wild Sprinkler", message = "Find the sprinkler in the garden. Automatic watering must be implemented separately." }
    };

    private int currentIndex;
    private bool[] shown;
    private bool mouseClickHandled;
    private bool actionEnabledByThisScript;

    private void Awake()
    {
        shown = new bool[tutorials.Length];
    }

    private void OnEnable()
    {
        if (interactAction != null && interactAction.action != null)
        {
            if (!interactAction.action.enabled)
            {
                interactAction.action.Enable();
                actionEnabledByThisScript = true;
            }
        }
    }

    private void OnDisable()
    {
        if (actionEnabledByThisScript &&
            interactAction != null &&
            interactAction.action != null)
        {
            interactAction.action.Disable();
        }
    }

    private void Start()
    {
        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);

        if (playerCamera == null)
            playerCamera = Camera.main;

        if (tutorials == null)
            tutorials = new Tutorial[0];
    }

    private void Update()
    {
        // VR: point the player camera at a cube and press the assigned action.
        if (interactAction != null &&
            interactAction.action != null &&
            interactAction.action.WasPressedThisFrame())
        {
            TryVRClick();
        }
    }

    public void ShowTutorial(int index)
    {
        if (tutorials == null || index < 0 || index >= tutorials.Length)
            return;

        if (tutorialPanel == null ||
            tutorialTitle == null ||
            tutorialText == null)
        {
            Debug.LogWarning("Assign the tutorial panel, title, and text.");
            return;
        }

        if (shown[index])
            return;

        currentIndex = index;
        shown[index] = true;

        DisplayCurrentTutorial();
        tutorialPanel.SetActive(true);
    }

    private void DisplayCurrentTutorial()
    {
        Tutorial tutorial = tutorials[currentIndex];

        tutorialTitle.text = tutorial.title;
        tutorialText.text = tutorial.message;

        if (nextButtonCube != null)
            nextButtonCube.SetActive(currentIndex < tutorials.Length - 1);
    }

    public void NextTutorial()
    {
        if (tutorials == null || tutorials.Length == 0)
            return;

        if (currentIndex < tutorials.Length - 1)
        {
            currentIndex++;
            shown[currentIndex] = true;
            DisplayCurrentTutorial();
        }
        else
        {
            CloseTutorial();
        }
    }

    public void CloseTutorial()
    {
        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);
    }

    private void TryVRClick()
    {
        if (playerCamera == null)
            return;

        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
            HandleButtonHit(hit.collider.gameObject);
    }

    private void HandleButtonHit(GameObject hitObject)
    {
        if (tutorialPanel == null || !tutorialPanel.activeInHierarchy)
            return;

        if (nextButtonCube != null &&
            (hitObject == nextButtonCube ||
             hitObject.transform.IsChildOf(nextButtonCube.transform)))
        {
            NextTutorial();
        }
        else if (closeButtonCube != null &&
                 (hitObject == closeButtonCube ||
                  hitObject.transform.IsChildOf(closeButtonCube.transform)))
        {
            CloseTutorial();
        }
    }
}