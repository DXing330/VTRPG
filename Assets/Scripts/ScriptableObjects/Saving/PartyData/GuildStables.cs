using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Store horses and wagons if you want to resize your operation for certain quests.
[CreateAssetMenu(fileName = "GuildStables", menuName = "ScriptableObjects/DataContainers/SavedData/GuildStables", order = 1)]
public class GuildStables : SavedData
{
    public string delimiterTwo;
    public List<string> storedHorses;
    public void StoreHorse(string newHorse){storedHorses.Add(newHorse);}
    public string TakeHorse(int index)
    {
        string horse = storedHorses[index];
        storedHorses.RemoveAt(index);
        return horse;
    }
    public List<string> storedWagons;
    public void StoreWagon(string newWagon){storedWagons.Add(newWagon);}
    public string TakeWagon(int index)
    {
        string wagon = storedWagons[index];
        storedWagons.RemoveAt(index);
        return wagon;
    }

    public override void NewGame()
    {
        dataPath = Application.persistentDataPath+"/"+filename;
        allData = newGameData;
        File.WriteAllText(dataPath, allData);
        Load();
        Save();
    }

    public override void Save()
    {
        dataPath = Application.persistentDataPath+"/"+filename;
        allData = "";
        for (int i = 0; i < storedHorses.Count; i++)
        {
            allData += storedHorses[i];
            if (i < storedHorses.Count - 1){allData += delimiterTwo;}
        }
        allData += delimiter;
        for (int i = 0; i < storedWagons.Count; i++)
        {
            allData += storedWagons[i];
            if (i < storedWagons.Count - 1){allData += delimiterTwo;}
        }
        allData += delimiter;
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
        dataList = allData.Split(delimiter).ToList();
        storedHorses = dataList[0].Split(delimiterTwo).ToList();
        storedWagons = dataList[1].Split(delimiterTwo).ToList();
    }
}
