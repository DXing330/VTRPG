using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FactionCity : MonoBehaviour
{
    public GeneralUtility utility;
    public string delimiter;
    public string delimiterTwo;
    public string factionName;
    public void SetFaction(string fName){factionName = fName;}
    public string GetFaction(){return factionName;}
    public string factionColor;
    public void SetColor(string fColor){factionColor = fColor;}
    public string GetColor(){return factionColor;}
    public int capital;
    public void SetCapital(int newInfo){capital = newInfo;}
    public void MakeCapital(){capital = 1;}
    public bool Capital(){return capital > 0;}
    // Higher reputation gives you more access to city features.
    // Increase reputation by doing favors or giving gifts.
    public int reputation;
    public int location;
    public void SetLocation(int loc){location = loc;}
    public int GetLocation(){return location;}
    public List<int> ownedTiles;
    public List<int> GetOwnedTiles()
    {
        return ownedTiles;
    }
    public bool OwnTile(int tileNumber)
    {
        return ownedTiles.Contains(tileNumber);
    }
    public void AddTile(int tileNumber)
    {
        ownedTiles.Add(tileNumber);
    }
    // Need more population for higher level buildings.
    // Higher level buildings are an abstraction for better specialists.
    // Ie higher level forge implies more skilled smiths.
    protected int basePopulationMax = 6;
    protected int populationPerLevel = 6;
    public int population; // Determines tax (gold) income.
    protected string populationStorageString = "Housing";
    protected void PopulationChange()
    {
        // First feed the people.
        if (population <= 0){return;}
        // One unit of food feeds six people?
        food -= population / 6;
        // If stored food is negative then population decreases.
        if (food < 0)
        {
            population--;
        }
        // If the stored food is more than population then population increases.
        else if (food > population)
        {
            population++;
        }
    }
    protected int baseStorage = 60;
    protected int storagePerLevel = 60;
    public int mana; // Used for altar upgrades / equipment enchantments / buffs / etc.
    protected string manaStorageString = "ManaStorage";
    public int gold; // Used for everything.
    protected string goldStorageString = "GoldStorage";
    public int food; // Used to maintain / increase population.
    protected string foodStorageString = "FoodStorage";
    public int materials; // Used for building upgrades / maintenance.
    protected string matsStorageString = "MatsStorage";
    public List<string> treasures; // Special resources / valuable equipment / quest items
    protected string treasureStorageString = "Vault";

    // Gameplay loop -> gather resources -> increase population -> level up specialist buildings -> more resources
    // For the gather resources stage, you need to upgrade your resource storages in order to afford the population increases and building upgrades.

    public List<string> buildingData;
    protected void RefreshBuildingData()
    {
        buildingData.Clear();
        for (int i = 0; i < buildings.Count; i++)
        {
            buildingData.Add(buildings[i].GetBuildingString());
        }
    }
    protected void LoadBuildingData(List<string> newInfo)
    {
        buildingData = new List<string>(newInfo);
        for (int i = 0; i < buildingData.Count; i++)
        {
            string[] blocks = buildingData[i].Split(buildings[0].delimiter);
            UpdateBuilding(blocks[0], int.Parse(blocks[1]));
        }
    }
    protected void UpdateBuilding(string bName, int bLevel)
    {
        for (int i = 0; i < buildings.Count; i++)
        {
            if (buildings[i].MatchName(bName))
            {
                buildings[i].SetLevel(bLevel);
                return;
            }
        }
    }
    public List<FactionCityBuilding> buildings;
    public void ResetBuildings()
    {
        for (int i = 0; i < buildings.Count; i++)
        {
            buildings[i].ResetLevel();
        }
    }
    public int GetTotalBuildingLevels()
    {
        int tLevel = 0;
        for (int i = 0; i < buildings.Count; i++)
        {
            tLevel += buildings[i].GetLevel();
        }
        return tLevel;
    }
    
    /*
    // Used by visitors.
    public int marketLevel;
    public int mageTowerLevel;
    public int trainingGroundLevel;
    // Used for unit generation.
    public int barracksLevel;
    public int forgeLevel;
    public int altarLevel;
    // Used for dungeons/sieges.
    public int wallLevel;
    public int fortificationLevel;
    // Used for resources.
    public int housingLevel;
    public int foodStorageLevel;
    public int materialStorageLevel;
    */

    public void ResetStats()
    {
        factionName = "";
        factionColor = "";
        capital = 0;
        reputation = 0;
        location = -1;
        ownedTiles.Clear();
        mana = 0;
        gold = 0;
        food = 0;
        materials = 0;
        treasures.Clear();
        ResetBuildings();
    }

    public string GetStats()
    {
        string data = "";
        data += "FactionName=" + factionName + delimiter;
        data += "FactionColor=" + factionColor + delimiter;
        data += "Capital=" + capital + delimiter;
        data += "Reputation=" + reputation + delimiter;
        data += "Location=" + location + delimiter;
        data += "OwnedTiles=" + String.Join(delimiterTwo, ownedTiles) + delimiter;
        data += "Population=" + population + delimiter;
        data += "Mana=" + mana + delimiter;
        data += "Gold=" + gold + delimiter;
        data += "Food=" + food + delimiter;
        data += "Materials=" + materials + delimiter;
        data += "Treasures=" + String.Join(delimiterTwo, treasures) + delimiter;
        RefreshBuildingData();
        data += "Buildings=" + String.Join(delimiterTwo, buildingData) + delimiter;
        return data;
    }

    public void SetStats(string data)
    {
        string[] blocks = data.Split(delimiter);
        for (int i = 0; i < blocks.Length; i++)
        {
            LoadStat(blocks[i]);
        }
    }

    protected void LoadStat(string stat)
    {
        string[] statsBlocks = stat.Split("=");
        if (statsBlocks.Length < 2){return;}
        string value = statsBlocks[1];
        switch (statsBlocks[0])
        {
            default:
            break;
            case "FactionName":
				factionName = value;
				break;
			case "FactionColor":
				factionColor = value;
				break;
			case "Capital":
				capital = int.Parse(value);
				break;
			case "Reputation":
				reputation = int.Parse(value);
				break;
			case "Location":
				SetLocation(int.Parse(value));
				break;
			case "OwnedTiles":
				ownedTiles = utility.ConvertStringListToIntList(value.Split(delimiterTwo).ToList());
				break;
			case "Population":
				population = int.Parse(value);
				break;
			case "Mana":
				mana = int.Parse(value);
				break;
			case "Gold":
				gold = int.Parse(value);
				break;
			case "Food":
				food = int.Parse(value);
				break;
			case "Materials":
				materials = int.Parse(value);
				break;
			case "Treasures":
				treasures = value.Split(delimiterTwo).ToList();
				break;
			case "Buildings":
				LoadBuildingData(value.Split(delimiterTwo).ToList());
				break;
        }
    }
}
