using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// Roguelike shop scene. Reads pre-generated shop state and mutates only in-memory data while inside the scene.
// Saving should happen only when leaving through StSStateManager.
public class StSStore : MonoBehaviour
{
    // --- Core References ---
    public PartyDataManager partyData;
    public StSStateManager stsManager;
    public StSShopSaveData shopData;
    public StSRewardSaveData rewardData;
    public ItemDetailViewer itemDetailViewer;
    public SpriteContainer variantSprites;
    public Sprite priestServiceSprite;

    // --- Skillbook Data ---
    public StatDatabase skillBookData;
    public StatDatabase colorlessSkillBookData;
    public PassiveDetailViewer passiveDetailViewer;
    public ActiveDescriptionViewer activeDetailViewer;
    public SpellDetailViewer spellDetailViewer;

    // --- Shared Left Panel ---
    public TMP_Text goldText;
    public TMP_Text selectedNameText;
    public TMP_Text selectedPriceText;
    public TMP_Text selectedDescriptionText;
    public GameObject buyButtonObject;

    // --- Fixed Shop Slots ---
    public List<ShopSkillBookSlotDisplay> bookSlots;
    public List<ShopTextSlotDisplay> consumableSlots;
    public List<ShopTextSlotDisplay> relicSlots;

    // --- Priest Service ---
    public ShopTextSlotDisplay priestServiceSlot;
    public SelectList priestActorSelect;
    public SelectList priestInjurySelect;
    public StatDatabase allInjuries;

    // --- Runtime Sold State ---
    protected List<bool> bookSold = new List<bool>();
    protected List<bool> consumableSold = new List<bool>();
    protected List<bool> relicSold = new List<bool>();
    protected bool priestServiceUsed = false;

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
        ResetRuntimeSoldState();
        ResetSelection();
        UpdateAllDisplays();
    }

    protected void ResetRuntimeSoldState()
    {
        bookSold = NewFalseList(shopData != null ? shopData.books.Count : 0);
        consumableSold = NewFalseList(shopData != null ? shopData.consumables.Count : 0);
        relicSold = NewFalseList(shopData != null ? shopData.relics.Count : 0);
        priestServiceUsed = false;
    }

    protected List<bool> NewFalseList(int count)
    {
        List<bool> values = new List<bool>();
        for (int i = 0; i < count; i++)
        {
            values.Add(false);
        }
        return values;
    }

    protected void UpdateAllDisplays()
    {
        UpdateGoldDisplay();
        UpdateBookSlots();
        UpdateTextSlots(consumableSlots, shopData.consumables, shopData.consumablePrices, consumableSold, "Consumable");
        UpdateTextSlots(relicSlots, shopData.relics, shopData.relicPrices, relicSold, "Relic");
        UpdatePriestServiceSlot();
        UpdateSelectionDisplay();
    }

    protected void UpdateGoldDisplay()
    {
        if (goldText == null || partyData == null){return;}
        goldText.text = partyData.inventory.GetGold().ToString();
    }

    // --- Slot Population ---
    protected void UpdateBookSlots()
    {
        if (bookSlots == null || shopData == null){return;}
        for (int i = 0; i < bookSlots.Count; i++)
        {
            if (bookSlots[i] == null){continue;}
            bookSlots[i].store = this;
            bookSlots[i].index = i;
            if (IndexValid(shopData.books, i))
            {
                bool sold = IsRuntimeSold(bookSold, i);
                bookSlots[i].gameObject.SetActive(!sold);
                bookSlots[i].SetSlot(shopData.books[i], ReturnPriceText(shopData.bookPrices, i), sold, BookIsColorless(shopData.books[i]));
            }
            else
            {
                bookSlots[i].ResetSlot();
                bookSlots[i].gameObject.SetActive(false);
            }
        }
    }

    protected void UpdateTextSlots(List<ShopTextSlotDisplay> slots, List<string> names, List<string> prices, List<bool> soldFlags, string section)
    {
        if (slots == null){return;}
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == null){continue;}
            slots[i].store = this;
            slots[i].section = section;
            slots[i].index = i;
            if (IndexValid(names, i))
            {
                bool sold = IsRuntimeSold(soldFlags, i);
                slots[i].gameObject.SetActive(!sold);
                slots[i].SetSlot(names[i], ReturnPriceText(prices, i), sold, ReturnSlotIcon(section, names[i]));
            }
            else
            {
                slots[i].ResetSlot();
                slots[i].gameObject.SetActive(false);
            }
        }
    }

    protected void UpdatePriestServiceSlot()
    {
        if (priestServiceSlot == null){return;}
        priestServiceSlot.gameObject.SetActive(shopData != null && !priestServiceUsed);
        priestServiceSlot.store = this;
        priestServiceSlot.section = "PriestService";
        priestServiceSlot.index = 0;
        priestServiceSlot.SetSlot("Restore Injury", shopData != null ? shopData.priestServicePrice : "", priestServiceUsed, priestServiceSprite);
        if (priestActorSelect != null && partyData != null)
        {
            priestActorSelect.SetSelectables(partyData.GetAllPartyNames());
        }
        UpdatePriestInjuries();
    }

    // --- Selection Entry Point ---
    public void SelectSlot(string section, int index)
    {
        selectedSection = section;
        selectedIndex = index;
        UpdateSelectionDisplay();
    }

    protected void ResetSelection()
    {
        selectedSection = "";
        selectedIndex = -1;
        ClearHighlights();
        UpdateSelectionDisplay();
    }

    protected void ClearHighlights()
    {
        SetBookHighlights(-1);
        SetTextHighlights(consumableSlots, -1);
        SetTextHighlights(relicSlots, -1);
        if (priestServiceSlot != null){priestServiceSlot.SetHighlighted(false);}
    }

    protected void UpdateSelectionDisplay()
    {
        ClearHighlights();
        ClearLeftPanel();
        if (selectedIndex < 0){return;}

        switch (selectedSection)
        {
            case "Book":
            SetBookHighlights(selectedIndex);
            UpdateBookSelection();
            return;
            case "Consumable":
            SetTextHighlights(consumableSlots, selectedIndex);
            UpdateConsumableSelection();
            return;
            case "Relic":
            SetTextHighlights(relicSlots, selectedIndex);
            UpdateRelicSelection();
            return;
            case "PriestService":
            if (priestServiceSlot != null){priestServiceSlot.SetHighlighted(true);}
            UpdatePriestSelection();
            return;
        }
    }

    protected void ClearLeftPanel()
    {
        if (selectedNameText != null){selectedNameText.text = "";}
        if (selectedPriceText != null){selectedPriceText.text = "";}
        if (selectedDescriptionText != null){selectedDescriptionText.text = "";}
        if (buyButtonObject != null){buyButtonObject.SetActive(false);}
    }

    protected void SetBookHighlights(int index)
    {
        if (bookSlots == null){return;}
        for (int i = 0; i < bookSlots.Count; i++)
        {
            if (bookSlots[i] != null){bookSlots[i].SetHighlighted(i == index);}
        }
    }

    protected void SetTextHighlights(List<ShopTextSlotDisplay> slots, int index)
    {
        if (slots == null){return;}
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null){slots[i].SetHighlighted(i == index);}
        }
    }

    // --- Book Detail Display ---
    protected void UpdateBookSelection()
    {
        if (!IndexValid(shopData.books, selectedIndex)){return;}
        string bookName = shopData.books[selectedIndex];
        SetLeftPanel("Book", bookName, ReturnPriceText(shopData.bookPrices, selectedIndex), ReturnSkillBookDescription(bookName), !IsRuntimeSold(bookSold, selectedIndex));
    }

    protected string ReturnSkillBookDescription(string bookName)
    {
        string bookData = ReturnSkillBookData(bookName);
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

    protected string ReturnSkillBookData(string bookName)
    {
        if (skillBookData != null)
        {
            string normalData = skillBookData.ReturnValue(bookName);
            if (normalData != ""){return normalData;}
        }
        if (colorlessSkillBookData != null)
        {
            return colorlessSkillBookData.ReturnValue(bookName);
        }
        return "";
    }

    protected bool BookIsColorless(string bookName)
    {
        if (skillBookData != null && skillBookData.ReturnValue(bookName) != "")
        {
            return false;
        }
        return colorlessSkillBookData != null && colorlessSkillBookData.ReturnValue(bookName) != "";
    }

    // --- Item / Relic Detail Display ---
    protected void UpdateConsumableSelection()
    {
        if (!IndexValid(shopData.consumables, selectedIndex)){return;}
        string itemName = shopData.consumables[selectedIndex];
        string description = itemName;
        if (itemDetailViewer != null)
        {
            itemDetailViewer.ViewItem();
            itemDetailViewer.ShowInfo(itemName);
            description = itemDetailViewer.itemInfo.text;
        }
        SetLeftPanel("Consumable", itemName, ReturnPriceText(shopData.consumablePrices, selectedIndex), description, !IsRuntimeSold(consumableSold, selectedIndex));
    }

    protected void UpdateRelicSelection()
    {
        if (!IndexValid(shopData.relics, selectedIndex)){return;}
        string relicName = shopData.relics[selectedIndex];
        SetLeftPanel("Relic", relicName, ReturnPriceText(shopData.relicPrices, selectedIndex), relicName, !IsRuntimeSold(relicSold, selectedIndex));
    }

    // --- Priest Service Detail Display ---
    public void SelectPriestActor()
    {
        UpdatePriestInjuries();
        if (selectedSection == "PriestService")
        {
            UpdateSelectionDisplay();
        }
    }

    protected void UpdatePriestInjuries()
    {
        if (priestInjurySelect == null){return;}
        priestInjurySelect.SetSelectables(ReturnSelectedActorRemovableInjuries());
    }

    public void SelectPriestInjury()
    {
        if (selectedSection == "PriestService")
        {
            UpdateSelectionDisplay();
        }
    }

    protected void UpdatePriestSelection()
    {
        string injuryName = GetSelectedPriestInjury();
        string description = injuryName == "" ? "Select an actor and permanent injury to restore." : "Remove the permanent injury: " + injuryName;
        SetLeftPanel("Priest Service", "Restore Injury", shopData.priestServicePrice, description, CanUsePriestService());
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
        List<string> removableInjuries = allInjuries.GetAllKeys();
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
        if (shopData == null || priestServiceUsed){return false;}
        if (priestActorSelect == null || priestActorSelect.GetSelected() < 0){return false;}
        if (GetSelectedPriestInjury() == ""){return false;}
        return partyData.inventory.EnoughGold(ReturnPriestPrice());
    }

    protected void SetLeftPanel(string section, string itemName, string price, string description, bool canBuy)
    {
        if (selectedNameText != null){selectedNameText.text = itemName;}
        if (selectedPriceText != null){selectedPriceText.text = price;}
        if (selectedDescriptionText != null){selectedDescriptionText.text = description;}
        if (buyButtonObject != null){buyButtonObject.SetActive(canBuy);}
    }

    protected Sprite ReturnSlotIcon(string section, string itemName)
    {
        return variantSprites.SpriteDictionary(itemName);
    }

    // --- Buy Button ---
    public void TryBuySelected()
    {
        switch (selectedSection)
        {
            case "Book":
            TryBuyBook();
            return;
            case "Consumable":
            TryBuyConsumable();
            return;
            case "Relic":
            TryBuyRelic();
            return;
            case "PriestService":
            TryUsePriestService();
            return;
        }
    }

    protected void TryBuyBook()
    {
        if (!IndexValid(shopData.books, selectedIndex)){return;}
        if (IsRuntimeSold(bookSold, selectedIndex)){return;}
        if (!TrySpendGold(shopData.bookPrices[selectedIndex])){return;}
        partyData.spellBook.GainBook(shopData.books[selectedIndex]);
        bookSold[selectedIndex] = true;
        UpdateAllDisplays();
    }

    protected void TryBuyConsumable()
    {
        if (!IndexValid(shopData.consumables, selectedIndex)){return;}
        if (IsRuntimeSold(consumableSold, selectedIndex)){return;}
        if (!TrySpendGold(shopData.consumablePrices[selectedIndex])){return;}
        partyData.inventory.AddItemQuantity(shopData.consumables[selectedIndex]);
        consumableSold[selectedIndex] = true;
        UpdateAllDisplays();
    }

    protected void TryBuyRelic()
    {
        if (!IndexValid(shopData.relics, selectedIndex)){return;}
        if (IsRuntimeSold(relicSold, selectedIndex)){return;}

        // TODO: hook this into run relic ownership once relic save/runtime is finalized.
        Debug.Log("Relic purchase is not fully implemented yet. Needs a run-owned relic list.");
    }

    public void TryUsePriestService()
    {
        if (!CanUsePriestService()){return;}
        int actorIndex = priestActorSelect.GetSelected();
        string injuryName = GetSelectedPriestInjury();
        if (!RemoveInjuryFromActor(actorIndex, injuryName)){return;}
        partyData.inventory.SpendGold(ReturnPriestPrice());
        priestServiceUsed = true;
        UpdateAllDisplays();
    }

    protected bool TrySpendGold(string priceString)
    {
        int price = 0;
        if (!int.TryParse(priceString, out price)){return false;}
        if (!partyData.inventory.EnoughGold(price)){return false;}
        partyData.inventory.SpendGold(price);
        return true;
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
        if (partyData == null || actorIndex < 0 || injuryName == ""){return false;}
        TacticActor actor = partyData.ReturnActorAtIndex(actorIndex);
        List<string> passives = new List<string>(actor.GetPassiveSkills());
        List<string> levels = new List<string>(actor.GetPassiveLevels());
        int injuryIndex = passives.IndexOf(injuryName);
        if (injuryIndex < 0){return false;}
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

    protected bool IndexValid(List<bool> list, int index)
    {
        return list != null && index >= 0 && index < list.Count;
    }

    protected bool IsRuntimeSold(List<bool> soldFlags, int index)
    {
        if (!IndexValid(soldFlags, index)){return false;}
        return soldFlags[index];
    }

    protected string ReturnPriceText(List<string> prices, int index)
    {
        if (!IndexValid(prices, index)){return "";}
        return prices[index];
    }
}
