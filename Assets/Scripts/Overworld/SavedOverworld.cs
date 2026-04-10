using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SavedOverworld", menuName = "ScriptableObjects/DataContainers/SavedData/SavedOverworld", order = 1)]
public class SavedOverworld : SavedData
{
    // These are basically the same, just split up for convenience.
    // SavedOverworld is more concerned with tile state.
    // OverworldState is more concerned with player state.
    public OverworldState overworldState;
    public string delimiterTwo;
    public OverworldGenerator owGen;
    public MapUtility mapUtility;
    public int overworldSize = 99;
    public int GetSize(){return overworldSize;}
    public int zoneCount = 9;
    public int zoneSizeDivisor = 3;
    public List<string> possibleLuxuries;
    public string RandomLuxury(){ return possibleLuxuries[UnityEngine.Random.Range(0, possibleLuxuries.Count)]; }
    public List<string> GetPossibleLuxuries() { return possibleLuxuries; }
    public List<string> luxuryToCityNames;
    public string GetCityNameFromLocation(int cityLocation)
    {
        int indexOf = cityLocations.IndexOf(cityLocation.ToString());
        string suppliedLuxury = cityLuxurySupplys[indexOf];
        int nameIndex = possibleLuxuries.IndexOf(suppliedLuxury);
        return luxuryToCityNames[nameIndex];
    }
    public string GetCityNameFromDemandedLuxury(string luxury)
    {
        int indexOf = cityLuxuryDemands.IndexOf(luxury);
        string suppliedLuxury = cityLuxurySupplys[indexOf];
        int nameIndex = possibleLuxuries.IndexOf(suppliedLuxury);
        return luxuryToCityNames[nameIndex];
    }
    public string RandomCityName()
    {
        return luxuryToCityNames[UnityEngine.Random.Range(0, luxuryToCityNames.Count)];
    }
    [System.NonSerialized]
    public List<string> terrainLayer;
    public string ReturnTerrain(int tileNumber){return terrainLayer[tileNumber];}
    [System.NonSerialized]
    public List<string> featureLayer;
    [System.NonSerialized]
    public List<string> luxuryLayer;
    [System.NonSerialized]
    public List<string> characterLayer;
    public void UpdateLayers(List<string> emptyList)
    {
        featureLayer = new List<string>(emptyList);
        luxuryLayer = new List<string>(emptyList);
        characterLayer = new List<string>(emptyList);
        for (int i = 0; i < cities.Count; i++)
        {
            featureLayer[int.Parse(cityLocations[i])] = cities[i];
        }
        for (int i = 0; i < luxuries.Count; i++)
        {
            luxuryLayer[int.Parse(luxuryLocations[i])] = luxuries[i];
        }
        for (int i = 0; i < features.Count; i++)
        {
            featureLayer[int.Parse(featureLocations[i])] = features[i];
        }
        for (int i = 0; i < characters.Count; i++)
        {
            characterLayer[int.Parse(characterLocations[i])] = characters[i];
        }
    }
    public int RandomTile()
    {
        return UnityEngine.Random.Range(0, terrainLayer.Count);
    }
    public string guildHubSprite;
    // Cities, Guild Hub
    public List<string> cities;
    public List<string> cityLocations;
    public int GetCityLocationFromLuxurySupplied(string luxury)
    {
        int indexOf = cityLuxurySupplys.IndexOf(luxury);
        return int.Parse(cityLocations[indexOf]);
    }
    public int GetCityLocationFromLuxuryDemanded(string luxury)
    {
        int indexOf = cityLuxuryDemands.IndexOf(luxury);
        return int.Parse(cityLocations[indexOf]);
    }
    // Luxuries, their locations will rarely change.
    public List<string> luxuries;
    public List<string> luxuryLocations;
    // Villages, Caves, Ruins, Bandit Camps
    // Note some features can override cities.
    public List<string> features;
    public int ReturnFeatureCount(string featureType)
    {
        return utility.CountStringsInList(features, featureType);
    }
    public List<string> featureLocations;
    public List<string> ReturnLocationsOfFeature(string featureType)
    {
        List<string> locations = new List<string>();
        for (int i = 0; i < features.Count; i++)
        {
            if (features[i] == featureType)
            {
                locations.Add(featureLocations[i]);
            }
        }
        return locations;
    }
    public bool AddFeature(string newFeature, string newLocation)
    {
        if (featureLocations.Contains(newLocation)) { return false; }
        features.Add(newFeature);
        featureLocations.Add(newLocation);
        QuickSave();
        return true;
    }
    public bool FeatureExist(int location)
    {
        int indexOf = featureLocations.IndexOf(location.ToString());
        return indexOf >= 0;
    }
    public void PlayerClearsDungeon()
    {
        RemoveFeatureAtLocation(overworldState.GetLocation());
    }
    public void RemoveFeatureAtLocation(int location)
    {
        int indexOf = featureLocations.IndexOf(location.ToString());
        if (indexOf < 0) { return; }
        featureLocations.RemoveAt(indexOf);
        features.RemoveAt(indexOf);
        QuickSave();
    }
    public void RemoveFeatures(string featureType)
    {
        for (int i = features.Count - 1; i >= 0; i--)
        {
            if (features[i] == featureType)
            {
                featureLocations.RemoveAt(i);
                features.RemoveAt(i);
            }
        }
        QuickSave();
    }
    public string GetFeatureFromLocation(int location)
    {
        int indexOf = featureLocations.IndexOf(location.ToString());
        return features[indexOf];
    }
    // Player, Monsters, Bandits, NPCs
    public List<string> characters;
    public bool SpecificCharacterOnTile(string characterType, int location)
    {
        int indexOf = characterLocations.IndexOf(location.ToString());
        if (indexOf < 0) { return false; }
        return characters[indexOf] == characterType;
    }
    public int ReturnCharacterCount(string characterType)
    {
        return utility.CountStringsInList(characters, characterType);
    }
    public List<string> characterLocations;
    public List<string> ReturnLocationsOfCharacter(string characterType)
    {
        List<string> locations = new List<string>();
        for (int i = 0; i < characters.Count; i++)
        {
            if (characters[i] == characterType)
            {
                locations.Add(characterLocations[i]);
            }
        }
        return locations;
    }
    public string CharacterOnTile(string location)
    {
        int indexOf = characterLocations.IndexOf(location);
        if (indexOf < 0) { return ""; }
        return characters[indexOf];
    }
    public bool AddCharacter(string newChar, string newLocation)
    {
        if (characterLocations.Contains(newLocation)) { return false; }
        characters.Add(newChar);
        characterLocations.Add(newLocation);
        QuickSave();
        return true;
    }
    public bool MoveCharacter(string currentLocation, string newLocation)
    {
        int indexOf = characterLocations.IndexOf(currentLocation);
        if (indexOf < 0){return false;}
        if (characterLocations.Contains(newLocation)){return false;}
        characterLocations[indexOf] = newLocation;
        return true;
    }
    public string MoveCharacterInDirection(string currentLocation, int direction = -1)
    {
        int indexOf = characterLocations.IndexOf(currentLocation);
        if (indexOf < 0) { return currentLocation; }
        if (direction < 0){ direction = UnityEngine.Random.Range(0, 6); }
        int newLocation = mapUtility.PointInDirection(int.Parse(currentLocation), direction, GetSize());
        if (newLocation < 0){ return currentLocation; }
        if (characterLocations.Contains(newLocation.ToString())) { return currentLocation; }
        characterLocations[indexOf] = newLocation.ToString();
        return newLocation.ToString();
    }
    public void RemoveCharacterAtLocation(int location, string characterType = "")
    {
        int indexOf = characterLocations.IndexOf(location.ToString());
        if (indexOf < 0) { return; }
        if (characterType == "")
        {
            characterLocations.RemoveAt(indexOf);
            characters.RemoveAt(indexOf);
        }
        else if (characters[indexOf] == characterType)
        {
            characterLocations.RemoveAt(indexOf);
            characters.RemoveAt(indexOf);
        }
        else { return; }
        QuickSave();
    }
    public int ReturnCharacterDistanceFromPlayer(string characterLocation)
    {
        int cLoc = int.Parse(characterLocation);
        int distance = mapUtility.DistanceBetweenTiles(cLoc, overworldState.GetLocation(), GetSize());
        return distance;
    }
    public int ReturnCharacterDirectionFromPlayer(string characterLocation)
    {
        int cLoc = int.Parse(characterLocation);
        int pLoc = overworldState.GetLocation();
        if (cLoc == pLoc){ return -1; }
        int direction = mapUtility.DirectionBetweenLocations(cLoc, pLoc, GetSize());
        return direction;
    }
    public List<string> cityLuxurySupplys; // List of what luxury the city exports.
    public List<string> cityLuxuryDemands;
    public int ReturnClosestCityDistance(int tileNumber)
    {
        return mapUtility.DistanceBetweenTiles(tileNumber, ReturnClosestCityLocation(tileNumber), overworldSize);
    }
    public int ReturnClosestCityLocation(int tileNumber)
    {
        int distance = 999;
        int cityIndex = -1;
        for (int i = 0; i < cityLocations.Count; i++)
        {
            // Don't count the guild hub as a city.
            if (i == cityLocations.Count/2){continue;}
            int newDistance = mapUtility.DistanceBetweenTiles(tileNumber, int.Parse(cityLocations[i]), overworldSize);
            if (newDistance < distance)
            {
                distance = newDistance;
                cityIndex = i;
            }
        }
        return int.Parse(cityLocations[cityIndex]);
    }
    // If you enter the center city then it's the guild hub, not a regular city.
    public bool CenterCity(int cityLocation)
    {
        return cityLocation == GetCenterCityLocation();
    }
    public int GetCenterCityLocation()
    {
        return GetCityLocationFromIndex(cityLocations.Count/2);
    }
    public int GetCityLocationFromIndex(int index)
    {
        if (index < 0 || index >= cityLocations.Count){return -1;}
        return int.Parse(cityLocations[index]);
    }
    protected void ResetData()
    {
        terrainLayer = new List<string>();
        cities = new List<string>();
        cityLocations = new List<string>();
        cityLuxurySupplys = new List<string>();
        cityLuxuryDemands = new List<string>();
        luxuries = new List<string>();
        luxuryLocations = new List<string>();
        features = new List<string>();
        featureLocations = new List<string>();
        characters = new List<string>();
        characterLocations = new List<string>();
        for (int i = 0; i < GetSize()*GetSize(); i++)
        {
            terrainLayer.Add("");
        }
    }
    protected void GenerateNewOverworld()
    {
        ResetData();
        List<string> zones = new List<string>();
        List<string> luxuryZoneOrder = new List<string>();
        List<string> possibleLuxuriesCopy = new List<string>(possibleLuxuries);
        // Generate the zones.
        for (int i = 0; i < zoneCount; i++)
        {
            // The middle zone has no city/luxury.
            if (i == zoneCount/2)
            {
                luxuryZoneOrder.Add("");
                zones.Add(owGen.GenerateZone(GetSize()/zoneSizeDivisor, "", true));
                continue;
            }
            // Pick a random luxury for each zone.
            int randomLuxIndex = UnityEngine.Random.Range(0, possibleLuxuriesCopy.Count);
            string randomLux = possibleLuxuriesCopy[randomLuxIndex];
            possibleLuxuriesCopy.RemoveAt(randomLuxIndex);
            luxuryZoneOrder.Add(randomLux);
            zones.Add(owGen.GenerateZone(GetSize()/zoneSizeDivisor, randomLux));
        }
        // Stitch them together.
        int extZoneRow = 0;
        int extZoneCol = 0;
        for (int i = 0; i < zones.Count; i++)
        {
            int intZoneRow = 0;
            int intZoneCol = 0;
            string[] zoneInfo = zones[i].Split("@");
            List<string> zoneTerrain = new List<string>(zoneInfo[0].Split("#").ToList());
            List<string> zoneCityLayer = new List<string>(zoneInfo[1].Split("#").ToList());
            List<string> zoneLuxuryLayer = new List<string>(zoneInfo[2].Split("#").ToList());
            for (int j = 0; j < zoneTerrain.Count; j++)
            {
                int tileNumber = (((extZoneRow*(GetSize()/zoneSizeDivisor))+(intZoneRow))*(GetSize()))+((extZoneCol*(GetSize()/zoneSizeDivisor))+(intZoneCol));
                terrainLayer[tileNumber] = zoneTerrain[j];
                if (zoneCityLayer[j] != "")
                {
                    cityLocations.Add((tileNumber).ToString());
                    if (i == zones.Count/2)
                    {
                        cities.Add(guildHubSprite);
                        cityLuxurySupplys.Add("");
                        cityLuxuryDemands.Add("");
                    }
                    else
                    {
                        cities.Add("City");
                        // Supply is based on what zone.
                        cityLuxurySupplys.Add(luxuryZoneOrder[i]);
                        // Demand is based on the opposite side zone.
                        cityLuxuryDemands.Add(luxuryZoneOrder[(luxuryZoneOrder.Count-1)-i]);
                    }
                }
                if (zoneLuxuryLayer[j] != "")
                {
                    luxuries.Add(luxuryZoneOrder[i]);
                    luxuryLocations.Add((tileNumber).ToString());
                }
                intZoneCol++;
                if (intZoneCol >= GetSize()/zoneSizeDivisor)
                {
                    intZoneCol = 0;
                    intZoneRow++;
                }
            }
            extZoneCol++;
            if (extZoneCol >= zoneSizeDivisor)
            {
                extZoneCol = 0;
                extZoneRow++;
            }
        }
    }
    public override void NewGame()
    {
        GenerateNewOverworld();
        Save();
    }
    // Hope this is quicker, this requires loading first;
    public void QuickSave()
    {
        string newData = "";
        string newLocations = "";
        for (int i = 0; i < features.Count; i++)
        {
            newData += features[i];
            newLocations += featureLocations[i];
            if (i < features.Count - 1)
            {
                newData += delimiterTwo;
                newLocations += delimiterTwo;
            }
        }
        dataList[7] = newData;
        dataList[8] = newLocations;
        newData = "";
        newLocations = "";
        for (int i = 0; i < characters.Count; i++)
        {
            newData += characters[i];
            newLocations += characterLocations[i];
            if (i < characters.Count - 1)
            {
                newData += delimiterTwo;
                newLocations += delimiterTwo;
            }
        }
        dataList[9] = newData;
        dataList[10] = newLocations;
        allData = "";
        for (int i = 0; i < dataList.Count; i++)
        {
            allData += dataList[i]+delimiter;
        }
        dataPath = Application.persistentDataPath+"/"+filename;
        File.WriteAllText(dataPath, allData);
    }
    public override void Save()
    {
        dataPath = Application.persistentDataPath+"/"+filename;
        allData = "";
        allData += String.Join(delimiterTwo, terrainLayer);
        allData += delimiter;
        allData += String.Join(delimiterTwo, cities);
        allData += delimiter;
        allData += String.Join(delimiterTwo, cityLocations);
        allData += delimiter;
        allData += String.Join(delimiterTwo, cityLuxurySupplys);
        allData += delimiter;
        allData += String.Join(delimiterTwo, cityLuxuryDemands);
        allData += delimiter;
        allData += String.Join(delimiterTwo, luxuries);
        allData += delimiter;
        allData += String.Join(delimiterTwo, luxuryLocations);
        allData += delimiter;
        allData += String.Join(delimiterTwo, features);
        allData += delimiter;
        allData += String.Join(delimiterTwo, featureLocations);
        allData += delimiter;
        allData += String.Join(delimiterTwo, characters);
        allData += delimiter;
        allData += String.Join(delimiterTwo, characterLocations);
        allData += delimiter;
        File.WriteAllText(dataPath, allData);
    }
    public void QuickLoad()
    {
        dataPath = Application.persistentDataPath+"/"+filename;
        allData = File.ReadAllText(dataPath);
        dataList = allData.Split(delimiter).ToList();
        features = dataList[7].Split(delimiterTwo).ToList();
        featureLocations = dataList[8].Split(delimiterTwo).ToList();
        characters = dataList[9].Split(delimiterTwo).ToList();
        characterLocations = dataList[10].Split(delimiterTwo).ToList();
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
        terrainLayer = dataList[0].Split(delimiterTwo).ToList();
        cities = dataList[1].Split(delimiterTwo).ToList();
        cityLocations = dataList[2].Split(delimiterTwo).ToList();
        cityLuxurySupplys = dataList[3].Split(delimiterTwo).ToList();
        cityLuxuryDemands = dataList[4].Split(delimiterTwo).ToList();
        luxuries = dataList[5].Split(delimiterTwo).ToList();
        luxuryLocations = dataList[6].Split(delimiterTwo).ToList();
        features = dataList[7].Split(delimiterTwo).ToList();
        featureLocations = dataList[8].Split(delimiterTwo).ToList();
        characters = dataList[9].Split(delimiterTwo).ToList();
        characterLocations = dataList[10].Split(delimiterTwo).ToList();
        utility.RemoveEmptyListItems(features);
        utility.RemoveEmptyListItems(featureLocations);
        utility.RemoveEmptyListItems(characters);
        utility.RemoveEmptyListItems(characterLocations);
    }
}
