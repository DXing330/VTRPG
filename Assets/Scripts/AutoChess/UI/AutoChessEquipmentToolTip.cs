using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class AutoChessEquipmentToolTip : ToolTipTarget
{
    public GameObject thisObject;
    public Image equipImage;
    public GameObject equipImageObject;
    public TMP_Text equipNameText;
    public void UpdateSlot(string newEquipName, Sprite newEquipSprite = null)
    {
        if (equipNameText != null)
        {
            equipNameText.text = newEquipName;
        }
        if (equipImageObject == null || equipImage == null){return;}
        if (newEquipSprite != null)
        {
            equipImageObject.SetActive(true);
            equipImage.sprite = newEquipSprite;
        }
        else
        {
            equipImageObject.SetActive(false);
        }
    }
    public AutoChessPrepEquipmentManager equipManager;
    public bool equipped = false;
    public void ClickInsideActorDisplay()
    {
        equipManager.ViewCurrentEquipment(this);
    }
    public void ClickButton()
    {
        if (equipped)
        {
            equipManager.ViewCurrentEquipment(this);
        }
        else
        {
            equipManager.ViewEquipmentInInventory(this);
        }
    }
}
