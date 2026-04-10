using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventDescriptionViewer : MonoBehaviour
{
    public ActiveDescriptionViewer effectDescriptions;

    public string ReturnEventDescription(string[] blocks)
    {
        string description = "";
        List<string> eventTarget = blocks[1].Split(",").ToList();
        List<string> eventEffect = blocks[2].Split(",").ToList();
        List<string> eventSpecifics = blocks[3].Split(",").ToList();
        for (int i = 0; i < eventTarget.Count; i++)
        {
            if (eventTarget[i] == "Chosen Actor" || eventTarget[i] == "All Actors")
            {
                description += ReturnActorEffectDescriptions(eventEffect[i], eventSpecifics[i]);
            }
            else if (eventTarget[i] == "Inventory")
            {
                description += ReturnInventoryEffectDescriptions(eventEffect[i], eventSpecifics[i]);
            }
            else if (eventTarget[i] == "Party")
            {
                description += "Gain " + eventSpecifics[i] + " " + eventEffect[i] + ".";
            }
            else if (eventTarget[i] == "Equipment")
            {
                description += "Gain " + eventSpecifics[i] + ".";
            }
            else if (eventTarget[i] == "Battle")
            {
                description += "Fight your way through.";
            }
            if (i < eventTarget.Count - 1)
            {
                description += "\n";
            }
        }
        return description;
    }

    protected string ReturnActorEffectDescriptions(string effect, string specifics)
    {
        switch (effect)
        {
            case "Remove":
                return "Remove the target from the party.";
        }
        // Any status applied through an event is permanent?
        return effectDescriptions.AED(effect, specifics, "-1");
    }

    protected string ReturnInventoryEffectDescriptions(string effect, string specifics)
    {
        return "Gain " + specifics + " " + effect;
    }
}
