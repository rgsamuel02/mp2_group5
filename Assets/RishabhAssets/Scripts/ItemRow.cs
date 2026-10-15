using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// One row in the shop or inventory list: preview, name, detail text and an optional button.
/// </summary>
public class ItemRow : MonoBehaviour
{
    public Image preview;
    public TMP_Text nameText;
    public TMP_Text detailText;
    public Button button;
    public TMP_Text buttonText;

    public void Bind(ShopItem item, string detail, string buttonLabel, UnityAction onClick)
    {
        preview.sprite = item.preview;
        nameText.text = item.displayName;
        detailText.text = detail;

        bool hasButton = onClick != null;
        button.gameObject.SetActive(hasButton);
        button.onClick.RemoveAllListeners();
        if (hasButton)
        {
            button.onClick.AddListener(onClick);
            if (buttonText) buttonText.text = buttonLabel;
        }
    }

    public void SetInteractable(bool interactable)
    {
        button.interactable = interactable;
    }
}
