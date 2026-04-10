using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StSEvent", menuName = "ScriptableObjects/StS/StSEvent", order = 1)]
public class StSEvent : SavedData
{
    public string delimiterTwo;
    public bool debug;
    public List<bool> debugEvents;
    // Stuff to track the event.
    public List<string> eventPool;
    public string GetRandomEvent()
    {
        int index = UnityEngine.Random.Range(0, eventPool.Count);
        if (debug && !debugEvents[index])
        {
            return GetRandomEvent();
        }
        string newEvent = eventPool[index];
        if (!debug)
        {
            eventPool.RemoveAt(index);
        }
        return newEvent;
    }
    public StatDatabase eventData;
    public StatDatabase eventDescription;
    public StatDatabase eventEquipment;
    public StatDatabase actorStats;
    public string eventName;
    public string GetEventName()
    {
        return eventName;
    }
    public string GetEventDescription()
    {
        return eventDescription.ReturnValue(GetEventName());
    }
    public string eventDetails;
    public string SceneChangeEvent()
    {
        if (choices.Count == 1)
        {
            SelectChoice(0);
            if (eventSpecifics.Count == 1)
            {
                // Reset the event for next time.
                eventName = "";
                Save();
                return eventSpecifics[0];
            }
        }
        return "";
    }
    public string currentChoice;
    public List<string> eventTarget;
    public List<string> eventEffect;
    public List<string> eventSpecifics;
    public List<string> choices;
    public List<string> GetChoices()
    {
        return new List<string>(choices);
    }
    // Stuff to apply skill effects.
    public SkillEffect skillEffect;

    public override void NewGame()
    {
        eventName = "";
        eventPool = new List<string>(eventData.GetAllKeys());
        Save();
    }

    public override void Save()
    {
        dataPath = Application.persistentDataPath + "/" + filename;
        allData = eventName + delimiter;
        allData += String.Join(delimiterTwo, eventPool);
        File.WriteAllText(dataPath, allData);
    }

    public override void Load()
    {
        dataPath = Application.persistentDataPath + "/" + filename;
        if (File.Exists(dataPath))
        {
            allData = File.ReadAllText(dataPath);
            if (allData == "")
            {
                return;
            }
            else
            {
                string[] blocks = allData.Split(delimiter);
                eventName = blocks[0];
                eventDetails = eventData.ReturnValue(eventName);
                choices = eventDetails.Split("&").ToList();
                eventPool = blocks[1].Split(delimiterTwo).ToList();
            }
        }
        else
        {
            return;
        }
    }

    public void ForceGenerate()
    {
        eventName = GetRandomEvent();
        eventDetails = eventData.ReturnValue(eventName);
        choices = eventDetails.Split("&").ToList();
    }

    public void GenerateEvent(PartyDataManager partyData)
    {
        Load();
        if (eventName == "")
        {
            eventName = GetRandomEvent();
            eventDetails = eventData.ReturnValue(eventName);
            choices = eventDetails.Split("&").ToList();
            Save();
        }
    }

    public void SelectChoice(int index)
    {
        currentChoice = choices[index];
        string[] blocks = currentChoice.Split("|");
        eventTarget = blocks[1].Split(",").ToList();
        eventEffect = blocks[2].Split(",").ToList();
        eventSpecifics = blocks[3].Split(",").ToList();
    }

    public void ApplyEventEffects(PartyDataManager partyData, TacticActor actor = null, int selectedIndex = -1)
    {
        for (int i = 0; i < eventTarget.Count; i++)
        {
            switch (eventTarget[i])
            {
                case "Party":
                    partyData.HireMember(actorStats.ReturnValue(eventEffect[i]), eventEffect[i] + " " + UnityEngine.Random.Range(0, 1000));
                    break;
                case "Inventory":
                    partyData.inventory.AddItemQuantity(eventEffect[i], int.Parse(eventSpecifics[i]));
                    break;
                case "Chosen Actor":
                    // Apply the effect.
                    ApplyEffectToActor(partyData, actor, selectedIndex, eventEffect[i], eventSpecifics[i]);
                    partyData.RemoveDeadPartyMembers();
                    break;
                case "All Actors":
                    // Apply the effect.
                    for (int j = 0; j < partyData.ReturnTotalPartyCount(); j++)
                    {
                        ApplyEffectToActor(partyData, partyData.ReturnActorAtIndex(j), j, eventEffect[i], eventSpecifics[i]);
                    }
                    partyData.RemoveDeadPartyMembers();
                    break;
                case "Equipment":
                    partyData.equipmentInventory.AddEquipmentByStats(eventEquipment.ReturnValue(eventSpecifics[i]));
                    break;
            }
        }
        partyData.SetFullParty();
        // Reset the event for next time.
        eventName = "";
        Save();
    }

    public void ApplyEffectToActor(PartyDataManager partyData, TacticActor actor, int partyIndex, string effect, string specifics)
    {
        if (actor == null){ return; }
        switch (effect)
        {
            case "Remove":
                partyData.RemovePartyMember(partyIndex);
                return;
            case "Status":
                // Make sure any statuses last forever.
                skillEffect.AffectActor(actor, effect, specifics, -1);
                partyData.UpdatePartyMember(actor, partyIndex);
                return;
        }
        skillEffect.AffectActor(actor, effect, specifics);
        partyData.UpdatePartyMember(actor, partyIndex);
    }
}
