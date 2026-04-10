using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CityManager : MonoBehaviour
{
    public FactionMap map;
    public int minimumCityDist = 6;
    public FactionCityData allCities;
    public FactionCity dummyCity;
    public List<FactionCity> cities;
    public FactionCity GenerateCity()
    {
        FactionCity newCity = Instantiate(dummyCity, transform.position, new Quaternion(0, 0, 0, 0));
        newCity.ResetStats();
        return newCity;
    }
    public List<string> possibleFactionColors;
    public List<string> possibleFactionNames;
    public List<string> possibleFactionStartingTiles;
    protected int GenerateCapitalLocation(int index)
    {
        // This naturally excludes luxury tiles.
        int potentialCapital = map.RandomTileOfType(possibleFactionStartingTiles[index]);
        if (map.ClosestCityDistance(potentialCapital) < minimumCityDist)
        {
            return GenerateCapitalLocation(index);
        }
        if (map.mapUtility.BorderTile(potentialCapital, map.mapSize))
        {
            return GenerateCapitalLocation(index);
        }
        return potentialCapital;
    }
    public void GenerateStartingCities()
    {
        allCities.NewGame();
        cities.Clear();
        for (int i = 0; i < possibleFactionColors.Count; i++)
        {
            FactionCity newCity = GenerateCity();
            newCity.SetFaction(possibleFactionNames[i]);
            newCity.SetColor(possibleFactionColors[i]);
            int capital = GenerateCapitalLocation(i);
            newCity.SetLocation(capital);
            newCity.MakeCapital();
            newCity.AddTile(capital);
            map.MakeCity(capital);
            map.UpdateHighlightedTile(capital, possibleFactionColors[i]);
            List<int> adjacentTiles = map.mapUtility.AdjacentTiles(capital, map.mapSize);
            for (int j = 0; j < adjacentTiles.Count; j++)
            {
                newCity.AddTile(adjacentTiles[j]);
                map.UpdateHighlightedTile(adjacentTiles[j], possibleFactionColors[i]);
            }
            cities.Add(newCity);
        }
        SaveCityData();
    }
    public void Load()
    {
        allCities.Load();
        List<string> cityData = allCities.GetSavedCities();
        for (int i = 0; i < cityData.Count; i++)
        {
            FactionCity newCity = GenerateCity();
            newCity.SetStats(cityData[i]);
            cities.Add(newCity);
        }
    }
    // Create/destroy cities through here, save/load them through the datamanager.
    public void SaveCityData()
    {
        allCities.Save(cities);
    }
}
