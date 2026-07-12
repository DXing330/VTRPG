using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AutoChessFactionEffectManager", menuName = "ScriptableObjects/AutoChess/AutoChessFactionEffectManager", order = 1)]
public class AutoChessFactionEffectManager : ScriptableObject
{
    public void ApplyFactionEffects(List<AutoActor> allActors, List<string> activeFactions, List<int> activeFactionCount, List<int> activeFactionStacks)
    {
        for (int i = 0; i < activeFactions.Count; i++)
        {
            ApplyFactionEffect(allActors, activeFactions[i], activeFactionCount[i], activeFactionStacks[i]);
        }
    }
    public void ApplyFactionEffect(List<AutoActor> allActors, string factionName, int factionCount, int stackCount)
    {
        for (int i = 0; i < allActors.Count; i++)
        {
            if (factionName == "Swift" && stackCount >= 40)
            {
                allActors[i].AddPassiveSkill("AKSwiftTwo", stackCount.ToString());
            }
            if (factionName == "Assist" && factionCount >= 3)
            {
                if (allActors[i].GetAutoChessLevel() > 1)
                {
                    allActors[i].AddPassiveSkill("AKAssist", "4");
                }
                else
                {
                    allActors[i].AddPassiveSkill("AKAssist", "2");
                }
                return;
            }
            ApplyFactionEffectToActor(allActors[i], factionName, factionCount, stackCount);
        }
    }
    public void ApplyFactionEffectToActor(AutoActor actor, string factionName, int factionCount, int stackCount)
    {
        // Harmony
        if (!actor.AutoChessFaction(factionName)){return;}
        string passiveName = "AK" + factionName;
        // Aegir/Yan/Raid/Resilient/Kjerag/Aid/Swift/Agile
        actor.AddPassiveSkill(passiveName, stackCount.ToString());
        switch (factionName)
        {
            default:
            break;
            // Differences Between 2/3 Stacks.
            case "Precision":
            case "Durable":
            if (factionCount >= 3)
            {
                actor.AddPassiveSkill(passiveName + " II", "1");
            }
            break;
            // Differences Between 2/6 Stacks.
            case "Laterano":
            case "Sargon":
            case "Victoria":
            if (factionCount >= 6)
            {
                actor.AddPassiveSkill(passiveName + " II", "1");
            }
            break;
        }
    }
}
