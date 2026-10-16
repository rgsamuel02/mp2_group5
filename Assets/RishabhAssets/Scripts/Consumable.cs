using TMPro;
using UnityEngine;

/// <summary>
/// An item with a limited number of uses that shows them on a label and despawns when they run out.
/// </summary>
public abstract class Consumable : MonoBehaviour
{
    [Min(1)] public int maxUses = 4;
    public TMP_Text usesLabel;
    public string labelFormat = "Uses Remaining: {0}/{1}";

    public int UsesLeft { get; private set; }

    protected virtual void Awake()
    {
        UsesLeft = maxUses;
        RefreshLabel();
    }

    /// <summary>Spends one use, despawning the item if that was the last. Returns false if none were left.</summary>
    public bool Consume()
    {
        if (UsesLeft <= 0) return false;
        UsesLeft--;
        RefreshLabel();
        if (UsesLeft == 0) Destroy(gameObject);
        return true;
    }

    void RefreshLabel()
    {
        if (usesLabel) usesLabel.text = string.Format(labelFormat, UsesLeft, maxUses);
    }
}
