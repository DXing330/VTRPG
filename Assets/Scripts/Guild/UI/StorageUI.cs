using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class StorageUI : MonoBehaviour
{
    void Start()
    {
        storage.Load();
        UpdateDungeonStorage();
        UpdateGoldStorage();
    }
    public GeneralUtility utility;
    public PartyDataManager partyData;
    public GuildStorage storage;
    public ItemDetailViewer itemDetailViewer;
    public void ViewDungeonItem(bool fromBag)
    {
        string selectedItem = "";
        if (fromBag)
        {
            selectedItem = dungeonBagSelect.GetSelectedString();
        }
        else
        {
            selectedItem = dungeonStorageSelect.GetSelectedString();
        }
        itemDetailViewer.ShowDungeonItemInfo(selectedItem);
    }
    public void ViewItem(bool fromBag)
    {
        string selectedItem = "";
        if (fromBag)
        {
            selectedItem = bagSelect.GetSelectedString();
        }
        else
        {
            selectedItem = storageSelect.GetSelectedString();
        }
        itemDetailViewer.ShowInfo(selectedItem);
    }
    public List<GameObject> panels;
    public void ActivatePanel(int index)
    {
        utility.DisableGameObjects(panels);
        panels[index].SetActive(true);
    }
    public SelectList dungeonBagSelect;
    public TMP_Text dungeonBagLimitText;
    public SelectList dungeonStorageSelect;
    public TMP_Text dungeonStorageLimitText;
    public void UpdateDungeonStorage(bool savePage = false)
    {
        int bagPage = 0;
        int storagePage = 0;
        if (savePage)
        {
            bagPage = dungeonBagSelect.GetPage();
            storagePage = dungeonStorageSelect.GetPage();
        }
        dungeonBagSelect.SetSelectables(partyData.dungeonBag.GetItems());
        dungeonBagLimitText.text = partyData.dungeonBag.ReturnBagLimitString();
        dungeonStorageSelect.SetSelectables(storage.GetStoredDungeonItems());
        dungeonStorageLimitText.text = storage.ReturnDungeonStorageLimitString();
        dungeonBagSelect.SetPage(bagPage);
        dungeonStorageSelect.SetPage(storagePage);
    }
    public void StoreItem()
    {
        if (storage.MaxedDungeonStorage()){return;}
        int selected = dungeonBagSelect.GetSelected();
        if (selected < 0){return;}
        partyData.dungeonBag.UseItem(dungeonBagSelect.GetSelectedString());
        storage.StoreDungeonItem(dungeonBagSelect.GetSelectedString());
        UpdateDungeonStorage(true);
    }
    public void StoreAllItems()
    {
        if (!storage.DungeonStorageAvailable(partyData.dungeonBag.GetItemCount())){return;}
        storage.StoreDungeonItems(partyData.dungeonBag.GetItems());
        partyData.dungeonBag.DropItems();
        UpdateDungeonStorage(true);
    }
    public void WithdrawItem()
    {
        if (partyData.dungeonBag.BagFull()){return;}
        int selected = dungeonStorageSelect.GetSelected();
        if (selected < 0){return;}
        partyData.dungeonBag.GainItem(dungeonStorageSelect.GetSelectedString());
        storage.WithdrawDungeonItem(dungeonStorageSelect.GetSelectedString());
        UpdateDungeonStorage(true);
    }
    public SelectList bagSelect;
    public TMP_Text bagLimitText;
    public SelectList storageSelect;
    public TMP_Text storageLimitText;
    public void UpdateInventoryStorage(bool savePage = false)
    {
        int bagPage = 0;
        int storagePage = 0;
        if (savePage)
        {
            bagPage = bagSelect.GetPage();
            storagePage = storageSelect.GetPage();
        }
        bagSelect.SetSelectables(partyData.inventory.GetItems());
        bagLimitText.text = partyData.inventory.ReturnBagLimitString();
        storageSelect.SetSelectables(storage.GetStoredItems());
        storageLimitText.text = storage.ReturnStorageLimitString();
        bagSelect.SetPage(bagPage);
        storageSelect.SetPage(storagePage);
    }
    public void StoreInventoryItem()
    {
        if (storage.MaxedStorage()){return;}
        int selected = bagSelect.GetSelected();
        if (selected < 0){return;}
        string selectedItem = bagSelect.GetSelectedString();
        partyData.inventory.RemoveItemAtIndex(selected);
        storage.StoreItem(selectedItem);
        UpdateInventoryStorage(true);
    }
    public void StoreAllInventoryItems()
    {
        if (!storage.StorageAvailable(partyData.inventory.GetItemCount())){return;}
        storage.StoreItems(partyData.inventory.GetItems());
        partyData.inventory.ClearItems();
        UpdateInventoryStorage(true);
    }
    public void WithdrawInventoryItem()
    {
        if (partyData.inventory.InventoryFull()){return;}
        int selected = storageSelect.GetSelected();
        if (selected < 0){return;}
        partyData.inventory.AddItemQuantity(storageSelect.GetSelectedString());
        storage.WithdrawItem(storageSelect.GetSelectedString());
        UpdateInventoryStorage(true);
    }
    public TMP_Text storedGold;
    public TMP_Text withdrawnGoldText;
    public int withdrawnGold;
    public TMP_Text bagGold;
    public TMP_Text depositedGoldText;
    public int depositedGold;
    public void UpdateGoldStorage()
    {
        storedGold.text = storage.GetStoredGold().ToString();
        bagGold.text = partyData.inventory.GetGold().ToString();
        withdrawnGold = 0;
        depositedGold = 0;
        withdrawnGoldText.text = withdrawnGold.ToString();
        depositedGoldText.text = depositedGold.ToString();
    }
    public void UpdateDWText()
    {
        withdrawnGoldText.text = withdrawnGold.ToString();
        depositedGoldText.text = depositedGold.ToString();
    }
    public void UpdateDeposit(int amount)
    {
        if (amount == -1)
        {
            depositedGold = partyData.inventory.GetGold();
        }
        else if (amount == 0)
        {
            depositedGold = 0;
        }
        else
        {
            int difference = partyData.inventory.GetGold() - depositedGold;
            if (difference >= amount)
            {
                depositedGold += amount;
            }
        }
        UpdateDWText();
    }
    public void UpdateWithdrawal(int amount)
    {
        if (amount == -1)
        {
            withdrawnGold = storage.GetStoredGold();
        }
        else if (amount == 0)
        {
            withdrawnGold = 0;
        }
        else
        {
            int difference = storage.GetStoredGold() - withdrawnGold;
            if (difference >= amount)
            {
                withdrawnGold += amount;
            }
        }
        UpdateDWText();
    }
    public void ConfirmDeposit()
    {
        partyData.inventory.SpendGold(depositedGold);
        storage.StoreGold(depositedGold);
        UpdateGoldStorage();
    }
    public void ConfirmWithdrawl()
    {
        partyData.inventory.GainGold(withdrawnGold);
        storage.WithdrawGold(withdrawnGold);
        UpdateGoldStorage();
    }
}
