using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This will store relics for the roguelike mode as well.
[CreateAssetMenu(fileName = "DungeonBag", menuName = "ScriptableObjects/DataContainers/SavedData/DungeonBag", order = 1)]
public class DungeonBag : SavedData
{
    public string delimiterTwo;
    public int maxCapacity = 16;
    public int GetMaxBagSpace(){return maxCapacity;}
    public void AddCapacity(int amount = 1)
    {
        maxCapacity += amount;
    }
    public bool BagFull()
    {
        return items.Count >= maxCapacity;
    }
    public void TrimItems()
    {
        if (items.Count > maxCapacity)
        {
            List<string> trimmedItems = new List<string>();
            List<int> trimmedHolders = new List<int>();
            for (int i = 0; i < maxCapacity; i++)
            {
                trimmedItems.Add(items[i]);
                trimmedHolders.Add(itemHeldByID[i]);
            }
            SetItems(trimmedItems);
            SetHolders(trimmedHolders);
        }
    }
    public List<int> itemHeldByID;
    public int GetHeldByIDFromIndex(int index)
    {
        return itemHeldByID[index];
    }
    public List<string> GetHeldItemsOfIDs(List<int> actorIDs)
    {
        List<string> heldItems = new List<string>();
        for (int i = 0; i < actorIDs.Count; i++)
        {
            heldItems.Add(GetHeldItemOfID(actorIDs[i]));
        }
        return heldItems;
    }
    public string GetHeldItemOfID(int ID)
    {
        if (ID < 0){return "";}
        for (int i = 0; i < itemHeldByID.Count; i++)
        {
            if (itemHeldByID[i] == ID){return items[i];}
        }
        return "";
    }
    public void SetHolders(List<int> newInfo)
    {
        itemHeldByID = newInfo;
    }
    public void UnequipItem(string itemName, TacticActor actor)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == itemName && itemHeldByID[i] == actor.GetID())
            {
                itemHeldByID[i] = -1;
                return;
            }
        }
    }
    public int UnequipItemAtIndex(int itemIndex)
    {
        if (itemIndex < 0 || itemIndex >= items.Count){return -1;}
        int oldHolder = itemHeldByID[itemIndex];
        itemHeldByID[itemIndex] = -1;
        return oldHolder;
    }
    public int EquipItemAtIndex(int itemIndex, TacticActor actor)
    {
        if (itemIndex < 0 || itemIndex >= items.Count){return -1;}
        int oldHolder = itemHeldByID[itemIndex];
        itemHeldByID[itemIndex] = actor.GetID();
        return oldHolder;
    }
    public List<string> items;
    public List<string> GetItems()
    {
        return new List<string>(items);
    }
    public int GetItemCount(){return items.Count;}
    public void DropItems()
    {
        items.Clear();
        itemHeldByID.Clear();
    }
    public void SetItems(List<string> newInfo)
    {
        utility.RemoveEmptyListItems(newInfo);
        items = newInfo;
    }
    public string ReturnBagLimitString()
    {
        return items.Count + "/" + maxCapacity;
    }
    public void GainItem(string newItem)
    {
        if (newItem == ""){return;}
        items.Add(newItem);
        itemHeldByID.Add(-1);
    }
    public void RemoveItemAt(int index)
    {
        if (index < 0 || index >= items.Count){return;}
        if (index >= itemHeldByID.Count){return;}
        items.RemoveAt(index);
        itemHeldByID.RemoveAt(index);
    }
    public void GainItems(List<string> newItems)
    {
        for (int i = 0; i < newItems.Count; i++)
        {
            GainItem(newItems[i]);
        }
        TrimItems();
    }
    public void UseItem(string usedItem)
    {
        int indexOf = items.IndexOf(usedItem);
        if (indexOf >= 0)
        {
            RemoveItemAt(indexOf);
        }
    }
    public bool ItemExists(string itemName)
    {
        int indexOf = items.IndexOf(itemName);
        return indexOf >= 0;
    }
    public List<string> ReturnAllChests()
    {
        List<string> chests = new List<string>();
        for (int i = items.Count - 1; i >= 0; i--)
        {
            if (items[i].Contains("Chest"))
            {
                chests.Add(items[i]);
                RemoveItemAt(i);
            }
        }
        return chests;
    }
    public int ReturnChestCount()
    {
        int count = 0;
        for (int i = items.Count - 1; i >= 0; i--)
        {
            if (items[i].Contains("Chest")){count++;}
        }
        return count;
    }
    public void TransformItemsOfType(string original, string transformed)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].Contains(original))
            {
                items[i] = transformed;
            }
        }
    }
    public bool QuantityOfItemExists(string itemName, int quantity = 1)
    {
        int count = utility.CountStringsInList(items, itemName);
        return count >= quantity;
    }
    public void RemoveItemsOfType(string type, int count)
    {
        for (int i = 0; i < count; i++)
        {
            RemoveItemOfType(type);
        }
    }
    public void RemoveItemOfType(string type)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].Contains(type))
            {
                RemoveItemAt(i);
                return;
            }
        }
    }
    public void RemoveAllItemsOfType(string type)
    {
        for (int i = items.Count - 1; i >= 0; i--)
        {
            if (items[i].Contains(type))
            {
                RemoveItemAt(i);
            }
        }
    }
    public void OpenChest(string chest)
    {
        for (int i = items.Count - 1; i >= 0; i--)
        {
            if (items[i] == chest)
            {
                RemoveItemAt(i);
                return;
            }
        }
    }
    public void DiscardItem(string item)
    {
        int indexOf = items.IndexOf(item);
        if (indexOf >= 0)
        {
            RemoveItemAt(indexOf);
        }
    }
    // RELICS
    public List<string> relics;
    public void SetRelics(List<string> newList)
    {
        utility.RemoveEmptyListItems(newList);
        relics = new List<string>(newList);
    }
    public List<string> GetRelics()
    {
        return relics;
    }
    public List<string> relicCounters;
    public void SetRelicCounters(List<string> newList)
    {
        utility.RemoveEmptyListItems(newList);
        relicCounters = new List<string>(newList);
    }
    public List<string> GetRelicCounters()
    {
        return relicCounters;
    }
    public void GainRelic(string relicName, string counter = "-1")
    {
        int index = relics.IndexOf(relicName);
        if (index >= 0){return;}
        relics.Add(relicName);
        relicCounters.Add(counter);
    }
    public bool RelicExists(string relicName)
    {
        int index = relics.IndexOf(relicName);
        if (index < 0){return false;}
        return true;
    }
    public void RemoveRelic(string relicName)
    {
        if (!RelicExists(relicName)){return;}
        int index = relics.IndexOf(relicName);
        relics.RemoveAt(index);
        relicCounters.RemoveAt(index);
    }
    public override void NewGame()
    {
        maxCapacity = 16;
        items.Clear();
        itemHeldByID.Clear();
        string[] newGameItems = newGameData.Split("|");
        for (int i = 0; i < newGameItems.Length; i++)
        {
            GainItem(newGameItems[i]);
        }
        relics.Clear();
        relicCounters.Clear();
        Save();
        Load();
    }
    public override void Save()
    {
        dataPath = GetSavePath();
        allData = "";
        allData += "Max=" + maxCapacity + delimiter;
        allData += "Items=" + String.Join(delimiterTwo, items) + delimiter;
        allData += "Holders=" + String.Join(delimiterTwo, itemHeldByID) + delimiter;
        allData += "Relics=" + String.Join(delimiterTwo, relics) + delimiter;
        allData += "RelicCounters=" + String.Join(delimiterTwo, relicCounters) + delimiter;
        File.WriteAllText(dataPath, allData);
    }
    public override void Load()
    {
        dataPath = GetSavePath();
        if (File.Exists(dataPath)){allData = File.ReadAllText(dataPath);}
        else{allData = newGameData;}
        dataList = allData.Split(delimiter).ToList();
        for (int i = 0; i < dataList.Count; i++)
        {
            LoadStat(dataList[i]);
        }
    }
    public override void LoadStat(string stat)
    {
        string[] statsBlocks = stat.Split("=");
        if (statsBlocks.Length < 2){return;}
        string key = statsBlocks[0];
        string value = statsBlocks[1];
        switch (key)
        {
            case "Max":
            maxCapacity = utility.SafeParseInt(value, 16);
            break;
            case "Items":
            SetItems(value.Split(delimiterTwo).ToList());
            break;
            case "Holders":
            SetHolders(value.Split(delimiterTwo).Select(x => utility.SafeParseInt(x, -1)).ToList());
            break;
            case "Relics":
            SetRelics(value.Split(delimiterTwo).ToList());
            break;
            case "RelicCounters":
            SetRelicCounters(value.Split(delimiterTwo).ToList());
            break;
            default:
            break;
        }
    }
}
