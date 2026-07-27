using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActorSubGameStats : ActorStats
{
    public override void InitializeStats()
    {
        base.InitializeStats();
        autoSkill = "";
        autoSkillCooldown = 99;
        autoChessEquipment.Clear();
        ResetFactions();
        autoChessTrait = new AutoChessTrait();
        akHealer = 0;
        baseRespawnTimer = 99;
        currentRespawnTimer = 0;
        autoChessTemporaryTraits.Clear();
    }
    [Header("AK AUTOCHESS STATS")]
    public int autoChessLevel = 1;
    public void SetAutoChessLevel(int newLevel){autoChessLevel = newLevel;}
    public int GetAutoChessLevel(){return autoChessLevel;}
    public List<AutoChessEquipment> autoChessEquipment = new();
    public List<AutoChessEquipment> GetAutoChessEquipment(){return autoChessEquipment;}
    public List<string> GetAutoChessEquipmentNames()
    {
        List<string> names = new List<string>();
        for (int i = 0; i < autoChessEquipment.Count; i++)
        {
            string name = autoChessEquipment[i].GetName();
            if (name.Length > 1)
            {
                names.Add(name);
            }
        }
        return names;
    }
    protected int autoChessMaxEquipCount = 3;
    public bool AutoChessMaxEquipCount(){return (autoChessEquipment.Count >= autoChessMaxEquipCount);}
    public void AddAutoChessEquipment(AutoChessEquipment newEquip)
    {
        if (AutoChessMaxEquipCount()){return;}
        autoChessEquipment.Add(newEquip);
    }
    public string autoSkill;
    public string GetAutoSkill(){return autoSkill;}
    public int autoSkillCooldown;
    public void SetSkillCoolDown(int newCD)
    {
        autoSkillCooldown = newCD;
    }
    public int GetAutoSkillCoolDown(){return autoSkillCooldown;}
    public List<AutoChessFaction> autoChessFactions = new();
    public List<string> GetAutoChessFactions()
    {
        List<string> factions = new();
        for (int i = 0; i < autoChessFactions.Count; i++)
        {
            factions.Add(autoChessFactions[i].ToString());
        }
        return factions;
    }
    public void ResetFactions(){autoChessFactions.Clear();}
    public void AddFaction(string factionName)
    {
        AutoChessFaction newFaction = Enum.Parse<AutoChessFaction>(factionName);
        if (autoChessFactions.Contains(newFaction)){return;}
        autoChessFactions.Add(newFaction);
    }
    protected readonly HashSet<string> autoChessMainFactions = new(){"Aegir", "Kjerag", "Laterano", "Sargon", "Victoria", "Yan"};
    public bool AutoChessFaction(string factionName)
    {
        for (int i = 0; i < autoChessFactions.Count; i++)
        {
            string factionString = autoChessFactions[i].ToString();
            if (factionString == factionName){return true;}
            if (factionString == "Harmony" && (autoChessMainFactions.Contains(factionName))){return true;}
        }
        return false;
    }
    public void SetFactionsFromString(string data)
    {
        autoChessFactions.Clear();
        string[] blocks = data.Split(",");
        for (int i = 0; i < blocks.Length; i++)
        {
            autoChessFactions.Add(Enum.Parse<AutoChessFaction>(blocks[i]));
        }
    }
    public AutoChessTrait autoChessTrait = new();
    public string autoChessAttackRangeShape = "Circle";
    public string GetAutoChessAttackRangeShape(){return autoChessAttackRangeShape;}
    public int akHealer = 0;
    public bool AKHealer(){return akHealer > 0;}
    public int akAOE = 0;
    public bool AKAOE(){return akAOE > 0;}
    public int baseRespawnTimer;
    public void ChangeRespawnTimer(int amount)
    {
        baseRespawnTimer += amount;
        baseRespawnTimer = Mathf.Max(1, baseRespawnTimer);
    }
    public int currentRespawnTimer = 0;
    public void ResetRespawnTimer(){currentRespawnTimer = 0;}
    public bool ReadyToRespawn()
    {
        PrepareToRespawn();
        return currentRespawnTimer > baseRespawnTimer;
    }
    public void PrepareToRespawn(){currentRespawnTimer++;}
    public List<AutoChessTrait> autoChessTemporaryTraits = new();
}
