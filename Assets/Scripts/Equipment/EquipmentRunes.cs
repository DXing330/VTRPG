using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EquipmentRunes : MonoBehaviour
{
    public GeneralUtility utility;
    public EquipmentRunesUI runeGrid;
    public Equipment equipment;
    public string equipSlot;
    public int runeSlots;
    public void SetEquipmentStats(string newStats)
    {
        equipment.SetAllStats(newStats);
        ResetSelected();
        if (equipment.GetSlot() == equipSlot)
        {
            LoadRunes();
        }
    }
    public List<GameObject> runeObjects;
    public SpriteContainer runeSprites;
    public List<string> runeNames;
    public void ResetRunes()
    {
        utility.DisableGameObjects(runeObjects);
        runeNames.Clear();
    }
    public Image equipSlotImage;
    public void ChangeEquipSlotColor(Color newColor)
    {
        equipSlotImage.color = newColor;
    }
    public List<Image> runeImages;
    [ContextMenu("Update Rune Images")]
    public void UpdateRuneImages()
    {
        utility.DisableGameObjects(runeObjects);
        int index = 0;
        for (int i = 0; i < Mathf.Min(runeNames.Count, runeObjects.Count); i++)
        {
            runeObjects[i].SetActive(true);
            runeImages[i].sprite = runeSprites.SpriteDictionary(runeNames[i]);
            index++;
        }
        // Also need to show open rune slots.
        if (runeSlots <= 0 || index >= runeObjects.Count){return;}
        for (int i = index; i < Mathf.Min(index + runeSlots, runeObjects.Count); i++)
        {
            runeObjects[i].SetActive(true);
            runeImages[i].sprite = runeSprites.SpriteDictionary("");
        }
    }
    protected void LoadRunes()
    {
        runeNames = new List<string>(equipment.GetRunes());
        runeSlots = equipment.GetRuneSlots();
        UpdateRuneImages();
    }
    // Select List Stuff.
    public int selected = -1;
    public void ResetSelected(){selected = -1;}
    public void Select(int index)
    {
        selected = index;
        runeGrid.SelectEquipSlot(equipSlot);
        runeGrid.ViewRune(GetSelectedRune());
    }
    public string selectedRune;
    public string GetSelectedRune()
    {
        if (selected < 0 || selected >= runeNames.Count){return "";}
        return runeNames[selected];
    }
}
