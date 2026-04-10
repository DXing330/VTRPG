using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EquipmentRunesUI : MonoBehaviour
{
    public PassiveDetailViewer detailViewer;
    public List<EquipmentRunes> runeGrid;
    public string testRuneGridStrings;
    [ContextMenu("Test Rune Grid Display")]
    public void TestRuneGridDisplay()
    {
        string[] blocks = testRuneGridStrings.Split("#");
        int index = 0;
        for (int i = 0; i < runeGrid.Count; i++)
        {
            if (index >= blocks.Length){break;}
            runeGrid[i].runeNames.Clear();
            for (int j = 0; j < runeGrid[i].runeImages.Count; j++)
            {
                if (index >= blocks.Length){break;}
                runeGrid[i].runeNames.Add(blocks[index]);
                index++;
            }
        }
        UpdateGridImages();
    }
    [ContextMenu("Update Grid Images")]
    public void UpdateGridImages()
    {
        for (int i = 0; i < runeGrid.Count; i++)
        {
            runeGrid[i].UpdateRuneImages();
        }
    }

    // Load the current actor equipment.
    List<string> equipmentStats;
    public void UpdateRuneGrid(string allEquipment)
    {
        equipmentStats = allEquipment.Split("@").ToList();
        for (int i = 0; i < runeGrid.Count; i++)
        {
            runeGrid[i].ResetRunes();
            for (int j = 0; j < equipmentStats.Count; j++)
            {
                runeGrid[i].SetEquipmentStats(equipmentStats[j]);
            }
        }
    }

    public InventoryRuneManager inventoryRunes;
    public Color selectedColor;
    public Color defaultColor;
    public void UpdateSelectedColor()
    {
        for (int i = 0; i < runeGrid.Count; i++)
        {
            if (runeGrid[i].equipSlot == selectedEquipSlot)
            {
                runeGrid[i].ChangeEquipSlotColor(selectedColor);
            }
            else
            {
                runeGrid[i].ChangeEquipSlotColor(defaultColor);
            }
        }
    }
    public string selectedEquipSlot = "";
    public void ResetEquipSlot()
    {
        selectedEquipSlot = "";
        UpdateSelectedColor();
        inventoryRunes.UpdateSlotInfo();
    }
    public void SelectEquipSlot(string newInfo)
    {
        selectedEquipSlot = newInfo;
        UpdateSelectedColor();
        inventoryRunes.UpdateSlotInfo();
    }
    public string GetSelectedEquipSlot()
    {
        return selectedEquipSlot;
    }
    public int SelectedSlotsAvailable()
    {
        if (selectedEquipSlot == ""){return 0;}
        for (int i = 0; i < runeGrid.Count; i++)
        {
            if (runeGrid[i].equipSlot == selectedEquipSlot)
            {
                return Mathf.Max(0, runeGrid[i].runeSlots);
            }
        }
        return 0;
    }
    public void ViewRune(string runeName)
    {
        if (runeName.Length <= 1){return;}
        detailViewer.ViewRunePassive(runeName);
    }
}
