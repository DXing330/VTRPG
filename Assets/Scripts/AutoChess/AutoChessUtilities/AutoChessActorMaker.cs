using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoChessActorMaker : ActorMaker
{
    public bool logActorMaker = false;
    public MapUtility mapUtility;
    public PartyData permanentPartyData;
    public StatDatabase actorData;
    public StatDatabase enemyData;
    public StatDatabase equipmentData;
    public StatDatabase autoChessEquipmentRarity;
    public SkillEffect skillEffect;
    public int currentID;
    public void ResetID(){currentID = 0;}
    public int GetCurrentID()
    {
        int returnedID = currentID;
        currentID++;
        return returnedID;
    }
    public AutoActor CreateActorByName(string actorName, int team, int location)
    {
        AutoActorRollUpData newActor = new AutoActorRollUpData();
        newActor.SetName(actorName);
        newActor.LoadBaseStats(actorData);
        AutoActor newAutoActor = CreateActor(newActor);
        newAutoActor.SetTeam(team);
        return newAutoActor;
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
            int oldLocation = newAutoActor.GetLocation();
            int newLocation = mapUtility.HorizontalReflectTile(oldLocation, mapSize);
            newAutoActor.SetLocation(newLocation);
            newAutoActor.FlipDirection();
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
            newActor.SetDirection(direction);
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
        if (equipment.GetTiming() != "Battle"){return;}
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
            // Needs Data From All Actors.
            case "Steam Heart":
            for (int i = 0; i < allActors.Count; i++)
            {
                List<AutoChessEquipment> actorEquipment = allActors[i].GetAutoChessEquipment();
                for (int j = 0; j < actorEquipment.Count; j++)
                {
                    if (actorEquipment[j].GetName().Contains("Hammer"))
                    {
                        ApplyAutoChessEquipmentEffect(actor, allActors, actorEquipment[j]);
                    }
                }
            }
            break;
            case "Thief's Gloves":
            if (actor.AutoChessMaxEquipCount()){return;}
            // Generate Up To Two Random Equipment.
            for (int i = 0; i < 2; i++)
            {
                if (actor.AutoChessMaxEquipCount()){return;}
                AutoChessEquipment newEquipment = new AutoChessEquipment();
                string equipmentName = autoChessEquipmentRarity.ReturnRandomKeyBasedOnIntValue(4);
                newEquipment.LoadAutoChessEquipStats(equipmentName, equipmentData.ReturnValue(equipmentName));
                actor.AddAutoChessEquipment(newEquipment);
                ApplyAutoChessEquipmentEffect(actor, allActors, newEquipment);
            }
            break;
        }
    }
}
