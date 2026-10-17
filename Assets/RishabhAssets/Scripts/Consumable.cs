using TMPro;
using UnityEngine;

// An item with limited uses, shown on a label. Despawns when they run out.
public abstract class Consumable : MonoBehaviour
{
    [Min(1)] public int maxUses = 4;
    public TMP_Text usesLabel;
    public string labelFormat = "Uses Remaining: {0}/{1}";

    int usesLeft;

    protected virtual void Awake()
    {
        usesLeft = maxUses;
        RefreshLabel();
    }

    public bool Consume()
    {
        if (usesLeft <= 0) return false;
        usesLeft--;
        RefreshLabel();
        TutorialPopup.NotifyUsed(this);
        if (usesLeft == 0) Destroy(gameObject);
        return true;
    }

    void RefreshLabel()
    {
        if (usesLabel) usesLabel.text = string.Format(labelFormat, usesLeft, maxUses);
    }
}
