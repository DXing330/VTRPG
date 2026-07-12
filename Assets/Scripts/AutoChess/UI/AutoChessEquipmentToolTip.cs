using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class AutoChessEquipmentToolTip : ToolTipTarget
{
    public GameObject thisObject;
    public Image equipImage;
    public Image GetEquipImage(){return equipImage;}
    public GameObject equipImageObject;
    public TMP_Text equipNameText;
    public string equipName;
    public void ResetEquipName(){equipName = "";}
    public void SetEquipName(string newName){equipName = newName;}
    public string GetEquipName(){return equipName;}
    public void UpdateSlot(string newEquipName, Sprite newEquipSprite = null)
    {
        SetEquipName(newEquipName);
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
    public void ClickButtonIsolated()
    {
        equipManager.ViewEquipment(this);
    }
}
