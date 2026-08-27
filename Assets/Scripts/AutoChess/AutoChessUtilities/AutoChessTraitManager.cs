using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// In charge of prepmanager traits.
[CreateAssetMenu(fileName = "AutoChessTraitManager", menuName = "ScriptableObjects/AutoChess/AutoChessTraitManager", order = 1)]
public class AutoChessTraitManager : ScriptableObject
{
    public GeneralUtility utility;
    public StatDatabase benchTraitData;
    private bool initializedBenchTraits = false;
    private Dictionary<string, int> benchTraits = new();
    private Dictionary<string, string> benchTraitFactionRequirements = new();
    private void InitializeBenchTraitData()
    {
        // Never Change So Just Do It Once On Startup.
        if (initializedBenchTraits){return;}
        benchTraits.Clear();
        benchTraitFactionRequirements.Clear();
        List<string> allActorsNames = benchTraitData.GetAllKeys();
        for (int i = 0; i < allActorsNames.Count; i++)
        {
            string[] data = benchTraitData.ReturnValue(allActorsNames[i]).Split("|");
            benchTraits[allActorsNames[i]] = int.Parse(data[0]);
            benchTraitFactionRequirements[allActorsNames[i]] = data[1];
        }
        initializedBenchTraits = true;
    }
    public int ReturnTraitSpecificsInt(AutoActorRollUpData actor, AutoChessPrepManager manager)
    {
        AutoChessTrait trait = actor.trait;
        int level = actor.GetLevel();
        int baseAmount = 1;
        string[] blocks = trait.specifics.Split("MultiBy");
        if (blocks.Length > 1)
        {
            baseAmount = utility.SafeParseInt(blocks[1], 1);
        }
        else
        {
            return level * utility.SafeParseInt(blocks[0], 1);
        }
        switch (blocks[0])
        {
            default:
            return level * baseAmount;
            case "Reroll":
            return manager.dataManager.GetRoundRerolls();
            case "ShopLevel":
            return level * baseAmount * manager.dataManager.GetLevel();
            case "GoldSpent":
            return level * baseAmount * manager.dataManager.GetRoundGold();
            case "UnitsBought":
            return level * baseAmount * manager.dataManager.GetRoundActors();
            case "SelfActiveUnits":
            return level * baseAmount * manager.ReturnFieldUnitsWithMatchingFactions(actor);
            case "BenchSize":
            return level * baseAmount * manager.dataManager.GetBenchActorData().Count;
        }
    }
    public void ApplyActorTraitToPrepManager(AutoChessPrepManager manager, AutoActorRollUpData actor, AutoChessTrait trait, bool copied = false)
    {
        if (trait == null){return;}
        int intSpecifics = ReturnTraitSpecificsInt(actor, manager);
        AutoActorRollUpData backActor = manager.GetBackActor(actor);
        AutoActorRollUpData frontActor = manager.GetFrontActor(actor);
        List<string> factionsToAdd = new List<string>();
        switch (trait.effect)
        {
            // Self/SelfActive/HighestActive/RandomActive
            default:
            manager.factionManager.GainStacksSwitch(actor, intSpecifics);
            break;
            case "SelfAndBackActive":
            factionsToAdd.AddRange(actor.GetFactions().Distinct().ToList());
            if (backActor != null)
            {
                factionsToAdd.AddRange(backActor.GetFactions().Distinct().ToList());
            }
            manager.factionManager.GainActiveStacks(factionsToAdd, intSpecifics);
            break;
            case "SelfAndFrontActive":
            factionsToAdd.AddRange(actor.GetFactions().Distinct().ToList());
            if (frontActor != null)
            {
                factionsToAdd.AddRange(frontActor.GetFactions().Distinct().ToList());
            }
            manager.factionManager.GainActiveStacks(factionsToAdd, intSpecifics);
            break;
            case "SelfAndFrontLineActive":
            List<AutoActorRollUpData> lineActors = manager.GetActorsInLineDirection(actor);
            for (int i = 0; i < lineActors.Count; i++)
            {
                factionsToAdd.AddRange(lineActors[i].GetFactions().Distinct().ToList());
            }
            manager.factionManager.GainActiveStacks(factionsToAdd, intSpecifics);
            break;
            case "AllActiveBench":
            for (int i = 0; i < manager.benchSlots.Count; i++)
            {
                factionsToAdd.AddRange(manager.benchSlots[i].GetFactions().Distinct().ToList());
            }
            manager.factionManager.GainActiveStacks(factionsToAdd, intSpecifics);
            break;
            // Don't Unlimited Copy.
            case "CopyFront":
            if (copied || frontActor == null || frontActor.GetTrait().timing != trait.specifics){break;}
            manager.dataManager.AddLog("Copied " + frontActor.GetName() + "'s trait.");
            ApplyActorTraitToPrepManager(manager, actor, frontActor.trait, true);
            break;
            case "CopyBack":
            if (copied || backActor == null || backActor.GetTrait().timing != trait.specifics){break;}
            manager.dataManager.AddLog("Copied " + backActor.GetName() + "'s trait.");
            ApplyActorTraitToPrepManager(manager, actor, backActor.trait, true);
            break;
            case "Reroll":
            manager.dataManager.GainFreeReroll(intSpecifics);
            break;
            case "Gold":
            manager.dataManager.GainGold(intSpecifics);
            break;
            case "NextRoundGold":
            manager.dataManager.GainNextRoundGold(intSpecifics);
            break;
            case "Equipment":
            manager.dataManager.GainEquipment(trait.specifics);
            break;
            case "AegirUnit":
            for (int i = 0; i < Mathf.Max(1, intSpecifics); i++)
            {
                List<string> randomAegirUnits = new List<string>{"Specter", "Skadi", "Andreana"};
                string randomGainedAegir = randomAegirUnits[manager.shopManager.shopData.autoChessShopRNG.SeedRange(0, randomAegirUnits.Count)];
                AutoActorRollUpData gainedAegir = new AutoActorRollUpData();
                gainedAegir.SetName(randomGainedAegir);
                if (manager.shopManager.RemoveFromPool(randomGainedAegir))
                {
                    manager.GainActor(gainedAegir);
                }
            }
            break;
            case "Unit":
            for (int i = 0; i < Mathf.Max(1, intSpecifics); i++)
            {
                string gainedUnitName = trait.specifics;
                AutoActorRollUpData gainedUnit = new AutoActorRollUpData();
                gainedUnit.SetName(gainedUnitName);
                if (manager.shopManager.RemoveFromPool(gainedUnitName))
                {
                    manager.GainActor(gainedUnit);
                }
            }
            break;
            case "HighestActiveUnit":
            string faction = manager.factionManager.HighestUnitCountFaction();
            if (faction == "")
            {
                manager.GainRandomActor();
                break;
            }
            for (int i = 0; i < Mathf.Max(1, intSpecifics); i++)
            {
                manager.GainActorOfFaction(faction);
            }
            break;
        }
    }
    public void CheckBenchTraits(AutoChessPrepManager manager, List<AutoActorRollUpData> benchSlots)
    {
        // Special Cases That Activate Even On The Bench.
        for (int i = 0; i < benchSlots.Count; i++)
        {
            if (!BenchTraitActive(manager, benchSlots[i])){continue;}
            manager.dataManager.AddLog($"{benchSlots[i].GetName()} : [Bench Trait]");
            ApplyActorTraitToPrepManager(manager, benchSlots[i], benchSlots[i].GetTrait());
        }
    }
    public bool BenchTraitActive(AutoChessPrepManager manager, AutoActorRollUpData actor)
    {
        InitializeBenchTraitData();
        string actorName = actor.GetName();
        if (!benchTraits.TryGetValue(actorName, out int benchTrait))
        {
            return false;
        }
        if (benchTrait <= 0)
        {
            return false;
        }
        string[] factionRequirement = benchTraitFactionRequirements[actorName].Split("OR");
        if (factionRequirement[0] == "None")
        {
            return true;
        }
        for (int i = 0; i < factionRequirement.Length; i++)
        {
            if (manager.factionManager.FactionActive(factionRequirement[i]))
            {
                return true;
            }
        }
        return false;
    }
}
