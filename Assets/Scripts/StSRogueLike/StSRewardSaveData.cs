using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[CreateAssetMenu(fileName = "StSRewardSaveData", menuName = "ScriptableObjects/StS/StSRewardSaveData", order = 1)]
public class StSRewardSaveData : SavedData
{
    public string delimiter2 = ",";
    // LCM (1*1,2*2,3*3)
    protected int SKILLWEIGHTBASE = 36;
    protected int RELICWEIGHTBASE = 32;
    public RNGUtility rewardSeed;
    public StatDatabase skillBookDB;
    public StatDatabase skillBookRarity;
    public StatDatabase colorlessSkillBookDB;
    public StatDatabase colorlessSkillBookRarity;
    public StatDatabase itemDB;
    public StatDatabase relicData;
    public StatDatabase relicLocations;
    public StatDatabase relicRarity;
    // Event Relics/Boss Relics Will Be Handled By Events/Bosses.
    public List<string> availableRelics;
    public List<string> availableShopRelics;
    // To Be Used Whenever A Relic Is Generated
    protected void RemoveRelicFromPool(string relicName)
    {
        if (availableRelics.Contains(relicName))
        {
            availableRelics.Remove(relicName);
        }
        else if (availableShopRelics.Contains(relicName))
        {
            availableShopRelics.Remove(relicName);
        }
    }
    protected void InitializeRelicLists()
    {
        availableRelics.Clear();
        availableShopRelics.Clear();
        List<string> allRelics = relicLocations.GetAllKeys();
        for (int i = 0; i < allRelics.Count; i++)
        {
            string location = relicLocations.ReturnValue(allRelics[i]);
            if (location == "" || location == "All")
            {
                availableRelics.Add(allRelics[i]);
                continue;
            }
            if (location == "Shop")
            {
                availableShopRelics.Add(allRelics[i]);
                continue;
            }
        }
        // Make both lists unique by removing duplicates.
        availableRelics = availableRelics.Distinct().ToList();
        availableShopRelics = availableShopRelics.Distinct().ToList();
    }
    public List<string> rewards;
    public List<string> rewardSpecifics;
    public override void NewGame()
    {
        rewards.Clear();
        rewardSpecifics.Clear();
        InitializeRelicLists();
    }
    public override void Save()
    {
        dataPath = Application.persistentDataPath + "/" + filename;
        allData = "";
        allData += "Relics=" + String.Join(delimiter2, availableRelics) + delimiter;
        allData += "ShopRelics=" + String.Join(delimiter2, availableShopRelics) + delimiter;
        allData += "Rewards=" + String.Join(delimiter2, rewards) + delimiter;
        allData += "RewardSpecifics=" + String.Join(delimiter2, rewardSpecifics) + delimiter;
        File.WriteAllText(dataPath, allData);
    }
    public override void Load()
    {
        dataPath = Application.persistentDataPath + "/" + filename;
        if (!File.Exists(dataPath))
        {
            NewGame();
            return;
        }
        allData = File.ReadAllText(dataPath);
        dataList = allData.Split(delimiter).ToList();
        for (int i = 0; i < dataList.Count; i++)
        {
            LoadStat(dataList[i]);
        }
    }
    public void LoadStat(string stat)
    {
        string[] statData = stat.Split("=");
        if (statData.Length < 2){return;}
        string key = statData[0];
        string value = statData[1];
        switch (key)
        {
            case "Relics":
            availableRelics = value.Split(delimiter2).ToList();
            break;
            case "ShopRelics":
            availableShopRelics = value.Split(delimiter2).ToList();
            break;
            case "Rewards":
            rewards = value.Split(delimiter2).ToList();
            break;
            case "RewardSpecifics":
            rewardSpecifics = value.Split(delimiter2).ToList();
            break;
        }
    }
    public int DetermineRewardRarity(List<int> rarityWeights)
    {
        int index = utility.ReturnIndexBasedOnWeight(rarityWeights, rewardSeed.Range(0, rarityWeights.Sum()));
        return (index + 1);
    }
    public string GetRewardOfRarity(int rarity, List<string> rewards, List<int> rewardRarities)
    {
        // Probably works.
        for (int i = 0; i < rewards.Count * 6; i++)
        {
            int index = rewardSeed.Range(0, rewards.Count);
            if (rewardRarities[index] == rarity)
            {
                return rewards[index];
            }
        }
        return "";
    }
    public string GenerateSkillBook(List<string> except = null, bool rare = false, bool colorless = false)
    {
        if (except == null)
        {
            except = new List<string>();
        }
        List<string> possibleSkills = skillBookDB.GetAllKeys();
        List<int> skillRarities = utility.ConvertStringListToIntList(skillBookRarity.GetAllValues());
        if (colorless)
        {
            possibleSkills = colorlessSkillBookDB.GetAllKeys();
            skillRarities = utility.ConvertStringListToIntList(colorlessSkillBookRarity.GetAllValues());
        }
        List<int> skillWeights = new List<int>();
        skillWeights.Add(SKILLWEIGHTBASE / 1); // 36 Weight = Common
        skillWeights.Add(SKILLWEIGHTBASE / 4);
        skillWeights.Add(SKILLWEIGHTBASE / 9); // 4 Weight = Rare
        if (rare)
        {
            skillWeights.Clear();
            skillWeights.Add(0);
            skillWeights.Add(0);
            skillWeights.Add(1); // Always Rare.
        }
        // Remove the exceptions.
        for (int i = 0; i < except.Count; i++)
        {
            int indexOf = possibleSkills.IndexOf(except[i]);
            if (indexOf < 0){continue;}
            possibleSkills.RemoveAt(indexOf);
            skillRarities.RemoveAt(indexOf);
        }
        // Determine the rarity of the reward.
        int rarity = DetermineRewardRarity(skillWeights);
        // Get a random skill of that rarity.
        string randomSkill = GetRewardOfRarity(rarity, possibleSkills, skillRarities);
        return randomSkill;
    }
    public List<string> GenerateSkillBookChoices(int choiceCount = 3, bool rare = false, int floor = 1)
    {
        // Skillbook Names
        List<string> choices = new List<string>();
        // Skillbook Data
        List<string> choiceData = new List<string>();
        for (int i = 0; i < choiceCount; i++)
        {
            string skill = GenerateSkillBook(choices, rare);
            choices.Add(skill);
        }
        for (int i = 0; i < choices.Count; i++)
        {
            choiceData.Add(skillBookDB.ReturnValue(choices[i]));
        }
        rewards.Add("Skillbook");
        rewardSpecifics.Add(string.Join("+", choiceData));
        return choiceData;
    }
    public int GenerateGold(int goldLevel)
    {
        return (utility.Exponent(10, goldLevel) + rewardSeed.Range(0, 10));
    }
    public string GenerateRelic(bool shop = false)
    {
        // Determine the rarity.
        List<int> rarityWeights = new List<int>();
        rarityWeights.Add(RELICWEIGHTBASE / 2); // 16 Weight = Common
        rarityWeights.Add(RELICWEIGHTBASE / 4); // 8 Weight = Uncommon
        rarityWeights.Add(RELICWEIGHTBASE / 8); // 4 Weight = Rare
        int rarity = DetermineRewardRarity(rarityWeights);
        List<string> relicPool = new List<string>(availableRelics);
        if (shop)
        {
            relicPool.AddRange(availableShopRelics);
        }
        // Generate a relic of that rarity.
        List<int> relicRarities = new List<int>();
        for (int i = 0; i < relicPool.Count; i++)
        {
            relicRarities.Add(utility.SafeParseInt(relicRarity.ReturnValue(relicPool[i])));
        }
        string randomRelic = GetRewardOfRarity(rarity, relicPool, relicRarities);
        RemoveRelicFromPool(randomRelic);
        // If none then do nothing.
        return randomRelic;
    }
    // Reward From Different Battle Types
    // Basic 1-1-30-0-0-0, Elite 1-1-60-1-0-0, Boss = 1-2-100-0-1-1, Event = 1-1-30-?-0-0
    public void GenerateBattleRewards(string battleType)
    {
        switch (battleType)
        {
            default:
            GenerateRewards();
            return;
            case "Elite":
            GenerateRewards(1,1,60,1);
            return;
            case "Boss":
            GenerateRewards(1,2,100,0,1,1);
            return;
        }
    }
    public void GenerateRewards(int skillBookCount = 1, int gold = 1, int itemChance = 30, int relicCount = 0, int rare = 0, int allyCount = 0, int skillBookChoices = 3)
    {
        rewards.Clear();
        rewardSpecifics.Clear();
        // Every fight gives gold.
        rewards.Add("Gold");
        rewardSpecifics.Add(GenerateGold(gold).ToString());
        for (int i = 0; i < skillBookCount; i++)
        {
            GenerateSkillBookChoices(skillBookChoices, rare == 1);
        }
        if (rewardSeed.Range(0, 100) < itemChance)
        {
            rewards.Add("Item");
            rewardSpecifics.Add(itemDB.ReturnRandomKey());
        }
        for (int i = 0; i < relicCount; i++)
        {
            string relicName = GenerateRelic();
            rewards.Add("Relic");
            rewardSpecifics.Add(relicName);
        }
    }
    // Some Events/Relics Add Specifics Rewards
    public void AddReward(string type, string specifics)
    {
        rewards.Add(type);
        rewardSpecifics.Add(specifics);
    }
    // TODO ALL Relic Gains Should Go Through Here And Then Check For Pickup Effects.
    public Relic dummyRelic;
    // BOOL since some relic cause selecting card rewards or selecting skills to enchant, which requires a UI popup.
    public bool GainRelic(string relicName, PartyDataManager partyData, StSStateManager stsManager)
    {
        bool rewardPopUp = false;
        List<string> allRelicStats = relicData.ReturnAllValues(relicName);
        int counters = -1;
        for (int i = 0; i < allRelicStats.Count; i++)
        {
            dummyRelic.LoadRelic(allRelicStats[i], relicName);
            if (dummyRelic.TrackCounters())
            {
                counters = dummyRelic.GetBaseCounters();
            }
            // TODO Apply on pickup effects.
            if (dummyRelic.PickUpRelic())
            {
                // PickUpEffects should not have any conditions.
                rewardPopUp = ApplyPickUpEffect(dummyRelic, partyData, stsManager);
            }
        }
        partyData.dungeonBag.GainRelic(relicName, counters.ToString());
        return rewardPopUp;
        // Don't Save Here, Save During The Normal Save Timing.
    }
    // Lots of relics activate run modifiers.
    public StSRunModifiersSaveData runModifiers;
    protected bool ApplyPickUpEffect(Relic relic, PartyDataManager partyData, StSStateManager stsManager)
    {
        // This means pick some amount of skills for the effect to apply.
        if (relic.GetTarget().Contains("AllySkill")){return true;}
        switch (relic.GetTarget())
        {
            default:
            return false;
            case "Allies":
            partyData.ApplyEffectToParty(relic.GetEffect(), relic.GetEffectSpecifics());
            return false;
            case "RewardSelect":
            return true;
            case "gameBool":
            runModifiers.EnableFlag(relic.GetEffect(), relic.GetEffectSpecifics(), "Relic", relic.GetName());
            return false;
            // TODO Move This Somewhere More Central, A Few Relics Need This Timing?
            case "Gold":
            partyData.inventory.GainGold(utility.SafeParseInt(relic.GetEffect()));
            return false;
            case "Relics":
            int relicCount = utility.SafeParseInt(relic.GetEffectSpecifics());
            // No relic gained can grant relics on pickup so it doesn't loop, even if it did it would quickly run out of relics that make relics.
            for (int i = 0; i < relicCount; i++)
            {
                GainRelic(GenerateRelic(), partyData, stsManager);
            }
            return false;
            case "SkillBooks":
            int bookCount = utility.SafeParseInt(relic.GetEffectSpecifics());
            for (int i = 0; i < bookCount; i++)
            {
                partyData.spellBook.GainBook(GenerateSkillBook());
            }
            return false;
            case "ColorlessSkillBooks":
            int cBookCount = utility.SafeParseInt(relic.GetEffectSpecifics());
            for (int i = 0; i < cBookCount; i++)
            {
                partyData.spellBook.GainBook(GenerateSkillBook(null, false, true));
            }
            return false;
            // Can pick and manage inventory if too full?
            case "Potions":
            return true;
        }
    }
}
