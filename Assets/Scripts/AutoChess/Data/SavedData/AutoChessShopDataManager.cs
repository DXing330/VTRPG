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
    public AutoChessLogDataManager logData; // Shop State + Changes.
    public void AddLog(string newLog)
    {
        logData.AddLog(newLog);
    }
    public AutoChessTactician tactician;
    public StatDatabase unitData;
    public StatDatabase unitRarity;
    public RNGUtility autoChessShopRNG;
    public int shopLevel;
    public int GetShopLevel()
    {
        return shopLevel;
    }
    public void SetShopLevel(int newData)
    {
        shopLevel = newData;
    }
    public void SetShopLevel(string newData)
    {
        shopLevel = utility.SafeParseInt(newData, 1);
    }
    public int frozenShop = 0;
    public void FreezeShop()
    {
        frozenShop = (frozenShop + 1) % 2;
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
        { 100, 0,  0,  0,  0,  0}, // Level 1
        { 70, 30,  0,  0,  0,  0}, // Level 2
        { 50, 30, 20,  0,  0,  0}, // Level 3
        { 30, 30, 20, 20,  0,  0}, // Level 4
        { 20, 30, 20, 20, 10,  0}, // Level 5
        { 20, 20, 25, 20, 10,  5}, // Level 6
        { 10, 15, 25, 25, 15, 10}, // Level 7
        {  5,  5, 25, 30, 20, 15}, // Level 8
        {  5,  5, 20, 25, 25, 20}, // Level 9
        {  5,  5, 15, 20, 30, 25}  // Level 10
    };
    [ContextMenu("Test Rarity Distribution")]
    public void TestRarityDistribution()
    {
        for (int j = 1; j <= rarityWeights.GetLength(0); j++)
        {
            shopLevel = j;
            int rolls = 100000;
            int[] counts = new int[rarityWeights.GetLength(1)];
            for (int i = 0; i < rolls; i++)
            {
                int rarity = DetermineRarity();
                counts[rarity - 1]++;
            }
            Debug.Log("Shop Level: " + shopLevel);
            for (int i = 0; i < rarityWeights.GetLength(1); i++)
            {
                float percent = counts[i] * 100f / rolls;
                Debug.Log("Rarity " + (i + 1) + ": " + percent + "%");
            }
        }
        shopLevel = 1;
    }
    protected int DetermineRarity()
    {
        int levelIndex = Mathf.Clamp(GetShopLevel() - 1, 0, rarityWeights.GetLength(0) - 1);
        int roll = autoChessShopRNG.SeedRange(0, 100);
        int cumulative = 0;
        for (int rarity = 0; rarity < rarityWeights.GetLength(1); rarity++)
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
    public bool RemoveFromPool(string newData)
    {
        int indexOf = currentPool.IndexOf(newData);
        if (indexOf < 0){return false;}
        currentPool.RemoveAt(indexOf);
        currentPoolRarity.RemoveAt(indexOf);
        currentPoolFactions.RemoveAt(indexOf);
        return true;
    }
    // When Selling/Rerolling.
    public void AddToPool(string newData, int level = 1)
    {
        int count = 1;
        if (level > 1){count = 3;}
        for (int i = 0; i < count; i++)
        {
            currentPool.Add(newData);
            currentPoolRarity.Add(unitRarity.ReturnValue(newData));
            string[] blocks = unitData.ReturnValue(newData).Split("|");
            currentPoolFactions.Add(blocks[0]);
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
    public List<string> currentPoolFactions;
    public void SetCurrentPoolFactions(string newData)
    {
        currentPoolFactions = newData.Split(delimiter2).ToList();
    }
    public List<string> ReturnCurrentPoolOfFaction(string faction)
    {
        List<string> pool = new List<string>();
        for (int i = 0; i < currentPoolFactions.Count; i++)
        {
            // Can't Get Higher Rarity Than Shop Level.
            if (int.Parse(currentPoolRarity[i]) > GetShopLevel()){continue;}
            string[] factionBlocks = currentPoolFactions[i].Split(",");
            if (!factionBlocks.Contains(faction)){continue;}
            pool.Add(currentPool[i]);
        }
        return pool;
    }
    public string ReturnRandomActorFromFaction(string faction)
    {
        return ReturnRandomActorFromPool(ReturnCurrentPoolOfFaction(faction));
    }
    public string ReturnRandomActorFromFactionAndRarity(string faction, int rarity)
    {
        List<string> factionPool = ReturnCurrentPoolOfFaction(faction);
        for (int i = factionPool.Count - 1; i >= 0; i--)
        {
            if (int.Parse(unitRarity.ReturnValue(factionPool[i])) < rarity)
            {
                factionPool.RemoveAt(i);
            }
        }
        return ReturnRandomActorFromPool(factionPool);
    }
    public string ReturnRandomActor()
    {
        List<string> randomPool = new List<string>(currentPool);
        for (int i = currentPool.Count - 1; i >= 0; i--)
        {
            if (int.Parse(currentPoolRarity[i]) > GetShopLevel())
            {
                randomPool.RemoveAt(i);
            }
        }
        return ReturnRandomActorFromPool(randomPool);
    }
    // Consumes The Actor From The Listing Automatically.
    public string ReturnRandomActorFromPool(List<string> pool)
    {
        string actorName = currentPool[0];
        if (pool.Count <= 0)
        {
            logData.AddLog("Empty Pool - Using Default Value");
            RemoveFromPool(actorName);
            return actorName;
        }
        int roll = autoChessShopRNG.SeedRange(0, pool.Count);
        actorName = pool[roll];
        RemoveFromPool(actorName);
        return actorName;
    }
    public List<string> currentListing;
    public void ModifyCurrentList(int index, string effect, string specifics)
    {
        string original = currentListing[index];
        AddToPool(currentListing[index]);
        currentListing.RemoveAt(index);
        string newRoll = original;
        switch (effect)
        {
            case "Faction":
            newRoll = ReturnRandomActorFromFaction(specifics);
            break;
            case "Copy":
            string copy = currentListing[int.Parse(specifics)];
            if (RemoveFromPool(copy))
            {
                newRoll = copy;
            }
            break;
        }
        if (newRoll != original)
        {
            logData.AddLog(newRoll + " Added To Shop Listing");
        }
        else
        {
            logData.AddLog("Unable To Find Replacement");
        }
        currentListing.Insert(index, newRoll);
    }
    public void GenerateCurrentListing(bool reroll = false)
    {
        logData.AddLog("Generating New Shop Listing");
        for (int i = 0; i < currentListing.Count; i++)
        {
            AddToPool(currentListing[i]);
        }
        currentListing.Clear();
        // Determine How Many Slots Are Available.
        int availableSlots = Mathf.Min(6, 3 + GetShopLevel() / 3);
        for (int i = 0; i < availableSlots; i++)
        {
            string newRoll = ReturnRandomActorFromPool(ReturnCurrentPoolOfRarity(DetermineRarity()));
            logData.AddLog(newRoll + " Added To Shop");
            currentListing.Add(newRoll);
        }
        if (tactician != null && reroll)
        {
            tactician.ApplyRerollEffect(this);
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
    public List<string> PVPCurrentListing;
    // Only Used By AI During Their Prep Phase.
    public List<string> GetPVPCurrentListing()
    {
        return PVPCurrentListing;
    }
    public void RemoveFromPVPListing(int index)
    {
        PVPCurrentListing.RemoveAt(index);
    }
    public void GeneratePVPCurrentListing(bool reroll = false)
    {
        logData.AddLog("Generating New Shop Listing");
        for (int i = 0; i < PVPCurrentListing.Count; i++)
        {
            AddToPool(PVPCurrentListing[i]);
        }
        PVPCurrentListing.Clear();
        // Determine How Many Slots Are Available.
        int availableSlots = Mathf.Min(6, 3 + GetShopLevel() / 3);
        for (int i = 0; i < availableSlots; i++)
        {
            string newRoll = ReturnRandomActorFromPool(ReturnCurrentPoolOfRarity(DetermineRarity()));
            logData.AddLog(newRoll + " Added To Shop");
            PVPCurrentListing.Add(newRoll);
        }
    }
    public override void NewRound()
    {
        if (frozenShop == 1)
        {
            logData.AddLog("Shop Frozen Last Round");
            frozenShop = 0;
            return;
        }
        GenerateCurrentListing();
    }
    [ContextMenu("New Game")]
    public override void NewGame()
    {
        shopLevel = 1;
        frozenShop = 0;
        currentPool.Clear();
        currentPoolRarity.Clear();
        currentPoolFactions.Clear();
        List<string> allNames = unitData.GetAllKeys();
        for (int i = 0; i < allNames.Count; i++)
        {
            // 6, 12, 18, 24, 30, 36 of each unit.
            int rarity = int.Parse(unitRarity.ReturnValue(allNames[i]));
            string[] dataBlocks = unitData.ReturnValue(allNames[i]).Split("|");
            for (int j = 0; j < 42 - 6 * rarity; j++)
            {
                currentPool.Add(allNames[i]);
                currentPoolRarity.Add(rarity.ToString());
                currentPoolFactions.Add(dataBlocks[0]);
            }
        }
        autoChessShopRNG.NewGame();
        GenerateCurrentListing();
        Save();
    }
    public override void Save()
    {
        dataPath = Application.persistentDataPath + "/" + filename;
        allData = "";
        allData += "ShopLevel=" + shopLevel + delimiter;
        allData += "FrozenShop=" + frozenShop + delimiter;
        allData += "CurrentPool=" + String.Join(delimiter2, currentPool) + delimiter;
        allData += "CurrentPoolRarity=" + String.Join(delimiter2, currentPoolRarity) + delimiter;
        allData += "CurrentPoolFactions=" + String.Join(delimiter2, currentPoolFactions) + delimiter;
        allData += "CurrentListing=" + String.Join(delimiter2, currentListing) + delimiter;
        File.WriteAllText(dataPath, allData);
        autoChessShopRNG.Save();
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
        autoChessShopRNG.Load();
    }
    public override void LoadStat(string data)
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
            case "FrozenShop":
            frozenShop = int.Parse(value);
            return;
            case "CurrentPool":
            SetCurrentPool(value);
            return;
            case "CurrentPoolRarity":
            SetCurrentPoolRarity(value);
            return;
            case "CurrentPoolFactions":
            SetCurrentPoolFactions(value);
            return;
            case "CurrentListing":
            SetCurrentListing(value);
            return;
        }
    }
}
