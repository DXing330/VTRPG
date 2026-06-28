using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// In charge of managing stacks/determining active factions/assist managing faction effects/assist with trait stacking.
public class AutoChessFactionManager : MonoBehaviour
{
    public AutoChessPrepManager prepManager;
    public AutoChessFactionDataManager factionData;
    public AutoChessFactionDisplay factionDisplay;
    public void UpdateFactionDisplay()
    {
        factionDisplay.UpdateFactionDisplay(activeFactions, allFactionsWithUnits);
    }
    public List<string> activeFactions;
    public bool FactionActive(string factionName)
    {
        int indexOf = allFactionsWithUnits.IndexOf(factionName);
        if (indexOf < 0)
        {
            return false;
        }
        int activeCount = allFactionCounts[indexOf];
        if (activeCount > 2){return true;}
        else if (activeCount == 2 && !factionData.MainFaction(factionName)){return true;}
        return false;
    }
    public string HighestStackActiveFaction()
    {
        UpdateActiveFactions();
        if (activeFactions.Count <= 0){return "";}
        int stackCount = -1;
        int index = -1;
        for (int i = 0; i < activeFactions.Count; i++)
        {
            int stacks = int.Parse(factionData.GetStacksOfFaction(activeFactions[i]));
            if (stacks > stackCount)
            {
                stackCount = stacks;
                index = i;
            }
        }
        return activeFactions[index];
    }
    public string HighestUnitCountFaction()
    {
        UpdateActiveFactions();
        if (allFactionsWithUnits.Count <= 0){return "";}
        int highestCount = -1;
        int index = -1;
        for (int i = 0; i < allFactionCounts.Count; i++)
        {
            if (allFactionCounts[i] > highestCount)
            {
                highestCount = allFactionCounts[i];
                index = i;
            }
        }
        return allFactionsWithUnits[index];
    }
    // TODO We Can Hard Check For Emblems First.
    public List<string> uniqueEmblems;
    public List<string> uniqueUnitNames;
    // Factions Based On Field.
    public List<string> allFactionsWithUnits;
    // Unique Unit Count In Each Faction.
    public List<int> allFactionCounts;
    public void UpdateFactionCount(string factionName)
    {
        int indexOf = allFactionsWithUnits.IndexOf(factionName);
        if (indexOf < 0)
        {
            allFactionsWithUnits.Add(factionName);
            allFactionCounts.Add(1);
            return;
        }
        allFactionCounts[indexOf]++;
    }
    public void UpdateActiveFactions()
    {
        // Refresh.
        uniqueUnitNames.Clear();
        uniqueEmblems.Clear();
        activeFactions.Clear();
        allFactionsWithUnits.Clear();
        allFactionCounts.Clear();
        for (int i = 0; i < prepManager.fieldSlots.Count; i++)
        {
            if (uniqueUnitNames.Contains(prepManager.fieldSlots[i].GetName())){continue;}
            List<string> factions = prepManager.fieldSlots[i].GetFactions();
            for (int j = 0; j < factions.Count; j++)
            {
                UpdateFactionCount(factions[j]);
            }
            uniqueUnitNames.Add(prepManager.fieldSlots[i].GetName());
        }
        // Check Econ Factions On Bench.
        for (int i = 0; i < prepManager.benchSlots.Count; i++)
        {
            if (uniqueUnitNames.Contains(prepManager.benchSlots[i].GetName())){continue;}
            List<string> factions = prepManager.benchSlots[i].GetFactions();
            for (int j = 0; j < factions.Count; j++)
            {
                if (!factionData.EconFaction(factions[j])){continue;}
                UpdateFactionCount(factions[j]);
            }
            uniqueUnitNames.Add(prepManager.benchSlots[i].GetName());
        }
        // Check If The Factions Should Be Active.
        for (int i = 0; i < allFactionsWithUnits.Count; i++)
        {
            if (FactionActive(allFactionsWithUnits[i]))
            {
                activeFactions.Add(allFactionsWithUnits[i]);
            }
        }
        UpdateFactionDisplay();
    }
    public void GainFactionStacks(string faction, int stackAmount)
    {
        factionData.GainFactionStacks(faction, stackAmount);
    }
    public RNGUtility autoChessShopRNG;
    // Assuming That Timing Is Already Checked.
    public void GainStacksSwitch(AutoActorRollUpData actor, int amount = 1)
    {
        AutoChessTrait trait = actor.trait;
        switch (trait.effect)
        {
            default:
            case "Self":
            List<string> actorFactions = new List<string>(actor.GetFactions()).Distinct().ToList();
            for (int i = 0; i < actorFactions.Count; i++)
            {
                GainFactionStacks(actorFactions[i], amount);
            }
            break;
            case "RandomActive":
            if (activeFactions.Count <= 0){return;}
            string randomFaction = activeFactions[autoChessShopRNG.SeedRange(0, activeFactions.Count)];
            GainFactionStacks(randomFaction, amount);
            break;
            case "HighestActive":
            if (activeFactions.Count <= 0){return;}
            GainFactionStacks(HighestStackActiveFaction(), amount);
            break;
        }
    }
}
