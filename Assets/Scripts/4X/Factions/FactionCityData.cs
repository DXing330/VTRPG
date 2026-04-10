using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FactionCityData", menuName = "ScriptableObjects/4X/FactionCityData", order = 1)]
public class FactionCityData : SavedData
{
    public string delimiterTwo;
    public List<string> savedCities;
    public List<string> GetSavedCities(){return savedCities;}

    public override void NewGame()
    {
        savedCities.Clear();
        Save();
    }

    public void Save(List<FactionCity> cities)
    {
        dataPath = Application.persistentDataPath+"/"+filename;
        allData = "";
        savedCities.Clear();
        for (int i = 0; i < cities.Count; i++)
        {
            savedCities.Add(cities[i].GetStats());
        }
        allData += String.Join(delimiter, savedCities);
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
        savedCities = allData.Split(delimiter).ToList();
    }
}
