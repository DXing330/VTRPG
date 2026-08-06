using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// In charge of managing stacks/determining active factions/assist managing faction effects/assist with trait stacking.
public class AutoChessFactionManager : MonoBehaviour
{
    public bool fast = false;
    public AutoChessDataManager dataManager;
    public AutoChessFactionDataManager factionData;
    public void SetDataManager(AutoChessDataManager newData)
    {
        dataManager = newData;
        factionData = newData.factionData;
        factionData.logData = dataManager.logData;
    }
    public AutoChessFactionDisplay factionDisplay;
    public void UpdateFactionDisplay()
    {
        if (factionDisplay == null || fast){return;}
        factionDisplay.UpdateFactionDisplay(activeFactions, allFactionsWithUnits);
    }
    public List<string> activeFactions;
    protected readonly HashSet<string> autoChessMainFactions = new(){"Aegir", "Kjerag", "Laterano", "Sargon", "Victoria", "Yan"};
    public bool FactionActive(string factionName)
    {
        int indexOf = allFactionsWithUnits.IndexOf(factionName);
        if (indexOf < 0)
        {
            return false;
        }
        int activeCount = allFactionCounts[indexOf];
        // Add A Harmony Bool And Check.
        bool harmony = allFactionsWithUnits.Contains("Harmony");
        if (autoChessMainFactions.Contains(factionName) && harmony)
        {
            activeCount++;
        }
        if (activeCount > 2){return true;}
        else if (activeCount == 2 && !factionData.MainFaction(factionName)){return true;}
        return false;
    }
    public int GetStacksOfFaction(string factionName)
    {
        return int.Parse(factionData.GetStacksOfFaction(factionName));
    }
    public string HighestStackActiveFaction()
    {
        UpdateActiveFactions();
        if (activeFactions.Count <= 0){return "";}
        int stackCount = -1;
        int index = -1;
        for (int i = 0; i < activeFactions.Count; i++)
        {
            int stacks = GetStacksOfFaction(activeFactions[i]);
            if (stacks > stackCount)
            {
                stackCount = stacks;
                index = i;
            }
        }
        return activeFactions[index];
    }
    public string HighestStackFaction()
    {
        return factionData.HighestStackFaction();
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
    public List<int> activeFactionCounts;
    public List<string> ReturnAllUnitsOfFaction(string factionName)
    {
        List<string> unitNames = new List<string>();
        List<string> fieldActors = dataManager.GetFieldActorData();
        AutoActorRollUpData actorRollUp = new AutoActorRollUpData();
        for (int i = 0; i < fieldActors.Count; i++)
        {
            if (fieldActors[i].Length <= 0){continue;}
            actorRollUp.LoadRollUpData(fieldActors[i]);
            if (unitNames.Contains(actorRollUp.GetName())){continue;}
            List<string> factions = actorRollUp.GetFactions();
            factions.AddRange(actorRollUp.GetEmblems());
            factions = factions.Distinct().ToList();
            if (factions.Contains(factionName))
            {
                unitNames.Add(actorRollUp.GetName());
            }
        }
        if (!factionData.EconFaction(factionName)){return unitNames;}
        List<string> benchActors = dataManager.GetBenchActorData();
        for (int i = 0; i < benchActors.Count; i++)
        {
            if (benchActors[i].Length <= 0){continue;}
            actorRollUp.LoadRollUpData(benchActors[i]);
            if (unitNames.Contains(actorRollUp.GetName())){continue;}
            List<string> factions = actorRollUp.GetFactions();
            if (factions.Contains(factionName))
            {
                unitNames.Add(actorRollUp.GetName());
            }
        }
        return unitNames;
    }
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
        activeFactions.Clear();
        activeFactionCounts.Clear();
        allFactionsWithUnits.Clear();
        allFactionCounts.Clear();
        AutoActorRollUpData actorRollUp = new AutoActorRollUpData();
        List<string> fieldActors = dataManager.GetFieldActorData();
        for (int i = 0; i < fieldActors.Count; i++)
        {
            if (fieldActors[i].Length <= 0){continue;}
            actorRollUp.LoadRollUpData(fieldActors[i]);
            if (uniqueUnitNames.Contains(actorRollUp.GetName())){continue;}
            List<string> factions = actorRollUp.GetFactions();
            // Check Emblems Here.
            factions.AddRange(actorRollUp.GetEmblems());
            factions = factions.Distinct().ToList();
            for (int j = 0; j < factions.Count; j++)
            {
                UpdateFactionCount(factions[j]);
            }
            uniqueUnitNames.Add(actorRollUp.GetName());
        }
        List<string> benchActors = dataManager.GetBenchActorData();
        // Check Econ Factions On Bench.
        for (int i = 0; i < benchActors.Count; i++)
        {
            if (benchActors[i].Length <= 0){continue;}
            actorRollUp.LoadRollUpData(benchActors[i]);
            if (uniqueUnitNames.Contains(actorRollUp.GetName())){continue;}
            List<string> factions = actorRollUp.GetFactions();
            for (int j = 0; j < factions.Count; j++)
            {
                if (!factionData.EconFaction(factions[j])){continue;}
                UpdateFactionCount(factions[j]);
            }
            uniqueUnitNames.Add(actorRollUp.GetName());
        }
        // Check If The Factions Should Be Active.
        for (int i = 0; i < allFactionsWithUnits.Count; i++)
        {
            if (FactionActive(allFactionsWithUnits[i]))
            {
                activeFactions.Add(allFactionsWithUnits[i]);
                activeFactionCounts.Add(allFactionCounts[i]);
            }
        }
        factionData.SetActiveFactions(activeFactions);
        factionData.SetActiveFactionCount(activeFactionCounts);
        UpdateFactionDisplay();
    }
    public void GainFactionStacks(string faction, int stackAmount)
    {
        factionData.GainFactionStacks(faction, stackAmount);
        UpdateFactionDisplay();
    }
    public RNGUtility autoChessShopRNG;
    // Assuming That Timing Is Already Checked.
    public void GainStacksFromTraitSwitch(AutoChessTrait trait, List<string> actorFactions, int amount = 1, List<string> frontFactions = null)
    {
        factionData.GainStacksFromTraitSwitch(trait, actorFactions, amount, frontFactions);
        UpdateFactionDisplay();
    }
    public void GainStacksSwitch(AutoActorRollUpData actor, int amount = 1)
    {
        AutoChessTrait trait = actor.trait;
        GainStacksFromTraitSwitch(trait, actor.GetFactions().Distinct().ToList(), amount);
    }
    public void GainActiveStacks(List<string> factionNames, int amount = 1)
    {
        for (int i = 0; i < factionNames.Count; i++)
        {
            if (!activeFactions.Contains(factionNames[i])){continue;}
            GainFactionStacks(factionNames[i], amount);
        }
    }
}
