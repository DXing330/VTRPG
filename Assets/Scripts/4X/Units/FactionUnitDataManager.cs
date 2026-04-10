using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FactionUnitData", menuName = "ScriptableObjects/4X/FactionUnitData", order = 1)]
public class FactionUnitDataManager : SavedData
{
    public string delimiterTwo;
    public List<string> savedUnits;
    public List<string> GetSavedUnits(){return savedUnits;}

    public override void NewGame()
    {
        savedUnits.Clear();
        Save();
    }

    public void Save(List<FactionUnit> units)
    {
        dataPath = Application.persistentDataPath+"/"+filename;
        allData = "";
        savedUnits.Clear();
        for (int i = 0; i < units.Count; i++)
        {
            savedUnits.Add(units[i].GetStats());
        }
        allData += String.Join(delimiter, savedUnits);
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
        savedUnits = allData.Split(delimiter).ToList();
    }
}
