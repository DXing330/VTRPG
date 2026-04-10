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
        allData = newGameData;
        dataPath = Application.persistentDataPath+"/"+filename;
        File.WriteAllText(dataPath, allData);
        Load();
    }

    public override void Save()
    {
        dataPath = Application.persistentDataPath+"/"+filename;
        allData = previousScene+delimiter+currentScene;
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
            LoadStat(dataList[i], i);
        }
    }

    protected void LoadStat(string stat, int index)
    {
        switch (index)
        {
            default:
            break;
            case 0:
            SetPreviousScene(stat);
            break;
            case 1:
            SetCurrentScene(stat);
            break;
        }
    }
}
