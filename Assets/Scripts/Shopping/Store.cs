using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Store : MonoBehaviour
{
    protected virtual void Start(){LoadStore();}
    public string storeName;
    public int baseEquipStock;
    public int baseItemStock;
    public int equipStockPerRank;
    public int itemStockPerRank;
    public PartyDataManager partyData;
    public InventoryUI inventoryUI;
    public ItemDetailViewer itemDetailViewer;
    public StatDatabase storeData;
    protected virtual void LoadStore()
    {
        string[] storeStuff = storeData.ReturnValue(storeName).Split("|");
        equipmentSold = storeStuff[0].Split(",").ToList();
        equipmentPrices = storeStuff[1].Split(",").ToList();
        itemsSolds = storeStuff[2].Split(",").ToList();
        itemsPrices = storeStuff[3].Split(",").ToList();
        if (dungeonItems)
        {
            dungeonItemsSolds = storeStuff[4].Split(",").ToList();
            dungeonItemsPrices = storeStuff[5].Split(",").ToList();
        }
        TrimStock();
        UpdateDisplay();
    }
    protected void TrimStock()
    {
        int rank = partyData.guildCard.GetGuildRank();
        for (int i = equipmentSold.Count - 1; i >= 0; i--)
        {
            if (i > baseEquipStock + (rank * equipStockPerRank))
            {
                equipmentSold.RemoveAt(i);
                equipmentPrices.RemoveAt(i);
                continue;
            }
            break;
        }
        for (int i = itemsSolds.Count - 1; i >= 0; i--)
        {
            if (i >= baseItemStock + (rank * itemStockPerRank))
            {
                itemsSolds.RemoveAt(i);
                itemsPrices.RemoveAt(i);
                continue;
            }
            break;
        }
    }
    public List<string> equipmentSold;
    public List<string> equipmentPrices;
    public List<string> itemsSolds;
    public List<string> itemsPrices;
    public List<string> dungeonItemsSolds;
    public List<string> dungeonItemsPrices;
    public List<TMP_Text> itemsOwned;
    public List<TMP_Text> equipmentOwned;
    public SelectStatTextList equipmentDisplay;
    public SelectStatTextList itemsDisplay;
    public bool dungeonItems = false;
    public SelectStatTextList dungeonItemDisplay;
    public bool buyingDungeonItems = false;
    protected void ResetBuying(int type)
    {
        buyingEquipment = false;
        buyingItems = false;
        if (type != 0)
        {
            equipmentDisplay.ResetSelected();
        }
        if (type != 1)
        {
            itemsDisplay.ResetSelected();
        }
        if (dungeonItems && type != 2)
        {
            buyingDungeonItems = false;
            dungeonItemDisplay.ResetSelected();
        }
    }
    public void ClickOnDungeonItem()
    {
        if (!dungeonItems){return;}
        ResetBuying(2);
        buyingDungeonItems = true;
        string item = dungeonItemDisplay.GetSelectedStat();
        itemDetailViewer.ShowDungeonItemInfo(item);
    }
    public bool buyingEquipment = false;
    public void ClickOnEquipment()
    {
        ResetBuying(0);
        buyingEquipment = true;
        itemDetailViewer.ViewEquip();
        itemDetailViewer.ShowInfo(equipmentDisplay.GetSelectedStat());
    }
    public bool buyingItems = false;
    public void ClickOnItem()
    {
        ResetBuying(1);
        buyingItems = true;
        itemDetailViewer.ViewItem();
        itemDetailViewer.ShowInfo(itemsDisplay.GetSelectedStat());
    }
    protected void UpdateDisplay()
    {
        equipmentDisplay.SetStatsAndData(equipmentSold, equipmentPrices);
        itemsDisplay.SetStatsAndData(itemsSolds, itemsPrices);
        if (dungeonItems)
        {
            dungeonItemDisplay.SetStatsAndData(dungeonItemsSolds, dungeonItemsPrices);
        }
        UpdateQuantityOwned();
    }
    protected void ResetQuantityOwned()
    {
        for (int i = 0; i < itemsOwned.Count; i++)
        {
            itemsOwned[i].text = "";
        }
        for (int i = 0; i < equipmentOwned.Count; i++)
        {
            equipmentOwned[i].text = "";
        }
    }
    public void UpdateQuantityOwned()
    {
        ResetQuantityOwned();
        int quantity = 0;
        // Needs to be based on the SelectStatTextList
        for (int i = 0; i < itemsDisplay.GetListLength(); i++)
        {
            string itemName = itemsDisplay.GetCurrentPageStat(i);
            if (itemName == ""){break;}
            quantity = partyData.inventory.ReturnQuantityOfItem(itemName);
            itemsOwned[i].text = quantity.ToString()+"X";
        }
        for (int i = 0; i < equipmentDisplay.GetListLength(); i++)
        {
            string equipName = equipmentDisplay.GetCurrentPageStat(i);
            if (equipName == "") { break; }
            quantity = partyData.equipmentInventory.ReturnEquipmentQuantity(equipName);
            equipmentOwned[i].text = quantity.ToString()+"X";
        }
    }
    public void TryToBuyItem()
    {
        int index = itemsDisplay.GetSelected();
        if (index == -1 || index >= itemsSolds.Count){return;}
        // Check the price.
        int price = int.Parse(itemsPrices[index]);
        if (!partyData.inventory.EnoughGold(price)){return;}
        // Pay the price.
        partyData.inventory.SpendGold(price);
        // Get the item.
        partyData.inventory.AddItemQuantity(itemsSolds[index]);
        UpdateQuantityOwned();
        inventoryUI.UpdateKeyValues();
    }
    public void TryToBuyEquipment()
    {
        int index = equipmentDisplay.GetSelected();
        if (index == -1 || index >= equipmentSold.Count){return;}
        int price = int.Parse(equipmentPrices[index]);
        if (!partyData.inventory.EnoughGold(price)){return;}
        partyData.inventory.SpendGold(price);
        partyData.equipmentInventory.AddEquipmentByName(equipmentSold[index]);
        UpdateQuantityOwned();
        inventoryUI.UpdateKeyValues();
    }
    public void TryToBuyDungeonItem()
    {
        if (!dungeonItems){return;}
        if (partyData.dungeonBag.BagFull()){return;}
        int index = dungeonItemDisplay.GetSelected();
        if (index < 0){return;}
        int price = int.Parse(dungeonItemDisplay.GetSelectedData());
        if (!partyData.inventory.EnoughGold(price)){return;}
        partyData.inventory.SpendGold(price);
        partyData.dungeonBag.GainItem(dungeonItemDisplay.GetSelectedStat());
        inventoryUI.UpdateKeyValues();
    }

    public void TryToBuy()
    {
        if (buyingEquipment)
        {
            TryToBuyEquipment();
        }
        else if (buyingItems)
        {
            TryToBuyItem();
        }
        else if (buyingDungeonItems)
        {
            TryToBuyDungeonItem();
        }
    }
}
