using System.Collections.Generic;
using UnityEngine;

// How-to popup shown on the first instance of an item and hidden on first use. Only one instance shows it at a time,
// and once completed no instance shows it again. If the showing instance disappears unused, the next one takes over.
public class TutorialPopup : MonoBehaviour
{
    [Tooltip("Instances sharing this id share one tutorial.")]
    public string tutorialId;
    public GameObject popup;

    static readonly HashSet<string> completed = new HashSet<string>();
    static readonly Dictionary<string, TutorialPopup> showing = new Dictionary<string, TutorialPopup>();

    // Statics survive Play sessions when domain reload is off.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetTutorials()
    {
        completed.Clear();
        showing.Clear();
    }

    public static void NotifyUsed(Component item)
    {
        if (item && item.TryGetComponent<TutorialPopup>(out var t)) t.MarkUsed();
    }

    void Awake()
    {
        if (!popup) return;
        bool show = !completed.Contains(tutorialId) && !showing.ContainsKey(tutorialId);
        popup.SetActive(show);
        if (show) showing[tutorialId] = this;
    }

    void OnDestroy() => StopShowing();

    void MarkUsed()
    {
        completed.Add(tutorialId);
        if (popup) popup.SetActive(false);
        StopShowing();
    }

    void StopShowing()
    {
        if (showing.TryGetValue(tutorialId, out var s) && s == this) showing.Remove(tutorialId);
    }
}
