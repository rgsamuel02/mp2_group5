
using System.Collections;
using UnityEngine;
using TMPro;

public class Scr_Ach : MonoBehaviour
{
    public GameObject achievementPanel;
    public TMP_Text achievementText;
    public float popupDuration = 3f;

    public int tMilestone = 30;
    public int cMilestone = 60;
    public int fMilestone = 5;

    [SerializeField] private int tomatoesHarvested;
    [SerializeField] private int cornHarvested;
    [SerializeField] private int flowersHarvested;

    [SerializeField] private bool tomatoAchievementUnlocked;
    [SerializeField] private bool cornAchievementUnlocked;
    [SerializeField] private bool flowerAchievementUnlocked;

    private Coroutine popupCoroutine;

    private void Start()
    {
        if (achievementPanel != null)
            achievementPanel.SetActive(false);

        CheckAchievements();
    }

    private void Update()
    {
        CheckAchievements();
    }

    public void RegisterHarvest(GardenResource crop, int amount)
    {
        if (amount <= 0)
            return;

        switch (crop)
        {
            case GardenResource.Tomatoes:
                tomatoesHarvested += amount;
                break;

            case GardenResource.Corn:
                cornHarvested += amount;
                break;

            case GardenResource.Flowers:
                flowersHarvested += amount;
                break;

            default:
                return;
        }

        CheckAchievements();
    }

    private void CheckAchievements()
    {
        if (!tomatoAchievementUnlocked &&
            tomatoesHarvested >= tMilestone)
        {
            tomatoAchievementUnlocked = true;

            ShowAchievement(
                "Tiptop Tomatoes!",
                "You've harvested 30 tomatoes! You can now sell them to the restaurant!"
            );
        }

        if (!cornAchievementUnlocked &&
            cornHarvested >= cMilestone)
        {
            cornAchievementUnlocked = true;

            ShowAchievement(
                "Coffee Champion!",
                "You've harvested 60 coffee cherries! You can now sell them to the cafe!"
            );
        }

        if (!flowerAchievementUnlocked &&
            flowersHarvested >= fMilestone)
        {
            flowerAchievementUnlocked = true;

            ShowAchievement(
                "Fantastic Flowers!",
                "You've harvested 5 flowers! You can now sell them to the orchestra!"
            );
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

    [ContextMenu("Reset Achievements For Demo")]
    private void ResetAchievementsForDemo()
    {
        tomatoesHarvested = 0;
        cornHarvested = 0;
        flowersHarvested = 0;

        tomatoAchievementUnlocked = false;
        cornAchievementUnlocked = false;
        flowerAchievementUnlocked = false;

        if (popupCoroutine != null)
        {
            StopCoroutine(popupCoroutine);
            popupCoroutine = null;
        }

        if (achievementPanel != null)
            achievementPanel.SetActive(false);

        Debug.Log("Achievement demo progress reset.");
    }

    public GardenAchievementSaveData CaptureSaveData()
    {
        return new GardenAchievementSaveData
        {
            tomatoesHarvested = tomatoesHarvested,
            cornHarvested = cornHarvested,
            flowersHarvested = flowersHarvested,

            tomatoAchievementUnlocked = tomatoAchievementUnlocked,
            cornAchievementUnlocked = cornAchievementUnlocked,
            flowerAchievementUnlocked = flowerAchievementUnlocked
        };
    }

    public void RestoreSaveData(GardenAchievementSaveData data)
    {
        if (data == null)
            return;

        tomatoesHarvested = data.tomatoesHarvested;
        cornHarvested = data.cornHarvested;
        flowersHarvested = data.flowersHarvested;

        tomatoAchievementUnlocked = data.tomatoAchievementUnlocked;
        cornAchievementUnlocked = data.cornAchievementUnlocked;
        flowerAchievementUnlocked = data.flowerAchievementUnlocked;

        // Hide any old popup. CheckAchievements won't re-unlock
        // achievements that are already marked as unlocked.
        if (achievementPanel != null)
            achievementPanel.SetActive(false);

        Debug.Log("Garden achievements restored.");
    }
}
