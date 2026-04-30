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
    public StatDatabase linkedSkillBookData;
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
    public TMP_Text priestServicePrice;
    public GameObject removeInjuryObject;
    public GameObject priestActorSelectObject;
    public GameObject priestInjurySelectObject;
    public SelectList priestActorSelect;
    public SelectList priestInjurySelect;
    public TMP_Text selectedInjuryDetails;
    public StatDatabase allInjuries;
    protected List<int> priestActorPartyIndexes = new List<int>();
    protected int selectedPriestPartyIndex = -1;
    // --- Standalone Testing ---
    public bool generateShopOnStart = false;
    // --- Runtime Sold State ---
    public List<string> bookNames;
    public List<string> relicNames;
    public List<string> itemNames;
    public List<bool> bookSold;
    public List<bool> consumableSold;
    public List<bool> relicSold;
    public bool priestServiceUsed = false;

    // --- Current Selection State ---
    public string selectedSection = "";
    public int selectedIndex = -1;

    protected virtual void Start()
    {
        if (generateShopOnStart)
        {
            GenerateStandaloneShop();
        }
        LoadStore();
    }

    [ContextMenu("Generate Standalone Shop")]
    public void GenerateStandaloneShop()
    {
        if (shopData == null || rewardData == null){return;}
        shopData.GenerateShop(rewardData);
        bookNames = new List<string>(shopData.books);
        relicNames = new List<string>(shopData.relics);
        itemNames = new List<string>(shopData.consumables);
    }

    // --- Scene Setup ---
    protected void LoadStore()
    {
        ResetSelection();
        UpdateAllDisplays();
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
        for (int i = 0; i < bookSlots.Count; i++)
        {
            bookSlots[i].store = this;
            bookSlots[i].index = i;
            if (bookSold[i])
            {
                bookSlots[i].ResetSlot();
                bookSlots[i].gameObject.SetActive(false);
            }
            else
            {
                bookSlots[i].gameObject.SetActive(true);
                bookSlots[i].SetSlot(shopData.books[i], ReturnPriceText(shopData.bookPrices, i), false, i >= 5);
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
        priestServiceSlot.gameObject.SetActive(!priestServiceUsed);
        priestServicePrice.text = ReturnPriestPrice().ToString();
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
        }
    }

    protected void ClearLeftPanel()
    {
        selectedNameText.text = "";
        selectedPriceText.text = "";
        selectedDescriptionText.text = "";
        buyButtonObject.SetActive(false);
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
        return linkedSkillBookData.ReturnValue(bookName);
    }

    // --- Item Detail Display ---
    protected void UpdateConsumableSelection()
    {
        string itemName = shopData.consumables[selectedIndex];
        string description = itemName;
        description = activeDetailViewer.ReturnActiveDescriptionFromName(itemName);
        SetLeftPanel("Consumable", itemName, ReturnPriceText(shopData.consumablePrices, selectedIndex), description, !IsRuntimeSold(consumableSold, selectedIndex));
    }

    protected void UpdateRelicSelection()
    {
        if (!IndexValid(shopData.relics, selectedIndex)){return;}
        string relicName = shopData.relics[selectedIndex];
        SetLeftPanel("Relic", relicName, ReturnPriceText(shopData.relicPrices, selectedIndex), relicName, !IsRuntimeSold(relicSold, selectedIndex));
    }

    // --- Priest Service Detail Display ---
    public void OpenPriestActorSelect()
    {
        if (!CanStartPriestService())
        {
            return;
        }
        selectedPriestPartyIndex = -1;
        removeInjuryObject.SetActive(true);
        priestActorSelectObject.SetActive(true);
        priestInjurySelectObject.SetActive(false);
        priestActorSelect.SetSelectables(ReturnInjuredActorNames());
    }
    public void SelectPriestActor()
    {
        int selectedListIndex = priestActorSelect.GetSelected();
        if (selectedListIndex < 0){return;}
        selectedPriestPartyIndex = priestActorPartyIndexes[selectedListIndex];
        priestInjurySelectObject.SetActive(true);
        priestInjurySelect.SetSelectables(ReturnActorRemovableInjuries(selectedPriestPartyIndex));
    }
    public void SelectPriestInjury()
    {
        // TODO Display the injury level and injury effects.
    }
    protected bool CanStartPriestService()
    {
        if (priestServiceUsed){return false;}
        if (!partyData.inventory.EnoughGold(ReturnPriestPrice())){return false;}
        if (ReturnInjuredActorNames().Count <= 0)
        {
            return false;
        }
        return true;
    }
    protected List<string> ReturnInjuredActorNames()
    {
        priestActorPartyIndexes.Clear();
        List<string> actorNames = new List<string>();
        List<string> partyNames = partyData.GetAllPartyNames();
        for (int i = 0; i < partyNames.Count; i++)
        {
            if (ReturnActorRemovableInjuries(i).Count > 0)
            {
                priestActorPartyIndexes.Add(i);
                actorNames.Add(partyNames[i]);
            }
        }
        return actorNames;
    }
    protected List<string> ReturnActorRemovableInjuries(int partyIndex)
    {
        List<string> injuries = new List<string>();
        TacticActor actor = partyData.ReturnActorAtIndex(partyIndex);
        List<string> passives = actor.GetPassiveSkills();
        List<string> removableInjuries = allInjuries.GetAllKeys();
        for (int i = 0; i < passives.Count; i++)
        {
            if (removableInjuries.Contains(passives[i]))
            {
                injuries.Add(passives[i]);
            }
        }
        return injuries;
    }
    // Left Panel Details
    protected void SetLeftPanel(string section, string itemName, string price, string description, bool canBuy)
    {
        selectedNameText.text = itemName;
        selectedPriceText.text = price;
        selectedDescriptionText.text = description;
        buyButtonObject.SetActive(canBuy);
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
    public void RemoveInjuryFromActor()
    {
        int actorIndex = priestActorSelect.GetSelected();
        int injuryIndex = priestInjurySelect.GetSelected();
        if (actorIndex < 0 || injuryIndex < 0){return;}
        TacticActor actor = partyData.ReturnActorAtIndex(actorIndex);
        List<string> passives = new List<string>(actor.GetPassiveSkills());
        List<string> levels = new List<string>(actor.GetPassiveLevels());
        passives.RemoveAt(injuryIndex);
        levels.RemoveAt(injuryIndex);
        actor.SetPassiveSkills(passives);
        actor.SetPassiveLevels(levels);
        partyData.UpdatePartyMember(actor, actorIndex);
        priestServiceUsed = true;
        partyData.inventory.SpendGold(ReturnPriestPrice());
        removeInjuryObject.SetActive(false);
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
