using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Stores items, equipment, cargo, etc.
[CreateAssetMenu(fileName = "GuildStorage", menuName = "ScriptableObjects/DataContainers/SavedData/GuildStorage", order = 1)]
public class GuildStorage : SavedData
{
    public string delimiterTwo;
    protected int minDungeonStorage = 600;
    public int maxDungeonStorage;
    public string ReturnDungeonStorageLimitString()
    {
        return storedDungeonItems.Count + "/" + maxDungeonStorage;
    }
    public bool MaxedDungeonStorage()
    {
        return storedDungeonItems.Count >= maxDungeonStorage;
    }
    public bool DungeonStorageAvailable(int count)
    {
        return storedDungeonItems.Count + count <= maxDungeonStorage;
    }
    public List<string> storedDungeonItems;
    public List<string> GetStoredDungeonItems(){return storedDungeonItems;}
    public void StoreDungeonItem(string newInfo)
    {
        storedDungeonItems.Add(newInfo);
        storedDungeonItems.Sort();
    }
    public void StoreDungeonItems(List<string> newInfo)
    {
        storedDungeonItems.AddRange(newInfo);
        storedDungeonItems.Sort();
    }
    public void WithdrawDungeonItem(string newInfo)
    {
        int indexOf = storedDungeonItems.IndexOf(newInfo);
        if (indexOf < 0){return;}
        storedDungeonItems.RemoveAt(indexOf);
        storedDungeonItems.Sort();
    }
    protected int minStorage = 600;
    public int maxStorage;
    public string ReturnStorageLimitString()
    {
        return storedItems.Count + "/" + maxStorage;
    }
    public bool StorageAvailable(int count)
    {
        return storedItems.Count + count <= maxStorage;
    }
    public bool MaxedStorage()
    {
        return storedItems.Count >= maxStorage;
    }
    public List<string> storedItems;
    public List<string> GetStoredItems(){return storedItems;}
    public void StoreItem(string newInfo)
    {
        storedItems.Add(newInfo);
        storedItems.Sort();
    }
    public void StoreItems(List<string> newInfo)
    {
        storedItems.AddRange(newInfo);
        storedItems.Sort();
    }
    public void WithdrawItem(string newInfo)
    {
        int indexOf = storedItems.IndexOf(newInfo);
        if (indexOf < 0){return;}
        storedItems.RemoveAt(indexOf);
        storedItems.Sort();
    }
    public int storedGold;
    public int GetStoredGold(){return storedGold;}
    public void StoreGold(int amount)
    {
        storedGold += amount;
    }
    public void WithdrawGold(int amount)
    {
        storedGold -= amount;
    }

    public override void NewGame()
    {
        dataPath = Application.persistentDataPath+"/"+filename;
        allData = newGameData;
        File.WriteAllText(dataPath, allData);
        Load();
        Save();
    }

    public override void Save()
    {
        dataPath = Application.persistentDataPath+"/"+filename;
        allData = "";
        allData += "Gold=" + storedGold + delimiter;
        allData += "MaxDItems=" + maxDungeonStorage + delimiter;
        allData += "DItems=" + String.Join(delimiterTwo, storedDungeonItems) + delimiter;
        allData += "MaxItems=" + maxStorage + delimiter;
        allData += "Items=" + String.Join(delimiterTwo, storedItems) + delimiter;
        File.WriteAllText(dataPath, allData);
    }

    public override void Load()
    {
        dataPath = Application.persistentDataPath+"/"+filename;
        if (File.Exists(dataPath)){allData = File.ReadAllText(dataPath);}
        else
        {
            NewGame();
            return;
        }
        dataList = allData.Split(delimiter).ToList();
        storedGold = 0;
        maxDungeonStorage = minDungeonStorage;
        maxStorage = minStorage;
        storedDungeonItems.Clear();
        storedItems.Clear();
        for (int i = 0; i < dataList.Count; i++)
        {
            LoadStat(dataList[i]);
        }
        utility.RemoveEmptyListItems(storedDungeonItems);
        utility.RemoveEmptyListItems(storedItems);
    }

    protected void LoadStat(string data)
    {
        string[] blocks = data.Split('=');
        if (blocks.Length < 2) { return; }
        string key = blocks[0];
        string stat = blocks[1];
        switch (key)
        {
            case "Gold":
                storedGold = int.Parse(stat);
                break;
            case "MaxDItems":
                maxDungeonStorage = int.Parse(stat);
                break;
            case "DItems":
                storedDungeonItems = stat.Split(delimiterTwo).ToList();
                break;
            case "MaxItems":
                maxStorage = int.Parse(stat);
                break;
            case "Items":
                storedItems = stat.Split(delimiterTwo).ToList();
                break;
        }
    }
}
