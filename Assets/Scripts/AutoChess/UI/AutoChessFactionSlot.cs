using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class AutoChessFactionSlot : ToolTipTarget
{
    public GameObject thisObject;
    public Image factionImage;
    public TMP_Text factionStacks;
    public GameObject factionActiveObject;
    public void UpdateSlot(Sprite newFactionSprite, string newFactionStacks, bool active)
    {
        factionImage.sprite = newFactionSprite;
        factionStacks.text = newFactionStacks;
        factionActiveObject.SetActive(active);
    }
    public AutoChessFactionDisplay factionDisplay;
    public void ClickButton()
    {
        factionDisplay.ClickFactionToolTipButton(tooltipIndex);
    }
}
