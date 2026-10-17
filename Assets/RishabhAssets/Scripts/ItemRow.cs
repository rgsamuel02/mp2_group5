using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// One shop or inventory row: preview, name, detail and an optional button.
public class ItemRow : MonoBehaviour
{
    public Image preview;
    public TMP_Text nameText;
    public TMP_Text detailText;
    public Button button;
    public TMP_Text buttonText;

    public ShopItem Item { get; private set; }

    public void Bind(ShopItem item, string detail, string buttonLabel = null, UnityAction onClick = null)
    {
        Item = item;
        preview.sprite = item.preview;
        nameText.text = item.displayName;
        detailText.text = detail;

        button.gameObject.SetActive(onClick != null);
        button.onClick.RemoveAllListeners();
        if (onClick == null) return;
        button.onClick.AddListener(onClick);
        if (buttonText) buttonText.text = buttonLabel;
    }

    public void SetInteractable(bool interactable) => button.interactable = interactable;
}
