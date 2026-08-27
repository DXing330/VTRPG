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
    public bool logDataManager = true;
    public void DisableLogs(){logDataManager = false;}
    public void EnableLogs(){logDataManager = true;}
    public AutoChessLogDataManager logData; // Shop State + Changes.
    public void AddLog(string newLog)
    {
        if (!logDataManager || logData == null){return;}
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
        int levelIndex = Mathf.Clamp(shopLevel - 1, 0, rarityWeights.GetLength(0) - 1);
        int roll = autoChessShopRNG.SeedRange(0, 100);
        int cumulative = 0;
        for (int r = 0; r < rarityWeights.GetLength(1); r++)
        {
            cumulative += rarityWeights[levelIndex, r];
            if (roll < cumulative) return r + 1;
        }
        return 1;
    }
    // Rebuilt During New Game.
    // All Actors Either Exist In The Bench/Pool/Listing.
    public bool RemoveFromPool(string actorName)
    {
        if (!_actorCache.TryGetValue(actorName, out var data)) return false;
        var bucket = _poolByRarity[data.rarity];
        for (int i = 0; i < bucket.Count; i++)
        {
            if (bucket[i] == actorName)
            {
                int last = bucket.Count - 1;
                bucket[i] = bucket[last];
                bucket.RemoveAt(last);
                _totalPoolCount--;
                return true;
            }
        }
        return false;
    }
    // When Selling/Rerolling.
    public void AddToPool(string actorName, int level = 1)
    {
        if (!_actorCache.TryGetValue(actorName, out var data)) return;
        int count = level > 1 ? 3 : 1;
        var bucket = _poolByRarity[data.rarity];
        for (int i = 0; i < count; i++)
            bucket.Add(actorName);
        _totalPoolCount += count;
    }
    // Used By Ethan + Yu Traits
    public string ReturnRandomActorFromFaction(string faction)
    {
        // Build list of all pool copies matching faction
        List<string> matches = new List<string>();
        for (int r = 1; r <= Mathf.Min(6, GetShopLevel()); r++)
        {
            foreach (var name in _poolByRarity[r])
            {
                string[] factions = _actorCache[name].faction.Split(",");
                foreach (var f in factions)
                {
                    if (f.Trim() == faction)
                    {
                        matches.Add(name);
                        break; // one copy = one entry
                    }
                }
            }
        }
        if (matches.Count == 0) return null;
        int roll = autoChessShopRNG.SeedRange(0, matches.Count);
        string chosen = matches[roll];
        RemoveFromPool(chosen);
        return chosen;
    }
    // Used By That One Tactician.
    public string ReturnRandomActorFromFactionAndRarity(string faction = "Laterano", int minRarity = 4)
    {
        List<string> matches = new List<string>();
        for (int r = minRarity; r <= 6; r++)
        {
            foreach (var name in _poolByRarity[r])
            {
                string[] factions = _actorCache[name].faction.Split(",");
                foreach (var f in factions)
                {
                    if (f.Trim() == faction)
                    {
                        matches.Add(name);
                        break;
                    }
                }
            }
        }
        if (matches.Count == 0) return null;
        int roll = autoChessShopRNG.SeedRange(0, matches.Count);
        string chosen = matches[roll];
        RemoveFromPool(chosen);
        return chosen;
    }
    public string DrawRandomOfRarity(int rarity)
    {
        var bucket = _poolByRarity[rarity];
        if (bucket.Count == 0) return null;
        int roll = autoChessShopRNG.SeedRange(0, bucket.Count);
        int last = bucket.Count - 1;
        string chosen = bucket[roll];
        bucket[roll] = bucket[last];
        bucket.RemoveAt(last);
        _totalPoolCount--;
        return chosen;
    }
    public string DrawRandomAny()
    {
        if (_totalPoolCount == 0) return null;
        int validPoolCount = 0;
        for (int r = 1; r <= Mathf.Min(GetShopLevel(), 6); r++)
            validPoolCount += _poolByRarity[r].Count;
        if (validPoolCount == 0) return null;
        int roll = autoChessShopRNG.SeedRange(0, validPoolCount);
        int cumulative = 0;
        for (int r = 1; r <= Mathf.Min(6, GetShopLevel()); r++)
        {
            cumulative += _poolByRarity[r].Count;
            if (roll < cumulative)
            {
                var bucket = _poolByRarity[r];
                int last = bucket.Count - 1;
                string chosen = bucket[last];
                bucket.RemoveAt(last);
                _totalPoolCount--;
                return chosen;
            }
        }
        return null;
    }
    public List<string> currentListing;
    public void ModifyCurrentList(int index, string effect, string specifics, bool AI = false)
    {
        string original = currentListing[index];
        if (AI)
        {
            original = PVPCurrentListing[index];
        }
        AddToPool(original);
        string newRoll = original;
        switch (effect)
        {
            case "Faction":
            newRoll = ReturnRandomActorFromFaction(specifics);
            break;
            case "Copy":
            string copy = original;
            if (RemoveFromPool(copy))
            {
                newRoll = copy;
            }
            break;
        }
        logData.AddLog(newRoll + " Added To Shop Listing");
        if (!AI)
        {
            currentListing[index] = newRoll;
        }
        else
        {
            PVPCurrentListing[index] = newRoll;
        }
    }
    public void GenerateCurrentListing(bool reroll = false)
    {
        logData.AddLog("Generating New Shop Listing");
        foreach (var item in currentListing)
            AddToPool(item);
        currentListing.Clear();
        int slots = Mathf.Min(6, 3 + GetShopLevel() / 3);
        for (int i = 0; i < slots; i++)
        {
            string chosen = DrawRandomOfRarity(DetermineRarity());
            if (chosen != null)
            {
                logData.AddLog(chosen + " Added To Shop");
                currentListing.Add(chosen);
            }
        }
        if (tactician != null && reroll)
            tactician.ApplyRerollEffect(this);
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
        foreach (var item in PVPCurrentListing)
            AddToPool(item);
        PVPCurrentListing.Clear();
        int slots = Mathf.Min(6, 3 + GetShopLevel() / 3);
        for (int i = 0; i < slots; i++)
        {
            string chosen = DrawRandomOfRarity(DetermineRarity());
            if (chosen != null)
                PVPCurrentListing.Add(chosen);
        }
        if (tactician != null && reroll)
            tactician.ApplyRerollEffect(this, true);
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
    // === RUNTIME STATE: Buckets only ===
    [System.NonSerialized] private List<string>[] _poolByRarity = new List<string>[7];
    [System.NonSerialized] private int _totalPoolCount = 0;
    // Cache: actor name -> (rarity as int, faction string, copies when sold)
    // Built once at NewGame, never parsed again
    [System.NonSerialized] private Dictionary<string, (int rarity, string faction)> _actorCache;
    // One-time init (call from NewGame or Awake)
    private void InitRarityBuckets()
    {
        for (int r = 1; r <= 6; r++)
            _poolByRarity[r] = new List<string>();
        foreach (var bucket in _poolByRarity)
            bucket?.Clear();
    }
    [ContextMenu("New Game")]
    public override void NewGame()
    {
        shopLevel = 1;
        frozenShop = 0;
        currentListing.Clear();
        PVPCurrentListing.Clear();
        // Clear buckets
        InitRarityBuckets();
        _totalPoolCount = 0;
        // Build fast lookup cache
        _actorCache = new Dictionary<string, (int, string)>();
        List<string> allNames = unitData.GetAllKeys();
        for (int i = 0; i < allNames.Count; i++)
        {
            string name = allNames[i];
            int rarity = int.Parse(unitRarity.ReturnValue(name));
            if (rarity > 6){continue;} // Special Units Not In Shop.
            string faction = unitData.ReturnValue(name).Split("|")[0];
            _actorCache[name] = (rarity, faction);
            int copies = 24 - 3 * rarity; // 21,18,15,12,9,6
            var bucket = _poolByRarity[rarity];
            for (int j = 0; j < copies; j++)
            {
                bucket.Add(name);
            }
            _totalPoolCount += copies;
        }
        autoChessShopRNG.NewGame();
        GenerateCurrentListing();
        Save();
    }
    public override void Save()
    {
        dataPath = GetSavePath();
        // Build count map: "ActorName:Rarity:Count"
        Dictionary<string, int> counts = new Dictionary<string, int>();
        for (int r = 1; r <= 6; r++)
        {
            foreach (var name in _poolByRarity[r])
            {
                if (!counts.ContainsKey(name))
                {
                    counts[name] = 0;
                }
                counts[name]++;
            }
        }
        List<string> poolEntries = new List<string>();
        foreach (var kvp in counts)
        {
            poolEntries.Add(kvp.Key + ":" + kvp.Value);
        }
        allData = "";
        allData += "ShopLevel=" + shopLevel + delimiter;
        allData += "FrozenShop=" + frozenShop + delimiter;
        allData += "Pool=" + String.Join(delimiter2, poolEntries) + delimiter;
        allData += "CurrentListing=" + String.Join(delimiter2, currentListing) + delimiter;
        allData += "PVPCurrentListing=" + String.Join(delimiter2, PVPCurrentListing) + delimiter;
        File.WriteAllText(dataPath, allData);
        autoChessShopRNG.Save();
    }
    public override void Load()
    {
        InitRarityBuckets();
        _actorCache = new Dictionary<string, (int, string)>();
        dataPath = GetSavePath();
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
            case "ShopLevel":
            shopLevel = int.Parse(value);
            break;
            case "FrozenShop":
            frozenShop = int.Parse(value);
            break;
            case "Pool":
            ParsePool(value);
            break;
            case "CurrentListing":
            if (!string.IsNullOrEmpty(value))
                currentListing = value.Split(delimiter2).ToList();
            break;
            case "PVPCurrentListing":
            if (!string.IsNullOrEmpty(value))
                PVPCurrentListing = value.Split(delimiter2).ToList();
            break;
        }
    }
    void ParsePool(string data)
    {
        if (string.IsNullOrEmpty(data)) return;
        string[] entries = data.Split(delimiter2);
        for (int i = 0; i < entries.Length; i++)
        {
            string[] parts = entries[i].Split(":");
            if (parts.Length < 2) continue;
            string name = parts[0];
            int count = int.Parse(parts[1]);
            if (!_actorCache.TryGetValue(name, out var info))
            {
                // Fallback: rebuild cache entry if missing (modded save?)
                int rarity = int.Parse(unitRarity.ReturnValue(name));
                string faction = unitData.ReturnValue(name).Split("|")[0];
                info = (rarity, faction);
                _actorCache[name] = info;
            }
            var bucket = _poolByRarity[info.rarity];
            if (bucket == null)  // ← DEFENSIVE CHECK
            {
                Debug.LogError($"Bucket for {name} with rarity {info.rarity} is null!");
                continue;
            }
            for (int j = 0; j < count; j++)
                bucket.Add(name);
            _totalPoolCount += count;
        }
    }
}
