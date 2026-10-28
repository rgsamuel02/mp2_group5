using System.Collections;
using TMPro;
using UnityEngine;

public class Scr_Victory : MonoBehaviour
{
    public GameObject victoryPanel;

    public float displayDuration = 5f;

    private Coroutine hideRoutine;
    private bool hasShownVictory;

    private void Awake()
    {
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(false);
        }
    }

    public void ShowVictoryPopup()
    {
        // Only show the victory message once.
        if (hasShownVictory)
            return;

        hasShownVictory = true;

        victoryPanel.SetActive(true);

        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
        }

        hideRoutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(displayDuration);

        if (victoryPanel != null)
        {
            victoryPanel.SetActive(false);
        }

        hideRoutine = null;
    }
}
