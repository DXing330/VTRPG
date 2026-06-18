using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
    using UnityEditor;
#endif

// A Single Effect Of A Choice Of An Event.
[System.Serializable]
public class StSEventEffect
{
    protected string eventEffectDelimiter = "|eEDelim|";
    // All Allies, Random Ally, Inventory, Etc.
    public string target;
    public string effect;
    public string effectSpecifics;
    public string ReturnDataString()
    {
        string allData = "Target=" + target + eventEffectDelimiter;
        allData += "Effect=" + effect + eventEffectDelimiter;
        allData += "Specifics=" + effectSpecifics + eventEffectDelimiter;
        return allData;
    }
    public void LoadDataString(string newData)
    {
        string[] blocks = newData.Split(eventEffectDelimiter);
        for (int i = 0; i < blocks.Length; i++)
        {
            string[] stats = blocks[i].Split("=");
            if (stats.Length < 2){continue;}
            string key = stats[0];
            string value = stats[1];
            switch (key)
            {
                case "Target":
                target = value;
                break;
                case "Effect":
                effect = value;
                break;
                case "Specifics":
                effectSpecifics = value;
                break;
            }
        }
    }
}
// A Single Choice Of An Event.
[System.Serializable]
public class StSEventChoice
{
    private const string eventChoiceDelimiter = "|eCDelim|";
    private const string eventEffectListDelimiter = "|eELDelim|";
    public string choiceText;
    public string choiceEffectDescription;
    public string condition;
    public string conditionSpecifics;
    public string cost;
    public string costSpecifics;
    public List<StSEventEffect> choiceEffects;
    public string ReturnDataString()
    {
        string allData = "";
        allData += "ChoiceText=" + choiceText + eventChoiceDelimiter;
        allData += "ChoiceEffectDescription=" + choiceEffectDescription + eventChoiceDelimiter;
        allData += "Condition=" + condition + eventChoiceDelimiter;
        allData += "ConditionSpecifics=" + conditionSpecifics + eventChoiceDelimiter;
        allData += "Cost=" + cost + eventChoiceDelimiter;
        allData += "CostSpecifics=" + costSpecifics + eventChoiceDelimiter;
        string effectsData = "";
        if (choiceEffects == null)
        {
            choiceEffects = new List<StSEventEffect>();
        }
        for (int i = 0; i < choiceEffects.Count; i++)
        {
            effectsData += choiceEffects[i].ReturnDataString();
            if (i < choiceEffects.Count - 1)
            {
                effectsData += eventEffectListDelimiter;
            }
        }
        allData += "Effects=" + effectsData;
        return allData;
    }
    public void LoadDataString(string newData)
    {
        choiceEffects = new List<StSEventEffect>();
        string[] blocks = newData.Split(eventChoiceDelimiter);
        for (int i = 0; i < blocks.Length; i++)
        {
            string[] stats = blocks[i].Split("=");
            if (stats.Length < 2) { continue; }
            string key = stats[0];
            string value = stats[1];
            switch (key)
            {
                case "ChoiceText":
                    choiceText = value;
                    break;
                case "ChoiceEffectDescription":
                    choiceEffectDescription = value;
                    break;
                case "Condition":
                    condition = value;
                    break;
                case "ConditionSpecifics":
                    conditionSpecifics = value;
                    break;
                case "Cost":
                    cost = value;
                    break;
                case "CostSpecifics":
                    costSpecifics = value;
                    break;
                case "Effects":
                    string[] effectBlocks = value.Split(eventEffectListDelimiter);
                    for (int e = 0; e < effectBlocks.Length; e++)
                    {
                        if (string.IsNullOrEmpty(effectBlocks[e])) { continue; }
                        StSEventEffect newEffect = new StSEventEffect();
                        newEffect.LoadDataString(effectBlocks[e]);
                        choiceEffects.Add(newEffect);
                    }
                    break;
            }
        }
    }
}
// A Basic Event With Straightforward Choices -> Results.
// No Loops, No Additional Selections, Scene Changes Allowed.
[System.Serializable]
public class StSEventData
{
    private const string eventDataDelimiter = "|eDDelim|";

    public string eventName;
    public string eventDescription;
    public string eventSpriteName;
    public string condition;
    public string conditionSpecifics;
    public List<StSEventChoice> choices;
    public StSEventChoice ReturnChoiceAtIndex(int index)
    {
        if (index < 0 || index >= choices.Count){return null;}
        return choices[index];
    }
    public string ReturnDataString()
    {
        if (choices == null)
        {
            choices = new List<StSEventChoice>();
        }
        string allData = "";
        allData += "EventName=" + eventName + eventDataDelimiter;
        allData += "EventDescription=" + eventDescription + eventDataDelimiter;
        allData += "EventSpriteName=" + eventSpriteName + eventDataDelimiter;
        allData += "Condition=" + condition + eventDataDelimiter;
        allData += "ConditionSpecifics=" + conditionSpecifics + eventDataDelimiter;
        allData += "ChoiceCount=" + choices.Count + eventDataDelimiter;
        for (int c = 0; c < choices.Count; c++)
        {
            StSEventChoice choice = choices[c];
            if (choice.choiceEffects == null)
            {
                choice.choiceEffects = new List<StSEventEffect>();
            }
            allData += "Choice" + c + "Text=" + choice.choiceText + eventDataDelimiter;
            allData += "Choice" + c + "EffectDescription=" + choice.choiceEffectDescription + eventDataDelimiter;
            allData += "Choice" + c + "Condition=" + choice.condition + eventDataDelimiter;
            allData += "Choice" + c + "ConditionSpecifics=" + choice.conditionSpecifics + eventDataDelimiter;
            allData += "Choice" + c + "Cost=" + choice.cost + eventDataDelimiter;
            allData += "Choice" + c + "CostSpecifics=" + choice.costSpecifics + eventDataDelimiter;
            allData += "Choice" + c + "EffectCount=" + choice.choiceEffects.Count + eventDataDelimiter;
            for (int e = 0; e < choice.choiceEffects.Count; e++)
            {
                StSEventEffect effect = choice.choiceEffects[e];
                allData += "Choice" + c + "Effect" + e + "Target=" + effect.target + eventDataDelimiter;
                allData += "Choice" + c + "Effect" + e + "Effect=" + effect.effect + eventDataDelimiter;
                allData += "Choice" + c + "Effect" + e + "Specifics=" + effect.effectSpecifics + eventDataDelimiter;
            }
        }
        return allData;
    }
    public void LoadDataString(string newData)
    {
        choices = new List<StSEventChoice>();
        int choiceCount = 0;
        string[] blocks = newData.Split(eventDataDelimiter);
        for (int i = 0; i < blocks.Length; i++)
        {
            string[] stats = blocks[i].Split("=");
            if (stats.Length < 2) { continue; }
            string key = stats[0];
            string value = stats[1];
            switch (key)
            {
                case "EventName":
                    eventName = value;
                    break;
                case "EventDescription":
                    eventDescription = value;
                    break;
                case "EventSpriteName":
                    eventSpriteName = value;
                    break;
                case "Condition":
                    condition = value;
                    break;
                case "ConditionSpecifics":
                    conditionSpecifics = value;
                    break;
                case "ChoiceCount":
                    choiceCount = int.Parse(value);
                    break;
            }
        }
        for (int c = 0; c < choiceCount; c++)
        {
            StSEventChoice newChoice = new StSEventChoice();
            newChoice.choiceEffects = new List<StSEventEffect>();
            int effectCount = 0;
            for (int i = 0; i < blocks.Length; i++)
            {
                string[] stats = blocks[i].Split("=");
                if (stats.Length < 2) { continue; }
                string key = stats[0];
                string value = stats[1];
                if (key == "Choice" + c + "Text")
                {
                    newChoice.choiceText = value;
                }
                else if (key == "Choice" + c + "EffectDescription")
                {
                    newChoice.choiceEffectDescription = value;
                }
                else if (key == "Choice" + c + "Condition")
                {
                    newChoice.condition = value;
                }
                else if (key == "Choice" + c + "ConditionSpecifics")
                {
                    newChoice.conditionSpecifics = value;
                }
                else if (key == "Choice" + c + "Cost")
                {
                    newChoice.cost = value;
                }
                else if (key == "Choice" + c + "CostSpecifics")
                {
                    newChoice.costSpecifics = value;
                }
                else if (key == "Choice" + c + "EffectCount")
                {
                    effectCount = int.Parse(value);
                }
            }
            for (int e = 0; e < effectCount; e++)
            {
                StSEventEffect newEffect = new StSEventEffect();
                for (int i = 0; i < blocks.Length; i++)
                {
                    string[] stats = blocks[i].Split("=");
                    if (stats.Length < 2) { continue; }
                    string key = stats[0];
                    string value = stats[1];
                    if (key == "Choice" + c + "Effect" + e + "Target")
                    {
                        newEffect.target = value;
                    }
                    else if (key == "Choice" + c + "Effect" + e + "Effect")
                    {
                        newEffect.effect = value;
                    }
                    else if (key == "Choice" + c + "Effect" + e + "Specifics")
                    {
                        newEffect.effectSpecifics = value;
                    }
                }
                newChoice.choiceEffects.Add(newEffect);
            }
            choices.Add(newChoice);
        }
    }
}
// Helps Store Basic Event Data.
[CreateAssetMenu(fileName = "StSEventSaveData", menuName = "ScriptableObjects/StS/StSEventSaveData", order = 1)]
public class StSEventSaveDataManager : SavedData
{
    public string delimiter2;
    public RNGUtility eventRNGSeed;
    public StSEventData GetRandomEvent()
    {
        int index = eventRNGSeed.SeedRange(0, availableEvents.Count + 1);
        if (index >= availableEvents.Count)
        {
            return null;
        }
        string eventName = availableEvents[index];
        availableEvents.RemoveAt(index);
        return allEvents[allEventNames.IndexOf(eventName)];
    }
    public StSEventData GetEventByName(string name)
    {
        int index = allEventNames.IndexOf(name);
        if (index < 0){return null;}
        return allEvents[index];
    }
    public List<string> allEventNames;
    public List<string> availableEvents;
    // Only Called In Editor.
    public void ResetAllEvents()
    {
        #if UNITY_EDITOR
            allEvents.Clear();
            EditorUtility.SetDirty(this);
        #endif
    }
    public void RebuildEventNameLists()
    {
        allEventNames = new List<string>();
        for (int i = 0; i < allEvents.Count; i++)
        {
            allEventNames.Add(allEvents[i].eventName);
        }
        availableEvents = new List<string>(allEventNames);
        #if UNITY_EDITOR
            EditorUtility.SetDirty(this);
        #endif
    }
    public List<StSEventData> allEvents;
    public void AddNewEvent(string newEventData)
    {
        StSEventData newEvent = new StSEventData();
        newEvent.LoadDataString(newEventData);
        allEvents.Add(newEvent);
        RebuildEventNameLists();
        Save();
    }
    // Probably Only Used In Editor, All Events Should Always Be Loaded In The Build.
    // Actually, Only Touch The Available Events.
    public override void Save()
    {
        dataPath = Application.persistentDataPath + "/" + filename;
        allData = "";
        allData += "AvailableEvents=" + String.Join(delimiter2, availableEvents) + delimiter;
        File.WriteAllText(dataPath, allData);
    }
    // Probably Only Used In Editor, All Events Should Always Be Loaded In The Build.
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
            case "AvailableEvents":
            availableEvents = value.Split(delimiter2).ToList();
            break;
        }
    }
    // Just reset the available events, don't delete the real event data.
    public override void NewGame()
    {
        RebuildEventNameLists();
    }
}
