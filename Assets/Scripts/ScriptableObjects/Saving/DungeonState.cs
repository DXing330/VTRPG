using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DungeonState", menuName = "ScriptableObjects/DataContainers/SavedData/DungeonState", order = 1)]
public class DungeonState : SavedState
{
    public Dungeon dungeon;

    public override void NewGame()
    {
        return;
    }

    public override void Save()
    {
        dataPath = Application.persistentDataPath + "/" + filename;
        allData = "";

        allData += "PrevScene=" + previousScene + delimiter;
        allData += "DungeonName=" + dungeon.GetDungeonName() + delimiter;
        allData += "DungeonSize=" + dungeon.GetDungeonSize() + delimiter;
        allData += "FloorTiles=" + String.Join(delimiterTwo, dungeon.GetCurrentFloorTiles()) + delimiter;

        allData += "PartyLocation=" + dungeon.GetPartyLocation() + delimiter;
        allData += "StairsDown=" + dungeon.GetStairsDown() + delimiter;
        allData += "TreasureLocations=" + String.Join(delimiterTwo, dungeon.treasureLocations) + delimiter;

        allData += "EnemySprites=" + String.Join(delimiterTwo, dungeon.allEnemySprites) + delimiter;
        allData += "EnemyParties=" + String.Join(delimiterTwo, dungeon.allEnemyParties) + delimiter;
        allData += "EnemyLocations=" + String.Join(delimiterTwo, dungeon.allEnemyLocations) + delimiter;

        allData += "CurrentFloor=" + dungeon.GetCurrentFloor() + delimiter;

        allData += "QuestGoals=" + String.Join(delimiterTwo, dungeon.GetQuestGoals()) + delimiter;
        allData += "GoalMappings=" + String.Join(delimiterTwo, dungeon.GetGoalMappings()) + delimiter;
        allData += "QuestSpecifics=" + String.Join(delimiterTwo, dungeon.GetQuestSpecifics()) + delimiter;
        allData += "QuestFloors=" + String.Join(delimiterTwo, dungeon.GetGoalFloors()) + delimiter;
        allData += "QuestTiles=" + String.Join(delimiterTwo, dungeon.GetGoalTiles()) + delimiter;

        allData += "QuestFought=" + dungeon.GetQuestFought() + delimiter;
        allData += "ViewedTiles=" + String.Join(delimiterTwo, dungeon.GetViewedTiles()) + delimiter;
        allData += "BossFought=" + dungeon.GetBossFought() + delimiter;
        allData += "MaxStomach=" + dungeon.GetMaxStomach() + delimiter;
        allData += "Stomach=" + dungeon.GetStomach() + delimiter;

        allData += "ItemLocations=" + String.Join(delimiterTwo, dungeon.itemLocations) + delimiter;
        allData += "TrapLocations=" + String.Join(delimiterTwo, dungeon.trapLocations) + delimiter;
        allData += "Weather=" + dungeon.GetWeather() + delimiter;

        allData += "PartyMods=" + String.Join(delimiterTwo, dungeon.partyModifiers) + delimiter;
        allData += "PartyModDurations=" + String.Join(delimiterTwo, dungeon.partyModifierDurations) + delimiter;
        allData += "DungeonLogs=" + String.Join(delimiterTwo, dungeon.GetDungeonLogs()) + delimiter;

        allData += "MerchantLocation=" + dungeon.GetMerchantLocation() + delimiter;
        allData += "MerchantRobbed=" + dungeon.GetMerchantRobbed() + delimiter;
        allData += "MerchantItems=" + dungeon.GetMerchantItems() + delimiter;
        allData += "MerchantPrices=" + dungeon.GetMerchantPrices() + delimiter;

        allData += "QuestBattleInfo=" + dungeon.GetQuestBattleInfo() + delimiter;
        allData += "CustomWeathers=" + dungeon.GetCustomWeathers() + delimiter;
        allData += "CustomTerrains=" + dungeon.GetCustomTerrains() + delimiter;

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

    protected void LoadStat(string data)
    {
        string[] blocks = data.Split("=");
        if (blocks.Length < 2){return;}
        string key = blocks[0];
        string stat = blocks[1];
        switch (key)
        {
            default: break;
            case "PrevScene": previousScene = stat; break;
            case "DungeonName": dungeon.SetDungeonName(stat, false); break;
            case "DungeonSize": dungeon.SetDungeonSize(int.Parse(stat)); break;
            case "FloorTiles": dungeon.LoadFloorTiles(stat.Split(delimiterTwo).ToList()); break;
            case "PartyLocation": dungeon.SetPartyLocation(int.Parse(stat)); break;
            case "StairsDown": dungeon.SetStairsDown(int.Parse(stat)); break;
            case "TreasureLocations": dungeon.SetTreasureLocations(stat.Split(delimiterTwo).ToList()); break;
            case "EnemySprites": dungeon.SetEnemySprites(stat.Split(delimiterTwo).ToList()); break;
            case "EnemyParties": dungeon.SetEnemyParties(stat.Split(delimiterTwo).ToList()); break;
            case "EnemyLocations": dungeon.SetEnemyLocations(stat.Split(delimiterTwo).ToList()); break;
            case "CurrentFloor": dungeon.SetCurrentFloor(int.Parse(stat)); break;
            case "QuestGoals": dungeon.SetQuestGoals(stat.Split(delimiterTwo).ToList()); break;
            case "GoalMappings": dungeon.SetGoalMappings(stat.Split(delimiterTwo).ToList()); break;
            case "QuestSpecifics": dungeon.SetQuestSpecifics(stat.Split(delimiterTwo).ToList()); break;
            case "QuestFloors": dungeon.SetQuestFloors(utility.ConvertStringListToIntList(stat.Split(delimiterTwo).ToList())); break;
            case "QuestTiles": dungeon.SetQuestTiles(stat.Split(delimiterTwo).ToList()); break;
            case "QuestFought": dungeon.SetQuestFought(int.Parse(stat)); break;
            case "ViewedTiles": dungeon.SetViewedTiles(stat.Split(delimiterTwo).ToList()); break;
            case "BossFought": dungeon.SetBossFought(int.Parse(stat)); break;
            case "MaxStomach": dungeon.SetMaxStomach(int.Parse(stat)); break;
            case "Stomach": dungeon.SetStomach(int.Parse(stat)); break;
            case "ItemLocations": dungeon.SetItemLocations(stat.Split(delimiterTwo).ToList()); break;
            case "TrapLocations": dungeon.SetTrapLocations(stat.Split(delimiterTwo).ToList()); break;
            case "Weather": dungeon.SetWeather(stat); break;
            case "PartyMods": dungeon.SetPartyBattleModifiers(stat.Split(delimiterTwo).ToList()); break;
            case "PartyModDurations": dungeon.SetPartyBattleModifierDurations(stat.Split(delimiterTwo).ToList()); break;
            case "DungeonLogs": dungeon.SetDungeonLogs(stat.Split(delimiterTwo).ToList()); break;
            case "MerchantLocation": dungeon.SetMerchantLocation(utility.SafeParseInt(stat)); break;
            case "MerchantRobbed": dungeon.SetMerchantRobbed(int.Parse(stat)); break;
            case "MerchantItems": dungeon.SetMerchantItems(stat.Split(delimiterTwo).ToList()); break;
            case "MerchantPrices": dungeon.SetMerchantPrices(stat.Split(delimiterTwo).ToList()); break;
            case "QuestBattleInfo": dungeon.SetQuestBattleInfo(stat); break;
            case "CustomWeathers": dungeon.SetCustomWeathers(stat); break;
            case "CustomTerrains": dungeon.SetCustomTerrains(stat); break;
        }
    }
}
