using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// In charge of generating and storing shop data, including pools.
[CreateAssetMenu(fileName = "AutoChessShopDataManager", menuName = "ScriptableObjects/AutoChess/AutoChessShopDataManager", order = 1)]
public class AutoChessShopDataManager : SavedData
{
    public string delimiter2;
    public StatDatabase unitData;
    public StatDatabase unitRarity;
    public RNGUtility autoChessShopRNG;
    public int shopLevel;
    public void SetShopLevel(string newData)
    {
        shopLevel = utility.SafeParseInt(newData, 1);
    }
    // Weights Determined By Formula.
    // 80-20-0-0-0-0
    // 60-30-10-0-0-0
    // 40-30-20-10-0-0
    // 30-25-25-15-5-0
    // 20-20-25-20-10-5
    // 10-15-25-25-15-10
    readonly int[,] rarityWeights =
    {
        { 80, 20,  0,  0,  0,  0}, // Level 1
        { 60, 30, 10,  0,  0,  0}, // Level 2
        { 40, 30, 20, 10,  0,  0}, // Level 3
        { 30, 25, 25, 15,  5,  0}, // Level 4
        { 20, 20, 25, 20, 10,  5}, // Level 5
        { 10, 15, 25, 25, 15, 10}  // Level 6
    };
    [ContextMenu("Test Rarity Distribution")]
    public void TestRarityDistribution()
    {
        for (int j = 1; j < 7; j++)
        {
            shopLevel = j;
            int rolls = 100000;
            int[] counts = new int[6];
            for (int i = 0; i < rolls; i++)
            {
                int rarity = DetermineRarity();
                counts[rarity - 1]++;
            }
            Debug.Log("Shop Level: " + shopLevel);
            for (int i = 0; i < 6; i++)
            {
                float percent = counts[i] * 100f / rolls;
                Debug.Log("Rarity " + (i + 1) + ": " + percent + "%");
            }
        }
        shopLevel = 1;
    }
    protected int DetermineRarity()
    {
        int levelIndex = Mathf.Clamp(shopLevel - 1, 0, 5);
        int roll = autoChessShopRNG.SeedRange(0, 100);
        int cumulative = 0;
        for (int rarity = 0; rarity < 6; rarity++)
        {
            cumulative += rarityWeights[levelIndex, rarity];
            if (roll < cumulative)
            {
                return rarity + 1; // convert 0-based column to rarity 1-6
            }
        }
        return 1;
    }
    // Rebuilt During New Game.
    // All Actors Either Exist In The Bench/Pool/Listing.
    public List<string> currentPool;
    public void RemoveFromPool(string newData)
    {
        int indexOf = currentPool.IndexOf(newData);
        if (indexOf < 0){return;}
        currentPool.RemoveAt(indexOf);
        currentPoolRarity.RemoveAt(indexOf);
    }
    // When Selling.
    public void AddToPool(string newData, string newRarity, int level = 1)
    {
        int count = 1;
        if (level > 1){count = 3;}
        for (int i = 0; i < count; i++)
        {
            currentPool.Add(newData);
            currentPoolRarity.Add(newRarity);
        }
    }
    public void SetCurrentPool(string newData)
    {
        currentPool = newData.Split(delimiter2).ToList();
    }
    public List<string> currentPoolRarity;
    public void SetCurrentPoolRarity(string newData)
    {
        currentPoolRarity = newData.Split(delimiter2).ToList();
    }
    public List<string> ReturnCurrentPoolOfRarity(int rarity)
    {
        string rarityString = rarity.ToString();
        List<string> pool = new List<string>();
        for (int i = 0; i < currentPoolRarity.Count; i++)
        {
            if (rarityString != currentPoolRarity[i]){continue;}
            pool.Add(currentPool[i]);
        }
        return pool;
    }
    public string ReturnRandomActorFromPool(List<string> pool)
    {
        if (pool.Count <= 0){return currentPool[0];}
        int roll = autoChessShopRNG.SeedRange(0, pool.Count);
        return pool[roll];
    }
    public List<string> currentListing;
    public void GenerateCurrentListing()
    {
        currentListing.Clear();
        // Determine How Many Slots Are Available.
        int availableSlots = 3 + shopLevel / 2;
        for (int i = 0; i < availableSlots; i++)
        {
            string newRoll = ReturnRandomActorFromPool(ReturnCurrentPoolOfRarity(DetermineRarity()));
            currentListing.Add(newRoll);
            RemoveFromPool(newRoll);
        }
    }
    public List<string> GetCurrentListing(){return currentListing;}
    public void SetCurrentListing(string newData)
    {
        currentListing = newData.Split(delimiter2).ToList();
        for (int i = currentListing.Count - 1; i >= 0; i--)
        {
            if (currentListing[i].Length <= 0){currentListing.RemoveAt(i);}
        }
    }
    public void RemoveFromListing(int index)
    {
        currentListing.RemoveAt(index);
    }
    [ContextMenu("New Game")]
    public override void NewGame()
    {
        shopLevel = 1;
        currentPool.Clear();
        currentPoolRarity.Clear();
        List<string> allNames = unitData.GetAllKeys();
        for (int i = 0; i < allNames.Count; i++)
        {
            // 4, 6, 8, 10, 12, 14 of each unit.
            int rarity = int.Parse(unitRarity.ReturnValue(allNames[i]));
            for (int j = 0; j < 16 - 2 * rarity; j++)
            {
                currentPool.Add(allNames[i]);
                currentPoolRarity.Add(rarity.ToString());
            }
        }
        GenerateCurrentListing();
        Save();
        autoChessShopRNG.Save();
    }
    public override void Save()
    {
        dataPath = Application.persistentDataPath + "/" + filename;
        allData = "";
        allData += "ShopLevel=" + shopLevel + delimiter;
        allData += "CurrentPool=" + String.Join(delimiter2, currentPool) + delimiter;
        allData += "CurrentPoolRarity=" + String.Join(delimiter2, currentPoolRarity) + delimiter;
        allData += "CurrentListing=" + String.Join(delimiter2, currentListing) + delimiter;
        File.WriteAllText(dataPath, allData);
    }
    public override void Load()
    {
        dataPath = Application.persistentDataPath + "/" + filename;
        if (File.Exists(dataPath))
        {
            allData = File.ReadAllText(dataPath);
        }
        else
        {
            NewGame();
            return;
        }
        string[] blocks = allData.Split(delimiter);
        for (int i = 0; i < blocks.Length; i++)
        {
            LoadStat(blocks[i]);
        }
    }
    public void LoadStat(string data)
    {
        string[] blocks = data.Split("=");
        if (blocks.Length < 2){return;}
        string key = blocks[0];
        string value = blocks[1];
        switch (key)
        {
            default:
            return;
            case "ShopLevel":
            SetShopLevel(value);
            return;
            case "CurrentPool":
            SetCurrentPool(value);
            return;
            case "CurrentPoolRarity":
            SetCurrentPoolRarity(value);
            return;
            case "CurrentListing":
            SetCurrentListing(value);
            return;
        }
    }
}
