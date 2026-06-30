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
        healer = 0;
        baseRespawnTimer = 99;
        currentRespawnTimer = 0;
        autoChessTemporaryTraits.Clear();
    }
    // AK AUTOCHESS STATS
    [Header("AK AUTOCHESS STATS")]
    public List<AutoChessEquipment> autoChessEquipment;
    public string autoSkill;
    public int autoSkillCooldown;
    public void SetSkillCoolDown(int newCD)
    {
        autoSkillCooldown = newCD;
    }
    public int GetAutoSkillCoolDown(){return autoSkillCooldown;}
    public List<AutoChessFaction> autoChessFactions;
    public void ResetFactions(){autoChessFactions.Clear();}
    public void AddFaction(string factionName)
    {
        autoChessFactions.Add(Enum.Parse<AutoChessFaction>(factionName));
    }
    public bool Faction(string factionName)
    {
        for (int i = 0; i < autoChessFactions.Count; i++)
        {
            if (autoChessFactions[i].ToString() == factionName){return true;}
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
    public AutoChessTrait autoChessTrait;
    public string attackRangeShape;
    public int healer = 0;
    public int baseRespawnTimer;
    public int currentRespawnTimer = 0;
    public List<AutoChessTrait> autoChessTemporaryTraits;
}
