using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Reusable icon presentation for shop slots, rewards, and other clickable icon UI.
public class IconDisplay : MonoBehaviour
{
    public Image iconImage;
    public Image highlightImage;
    public TMP_Text displayText;
    public Color defaultHighlightColor = new Color32(255, 250, 235, 165);
    protected Color defaultIconColor = Color.white;
    protected Vector3 defaultIconScale = Vector3.one;
    protected bool initializedAppearance = false;

    protected void InitializeAppearance()
    {
        if (initializedAppearance || iconImage == null){return;}
        defaultIconColor = iconImage.color;
        defaultIconScale = iconImage.rectTransform.localScale;
        initializedAppearance = true;
    }

    protected void RestoreDefaultAppearance()
    {
        if (iconImage == null){return;}
        iconImage.color = defaultIconColor;
        iconImage.rectTransform.localScale = defaultIconScale;
    }

    public void SetIcon(Sprite icon, string text = "")
    {
        InitializeAppearance();
        RestoreDefaultAppearance();
        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }
        SetText(text);
    }

    public void SetText(string text)
    {
        if (displayText != null)
        {
            displayText.text = text;
        }
    }

    public void ResetDisplay()
    {
        InitializeAppearance();
        RestoreDefaultAppearance();
        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }
        SetText("");
        SetHighlighted(false);
    }

    public void SetHighlighted(bool highlighted)
    {
        SetHighlighted(highlighted, defaultHighlightColor);
    }

    public void SetHighlighted(bool highlighted, Color highlightColor)
    {
        if (highlightImage == null){return;}
        highlightImage.gameObject.SetActive(highlighted);
        highlightImage.color = highlightColor;
        if (iconImage != null)
        {
            highlightImage.sprite = iconImage.sprite;
            highlightImage.enabled = iconImage.sprite != null;
        }
    }
}
