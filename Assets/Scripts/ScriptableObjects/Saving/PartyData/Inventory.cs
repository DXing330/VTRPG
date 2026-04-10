using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Mainly used to store battle items and gold.
[CreateAssetMenu(fileName = "Inventory", menuName = "ScriptableObjects/DataContainers/SavedData/Inventory", order = 1)]
public class Inventory : SavedData
{
    public string delimiterTwo;
    // GOLD
    public int gold;
    public void SetGold(int amount)
    {
        gold = amount;
    }
    public void LoseGold(int amount = -1)
    {
        if (amount < 0 || amount >= gold)
        {
            gold = 0;
            return;
        }
        gold -= amount;
    }
    public int GetGold(){return gold;}
    public void GainGold(int amount)
    {
        gold += amount;
    }
    public bool EnoughGold(int amount)
    {
        return gold >= amount;
    }
    public void SpendGold(int amount)
    {
        gold -= amount;
    }
    public void CollectPay(int days, int rank)
    {
        GainGold(Mathf.Max(1, days * rank));
    }
    // ITEM LIMIT
    public int minimumItemLimit = 16;
    public int itemLimit;
    public void SetItemLimit(int newInfo)
    {
        itemLimit = newInfo;
    }
    public int GetItemLimit()
    {
        return itemLimit;
    }
    public void IncreaseItemLimit(int newInfo = 1)
    {
        itemLimit += newInfo;
    }
    public bool InventoryFull()
    {
        return items.Count >= itemLimit;
    }
    public string ReturnBagLimitString()
    {
        return items.Count + "/" + itemLimit;
    }
    // SAVING/LOADING
    public override void NewGame()
    {
        allData = newGameData;
        if (allData.Contains(delimiter)){dataList = allData.Split(delimiter).ToList();}
        else{return;}
        for (int i = 0; i < dataList.Count; i++)
        {
            LoadStat(dataList[i]);
        }
        Save();
    }

    public override void Save()
    {
        dataPath = Application.persistentDataPath+"/"+filename;
        allData = "Gold=" + GetGold() + delimiter;
        allData += "ItemLimit=" + GetItemLimit() + delimiter;
        allData += "Items=" + String.Join(delimiterTwo, items) + delimiter;
        allData += "IDs=" + String.Join(delimiterTwo, assignedActorIDs) + delimiter;
        File.WriteAllText(dataPath, allData);
    }

    public override void Load()
    {
        dataPath = Application.persistentDataPath+"/"+filename;
        if (File.Exists(dataPath)){allData = File.ReadAllText(dataPath);}
        else{allData = newGameData;}
        if (allData.Contains(delimiter)){dataList = allData.Split(delimiter).ToList();}
        else{return;}
        gold = 0;
        itemLimit = minimumItemLimit;
        ClearItems();
        for (int i = 0; i < dataList.Count; i++)
        {
            LoadStat(dataList[i]);
        }
        RemoveEmptyItems();
    }

    protected void LoadStat(string data)
    {
        string[] blocks = data.Split("=");
        if (blocks.Length < 2){return;}
        string stat = blocks[1];
        switch (blocks[0])
        {
            default:
            break;
            case "Gold":
            SetGold(int.Parse(stat));
            break;
            case "ItemLimit":
            SetItemLimit(int.Parse(stat));
            break;
            case "Items":
            items = stat.Split(delimiterTwo).ToList();
            break;
            case "IDs":
            assignedActorIDs = stat.Split(delimiterTwo).ToList();
            break;
        }
    }
    // ITEMS
    public List<string> items;
    public void ClearItems()
    {
        items = new List<string>();
        assignedActorIDs = new List<string>();
    }
    public List<string> GetItems()
    {
        return items;
    }
    public int GetItemCount(){return items.Count;}
    public List<string> assignedActorIDs;
    public void UnassignItem(int itemIndex)
    {
        assignedActorIDs[itemIndex] = "-1";
    }
    public void AssignToActor(int itemIndex, int actorID)
    {
        assignedActorIDs[itemIndex] = actorID.ToString();
    }
    public int AssignedToIDCount(int id)
    {
        string idStr = id.ToString();
        int count = 0;
        for (int i = 0; i < assignedActorIDs.Count; i++)
        {
            if (assignedActorIDs[i] == idStr){count++;}
        }
        return count;
    }
    public string GetAssignedActorIDFromIndex(int index)
    {
        return assignedActorIDs[index];
    }
    public void ActorUsesItem(string itemName, int actorID)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == itemName && assignedActorIDs[i] == actorID.ToString())
            {
                RemoveItemAtIndex(i);
                return;
            }
        }
    }
    public int ReturnQuantityOfItem(string itemName)
    {
        int count = 0;
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == itemName)
            {
                count++;
            }
        }
        return count;
    }
    public bool ItemExists(string itemName)
    {
        return ReturnQuantityOfItem(itemName) > 0;
    }
    public bool QuantityExists(int quantity, string itemName)
    {
        return ReturnQuantityOfItem(itemName) >= quantity;
    }
    public bool MultiQuantityExists(List<string> itemNames, List<int> quantities)
    {
        for (int i = 0; i < itemNames.Count; i++)
        {
            if (!QuantityExists(quantities[i], itemNames[i]))
            {
                return false;
            }
        }
        return true;
    }
    // Always add/remove items/IDs together
    protected void GainItem(string itemName)
    {
        if (InventoryFull()){return;}
        if (itemName.Length <= 1){return;}
        items.Add(itemName);
        assignedActorIDs.Add("-1");
    }
    protected void RemoveEmptyItems()
    {
        for (int i = items.Count - 1; i >= 0; i--)
        {
            if (items[i].Length <= 1)
            {
                RemoveItemAtIndex(i);
            }
        }
    }
    public void RemoveItemAtIndex(int index)
    {
        items.RemoveAt(index);
        assignedActorIDs.RemoveAt(index);
    }
    protected void RemoveItem(string itemName)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == itemName)
            {
                RemoveItemAtIndex(i);
                return;
            }
        }
    }
    public void RemoveRune(string runeName)
    {
        RemoveItem(runeName);
    }
    // Should only be called after confirming that the quantity exists.
    public void RemoveItemQuantity(int quantity, string itemName)
    {
        for (int i = 0; i < quantity; i++)
        {
            RemoveItem(itemName);
        }
    }
    public void RemoveMultiItems(List<string> itemNames, List<int> quantities)
    {
        for (int i = 0; i < itemNames.Count; i++)
        {
            RemoveItemQuantity(quantities[i], itemNames[i]);
        }
    }
    public void AddItemQuantity(string itemName, int quantity = 1)
    {
        for (int i = 0; i < quantity; i++)
        {
            if (InventoryFull()){break;}
            GainItem(itemName);
        }
    }
}
