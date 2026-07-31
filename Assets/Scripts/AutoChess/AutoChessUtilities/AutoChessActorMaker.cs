using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoChessActorMaker : ActorMaker
{
    public MapUtility mapUtility;
    public PartyData permanentPartyData;
    public StatDatabase actorData;
    public StatDatabase enemyData;
    public StatDatabase equipmentData;
    public SkillEffect skillEffect;
    public int currentID;
    public void ResetID(){currentID = 0;}
    public int GetCurrentID()
    {
        int returnedID = currentID;
        currentID++;
        return returnedID;
    }
    public AutoActor CreateActorOnTeam(string actorRollUpData, int team)
    {
        AutoActorRollUpData newActor = new AutoActorRollUpData();
        newActor.LoadRollUpData(actorRollUpData);
        AutoActor newAutoActor = CreateActor(newActor);
        newAutoActor.SetTeam(team);
        // Flip The Right Team.
        if (team != 0)
        {
            int location = newAutoActor.GetLocation();
            location = mapUtility.HorizontalReflectTile(location, mapSize);
            newAutoActor.SetLocation(location);
        }
        return newAutoActor;
    }
    public AutoActor CreateActor(string actorRollUpData, int location = -1, int direction = -1)
    {
        AutoActorRollUpData newActor = new AutoActorRollUpData();
        newActor.LoadRollUpData(actorRollUpData);
        return CreateActor(newActor, location, direction);
    }
    public AutoActor CreateActor(AutoActorRollUpData rollUpActor, int location = -1, int direction = -1)
    {
        AutoActor newActor = new AutoActor();
        string actorName = rollUpActor.GetName();
        string actorStats = actorData.ReturnValue(actorName);
        newActor.SetPersonalName(actorName);
        newActor.SetSpriteName(actorName);
        newActor.AutoChessSetInitialStatsFromString(actorStats, rollUpActor.GetLevel());
        if (actorName == "Familiar" || actorName == "Player")
        {
            permanentPartyData.Load();
            // Load The Stats From The Main Game.
            string mainGameStats = permanentPartyData.GetStatFromName(actorName);
            string equipmentString = permanentPartyData.GetEquipmentFromName(actorName);
            newActor.SetInitialStatsFromString(mainGameStats);
            ApplyEquipmentToActor(newActor, equipmentString);
        }
        passiveOrganizer.OrganizeActorPassives(newActor);
        if (location < 0)
        {
            newActor.SetLocation(rollUpActor.GetLocation());
        }
        else
        {
            newActor.SetLocation(location);
        }
        if (direction < 0)
        {
            newActor.SetDirection(rollUpActor.GetDirection());
        }
        else
        {
            newActor.SetDirection(location);
        }
        newActor.SetTeam(0);
        newActor.SetID(GetCurrentID());
        AddAutoChessEquipmentToActor(rollUpActor, newActor);
        return newActor;
    }
    public AutoActor CreateEnemyAutoActor(string actorName, int actorLocation, int difficultyScaling = 0)
    {
        AutoActor newActor = new AutoActor();
        string actorStats = enemyData.ReturnValue(actorName);
        newActor.SetPersonalName(actorName);
        newActor.AutoChessEnemySetInitialStatsFromString(actorStats, difficultyScaling);
        passiveOrganizer.OrganizeActorPassives(newActor);
        newActor.SetSpriteName(actorName);
        newActor.SetLocation(actorLocation);
        newActor.SetTeam(1);
        newActor.SetID(GetCurrentID());
        return newActor;
    }
    public void AddAutoChessEquipmentToActor(AutoActorRollUpData rollUpActor, AutoActor actor)
    {
        List<string> equipmentNames = rollUpActor.GetEquipmentNames();
        for (int i = 0; i < equipmentNames.Count; i++)
        {
            if (equipmentNames[i].Length <= 0){continue;}
            AutoChessEquipment newEquip = new AutoChessEquipment();
            newEquip.LoadAutoChessEquipStats(equipmentNames[i], equipmentData.ReturnValue(equipmentNames[i]));
            actor.AddAutoChessEquipment(newEquip);
        }
    }
    public void ApplyAutoChessEquipmentEffects(AutoActor actor, List<AutoActor> allActors)
    {
        List<AutoChessEquipment> actorEquipment = actor.GetAutoChessEquipment();
        for (int i = 0; i < actorEquipment.Count; i++)
        {
            ApplyAutoChessEquipmentEffect(actor, allActors, actorEquipment[i]);
        }
    }
    protected void ApplyAutoChessEquipmentEffect(AutoActor actor, List<AutoActor> allActors, AutoChessEquipment equipment)
    {
        if (equipment.GetTiming() != "Battle"){return;}
        if (equipment.GetTarget() == "Custom")
        {
            ApplyCustomAutoChessEquipmentEffect(actor, allActors, equipment);
            return;
        }
        if (equipment.GetTarget() == "Faction")
        {
            actor.AddFaction(equipment.GetSpecifics());
            return;
        }
        string[] effects = equipment.GetEffect().Split(",");
        string[] specifics = equipment.GetSpecifics().Split(",");
        for (int i = 0; i < effects.Length; i++)
        {
            skillEffect.AffectActor(actor, effects[i], specifics[i]);
        }
    }
    protected void ApplyCustomAutoChessEquipmentEffect(AutoActor actor, List<AutoActor> allActors, AutoChessEquipment equipment)
    {
        switch (equipment.GetName())
        {
            default:
            return;
            case "Gun-Knight's Might":
            break;
            // Needs Data About Purchased Count.
            case "Tianshi's Cauldron":
            break;
            // Needs Data From All Actors.
            case "Steam Heart":
            break;
            case "Kjeragandr's Tears":
            break;
            case "Desert Compass":
            break;
            case "Thief's Gloves":
            if (actor.AutoChessMaxEquipCount()){return;}
            // Generate Up To Two Random Equipment.
            break;
        }
    }
}
