using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActorMaker : MonoBehaviour
{
    public GeneralUtility utility;
    public Equipment dummyEquip = new Equipment();
    public TacticActor actorPrefab;
    public BattleModifier battleModifier;
    public StatDatabase actorStats;
    public StatDatabase elementPassives;
    public StatDatabase speciesPassives;
    public PassiveOrganizer passiveOrganizer;
    public MapPatternLocations mapPatterns;
    public int mapSize;
    public void SetMapSize(int newSize) { mapSize = newSize; }

    [ContextMenu("New Actor")]
    public TacticActor CreateActor()
    {
        TacticActor newActor = Instantiate(actorPrefab, transform.position, new Quaternion(0, 0, 0, 0));
        // Need to reset somethings so that they don't carryover.
        ResetActor(newActor);
        return newActor;
    }
    protected void ResetActor(TacticActor actor)
    {
        actor.ResetEquipment();
        actor.ResetPassives();
        actor.InitializeStats();
        actor.ResetCounter();
        actor.SetCurrentHealth(0);
    }
    protected void SetActorName(TacticActor actor, string actorName)
    {
        actor.SetInitialStatsFromString(actorStats.ReturnValue(actorName));
    }
    protected void AddElementPassive(TacticActor actor, string element)
    {
        if (element == "")
        {
            return;
        }
        string elemental = elementPassives.ReturnValue(element);
        if (elemental == "")
        {
            return;
        }
        string[] blocks = elemental.Split("|");
        string[] passives = blocks[0].Split(",");
        string[] levels = blocks[1].Split(",");
        for (int i = 0; i < passives.Length; i++)
        {
            actor.AddPassiveSkill(passives[i], levels[i]);
        }
    }

    public void AddAttributePassives(TacticActor actor)
    {
        List<string> attributes = actor.GetAttributes();
        for (int i = 0; i < attributes.Count; i++)
        {
            actor.AddPassiveSkill(attributes[i], "1");
        }
    }

    public void AddElementPassives(TacticActor actor)
    {
        List<string> elements = actor.GetElements();
        for (int i = 0; i < elements.Count; i++)
        {
            AddElementPassive(actor, elements[i]);
        }
    }

    public void AddSpeciesPassives(TacticActor actor)
    {
        string speciesPassive = speciesPassives.ReturnValue(actor.GetSpecies());
        if (speciesPassive == "")
        {
            return;
        }
        string[] blocks = speciesPassive.Split("|");
        string[] passives = blocks[0].Split(",");
        string[] levels = blocks[1].Split(",");
        for (int i = 0; i < passives.Length; i++)
        {
            actor.AddPassiveSkill(passives[i], levels[i]);
        }
    }
    public TacticActor SummonActor(int location, string actorName, int team = 0)
    {
        TacticActor newActor = SpawnActor(location, actorName, team);
        AddElementPassives(newActor);
        AddAttributePassives(newActor);
        AddSpeciesPassives(newActor);
        passiveOrganizer.OrganizeActorPassives(newActor);
        newActor.StartTurnResetStats();
        return newActor;
    }
    public TacticActor SpawnActor(int location, string actorName, int team = 0)
    {
        TacticActor newActor = CreateActor();
        newActor.SetLocation(location);
        SetActorName(newActor, actorName);
        newActor.SetPersonalName(actorName);
        newActor.SetTeam(team);
        passiveOrganizer.OrganizeActorPassives(newActor);
        newActor.StartTurnResetStats();
        return newActor;
    }
    // Actor Maker is in charge of equipment in relation to actor starting stats for now.
    public StatDatabase weaponReach;
    public RuneGridManager runeGridManager;
    protected void ApplyWeaponReach(TacticActor actor, Equipment equip)
    {
        if (equip.GetSlot() != "Weapon"){return;}
        string reach = weaponReach.ReturnValue(equip.GetEquipType());
        if (reach.Length <= 0){return;}
        actor.SetWeaponReach(utility.SafeParseInt(reach));
    }
    // ALL Base Equipment Changes Should Go Through Here.
    public void ApplyEquipmentToActor(TacticActor actor, string allEquipmentString)
    {
        List<string> equipmentSets = new List<string>();
        string[] equipData = allEquipmentString.Split("@");
        for (int i = 0; i < equipData.Length; i++)
        {
            if (equipData[i].Length < 6){continue;}
            dummyEquip.SetAllStats(equipData[i]);
            dummyEquip.EquipToActor(actor);
            actor.EquipToActor(dummyEquip);
            ApplyWeaponReach(actor, dummyEquip);
            if (dummyEquip.GetEquipSet() != "")
            {
                equipmentSets.Add(dummyEquip.GetEquipSet());
            }
        }
        List<string> uniqueSets = equipmentSets.Distinct().ToList();
        List<int> uniqueSetLevels = new List<int>();
        // TODO Test That This Works As Intended With Sets.
        for (int i = 0; i < uniqueSets.Count; i++)
        {
            uniqueSetLevels.Add(utility.CountStringsInList(equipmentSets, uniqueSets[i]));
            actor.AddPassiveSkill(uniqueSets[i], uniqueSetLevels[i].ToString());
        }
        runeGridManager.ApplyRuneGridToActor(actor);
        actor.UpdateCurrentEquipment();
    }
    // For testing equipment.
    public TacticActor SpawnActorWithEquipment(TacticActor actor, int location, string actorName, int team, string equipment)
    {
        SetActorName(actor, actorName);
        ResetActor(actor);
        ApplyEquipmentToActor(actor, equipment);
        AddElementPassives(actor);
        AddAttributePassives(actor);
        AddSpeciesPassives(actor);
        passiveOrganizer.OrganizeActorPassives(actor);
        actor.StartTurnResetStats();
        return actor;
    }
    public List<TacticActor> SpawnTeamInPattern(string pattern, int team, List<string> teamNames, List<string> teamStats = null, List<string> teamPersonalNames = null, List<string> teamEquipment = null, List<string> teamIDs = null)
    {
        if (teamStats == null) { teamStats = new List<string>(); }
        if (teamPersonalNames == null) { teamPersonalNames = new List<string>(); }
        if (teamEquipment == null) { teamEquipment = new List<string>(); }
        if (teamIDs == null) { teamIDs = new List<string>(); }
        List<TacticActor> actors = new List<TacticActor>();
        // Randomize the team name order to randomize their spawn locations?
        List<int> patternLocations = mapPatterns.ReturnTilesOfPattern(pattern, teamNames.Count, mapSize);
        for (int i = 0; i < teamNames.Count; i++)
        {
            actors.Add(SpawnActor(patternLocations[i], teamNames[i], team));
            if (i < teamStats.Count)
            {
                actors[i].SetInitialStatsFromString(teamStats[i]);
            }
            if (i < teamPersonalNames.Count)
            {
                actors[i].SetPersonalName(teamPersonalNames[i]);
            }
            else { actors[i].SetPersonalName(actors[i].GetSpriteName()); }
            if (i < teamEquipment.Count)
            {
                ApplyEquipmentToActor(actors[i], teamEquipment[i]);
            }
            if (i < teamIDs.Count)
            {
                actors[i].SetID(int.Parse(teamIDs[i]));
            }
            else
            {
                actors[i].ResetID();
            }
            // Add the elemental passives at the end.
            AddElementPassives(actors[i]);
            AddAttributePassives(actors[i]);
            AddSpeciesPassives(actors[i]);
            passiveOrganizer.OrganizeActorPassives(actors[i]);
            actors[i].StartTurnResetStats();
        }
        return actors;
    }
    public void ChangeActorForm(TacticActor actor, string newForm)
    {
        // Change the sprite name.
        // Update the base stats of the actor.
        actor.SetSpriteName((newForm));
        actor.ResetElements();
        actor.ChangeFormFromString(actorStats.ReturnValue(newForm));
        AddElementPassives(actor);
        AddAttributePassives(actor);
        AddSpeciesPassives(actor);
        // Set the new base health equal to the current health.
        actor.SetBaseHealth(actor.GetHealth());
        passiveOrganizer.OrganizeActorPassives(actor);
    }
    public TacticActor CloneActor(TacticActor actor, int location)
    {
        TacticActor newActor = CreateActor();
        SetActorName(newActor, actor.GetSpriteName());
        newActor.SetPersonalName(actor.GetPersonalName());
        newActor.SetLocation(location);
        newActor.CopyBaseStats(actor);
        newActor.SetTeam(actor.GetTeam());
        passiveOrganizer.OrganizeActorPassives(actor);
        return newActor;
    }
    public void ApplyBattleModifiers(List<TacticActor> actors, List<string> battleMods)
    {
        for (int i = 0; i < battleMods.Count; i++)
        {
            battleModifier.LoadModifierByName(battleMods[i]);
            for (int j = 0; j < actors.Count; j++)
            {
                battleModifier.ApplyModifiers(actors[j]);
            }
        }
    }
}
