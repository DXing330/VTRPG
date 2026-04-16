using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

// Roguelike shop scene. Reads pre-generated shop state and only mutates in-memory data while inside the scene.
// Saving should happen only when leaving through StSStateManager.
public class StSStore : MonoBehaviour
{
    // --- Core References ---
    public PartyDataManager partyData;
    public StSStateManager stsManager;
    public StSShopSaveData shopData;
    public StSRewardSaveData rewardData;
    public InventoryUI inventoryUI;
    public ItemDetailViewer itemDetailViewer;

    // --- Skillbook Data ---
    public StatDatabase skillBookData;
    public StatDatabase colorlessSkillBookData;
    public PassiveDetailViewer passiveDetailViewer;
    public ActiveDescriptionViewer activeDetailViewer;
    public SpellDetailViewer spellDetailViewer;

    // --- General UI ---
    public TMP_Text goldText;
    public TMP_Text selectedSectionText;
    public TMP_Text selectedNameText;
    public TMP_Text selectedPriceText;
    public TMP_Text selectedDescriptionText;
    public GameObject buyButtonObject;

    // --- Book Sections ---
    public SelectStatTextList normalBookDisplay;
    public SelectStatTextList rareBookDisplay;
    public SelectStatTextList colorlessBookDisplay;

    // --- Item / Relic Sections ---
    public SelectStatTextList consumableDisplay;
    public SelectStatTextList relicDisplay;

    // --- Priest Service ---
    public GameObject priestServiceObject;
    public TMP_Text priestServicePriceText;
    public SelectList priestActorSelect;
    public SelectList priestInjurySelect;
    public TMP_Text priestServiceDescriptionText;
    public GameObject priestBuyButtonObject;
    public List<string> removableInjuries;

    // --- Current Selection State ---
    public string selectedSection = "";
    public int selectedIndex = -1;

    protected virtual void Start()
    {
        LoadStore();
    }

    // --- Scene Setup ---
    protected void LoadStore()
    {
        ResetSelection();
        UpdateAllDisplays();
        UpdatePriestSection();
    }

    protected void ResetSelection()
    {
        selectedSection = "";
        selectedIndex = -1;
        if (normalBookDisplay != null){normalBookDisplay.ResetSelected();}
        if (rareBookDisplay != null){rareBookDisplay.ResetSelected();}
        if (colorlessBookDisplay != null){colorlessBookDisplay.ResetSelected();}
        if (consumableDisplay != null){consumableDisplay.ResetSelected();}
        if (relicDisplay != null){relicDisplay.ResetSelected();}
        UpdateSelectionDisplay();
    }

    protected void UpdateAllDisplays()
    {
        UpdateGoldDisplay();
        UpdateBookDisplays();
        UpdateItemDisplays();
        UpdateRelicDisplay();
        UpdateSelectionDisplay();
    }

    protected void UpdateGoldDisplay()
    {
        if (goldText == null || partyData == null){return;}
        goldText.text = partyData.inventory.GetGold().ToString();
        if (inventoryUI != null)
        {
            inventoryUI.UpdateKeyValues();
        }
    }

    // --- Book Display ---
    protected void UpdateBookDisplays()
    {
        if (normalBookDisplay != null)
        {
            normalBookDisplay.SetStatsAndData(BuildDisplayStats(shopData.normalBooks, shopData.normalBookSold), BuildDisplayPrices(shopData.normalBookPrices, shopData.normalBookSold));
        }
        if (rareBookDisplay != null)
        {
            rareBookDisplay.SetStatsAndData(BuildDisplayStats(shopData.rareBooks, shopData.rareBookSold), BuildDisplayPrices(shopData.rareBookPrices, shopData.rareBookSold));
        }
        if (colorlessBookDisplay != null)
        {
            colorlessBookDisplay.SetStatsAndData(BuildDisplayStats(shopData.colorlessBooks, shopData.colorlessBookSold), BuildDisplayPrices(shopData.colorlessBookPrices, shopData.colorlessBookSold));
        }
    }

    // --- Item / Relic Display ---
    protected void UpdateItemDisplays()
    {
        if (consumableDisplay != null)
        {
            consumableDisplay.SetStatsAndData(BuildDisplayStats(shopData.consumables, shopData.consumableSold), BuildDisplayPrices(shopData.consumablePrices, shopData.consumableSold));
        }
    }

    protected void UpdateRelicDisplay()
    {
        if (relicDisplay != null)
        {
            relicDisplay.SetStatsAndData(BuildDisplayStats(shopData.relics, shopData.relicSold), BuildDisplayPrices(shopData.relicPrices, shopData.relicSold));
        }
    }

    protected List<string> BuildDisplayStats(List<string> names, List<string> soldFlags)
    {
        List<string> display = new List<string>();
        for (int i = 0; i < names.Count; i++)
        {
            if (IsSold(soldFlags, i))
            {
                display.Add(names[i] + " (Sold)");
            }
            else
            {
                display.Add(names[i]);
            }
        }
        return display;
    }

    protected List<string> BuildDisplayPrices(List<string> prices, List<string> soldFlags)
    {
        List<string> display = new List<string>();
        for (int i = 0; i < prices.Count; i++)
        {
            if (IsSold(soldFlags, i))
            {
                display.Add("Sold");
            }
            else
            {
                display.Add(prices[i]);
            }
        }
        return display;
    }

    // --- Section Selection ---
    public void SelectNormalBook()
    {
        SelectSection("NormalBook", normalBookDisplay != null ? normalBookDisplay.GetSelected() : -1);
    }

    public void SelectRareBook()
    {
        SelectSection("RareBook", rareBookDisplay != null ? rareBookDisplay.GetSelected() : -1);
    }

    public void SelectColorlessBook()
    {
        SelectSection("ColorlessBook", colorlessBookDisplay != null ? colorlessBookDisplay.GetSelected() : -1);
    }

    public void SelectConsumable()
    {
        SelectSection("Consumable", consumableDisplay != null ? consumableDisplay.GetSelected() : -1);
    }

    public void SelectRelic()
    {
        SelectSection("Relic", relicDisplay != null ? relicDisplay.GetSelected() : -1);
    }

    protected void SelectSection(string section, int index)
    {
        selectedSection = section;
        selectedIndex = index;
        UpdateSelectionDisplay();
    }

    protected void UpdateSelectionDisplay()
    {
        if (selectedSectionText != null){selectedSectionText.text = selectedSection;}
        if (selectedNameText != null){selectedNameText.text = "";}
        if (selectedPriceText != null){selectedPriceText.text = "";}
        if (selectedDescriptionText != null){selectedDescriptionText.text = "";}
        if (buyButtonObject != null){buyButtonObject.SetActive(false);}

        if (selectedIndex < 0){return;}

        switch (selectedSection)
        {
            case "NormalBook":
            UpdateBookSelection(shopData.normalBooks, shopData.normalBookPrices, shopData.normalBookSold, false);
            return;
            case "RareBook":
            UpdateBookSelection(shopData.rareBooks, shopData.rareBookPrices, shopData.rareBookSold, false);
            return;
            case "ColorlessBook":
            UpdateBookSelection(shopData.colorlessBooks, shopData.colorlessBookPrices, shopData.colorlessBookSold, true);
            return;
            case "Consumable":
            UpdateConsumableSelection();
            return;
            case "Relic":
            UpdateRelicSelection();
            return;
        }
    }

    // --- Book Detail Display ---
    protected void UpdateBookSelection(List<string> books, List<string> prices, List<string> soldFlags, bool colorless)
    {
        if (!IndexValid(books, selectedIndex)){return;}
        string bookName = books[selectedIndex];
        if (selectedNameText != null){selectedNameText.text = bookName;}
        if (selectedPriceText != null){selectedPriceText.text = ReturnPriceText(prices, selectedIndex);}
        if (selectedDescriptionText != null)
        {
            selectedDescriptionText.text = ReturnSkillBookDescription(bookName, colorless);
        }
        if (buyButtonObject != null)
        {
            buyButtonObject.SetActive(!IsSold(soldFlags, selectedIndex));
        }
    }

    protected string ReturnSkillBookDescription(string bookName, bool colorless)
    {
        StatDatabase sourceData = colorless ? colorlessSkillBookData : skillBookData;
        if (sourceData == null){return "";}
        string bookData = sourceData.ReturnValue(bookName);
        if (bookData == ""){return "";}
        string[] blocks = bookData.Split("_");
        if (blocks.Length < 2){return bookName;}
        string bookType = blocks[0];
        string skillName = blocks[1];
        switch (bookType)
        {
            case "Passive":
            return passiveDetailViewer != null ? passiveDetailViewer.ReturnSpecificPassiveLevelEffect(skillName, 1) : skillName;
            case "Skill":
            return activeDetailViewer != null ? activeDetailViewer.ReturnActiveDescriptionFromName(skillName) : skillName;
            case "Spell":
            return spellDetailViewer != null ? spellDetailViewer.ReturnSpellDescriptionFromName(skillName) : skillName;
        }
        return skillName;
    }

    // --- Item / Relic Detail Display ---
    protected void UpdateConsumableSelection()
    {
        if (!IndexValid(shopData.consumables, selectedIndex)){return;}
        string itemName = shopData.consumables[selectedIndex];
        if (selectedNameText != null){selectedNameText.text = itemName;}
        if (selectedPriceText != null){selectedPriceText.text = ReturnPriceText(shopData.consumablePrices, selectedIndex);}
        if (selectedDescriptionText != null && itemDetailViewer != null)
        {
            itemDetailViewer.ViewItem();
            itemDetailViewer.ShowInfo(itemName);
            selectedDescriptionText.text = itemDetailViewer.itemInfo.text;
        }
        if (buyButtonObject != null)
        {
            buyButtonObject.SetActive(!IsSold(shopData.consumableSold, selectedIndex));
        }
    }

    protected void UpdateRelicSelection()
    {
        if (!IndexValid(shopData.relics, selectedIndex)){return;}
        string relicName = shopData.relics[selectedIndex];
        if (selectedNameText != null){selectedNameText.text = relicName;}
        if (selectedPriceText != null){selectedPriceText.text = ReturnPriceText(shopData.relicPrices, selectedIndex);}
        if (selectedDescriptionText != null)
        {
            // TODO Replace with authored relic description display once relic runtime/data is complete.
            selectedDescriptionText.text = relicName;
        }
        if (buyButtonObject != null)
        {
            buyButtonObject.SetActive(!IsSold(shopData.relicSold, selectedIndex));
        }
    }

    // --- Buy Button ---
    public void TryBuySelected()
    {
        switch (selectedSection)
        {
            case "NormalBook":
            TryBuyBook(shopData.normalBooks, shopData.normalBookPrices, shopData.normalBookSold);
            return;
            case "RareBook":
            TryBuyBook(shopData.rareBooks, shopData.rareBookPrices, shopData.rareBookSold);
            return;
            case "ColorlessBook":
            TryBuyBook(shopData.colorlessBooks, shopData.colorlessBookPrices, shopData.colorlessBookSold);
            return;
            case "Consumable":
            TryBuyConsumable();
            return;
            case "Relic":
            TryBuyRelic();
            return;
        }
    }

    protected void TryBuyBook(List<string> books, List<string> prices, List<string> soldFlags)
    {
        if (!IndexValid(books, selectedIndex)){return;}
        if (IsSold(soldFlags, selectedIndex)){return;}
        if (!TrySpendGold(prices[selectedIndex])){return;}
        partyData.spellBook.GainBook(books[selectedIndex]);
        soldFlags[selectedIndex] = "1";
        UpdateAllDisplays();
    }

    protected void TryBuyConsumable()
    {
        if (!IndexValid(shopData.consumables, selectedIndex)){return;}
        if (IsSold(shopData.consumableSold, selectedIndex)){return;}
        if (!TrySpendGold(shopData.consumablePrices[selectedIndex])){return;}
        partyData.inventory.AddItemQuantity(shopData.consumables[selectedIndex]);
        shopData.consumableSold[selectedIndex] = "1";
        UpdateAllDisplays();
    }

    protected void TryBuyRelic()
    {
        if (!IndexValid(shopData.relics, selectedIndex)){return;}
        if (IsSold(shopData.relicSold, selectedIndex)){return;}

        // TODO: hook this into run relic ownership once relic save/runtime is finalized.
        Debug.Log("Relic purchase is not fully implemented yet. Needs a run-owned relic list.");
    }

    protected bool TrySpendGold(string priceString)
    {
        int price = 0;
        if (!int.TryParse(priceString, out price)){return false;}
        if (!partyData.inventory.EnoughGold(price)){return false;}
        partyData.inventory.SpendGold(price);
        return true;
    }

    // --- Priest Service Setup ---
    protected void UpdatePriestSection()
    {
        if (priestServiceObject != null)
        {
            priestServiceObject.SetActive(shopData != null && shopData.priestServiceUsed != "1");
        }
        if (priestServicePriceText != null)
        {
            priestServicePriceText.text = shopData != null ? shopData.priestServicePrice : "";
        }
        if (priestActorSelect != null && partyData != null)
        {
            priestActorSelect.SetSelectables(partyData.GetAllPartyNames());
        }
        UpdatePriestInjuries();
    }

    public void SelectPriestActor()
    {
        UpdatePriestInjuries();
    }

    protected void UpdatePriestInjuries()
    {
        if (priestInjurySelect == null)
        {
            return;
        }
        priestInjurySelect.SetSelectables(ReturnSelectedActorRemovableInjuries());
        UpdatePriestDescription();
    }

    public void SelectPriestInjury()
    {
        UpdatePriestDescription();
    }

    protected void UpdatePriestDescription()
    {
        if (priestServiceDescriptionText == null)
        {
            return;
        }
        string injuryName = GetSelectedPriestInjury();
        if (injuryName == "")
        {
            priestServiceDescriptionText.text = "";
        }
        else
        {
            priestServiceDescriptionText.text = "Remove the permanent injury: " + injuryName;
        }
        if (priestBuyButtonObject != null)
        {
            priestBuyButtonObject.SetActive(CanUsePriestService());
        }
    }

    protected List<string> ReturnSelectedActorRemovableInjuries()
    {
        List<string> injuries = new List<string>();
        if (partyData == null || priestActorSelect == null || priestActorSelect.GetSelected() < 0)
        {
            return injuries;
        }
        TacticActor actor = partyData.ReturnActorAtIndex(priestActorSelect.GetSelected());
        List<string> passives = actor.GetPassiveSkills();
        for (int i = 0; i < passives.Count; i++)
        {
            if (removableInjuries != null && removableInjuries.Contains(passives[i]))
            {
                injuries.Add(passives[i]);
            }
        }
        return injuries;
    }

    protected string GetSelectedPriestInjury()
    {
        if (priestInjurySelect == null || priestInjurySelect.GetSelected() < 0)
        {
            return "";
        }
        return priestInjurySelect.GetSelectedString();
    }

    protected bool CanUsePriestService()
    {
        if (shopData == null || shopData.priestServiceUsed == "1")
        {
            return false;
        }
        if (priestActorSelect == null || priestActorSelect.GetSelected() < 0)
        {
            return false;
        }
        if (GetSelectedPriestInjury() == "")
        {
            return false;
        }
        return partyData.inventory.EnoughGold(ReturnPriestPrice());
    }

    public void TryUsePriestService()
    {
        if (!CanUsePriestService()){return;}
        int actorIndex = priestActorSelect.GetSelected();
        string injuryName = GetSelectedPriestInjury();
        if (!RemoveInjuryFromActor(actorIndex, injuryName)){return;}
        partyData.inventory.SpendGold(ReturnPriestPrice());
        shopData.priestServiceUsed = "1";
        UpdateAllDisplays();
        UpdatePriestSection();
    }

    protected int ReturnPriestPrice()
    {
        int price = 0;
        if (shopData == null){return 0;}
        int.TryParse(shopData.priestServicePrice, out price);
        return price;
    }

    protected bool RemoveInjuryFromActor(int actorIndex, string injuryName)
    {
        if (partyData == null || actorIndex < 0 || injuryName == "")
        {
            return false;
        }
        TacticActor actor = partyData.ReturnActorAtIndex(actorIndex);
        List<string> passives = new List<string>(actor.GetPassiveSkills());
        List<string> levels = new List<string>(actor.GetPassiveLevels());
        int injuryIndex = passives.IndexOf(injuryName);
        if (injuryIndex < 0)
        {
            return false;
        }
        passives.RemoveAt(injuryIndex);
        if (injuryIndex < levels.Count)
        {
            levels.RemoveAt(injuryIndex);
        }
        actor.SetPassiveSkills(passives);
        actor.SetPassiveLevels(levels);
        partyData.UpdatePartyMember(actor, actorIndex);
        return true;
    }

    // --- Leaving ---
    public void LeaveStore()
    {
        if (stsManager != null)
        {
            stsManager.ReturnToMap();
        }
    }

    // --- Helpers ---
    protected bool IndexValid(List<string> list, int index)
    {
        return list != null && index >= 0 && index < list.Count;
    }

    protected bool IsSold(List<string> soldFlags, int index)
    {
        if (!IndexValid(soldFlags, index)){return false;}
        return soldFlags[index] == "1";
    }

    protected string ReturnPriceText(List<string> prices, int index)
    {
        if (!IndexValid(prices, index)){return "";}
        return prices[index];
    }
}
