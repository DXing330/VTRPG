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
        for (int i = 0; i < rewards.Count * 2; i++)
        {
            int index = rewardSeed.Range(0, rewards.Count);
            if (rewardRarities[index] == rarity)
            {
                return rewards[index];
            }
        }
        return "";
    }
    public List<string> GenerateSkillBookChoices(int choiceCount = 3, bool rare = false, int floor = 1)
    {
        List<string> possibleSkills = skillBookDB.GetAllKeys();
        List<int> skillRarities = utility.ConvertStringListToIntList(skillBookRarity.GetAllValues());
        // Need the weights for rarity so not all skills are equally likely.
        // 1 - Common, 2 - Uncommon, 3 - Rare
        List<int> skillWeights = new List<int>();
        skillWeights.Add(SKILLWEIGHTBASE / 1); // 36 Weight = Common
        skillWeights.Add(SKILLWEIGHTBASE / 4);
        skillWeights.Add(SKILLWEIGHTBASE / 9); // 4 Weight = Rare
        if (rare)
        {
            // Change to only include rare skillbooks.
            for (int i = possibleSkills.Count - 1; i >= 0; i--)
            {
                string rarity = skillBookRarity.ReturnValue(possibleSkills[i]);
                if (rarity != "3")
                {
                    possibleSkills.RemoveAt(i);
                    skillRarities.RemoveAt(i);
                }
            }
        }
        List<string> choices = new List<string>();
        for (int i = 0; i < choiceCount; i++)
        {
            // Already removed all nonrare.
            if (rare)
            {
                int rareIndex = rewardSeed.Range(0, possibleSkills.Count);
                choices.Add(skillBookDB.ReturnValue(possibleSkills[rareIndex]));
                possibleSkills.RemoveAt(rareIndex);
                skillRarities.RemoveAt(rareIndex);
                continue;
            }
            // Determine the rarity of the reward.
            int rarity = DetermineRewardRarity(skillWeights);
            // Get a random skill of that rarity.
            string randomSkill = GetRewardOfRarity(rarity, possibleSkills, skillRarities);
            choices.Add(skillBookDB.ReturnValue(randomSkill));
            int randomIndex = possibleSkills.IndexOf(randomSkill);
            possibleSkills.RemoveAt(randomIndex);
            skillRarities.RemoveAt(randomIndex);
        }
        rewards.Add("Skillbook");
        rewardSpecifics.Add(String.Join("+", choices));
        return choices;
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
        // Generate a relic of that rarity.
        List<int> relicRarities = new List<int>();
        for (int i = 0; i < availableRelics.Count; i++)
        {
            relicRarities.Add(utility.SafeParseInt(relicRarity.ReturnValue(availableRelics[i])));
        }
        string randomRelic = GetRewardOfRarity(rarity, availableRelics, relicRarities);
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
    public void GainRelic(string relicName, PartyDataManager partyData, StSStateManager stsManager)
    {
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
                ApplyPickUpEffect(dummyRelic, partyData);
            }
        }
        partyData.dungeonBag.GainRelic(relicName, counters.ToString());
        // Don't Save Here, Save During The Normal Save Timing.
    }
    // Lots of relics activate run modifiers.
    public StSRunModifiersSaveData runModifiers;
    protected void ApplyPickUpEffect(Relic relic, PartyDataManager partyData)
    {
        switch (relic.GetTarget())
        {
            default:
            break;
            case "Allies":
            partyData.ApplyEffectToParty(relic.GetEffect(), relic.GetEffectSpecifics());
            break;
        }
    }
}
