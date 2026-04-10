using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class BattleEndManager : MonoBehaviour
{
    public bool test = false;
    public BattleStatsTracker battleStatsTracker;
    public PartyDataManager partyData;
    public TacticActor dummyActor;
    public Equipment dummyEquip;
    public SceneMover sceneMover;
    public GameObject battleEndScreen;
    public TMP_Text battleResult;
    public List<string> actorNames;
    public List<string> skillUpNames;
    public StatTextList allSkillUps;
    public StatTextList allNewAllies;
    public StatTextList allLootDrops;
    public int maxClassLevel = 6;
    public int maxWeaponLevel = 6;
    public int winnerTeam = -1;
    public void SetWinnerTeam(int newInfo) { winnerTeam = newInfo; }

    protected bool CheckAltWinConditions(BattleMap map, string alt, string specifics)
    {
        switch (alt)
        {
            default:
            return false;
            // Check that the required ally type has escaped.
            case "Escape":
            return map.ActorEscaped(specifics);
            // Check that you've captured the required enemy type.
            case "Capture":
            return map.CapturedActor(specifics);
            // Check that you've defeated all the required enemy types.
            case "Defeat":
            return map.EnemyExists(specifics);
        }
    }

    public int FindWinningTeam(BattleMap map, BattleState battleState)
    {
        // Check for party alternate win conditions.
        // Losing/retreating/being captured is generally an autoloss, so enemies don't need alternate win conditions.
        int winningTeam = -1;
        if (CheckAltWinConditions(map, battleState.GetAltWinCon(), battleState.GetAltWinConSpecifics()))
        {
            winningTeam = 0;
            return winningTeam;
        }
        List<TacticActor> actors = map.battlingActors;
        List<int> teams = new List<int>();
        for (int i = 0; i < actors.Count; i++)
        {
            if (teams.IndexOf(actors[i].GetTeam()) < 0)
            {
                teams.Add(actors[i].GetTeam());
            }
        }
        if (teams.Count == 1)
        {
            return teams[0];
        }
        return winningTeam;
    }

    public void UpdatePartyAfterBattle(BattleMap map, int winningTeam = 0)
    {
        List<TacticActor> actors = map.ReturnEndOfBattleActors();
        if (test)
        {
            // Update the details.
            // Reset the battle.
            return;
        }
        if (winningTeam != 0)
        {
            PartyDefeated();
            return;
        }
        List<int> IDs = new List<int>();
        List<string> stats = new List<string>();
        for (int i = 0; i < actors.Count; i++)
        {
            if (actors[i].GetTeam() != 0 || actors[i].GetID() < 0){continue;}
            IDs.Add(actors[i].GetID());
            stats.Add(actors[i].ReturnPersistentStats());
        }
        partyData.UpdatePartyAfterBattle(IDs, stats);
        partyData.Save();
    }

    /*public void UpdateOverworldAfterBattle(int winningTeam)
    {
        if (winningTeam != 0 || subGame) { return; }
        if (overworldState == null){return;}
        string battleType = overworldState.GetBattleType();
        int location = overworldState.GetLocation();
        if (battleType == "") { return; }
        switch (battleType)
        {
            case "Quest":
                overworld.RemoveFeatureAtLocation(location);
                partyData.guildCard.CompleteDefeatQuest(location);
                break;
            case "Feature":
                overworld.RemoveFeatureAtLocation(location);
                break;
            case "Event":
                break;
        }
    }*/

    public void PartyDefeated()
    {
        partyData.PartyDefeated();
    }

    public void EndBattle(int winningTeam)
    {
        if (test)
        {
            // Update the details.
            Debug.Log("Battle Count:"+battleStatsTracker.simulatorState.multiBattleCount);
            Debug.Log("Current Count:"+battleStatsTracker.simulatorState.multiBattleCurrent);
            battleStatsTracker.DisplayDamageStats(winningTeam);
            // Reset the battle.
            return;
        }
        SetWinnerTeam(winningTeam);
        if (winnerTeam == 0)
        {
            battleResult.text = "<color=green>Victory!</color>";
            CalculateSkillUps(true);
            CalculateSkillUps(false);
            allNewAllies.Disable();
            allLootDrops.Disable();
        }
        else
        {
            battleResult.text = "<color=red>Defeat...</color>";
            allSkillUps.Disable();
            allNewAllies.Disable();
            allLootDrops.Disable();
        }
        battleEndScreen.SetActive(true);
    }

    public void ReturnFromBattle()
    {
        partyData.SetFullParty();
        sceneMover.ReturnFromBattle(winnerTeam);
    }

    protected void CalculateSkillUps(bool permanent = true, bool easySkillUps = false)
    {
        List<string> spriteNames = new List<string>();
        List<string> names = new List<string>();
        List<string> baseStats = new List<string>();
        List<string> equipment = new List<string>();
        if (permanent)
        {
            actorNames.Clear();
            skillUpNames.Clear();
            spriteNames = partyData.permanentPartyData.GetSpriteNames();
            names = partyData.permanentPartyData.GetNames();
            baseStats = partyData.permanentPartyData.GetBaseStats();
            equipment = partyData.permanentPartyData.GetEquipmentStats();
        }
        else
        {
            spriteNames = partyData.mainPartyData.GetSpriteNames();
            names = partyData.mainPartyData.GetNames();
            baseStats = partyData.mainPartyData.GetBaseStats();
            equipment = partyData.mainPartyData.GetEquipmentStats();
        }
        for (int i = 0; i < spriteNames.Count; i++)
        {
            dummyActor.SetInitialStatsFromString(baseStats[i]);
            dummyActor.ResetEquipment();
            string[] equipData = equipment[i].Split("@");
            for (int j = 0; j < equipData.Length; j++)
            {
                dummyEquip.SetAllStats(equipData[j]);
                dummyEquip.EquipWeapon(dummyActor);
            }
            int passiveLevel = dummyActor.GetLevelFromPassive(spriteNames[i]);
            if (passiveLevel > 0 && passiveLevel < maxClassLevel)
            {
                int RNG = Random.Range(0, (passiveLevel + 1) * (passiveLevel + 1));
                if (easySkillUps)
                {
                    RNG = 0;
                }
                if (RNG == 0)
                {
                    dummyActor.SetLevelOfPassive(spriteNames[i], passiveLevel + 1);
                    dummyActor.ReloadPassives();
                    baseStats[i] = dummyActor.GetInitialStats();
                    AddSkillUp(names[i], spriteNames[i]);
                }
            }
            string weaponType = dummyActor.GetWeaponType() + " User";
            if (weaponType == " User") { continue; } // If no weapon is equipped then continue.
            passiveLevel = dummyActor.GetLevelFromPassive(weaponType);
            if (passiveLevel <= 0)
            {
                dummyActor.AddPassiveSkill(weaponType, "1");
                dummyActor.ReloadPassives();
                baseStats[i] = dummyActor.GetInitialStats();
                AddSkillUp(names[i], weaponType);
            }
            else if (passiveLevel < maxWeaponLevel)
            {
                int RNG = Random.Range(0, (passiveLevel + 1) * (passiveLevel + 1));
                if (easySkillUps)
                {
                    RNG = 0;
                }
                if (RNG == 0)
                {
                    dummyActor.SetLevelOfPassive(weaponType, passiveLevel + 1);
                    dummyActor.ReloadPassives();
                    baseStats[i] = dummyActor.GetInitialStats();
                    AddSkillUp(names[i], weaponType);
                }
            }
        }
        if (actorNames.Count <= 0) { allSkillUps.Disable(); return; }
        allSkillUps.Enable();
        allSkillUps.SetStatsAndData(actorNames, skillUpNames);
    }

    protected void AddSkillUp(string actor, string skill)
    {
        int indexOf = actorNames.IndexOf(actor);
        if (indexOf < 0)
        {
            actorNames.Add(actor);
            skillUpNames.Add(skill+"+1");
        }
        else
        {
            skillUpNames[indexOf] += ", "+skill+"+1";
        }
    }
}
