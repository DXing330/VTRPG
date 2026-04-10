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
            for (int i = 0; i < maxCapacity; i++)
            {
                trimmedItems.Add(items[i]);
            }
            SetItems(trimmedItems);
        }
    }
    public List<string> items;
    public int GetItemCount(){return items.Count;}
    public void DropItems(){items.Clear();}
    public List<string> GetItems(){return items;}
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
            items.RemoveAt(indexOf);
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
                items.RemoveAt(i);
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
                items.RemoveAt(i);
                return;
            }
        }
    }
    public void RemoveAllItemsOfType(string type)
    {
        for (int i = 0; i < items.Count; i++)
        {
            RemoveItemOfType(type);
        }
    }
    public void OpenChest(string chest)
    {
        for (int i = items.Count - 1; i >= 0; i--)
        {
            if (items[i] == chest)
            {
                items.RemoveAt(i);
                return;
            }
        }
    }
    public void DiscardItem(string item)
    {
        int indexOf = items.IndexOf(item);
        if (indexOf >= 0)
        {
            items.RemoveAt(indexOf);
        }
    }
    public override void NewGame()
    {
        allData = newGameData;
        if (allData.Contains(delimiter)){dataList = allData.Split(delimiter).ToList();}
        else{return;}
        maxCapacity = int.Parse(dataList[0]);
        items = dataList[1].Split(delimiterTwo).ToList();
        Save();
        Load();
    }
    public override void Save()
    {
        dataPath = Application.persistentDataPath+"/"+filename;
        allData = "";
        allData += maxCapacity + delimiter;
        allData += String.Join(delimiterTwo, items) + delimiter;
        File.WriteAllText(dataPath, allData);
    }
    public override void Load()
    {
        dataPath = Application.persistentDataPath+"/"+filename;
        if (File.Exists(dataPath)){allData = File.ReadAllText(dataPath);}
        else{allData = newGameData;}
        dataList = allData.Split(delimiter).ToList();
        for (int i = 0; i < dataList.Count; i++)
        {
            LoadStat(dataList[i], i);
        }
    }
    protected void LoadStat(string stat, int index)
    {
        switch (index)
        {
            case 0:
            maxCapacity = int.Parse(stat);
            break;
            case 1:
            SetItems(stat.Split(delimiterTwo).ToList());
            break;
            default:
            break;
        }
    }
}
