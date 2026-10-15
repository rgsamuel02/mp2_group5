
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Scr_TutorialPopup : MonoBehaviour
{
    [Header("Tutorial Content")]
    public string tutorialId = "fertilizer_bin";
    public string tutorialTitle = "Refill Fertilizer";

    [TextArea(2, 4)]
    public string tutorialDescription =
        "Point at this bin and use the fertilizer interaction to refill your supply.";

    [Header("Popup References")]
    public GameObject popupObject;
    public TMP_Text titleText;
    public TMP_Text descriptionText;

    [Header("Display Settings")]
    public float encounterDistance = 2f;
    public float displayDuration = 8f;

    private Camera playerCamera;
    private bool hasCheckedEncounter;

    private static readonly HashSet<string> shownTutorials =
        new HashSet<string>();

    private Coroutine hideRoutine;

    private void Start()
    {
        playerCamera = Camera.main;

        if (popupObject != null)
            popupObject.SetActive(false);

        if (playerCamera == null)
        {
            Debug.LogWarning(
                "Tutorial popup could not find the Main Camera."
            );
        }
    }

    private void Update()
    {
        if (hasCheckedEncounter || playerCamera == null)
            return;

        float distance = Vector3.Distance(
            playerCamera.transform.position,
            transform.position
        );

        if (distance <= encounterDistance)
        {
            hasCheckedEncounter = true;

            if (!shownTutorials.Contains(tutorialId))
                ShowTutorial();
        }
    }

    private void ShowTutorial()
    {
        if (popupObject == null)
        {
            Debug.LogWarning(
                "Assign the popup object for " + tutorialId
            );
            return;
        }

        shownTutorials.Add(tutorialId);

        if (titleText != null)
            titleText.text = tutorialTitle;

        if (descriptionText != null)
            descriptionText.text = tutorialDescription;

        popupObject.SetActive(true);

        if (hideRoutine != null)
            StopCoroutine(hideRoutine);

        hideRoutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(displayDuration);

        if (popupObject != null)
            popupObject.SetActive(false);

        hideRoutine = null;
    }

    private void OnDestroy()
    {
        if (hideRoutine != null)
            StopCoroutine(hideRoutine);
    }
}
