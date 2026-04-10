using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryRuneManager : MonoBehaviour
{
    void Start()
    {
        UpdateInventoryRunes();
    }
    public GeneralUtility utility;
    public EquipmentRunesUI runeGrid;
    public SpriteContainer runeSprites;
    public ArmoryUI armory;
    public Inventory inventory;
    public string selectedRune;
    public void ResetRune()
    {
        selectedRune = "";
        runeDescription.text = "";
    }
    public void SelectRune(int index)
    {
        selectedRune = cRunes[index];
        runeDescription.text = runeGrid.detailViewer.GetRunePassiveString(selectedRune);
    }
    public string GetSelectedRune(){return selectedRune;}
    public int page = 0;
    public void ChangePage(bool right)
    {
        page = utility.ChangePage(page, right, runeObjects, runes);
        UpdateCurrentPage();
    }
    public List<GameObject> runeObjects;
    public List<Image> runeImages;
    public List<string> runes;
    public List<string> cRunes;
    public List<TMP_Text> runeNameTexts;
    public TMP_Text runeDescription;

    protected void ResetRunes()
    {
        page = 0;
        runes.Clear();
        ResetPage();
    }
    protected void ResetPage()
    {
        ResetRune();
        utility.DisableGameObjects(runeObjects);
        for (int i = 0; i < runeNameTexts.Count; i++)
        {
            runeNameTexts[i].text = "";
        }
    }

    // Get the runes from the inventory.
    public void UpdateInventoryRunes()
    {
        ResetRunes();
        for (int i = 0; i < inventory.items.Count; i++)
        {
            if (inventory.items[i].Contains("Rune"))
            {
                runes.Add(inventory.items[i]);
            }
        }
        UpdateCurrentPage();
    }

    // Display the runes.
    public void UpdateCurrentPage()
    {
        ResetPage();
        cRunes = utility.GetCurrentPageStrings(page, runeObjects, runes);
        for (int i = 0; i < cRunes.Count; i++)
        {
            runeObjects[i].SetActive(true);
            runeImages[i].sprite = runeSprites.SpriteDictionary(cRunes[i]);
            runeNameTexts[i].text = cRunes[i];
        }
    }

    public TMP_Text equipSlot;
    public TMP_Text runeSlotAvailable;
    public void UpdateSlotInfo()
    {
        equipSlot.text = runeGrid.GetSelectedEquipSlot();
        runeSlotAvailable.text = runeGrid.SelectedSlotsAvailable().ToString();
    }

    public void InsertRune()
    {
        // Check that a rune is selected.
        if (selectedRune == ""){return;}
        // Check that an equipment has been selected.
        if (runeGrid.GetSelectedEquipSlot() == ""){return;}
        // Check that the equipment has rune slots.
        if (runeGrid.SelectedSlotsAvailable() <= 0){return;}
        // Equip the rune into the next slot.
        armory.InsertRune(selectedRune, runeGrid.GetSelectedEquipSlot());
        // Remove the rune from the inventory.
        inventory.RemoveRune(selectedRune);
        UpdateInventoryRunes();
    }
}
