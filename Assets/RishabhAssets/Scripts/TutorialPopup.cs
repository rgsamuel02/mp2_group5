using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A how-to popup shown on the first instance of an item, hidden the first time that item is used.
/// Once a tutorial has been completed, later instances never show it. While one instance is showing it,
/// other instances don't; if that instance disappears unused (e.g. returned to the inventory), the next one shows it.
/// </summary>
public class TutorialPopup : MonoBehaviour
{
    [Tooltip("Instances sharing this id share one tutorial.")]
    public string tutorialId;
    [Tooltip("The popup to show and hide (usually the TutorialCanvas child).")]
    public GameObject popup;

    static readonly HashSet<string> completed = new HashSet<string>();
    static readonly Dictionary<string, TutorialPopup> showing = new Dictionary<string, TutorialPopup>();

    // Statics survive between Play sessions when domain reload is off, so clear them on every start.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetTutorials()
    {
        completed.Clear();
        showing.Clear();
    }

    public bool IsShowing => popup && popup.activeSelf;

    void Awake()
    {
        if (!popup) return;
        bool show = !completed.Contains(tutorialId) && !showing.ContainsKey(tutorialId);
        popup.SetActive(show);
        if (show) showing[tutorialId] = this;
    }

    void OnDestroy()
    {
        if (showing.TryGetValue(tutorialId, out var s) && s == this) showing.Remove(tutorialId);
    }

    /// <summary>The player has used this item: the tutorial is done for good.</summary>
    public void MarkUsed()
    {
        completed.Add(tutorialId);
        if (popup) popup.SetActive(false);
        if (showing.TryGetValue(tutorialId, out var s) && s == this) showing.Remove(tutorialId);
    }

    /// <summary>Completes the tutorial on <paramref name="item"/>, if it has one.</summary>
    public static void NotifyUsed(Component item)
    {
        if (item && item.TryGetComponent<TutorialPopup>(out var t)) t.MarkUsed();
    }
}
