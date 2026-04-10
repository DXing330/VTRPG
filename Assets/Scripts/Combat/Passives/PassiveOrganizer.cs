using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PassiveOrganizer", menuName = "ScriptableObjects/BattleLogic/PassiveOrganizer", order = 1)]
public class PassiveOrganizer : ScriptableObject
{
    public List<string> testPassiveList;
    public List<string> testPassiveLevels;
    public MultiKeyStatDatabase passiveNameLevels;
    public StatDatabase allPassives;
    public StatDatabase passiveNames;
    public List<string> startBattlePassives;
    public List<string> startTurnPassives;
    public List<string> endTurnPassives;
    public List<string> attackingPassives;
    public List<string> defendingPassives;
    public List<string> takeDamagePassives;
    public List<string> movingPassives;
    public List<string> afterAttackPassives;
    public List<string> afterDefendPassives;
    public List<string> outOfCombatPassives;
    public List<string> adjustActivesPassives;
    public List<string> adjustSpellsPassives;
    public List<string> afterSkillPassives;
    public List<string> afterSpellPassives;

    protected void ClearLists()
    {
        startBattlePassives.Clear();
        startTurnPassives.Clear();
        endTurnPassives.Clear();
        attackingPassives.Clear();
        defendingPassives.Clear();
        takeDamagePassives.Clear();
        movingPassives.Clear();
        afterAttackPassives.Clear();
        afterDefendPassives.Clear();
        outOfCombatPassives.Clear();
        adjustActivesPassives.Clear();
        adjustSpellsPassives.Clear();
        afterSkillPassives.Clear();
    }

    protected void OrganizePassivesList(List<string> passives, List<string> passiveLevels)
    {
        ClearLists();
        string passiveName = "";
        for (int i = 0; i < passives.Count; i++)
        {
            for (int j = 1; j <= int.Parse(passiveLevels[i]); j++)
            {
                passiveName = passiveNameLevels.GetMultiKeyValue(passives[i], j.ToString());
                // Don't add any extra passive if they're still the same name at higher levels.
                if (passiveName == passives[i] && j > 1)
                {
                    continue;
                }
                SortPassive(passiveName);
            }
        }
    }

    public void AddSortedPassiveNewLevel(TacticActor actor, string passive, int passiveLevel)
    {
        string passiveName = passiveNameLevels.GetMultiKeyValue(passive, passiveLevel.ToString());
        if (passiveName == passive && passiveLevel > 1){return;}
        AddSortedPassive(actor, passiveName);
    }

    public void AddSortedPassive(TacticActor actor, string passiveName)
    {
        string passiveData = allPassives.ReturnValue(passiveName);
        string[] blocks = passiveData.Split("|");
        string timing = blocks[0];
        actor.AddSortedPassive(passiveData, timing);
    }

    public void RemoveSortedPassive(TacticActor actor, string passiveName)
    {
        string passiveData = allPassives.ReturnValue(passiveName);
        string[] blocks = passiveData.Split("|");
        string timing = blocks[0];
        actor.RemoveSortedPassive(passiveData, timing);
    }

    protected void SortPassive(string passive, bool data = false)
    {
        string passiveDetails = "";
        string timing = "";
        if (data)
        {
            passiveDetails = passive;
        }
        else
        {
            passiveDetails = allPassives.ReturnValue(passive);
        }
        string[] detailBlocks = passiveDetails.Split("|");
        timing = detailBlocks[0];
        switch (timing)
        {
            case "Moving":
                movingPassives.Add(passiveDetails);
                break;
            case "Start":
                startTurnPassives.Add(passiveDetails);
                break;
            case "End":
                endTurnPassives.Add(passiveDetails);
                break;
            case "Attack":
                attackingPassives.Add(passiveDetails);
                break;
            case "Defend":
                defendingPassives.Add(passiveDetails);
                break;
            case "BS":
                startBattlePassives.Add(passiveDetails);
                break;
            case "TakeDamage":
                takeDamagePassives.Add(passiveDetails);
                break;
            case "AfterAttack":
                afterAttackPassives.Add(passiveDetails);
                break;
            case "AfterDefend":
                afterDefendPassives.Add(passiveDetails);
                break;
            case "OOC":
                outOfCombatPassives.Add(passiveDetails);
                break;
            case "AdjustActives":
                adjustActivesPassives.Add(passiveDetails);
                break;
            case "AdjustSpells":
                adjustSpellsPassives.Add(passiveDetails);
                break;
            case "AfterSkill":
                afterSkillPassives.Add(passiveDetails);
                break;
            case "AfterSpell":
                afterSpellPassives.Add(passiveDetails);
                break;
        }
    }

    protected void OrganizeCustomPassives(TacticActor actor)
    {
        List<string> customPassives = actor.GetCustomPassives();
        for (int i = 0; i < customPassives.Count; i++)
        {
            List<string> customData = customPassives[i].Split("|").ToList();
            if (customData.Count < 5){continue;}
            SortPassive(customPassives[i], true);
        }
    }

    public StatDatabase allRunePassives;

    protected void OrganizeRunePassives(TacticActor actor)
    {
        List<string> runePassives = actor.GetRunePassives();
        for (int i = 0; i < runePassives.Count; i++)
        {
            string passiveDetails = allRunePassives.ReturnValue(runePassives[i]);
            string[] blocks = passiveDetails.Split("|");
            SortPassive(passiveDetails, true);
        }
    }

    public void OrganizeActorPassives(TacticActor actor)
    {
        OrganizePassivesList(actor.GetPassiveSkills(), actor.GetPassiveLevels());
        OrganizeCustomPassives(actor);
        OrganizeRunePassives(actor);
        actor.SetStartBattlePassives(startBattlePassives);
        actor.SetStartTurnPassives(startTurnPassives);
        actor.SetEndTurnPassives(endTurnPassives);
        actor.SetAttackingPassives(attackingPassives);
        actor.SetDefendingPassives(defendingPassives);
        actor.SetTakeDamagePassives(takeDamagePassives);
        actor.SetMovingPassives(movingPassives);
        actor.SetAfterAttackPassives(afterAttackPassives);
        actor.SetAfterDefendPassives(afterDefendPassives);
        actor.SetOOCPassives(outOfCombatPassives);
        actor.SetAdjustActivesPassives(adjustActivesPassives);
        actor.SetAdjustSpellsPassives(adjustSpellsPassives);
        actor.SetAfterSkillPassives(afterSkillPassives);
        actor.SetAfterSpellPassives(afterSpellPassives);
    }
}
