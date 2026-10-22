using System.Collections;
using UnityEngine;
using TMPro;

public class Scr_Ach : MonoBehaviour
{
    public GameObject achievementPanel;
    public TMP_Text achievementText;
    public float popupDuration = 3f;

    public int tMilestone = 100;
    public int cMilestone = 150;
    public int fMilestone = 30;

    private int tomatoesHarvested;
    private int cornHarvested;
    private int flowersHarvested;

    private bool tomatoAchievementUnlocked;
    private bool cornAchievementUnlocked;
    private bool flowerAchievementUnlocked;

    private Coroutine popupCoroutine;

    private void Start()
    {
        if (achievementPanel != null)
            achievementPanel.SetActive(false);
    }

    public void RegisterHarvest(GardenResource crop, int amount)
    {
        if (amount <= 0)
            return;

        switch (crop)
        {
            case GardenResource.Tomatoes:
                tomatoesHarvested += amount;

                if (!tomatoAchievementUnlocked &&
                    tomatoesHarvested >= tMilestone)
                {
                    tomatoAchievementUnlocked = true;
                    ShowAchievement(
                        "Tiptop Tomatoes!",
                        "You've harvested 100 tomatoes!"
                    );
                }
                break;

            case GardenResource.Corn:
                cornHarvested += amount;

                if (!cornAchievementUnlocked &&
                    cornHarvested >= cMilestone)
                {
                    cornAchievementUnlocked = true;
                    ShowAchievement(
                        "Coffee Champion!",
                        "You've harvested 150 coffee berries!"
                    );
                }
                break;

            case GardenResource.Flowers:
                flowersHarvested += amount;

                if (!flowerAchievementUnlocked &&
                    flowersHarvested >= fMilestone)
                {
                    flowerAchievementUnlocked = true;
                    ShowAchievement(
                        "Fantastic Flowers!",
                        "You've harvested 30 flowers!"
                    );
                }
                break;
        }
    }

    private void ShowAchievement(string title, string description)
    {
        if (achievementPanel == null || achievementText == null)
        {
            Debug.LogWarning(
                "Achievement popup references are missing."
            );
            return;
        }

        achievementText.text =
            "ACHIEVEMENT UNLOCKED!\n" +
            title + "\n" +
            description;

        achievementPanel.SetActive(true);

        if (popupCoroutine != null)
            StopCoroutine(popupCoroutine);

        popupCoroutine = StartCoroutine(HidePopup());
    }

    private IEnumerator HidePopup()
    {
        yield return new WaitForSeconds(popupDuration);

        if (achievementPanel != null)
            achievementPanel.SetActive(false);

        popupCoroutine = null;
    }
}