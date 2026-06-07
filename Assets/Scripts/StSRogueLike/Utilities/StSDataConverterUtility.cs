using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StSDataConverterUtility : MonoBehaviour
{
    public StatDatabase oldDatabase;
    public StSEventSaveDataManager newEventData;
    public string oldEventDataString;
    [ContextMenu("DebugRoundTrip")]
    public void DebugRoundTripAllEvents()
    {
        string allErrors = "";
        for (int i = 0; i < newEventData.allEvents.Count; i++)
        {
            StSEventData original = newEventData.allEvents[i];
            string data = original.ReturnDataString();
            StSEventData loaded = new StSEventData();
            loaded.LoadDataString(data);
            if (original.eventName != loaded.eventName)
            {
                //Debug.LogWarning("Event name mismatch at " + i);
                allErrors += "Event name mismatch at " + i + "\n";
            }
            if (original.eventDescription != loaded.eventDescription)
            {
                //Debug.LogWarning("Event description mismatch at " + i + ": " + original.eventName);
                allErrors += "Event description mismatch at " + i + ": " + original.eventName + "\n";
            }
            if (original.eventSpriteName != loaded.eventSpriteName)
            {
                //Debug.LogWarning("Event sprite mismatch at " + i + ": " + original.eventName);
                allErrors += "Event sprite mismatch at " + i + ": " + original.eventName + "\n";
            }
            if (original.choices.Count != loaded.choices.Count)
            {
                //Debug.LogWarning("Choice count mismatch at " + i + ": " + original.eventName);
                allErrors += "Choice count mismatch at " + i + ": " + original.eventName + "\n";
                continue;
            }
            for (int c = 0; c < original.choices.Count; c++)
            {
                if (original.choices[c].choiceText != loaded.choices[c].choiceText)
                {
                    //Debug.LogWarning("Choice text mismatch at event " + i + ", choice " + c + ": " + original.eventName);
                    allErrors += "Choice text mismatch at event " + i + ", choice " + c + ": " + original.eventName + "\n";
                }
                if (original.choices[c].choiceEffects.Count != loaded.choices[c].choiceEffects.Count)
                {
                    //Debug.LogWarning("Effect count mismatch at event " + i + ", choice " + c + ": " + original.eventName);
                    allErrors += "Effect count mismatch at event " + i + ", choice " + c + ": " + original.eventName + "\n";
                    continue;
                }
                for (int e = 0; e < original.choices[c].choiceEffects.Count; e++)
                {
                    StSEventEffect originalEffect = original.choices[c].choiceEffects[e];
                    StSEventEffect loadedEffect = loaded.choices[c].choiceEffects[e];
                    if (originalEffect.target != loadedEffect.target ||
                        originalEffect.effect != loadedEffect.effect ||
                        originalEffect.effectSpecifics != loadedEffect.effectSpecifics)
                    {
                        Debug.LogWarning(
                            "Effect mismatch at event " + i +
                            ", choice " + c +
                            ", effect " + e +
                            ": " + original.eventName
                        );
                    }
                }
            }
        }
        Debug.Log(allErrors);
    }
    // Test New Event Details
    [ContextMenu("DebugAllEvents")]
    public void DebugAllEvents()
    {
        string allEventDetails = "";
        for (int i = 0; i < newEventData.allEventNames.Count; i++)
        {
            allEventDetails += DebugPrintEvent(newEventData.allEventNames[i]) + "\n";
        }
        Debug.Log(allEventDetails);
    }
    public string DebugPrintEvent(string eventName)
    {
        string eventDetails = "";
        for (int i = 0; i < newEventData.allEvents.Count; i++)
        {
            StSEventData ev = newEventData.allEvents[i];
            if (ev.eventName != eventName) { continue; }
            //Debug.Log("EVENT: " + ev.eventName);
            eventDetails += "EVENT: " + ev.eventName + "\n";
            //Debug.Log("DESC: " + ev.eventDescription);
            eventDetails += "DESC: " + ev.eventDescription + "\n";
            //Debug.Log("SPRITE: " + ev.eventSpriteName);
            eventDetails += "SPRITE: " + ev.eventSpriteName + "\n";
            for (int c = 0; c < ev.choices.Count; c++)
            {
                StSEventChoice choice = ev.choices[c];
                //Debug.Log("Choice " + c + ": " + choice.choiceText);
                eventDetails += "Choice " + c + ": " + choice.choiceText + "\n";
                for (int e = 0; e < choice.choiceEffects.Count; e++)
                {
                    StSEventEffect effect = choice.choiceEffects[e];
                    /*Debug.Log(
                        "  Effect " + e +
                        " | Target: " + effect.target +
                        " | Effect: " + effect.effect +
                        " | Specifics: " + effect.effectSpecifics
                    );*/
                    eventDetails += "  Effect " + e +
                        " | Target: " + effect.target +
                        " | Effect: " + effect.effect +
                        " | Specifics: " + effect.effectSpecifics + "\n";
                }
            }
            return eventDetails;
        }
        Debug.LogWarning("Event not found: " + eventName);
        return "Event not found: " + eventName;
    }
    // TODO Convert The Old Events Into New Ones.
    [ContextMenu("Convert Events")]
    public void ConvertEventsToNewFormat()
    {
        string[] oldEventBlocks = oldEventDataString.Split("@");
        newEventData.ResetAllEvents();
        for (int i = 0; i < oldEventBlocks.Length; i++)
        {
            if (string.IsNullOrEmpty(oldEventBlocks[i])) { continue; }
            StSEventData newEvent = ConvertOldEvent(oldEventBlocks[i]);
            if (string.IsNullOrEmpty(newEvent.eventName)) { continue; }
            newEventData.allEvents.Add(newEvent);
        }
        newEventData.RebuildEventNameLists();
        newEventData.Save();
    }
    public StSEventData ConvertOldEvent(string oldEventData)
    {
        StSEventData newEvent = new StSEventData();
        newEvent.condition = "None";
        newEvent.conditionSpecifics = "None";
        newEvent.choices = new List<StSEventChoice>();
        string[] eventBlocks = oldEventData.Split("#");
        if (eventBlocks.Length < 4)
        {
            Debug.LogWarning("Old event had too few blocks: " + oldEventData);
            return newEvent;
        }
        newEvent.eventName = eventBlocks[0];
        newEvent.eventDescription = eventBlocks[1];
        newEvent.eventSpriteName = eventBlocks[2];
        string choicesData = eventBlocks[3];
        string[] oldChoices = choicesData.Split("&");
        for (int i = 0; i < oldChoices.Length; i++)
        {
            if (string.IsNullOrEmpty(oldChoices[i])) { continue; }
            StSEventChoice newChoice = ConvertOldChoice(oldChoices[i]);
            newEvent.choices.Add(newChoice);
        }
        return newEvent;
    }
    public StSEventChoice ConvertOldChoice(string oldChoiceData)
    {
        StSEventChoice newChoice = new StSEventChoice();
        newChoice.condition = "None";
        newChoice.conditionSpecifics = "None";
        newChoice.cost = "None";
        newChoice.costSpecifics = "None";
        newChoice.choiceEffects = new List<StSEventEffect>();
        string[] choiceBlocks = oldChoiceData.Split("|");
        if (choiceBlocks.Length < 5)
        {
            Debug.LogWarning("Old choice had too few blocks: " + oldChoiceData);
            return newChoice;
        }
        string oldChoiceType = choiceBlocks[0];
        string oldTargetsData = choiceBlocks[1];
        string oldEffectsData = choiceBlocks[2];
        string oldSpecificsData = choiceBlocks[3];
        newChoice.choiceText = choiceBlocks[4];
        string[] oldTargets = oldTargetsData.Split(",");
        string[] oldEffects = oldEffectsData.Split(",");
        string[] oldSpecifics = oldSpecificsData.Split(",");
        int effectCount = Mathf.Max(oldTargets.Length, oldEffects.Length, oldSpecifics.Length);
        for (int i = 0; i < effectCount; i++)
        {
            StSEventEffect newEffect = new StSEventEffect();
            if (i < oldTargets.Length)
            {
                newEffect.target = ConvertOldTarget(oldTargets[i]);
            }
            else
            {
                newEffect.target = "None";
            }
            if (i < oldEffects.Length)
            {
                newEffect.effect = oldEffects[i];
            }
            else
            {
                newEffect.effect = "None";
            }
            if (i < oldSpecifics.Length)
            {
                newEffect.effectSpecifics = oldSpecifics[i];
            }
            else
            {
                newEffect.effectSpecifics = "None";
            }
            newChoice.choiceEffects.Add(newEffect);
        }
        return newChoice;
    }
    public string ConvertOldTarget(string oldTarget)
    {
        if (oldTarget == "Chosen Actor")
        {
            return "RandomActor";
        }
        return oldTarget;
    }
}
