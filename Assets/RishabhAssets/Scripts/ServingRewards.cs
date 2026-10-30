using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Pays the player and raises Customer Satisfaction when a cup is drunk, and keeps the TV's tallies, rank progression, and achievements.
// Satisfaction gained = base - splats / 2, clamped to [min, base].
public class ServingRewards : MonoBehaviour
{
    public PlayerWallet wallet;
    public TMP_Text satisfactionText;
    public string satisfactionFormat = "Customer Satisfaction: {0}";
    public TMP_Text servedText;
    public string servedFormat = "Customers Served: {0}";
    public TMP_Text splatsSweptText;
    public string splatsSweptFormat = "Splats Swept: {0}";

    [Header("Progression")]
    public TMP_Text rankText;
    public string rankFormat = "Current Rank: {0}";
    public TMP_Text goalText;
    public TableReciever tableReciever;
    public CafeSouffleReceiver souffleReceiver;
    public GameObject unlockPopup;
    public TMP_Text unlockPopupText;
    public float unlockPopupDuration = 2.5f;

    [Header("Achievements")]
    public GameObject goldCupPrefab;
    public GameObject[] achTexts = new GameObject[3];
    public Transform[] trophyAnchors = new Transform[3];

    [Header("Money")]
    public int plainPayment = 200;
    public int flavoredPayment = 250;
    public int souffleBonus = 30;

    [Header("Satisfaction")]
    public int plainSatisfaction = 10;
    public int plainMinSatisfaction = 1;
    public int flavoredSatisfaction = 20;
    public int flavoredMinSatisfaction = 2;

    static readonly string[] Ranks =
    {
        "Lowly Owner",
        "Apprentice Owner",
        "Confident Owner",
        "Radiant Owner"
    };

    static readonly string[] Goals =
    {
        "Goal 1: Accept souffles from Hell's Kitchen.\n\nServe two customers.",
        "Goal 2: Accept coffee plants from the Garden.\n\nClean up four splats and earn 1000¥.",
        "Goal 3: Accept Violas from the Orchestra.\n\nReach a Customer Satisfaction of 20.",
        "Congratulations, Radiant Owner! You've 'won' cafe-running!"
    };

    static readonly string[] UnlockMessages =
    {
        "",
        "UNLOCKED: Souffles from Hell's Kitchen!",
        "UNLOCKED: Coffee Plants from the Garden!",
        "UNLOCKED: Violas from the Orchestra!"
    };

    int satisfaction, customersServed, splatsSwept, cupsBrewed;
    int stage;
    int queuedStage;
    readonly Queue<int> unlockQueue = new Queue<int>();
    Coroutine unlockRoutine;
    readonly bool[] achEarned = new bool[3];

    void Awake()
    {
        if (CafePersistence.Instance != null && !transform.IsChildOf(CafePersistence.Instance.transform))
            return;

        if (stage == 0)
        {
            StevenChocolateSouffle.SetUnlocked(false);
            RebeccaCoffeePlant.SetUnlocked(false);
            BryceViola.SetUnlocked(false);
        }

        if (souffleReceiver) souffleReceiver.Initialize();
        if (unlockPopup) unlockPopup.SetActive(false);
        if (achTexts != null)
        {
            foreach (var a in achTexts)
                if (a) a.SetActive(false);
        }
    }

    void Start()
    {
        // Both the table and delivery crate are active from startup; goals gate picking up the items.
        if (tableReciever)
            tableReciever.gameObject.SetActive(true);
        if (souffleReceiver)
            souffleReceiver.gameObject.SetActive(true);
        if (unlockPopup)
            unlockPopup.SetActive(false);
        EvaluateProgression();
        EvaluateAchievements();
        RefreshLabels();
    }

    void OnEnable()
    {
        Customer.AnyServed += OnServed;
        Splat.AnySwept -= OnSwept;
        Splat.AnySwept += OnSwept;
        CoffeeMachine.AnyBrewed -= OnCupBrewed;
        CoffeeMachine.AnyBrewed += OnCupBrewed;
        if (wallet)
        {
            wallet.BalanceChanged -= OnBalanceChanged;
            wallet.BalanceChanged += OnBalanceChanged;
        }
        RefreshLabels();
    }

    void OnDisable()
    {
        Customer.AnyServed -= OnServed;
        Splat.AnySwept -= OnSwept;
        CoffeeMachine.AnyBrewed -= OnCupBrewed;
        if (wallet) wallet.BalanceChanged -= OnBalanceChanged;

        if (unlockRoutine != null)
        {
            StopCoroutine(unlockRoutine);
            unlockRoutine = null;
        }
        if (unlockPopup) unlockPopup.SetActive(false);
        while (unlockQueue.Count > 0)
            ApplyStage(unlockQueue.Dequeue());
    }

    void OnServed(Customer customer, CoffeeCup cup)
    {
        bool flavored = cup && cup.IsFlavored;
        bool hadSouffle = customer && customer.HasConsumedSouffle;

        int splats = FindObjectsByType<Splat>(FindObjectsSortMode.None).Length;
        int max = flavored ? flavoredSatisfaction : plainSatisfaction;
        int min = flavored ? flavoredMinSatisfaction : plainMinSatisfaction;
        int delta = Mathf.Clamp(max - splats / 2, min, max);
        if (hadSouffle) delta += Mathf.RoundToInt((max - delta) * 0.5f);
        if (HasJukeboxAndViola()) delta += Mathf.RoundToInt((max - delta) * 0.25f);
        satisfaction += delta;
        customersServed++;

        wallet.Add((flavored ? flavoredPayment : plainPayment) + (hadSouffle ? souffleBonus : 0));

        EvaluateProgression();
        EvaluateAchievements();
        RefreshLabels();
    }

    static bool HasJukeboxAndViola()
    {
        if (!BryceViola.AnyPlacedOnGrid) return false;
        foreach (var occ in FindObjectsByType<GridOccupant>(FindObjectsSortMode.None))
        {
            if (!occ || !occ.isPlaced) continue;
            if (occ.name.StartsWith("Jukebox")) return true;
            if (occ.TryGetComponent<GrabbableItem>(out var g) && g.item && g.item.displayName == "Jukebox")
                return true;
        }
        return false;
    }

    void OnCupBrewed(CoffeeCup cup)
    {
        cupsBrewed++;
        EvaluateAchievements();
    }

    void OnSwept(Splat splat)
    {
        splatsSwept++;
        EvaluateProgression();
        EvaluateAchievements();
        RefreshLabels();
    }

    void OnBalanceChanged()
    {
        EvaluateProgression();
        RefreshLabels();
    }

    void EvaluateProgression()
    {
        if (queuedStage == 0 && customersServed >= 2)
            EnqueueUnlock(1);
        if (queuedStage == 1 && splatsSwept >= 4 && wallet && wallet.Balance >= 1000)
            EnqueueUnlock(2);
        if (queuedStage == 2 && satisfaction >= 20)
            EnqueueUnlock(3);
    }

    void EnqueueUnlock(int nextStage)
    {
        queuedStage = nextStage;
        unlockQueue.Enqueue(nextStage);
        if (unlockRoutine == null && isActiveAndEnabled)
            unlockRoutine = StartCoroutine(ProcessUnlocks());
    }

    IEnumerator ProcessUnlocks()
    {
        while (unlockQueue.Count > 0)
        {
            int nextStage = unlockQueue.Dequeue();

            // Show unlock reveal popup BEFORE the state change happens.
            if (unlockPopup)
            {
                if (unlockPopupText && nextStage < UnlockMessages.Length)
                    unlockPopupText.text = UnlockMessages[nextStage];
                unlockPopup.SetActive(true);
                yield return new WaitForSeconds(unlockPopupDuration);
                unlockPopup.SetActive(false);
            }

            ApplyStage(nextStage);
            EvaluateProgression();
        }
        unlockRoutine = null;
    }

    void ApplyStage(int nextStage)
    {
        stage = nextStage;
        if (stage == 1)
        {
            if (souffleReceiver) souffleReceiver.Unlock();
            if (tableReciever) tableReciever.UnlockSouffles();
        }
        else if (stage == 2 && tableReciever)
        {
            tableReciever.UnlockCoffeePlants();
        }
        else if (stage == 3 && tableReciever)
        {
            tableReciever.UnlockViola();
        }
        RefreshLabels();
    }

    void EvaluateAchievements()
    {
        if (!achEarned[0] && cupsBrewed >= 5)
            GrantAchievement(0);
        if (!achEarned[1] && splatsSwept >= 5)
            GrantAchievement(1);
        if (!achEarned[2] && satisfaction >= 20)
            GrantAchievement(2);
    }

    void GrantAchievement(int index)
    {
        if (index < 0 || index >= achEarned.Length || achEarned[index]) return;
        achEarned[index] = true;

        if (achTexts != null && index < achTexts.Length && achTexts[index])
            achTexts[index].SetActive(true);

        if (goldCupPrefab && trophyAnchors != null && index < trophyAnchors.Length && trophyAnchors[index])
        {
            var anchor = trophyAnchors[index];
            var parent = transform.parent ? transform.parent : null;
            Instantiate(goldCupPrefab, anchor.position, anchor.rotation, parent);
        }
    }

    void RefreshLabels()
    {
        if (satisfactionText) satisfactionText.text = string.Format(satisfactionFormat, satisfaction);
        if (servedText) servedText.text = string.Format(servedFormat, customersServed);
        if (splatsSweptText) splatsSweptText.text = string.Format(splatsSweptFormat, splatsSwept);
        int clampedStage = Mathf.Clamp(stage, 0, Ranks.Length - 1);
        if (rankText) rankText.text = string.Format(rankFormat, Ranks[clampedStage]);
        if (goalText) goalText.text = Goals[clampedStage];
    }
}
