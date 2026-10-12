using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AutoChessFactionEffectManager", menuName = "ScriptableObjects/AutoChess/AutoChessFactionEffectManager", order = 1)]
public class AutoChessFactionEffectManager : ScriptableObject
{
    public StatDatabase factionBreakpointsData;
    private bool breakpointsInitialized = false;
    private Dictionary<string, int> factionBreakpoints = new Dictionary<string, int>();
    protected void InitializeBreakpoints()
    {
        if (breakpointsInitialized){return;}
        List<string> allKeys = factionBreakpointsData.GetAllKeys();
        for (int i = 0; i < allKeys.Count; i++)
        {
            string[] breakpoints = factionBreakpointsData.ReturnValue(allKeys[i]).Split("|");
            // No Second Tier Effect Possible.
            if (breakpoints.Length < 2)
            {
                continue;
            }
            factionBreakpoints[allKeys[i]] = int.Parse(breakpoints[1]);
        }
        breakpointsInitialized = true;
    }
    public void ApplyFactionEffects(List<AutoActor> allActors, AutoChessFactionDataManager factionData)
    {
        InitializeBreakpoints();
        List<string> activeFactions = factionData.GetActiveFactions();
        List<int> activeFactionCount = factionData.GetActiveFactionCount();
        List<int> activeFactionStacks = factionData.GetActiveFactionStacks();
        for (int i = 0; i < activeFactions.Count; i++)
        {
            ApplyFactionEffect(allActors, activeFactions[i], activeFactionCount[i], activeFactionStacks[i]);
        }
        int eliteCount = 0;
        for (int i = 0; i < allActors.Count; i++)
        {
            if (allActors[i].GetLevel() > 1){eliteCount++;}
            if (allActors[i].autoChessTrait.timing == "DuringBattle")
            {
                ApplyScalingFactionTraitToActor(allActors[i], factionData);
            }
        }
        if (eliteCount >= 2)
        {
            int eliteLevel = 1;
            if (eliteCount >= 5){eliteLevel = 2;}
            for (int i = 0; i < allActors.Count; i++)
            {
                if (allActors[i].GetLevel() > 1)
                {
                    allActors[i].AddPassiveSkill("AKElite", eliteLevel.ToString());
                }
            }
        }
    }
    public void ApplyFactionEffect(List<AutoActor> allActors, string factionName, int factionCount, int stackCount)
    {
        for (int i = 0; i < allActors.Count; i++)
        {
            bool allUnits = false;
            // Active Swift Applies To All.
            if (factionName == "Swift" && stackCount >= 40)
            {
                allActors[i].AddPassiveSkill("AKSwift II", stackCount.ToString());
            }
            // Active Assist Applies To All.
            if (factionName == "Assist")
            {
                allActors[i].AddPassiveSkill("AKAssist", "2");
                return;
            }
            // Durable -> All
            // Aid -> All
            // Precision -> All Ranged (Passive Condition Checks For Ranged So Give Everyone)
            // Resilient -> All Melee (Condition Checks)
            if (factionName == "Durable" || factionName == "Aid" || factionName == "Precision" || factionName == "Resilient")
            {
                allUnits = true;
            }
            ApplyFactionEffectToActor(allActors[i], factionName, factionCount, stackCount, allUnits);
        }
    }
    public void ApplyFactionEffectToActor(AutoActor actor, string factionName, int factionCount, int stackCount, bool allUnits = false)
    {
        // Harmony
        if (!actor.AutoChessFaction(factionName) && !allUnits){return;}
        string passiveName = "AK" + factionName;
        if (factionName == "Resilient" && actor.GetAttackRange() > 1)
        {
            return;
        }
        // Aegir/Yan/Raid/Resilient/Kjerag/Aid/Swift/Agile
        actor.AddPassiveSkill(passiveName, stackCount.ToString());
        if(factionBreakpoints.TryGetValue(factionName, out int breakpoint))
        {
            if (factionCount >= breakpoint)
            {
                actor.AddPassiveSkill(passiveName + " II", "1");
            }
        }
    }
    public void ApplyScalingFactionTraitToActor(AutoActor actor, AutoChessFactionDataManager factionData)
    {
        string[] specifics = actor.autoChessTrait.specifics.Split("Scaling");
        if (specifics.Length < 2){return;}
        string[] scalingFactions = specifics[1].Split("AND");
        int stackCount = 0;
        for (int i = 0; i < scalingFactions.Length; i++)
        {
            if (scalingFactions[i] == "Main")
            {
                stackCount += factionData.GetMainFactionStacks();
                continue;
            }
            stackCount += int.Parse(factionData.GetStacksOfFaction(scalingFactions[i]));
        }
        stackCount *= Mathf.Max(1, actor.GetLevel());
        string[] scalingStats = specifics[0].Split("AND");
        int scalingEvery = 1;
        if (specifics.Length > 2)
        {
            scalingEvery = int.Parse(specifics[2]);
        }
        for (int i = 0 ; i < scalingStats.Length; i++)
        {
            switch (scalingStats[i])
            {
                default:
                break;
                case "Attack":
                actor.UpdateBaseAttack(actor.GetBaseAttack() * stackCount / 100 / scalingEvery);
                break;
                case "Health":
                actor.UpdateBaseHealth(actor.GetBaseHealth() * stackCount / 100 / scalingEvery, false);
                actor.HealToMaxHealth();
                break;
                case "Defense":
                actor.UpdateBaseDefense(actor.GetBaseDefense() * stackCount / 100 / scalingEvery);
                break;
                case "AttackSpeed":
                actor.UpdateBaseAttackSpeed(stackCount / scalingEvery);
                break;
            }
        }
    }

}
