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
    protected float highlightSizeIncrease = 1.1f;
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
        iconImage.color = defaultIconColor;
        iconImage.rectTransform.localScale = defaultIconScale;
    }

    public void SetIcon(string iconName, SpriteContainer sprites, string text = "")
    {
        RestoreDefaultAppearance();
        sprites.ApplyToImage(iconImage, iconName, defaultIconColor, defaultIconScale);
        sprites.ApplyToImage(highlightImage, iconName, defaultIconColor, defaultIconScale);
        // Make the highlightImage white, and a little bigger.
        highlightImage.color = defaultIconColor;
        highlightImage.transform.localScale *= highlightSizeIncrease;
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
