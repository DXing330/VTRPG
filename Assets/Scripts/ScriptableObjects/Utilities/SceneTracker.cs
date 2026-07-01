using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SceneTracker", menuName = "ScriptableObjects/DataContainers/SavedData/SceneTracker", order = 1)]
public class SceneTracker : SavedData
{
    public List<string> sceneNames;
    public string previousScene;
    public void SetPreviousScene(string sceneName)
    {
        previousScene = sceneName;
    }
    public string GetPreviousScene(){return previousScene;}
    public string currentScene;
    public void SetCurrentScene(string sceneName)
    {
        currentScene = sceneName;
        Save();
    }
    public string GetCurrentScene(){return currentScene;}

    public override void NewGame()
    {
        previousScene = "Hub";
        currentScene = "Hub";
        Save();
        Load();
    }

    public override void Save()
    {
        dataPath = Application.persistentDataPath+"/"+filename;
        allData = "";
        allData += "PreviousScene=" + previousScene + delimiter;
        allData += "CurrentScene=" + currentScene + delimiter;
        File.WriteAllText(dataPath, allData);
    }

    public override void Load()
    {
        dataPath = Application.persistentDataPath+"/"+filename;
        if (File.Exists(dataPath)){allData = File.ReadAllText(dataPath);}
        else{allData = newGameData;}
        if (allData.Contains(delimiter)){dataList = allData.Split(delimiter).ToList();}
        else
        {
            dataList.Clear();
            dataList.Add(allData);
        }
        for (int i = 0; i < dataList.Count; i++)
        {
            LoadStat(dataList[i]);
        }
    }

    public override void LoadStat(string data)
    {
        string[] blocks = data.Split("=");
        if (blocks.Length < 2){return;}
        string key = blocks[0];
        string value = blocks[1];
        switch (key)
        {
            default:
            break;
            case "PreviousScene":
            SetPreviousScene(value);
            break;
            case "CurrentScene":
            SetCurrentScene(value);
            break;
        }
    }
}
