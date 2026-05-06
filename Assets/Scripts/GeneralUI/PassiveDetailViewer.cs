using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PassiveDetailViewer : MonoBehaviour
{
    public string MapTilePassives(BattleMap map, int tileNumber)
    {
        string tileDetails = map.mapInfo[tileNumber];
        string[] tilePassives = map.terrainPassives.ReturnValue(tileDetails).Split("!");
        for (int i = 0; i < tilePassives.Length; i++)
        {
            if (tilePassives[i].Length < 6){continue;}
            tileDetails += "\n";
            tileDetails += ReturnPassiveDetails(tilePassives[i]);
        }
        return tileDetails;
    }
    public string MapTEffectPassives(BattleMap map, int tileNumber)
    {
        string tileDetails = map.terrainEffectTiles[tileNumber];
        if (tileDetails == ""){return "";}
        string[] tilePassives = map.terrainEffectData.ReturnValue(tileDetails).Split("!");
        for (int i = 0; i < tilePassives.Length; i++)
        {
            if (tilePassives[i].Length < 6){continue;}
            tileDetails += "\n";
            tileDetails += ReturnPassiveDetails(tilePassives[i]);
        }
        return tileDetails;
    }
    public int textSize;
    public void UpdateTextSize()
    {
        passiveStatTextList.UpdateTextSize();
    }
    public string passiveGroupName;
    public StatTextText passiveGroupText;
    public void SetPassiveGroupName(string newGroup){passiveGroupName = newGroup;}
    public string passiveLevel;
    public void SetPassiveLevel(string newLevel){passiveLevel = newLevel;}
    public List<string> passiveNames;
    public List<string> passiveInfo;
    public List<string> passiveDescription;
    public MultiKeyStatDatabase passiveNameLevels;
    public int GetMaxLevelFromPassiveName(string passiveName)
    {
        return passiveNameLevels.GetHighestLevelFromPassive(passiveName);
    }
    public StatDatabase allPassives;
    public StatDatabase runePassives;
    public void ViewRunePassives(TacticActor actor)
    {
        List<string> rPassives = actor.GetRunePassives();
        panel.SetActive(true);
        passiveNames.Clear();
        passiveInfo.Clear();
        passiveDescription.Clear();
        passiveGroupText.SetStatText("Rune Passives");
        passiveGroupText.SetText("");
        for (int i = 0; i < rPassives.Count; i++)
        {
            passiveNames.Add(rPassives[i]);
            passiveDescription.Add(ReturnPassiveDetails(runePassives.ReturnValue(rPassives[i])));
        }
        passiveStatTextList.SetStatsAndData(passiveNames, passiveDescription);
    }
    public string GetRunePassiveString(string runeName)
    {
        string runePassive = runePassives.ReturnValue(runeName);
        return ReturnPassiveDetails(runePassive);
    }
    public void ViewRunePassive(string runeName)
    {
        SetPassiveGroupName(runeName);
        SetPassiveLevel("1");
        panel.SetActive(true);
        passiveNames.Clear();
        passiveInfo.Clear();
        passiveDescription.Clear();
        passiveNames.Add(runeName);
        passiveInfo.Add(runePassives.ReturnValue(passiveNames[0]));
        passiveDescription.Add(ReturnPassiveDetails(passiveInfo[0]));
        passiveStatTextList.SetStatsAndData(passiveNames, passiveDescription);
        passiveGroupText.SetStatText(passiveGroupName);
        passiveGroupText.SetText(passiveLevel);
    }
    public GameObject panel;
    public void DisablePanel(){panel.SetActive(false);}
    public StatTextList passiveStatTextList;
    public SelectStatTextList passiveSelect;
    public void SelectPassive()
    {
        if (passiveSelect.GetSelected() < 0){return;}
        UpdatePassiveNames(passiveSelect.GetSelectedStat(), passiveSelect.GetSelectedData());
    }

    public List<string> ReturnAllPassiveInfo(List<string> groups, List<string> levels)
    {
        List<string> allInfo = new List<string>();
        for (int i = 0; i < groups.Count; i++)
        {
            int level = int.Parse(levels[i]);
            for (int j = 0; j < level; j++)
            {
                string passiveName = passiveNameLevels.GetMultiKeyValue(groups[i], (j+1).ToString());
                allInfo.Add(allPassives.ReturnValue(passiveName));
            }
        }
        allInfo = allInfo.Distinct().ToList();
        return allInfo;
    }

    public string ReturnSpecificPassiveLevelEffect(string group, int level)
    {
        string passiveName = passiveNameLevels.GetMultiKeyValue(group, (level).ToString());
        return ReturnPassiveDetails(allPassives.ReturnValue(passiveName));
    }

    public (List<string> names, List<string> details) ReturnAllPassiveLevelsFromName(string passiveName)
    {
        int maxLevel = GetMaxLevelFromPassiveName(passiveName);
        List<string> allNames = new List<string>();
        List<string> allDetails = new List<string>();
        for (int i = 0; i < maxLevel; i++)
        {
            allNames.Add(passiveNameLevels.GetMultiKeyValue(passiveGroupName, (i+1).ToString()));
            allDetails.Add(ReturnPassiveDetails(allPassives.ReturnValue(allNames[i])));
        }
        return (allNames, allDetails);
    }

    // Used to make the display popup and the text equal to the passive details.
    public void UpdatePassiveNames(string group, string newLevel)
    {
        SetPassiveGroupName(group);
        SetPassiveLevel(newLevel);
        panel.SetActive(true);
        int level = int.Parse(passiveLevel);
        passiveNames.Clear();
        passiveInfo.Clear();
        passiveDescription.Clear();
        for (int i = 0; i < level; i++)
        {
            passiveNames.Add(passiveNameLevels.GetMultiKeyValue(passiveGroupName, (i+1).ToString()));
            passiveInfo.Add(allPassives.ReturnValue(passiveNames[i]));
            passiveDescription.Add(ReturnPassiveDetails(passiveInfo[i]));
        }
        passiveStatTextList.SetStatsAndData(passiveNames, passiveDescription);
        passiveGroupText.SetStatText(passiveGroupName);
        passiveGroupText.SetText(passiveLevel);
    }
    
    public void ViewCustomPassives(TacticActor actor)
    {
        List<string> customPassives = actor.GetCustomPassives();
        panel.SetActive(true);
        passiveNames.Clear();
        passiveInfo.Clear();
        passiveDescription.Clear();
        passiveGroupText.SetStatText("Custom Passives");
        passiveGroupText.SetText(customPassives.Count.ToString());
        for (int i = 0; i < customPassives.Count; i++)
        {
            passiveNames.Add("Custom "+(i+1));
            passiveDescription.Add(ReturnPassiveDetails(customPassives[i]));
        }
        passiveStatTextList.SetStatsAndData(passiveNames, passiveDescription);
    }

    public List<string> ReturnAllPassiveDetails(TacticActor actor)
    {
        List<string> allDetails = new List<string>();
        // Loop through passives and levels.
        List<string> allAPassives = actor.GetPassiveSkills();
        List<string> allPassiveLevels = actor.GetPassiveLevels();
        for (int i = 0; i < allAPassives.Count; i++)
        {
            int level = int.Parse(allPassiveLevels[i]);
            if (level <= 0){continue;}
            for (int j = 1; j < level + 1; j++)
            {
                allDetails.Add(ReturnPassiveDetails(allPassives.ReturnValue(passiveNameLevels.GetMultiKeyValue(allAPassives[i], j.ToString()))));
            }
        }
        // Loop through custom passives.
        allAPassives = actor.GetCustomPassives();
        for (int i = 0; i < allAPassives.Count; i++)
        {
            allDetails.Add(ReturnPassiveDetails(allAPassives[i]));
        }
        return allDetails;
    }

    public string ReturnAuraDetails(AuraEffect aura)
    {
        string description = aura.GetAuraName() + " (" + aura.GetTeamTarget() + ") :\n";
        // There are some special things auras can do, like trigger basic attacks.
        // TODO make a switch of those special cases here.
        description += PassiveEffect(aura.effect, aura.effectSpecifics, aura.target);
        description += PassiveConditionText(aura.condition, aura.conditionSpecifics);
        return description;
    }

    public string ReturnPassiveDetailsFromName(string passiveName)
    {
        return ReturnPassiveDetails(allPassives.ReturnValue(passiveName));
    }

    public bool SpellGrantingPassive(string passiveName)
    {
        string newInfo = allPassives.ReturnValue(passiveName);
        if (!newInfo.Contains("|"))
        {
            return false;
        }
        string[] blocks = newInfo.Split("|");
        return blocks[4].Contains("Spell");
    }

    public bool SkillGrantingPassive(string passiveName)
    {
        string newInfo = allPassives.ReturnValue(passiveName);
        if (!newInfo.Contains("|"))
        {
            return false;
        }
        string[] blocks = newInfo.Split("|");
        return blocks[4].Contains("Skill");
    }

    public string ReturnPassiveDetails(string newInfo)
    {
        if (!newInfo.Contains("|"))
        {
            return "";
        }
        string[] dataBlocks = newInfo.Split("|");
        string description = "";
        description += PassiveTiming(dataBlocks[0]);
        string[] effects = dataBlocks[4].Split(",");
        string[] effectSpecifics = dataBlocks[5].Split(",");
        for (int i = 0; i < effects.Length; i++)
        {
            description += PassiveEffect(effects[i], effectSpecifics[i], dataBlocks[3]);
            if (i < effects.Length - 1)
            {
                description += " and";
            }
        }
        string[] conditions = dataBlocks[1].Split(",");
        string[] specifics = dataBlocks[2].Split(",");
        for (int i = 0; i < conditions.Length; i++)
        {
            description += PassiveConditionText(conditions[i], specifics[i]);
            if (i < conditions.Length - 1)
            {
                description += " and";
            }
            else
            {
                description += ".";
            }
        }
        return description;
    }

    protected string PassiveTiming(string data)
    {
        switch (data)
        {
            case "BS":
            return "At the start of each battle,";
            case "Start":
            return "At the start of each turn,";
            case "Moving":
            return "When moving,";
            case "Attack":
            return "When attacking,";
            case "Defend":
            return "When being attacked,";
            case "TakeDamage":
            return "When receiving attack damage,";
            case "End":
            return "At the end of each turn,";
            case "Death":
            return "Upon death,";
            case "AfterAttack":
            return "After attacking,";
            case "AfterDefend":
            return "After being attacked,";
            case "AdjustActives":
            return "When using a skill,";
            case "AdjustSpells":
            return "When casting a spell,";
            case "AfterSkill":
            return "After using a skill,";
            case "AfterSpell":
            return "After using a spell,";
        }
        return "";
    }

    protected string PassiveConditionText(string condition, string specifics)
    {
        // Consolidate A/D conditions into one.
        string conTarget = "you";
        string comparedTarget = "";
        if (condition.EndsWith("A"))
        {
            conTarget = "the attacker";
            comparedTarget = "the target";
            condition = condition[..^1];
        }
        if (condition.EndsWith("D"))
        {
            conTarget = "the target";
            comparedTarget = "the attacker";
            condition = condition[..^1];
        }
        switch (condition)
        {
            case "None":
                return "";
            case "Killing":
                return " if attack is greater than the sum of the target's health and defense";
            case "AllyCount<":
                return " if there are less than " + specifics + " allies left for " + conTarget;
            case "AllyCount>":
                return " if there are more than " + specifics + " allies left for " + conTarget;
            case "Ally<Enemy":
                return " if there are less allies than enemies for " + conTarget;
            case "Ally>Enemy":
                return " if there are more allies than enemies for " + conTarget;
            case "AllyEqualsEnemy":
                return " if there are equal allies and enemies for " + conTarget;
            case "EnemyCount<":
                return " if there are less than " + specifics + " enemies left for " + conTarget;
            case "EnemyCount>":
                return " if there are more than " + specifics + " enemies left for " + conTarget;
            case "AdjacentAllyCount>":
                return " if there are more than " + specifics + " allies adjacent for " + conTarget;
            case "AdjacentAllyCount<":
                return " if there are less than " + specifics + " allies adjacent for " + conTarget;
            case "AdjacentAlly":
                return " if another ally is adjacent to " + conTarget;
            case "AdjacentAlly<>":
                return " if another ally is not adjacent to " + conTarget;
            case "AdjacentAllySprite":
                return " if a " + specifics + " ally is adjacent to " + conTarget;
            case "AdjacentEnemyCount>":
                return " if there are more than " + specifics + " enemies adjacent to " + conTarget;
            case "AdjacentEnemyCount<":
                return " if there are less than " + specifics + " enemies adjacent to " + conTarget;
            case "Direction":
                switch (specifics)
                {
                    case "Front":
                        return " if the attacker is facing the front side of the target";
                    case "Back":
                        return " if the attacker is facing the back side of the target";
                    case "Same":
                        return " if the attacker is facing the back of the target";
                    case "Opposite":
                        return " if the attacker is facing the target";
                }
                break;
            case "Direction<>":
                switch (specifics)
                {
                    case "Front":
                        return " if the attacker is not facing the front side of the target";
                    case "Back":
                        return " if the attacker is not facing the back side of the target";
                    case "Same":
                        return " if the attacker is not facing the back of the target";
                    case "Opposite":
                        return " if the attacker is not facing the target";
                }
                break;
            case "Elevation<":
                return " if the attacker is on lower elevation";
            case "Elevation>":
                return " if the attacker is on higher elevation";
            case "ElevationEquals":
                return " if the attacker is on equal elevation";
            case "RawElevation<":
                return " if tile elevation is less than " + specifics;
            case "RawElevation>":
                return " if tile elevation is more than " + specifics;
            case "RawElevation":
                return " if tile elevation is equal to " + specifics;
            case "Distance":
                return " if within " + specifics + " tile(s)";
            case "Distance>":
                return " if more than " + specifics + " tile(s) away";
            case "Health":
                return " if " + conTarget + " health is "+specifics;
            case "Energy":
                return " if " + conTarget + " energy is "+specifics;
            case "Tile":
                return " if " + conTarget + " on a " + specifics + " tile";
            case "Tile<>":
                return " if " + conTarget + " not on a " + specifics + " tile";
            case "TileEffect":
                return " if " + conTarget + " on a " + specifics + " tile";
            case "TileEffect<>":
                return " if " + conTarget + " not on a " + specifics + " tile";
            case "Weapon":
                return " if a " + specifics + " weapon is equipped to " + conTarget;
            case "Weapon<>":
                return " if no weapon is equipped to " + conTarget;
            case "Weather":
                return " if the weather is " + specifics + "";
            case "Weather<>":
                return " if the weather is not " + specifics + "";
            case "Time":
                return " if the time of day is " + specifics + "";
            case "Time<>":
                return " if the time of day is not " + specifics + "";
            case "MoveType":
                return " if movement type of " + conTarget + " equals " + specifics;
            case "MoveType<>":
                return " if movement type of " + conTarget + " is not " + specifics;
            case "MentalState":
                return " if " + conTarget + " is " + specifics;
            case "Status":
                return " if " + conTarget + " has " + specifics + " status";
            case "StatusCount>":
                return " if " + conTarget + " has more than " + specifics + " status effects";
            case "Status<>":
                return " if " + conTarget + " does not have " + specifics + " status";
            case "Range>":
                return " if attack range is greater than " + specifics + " for " + conTarget;
            case "Range<":
                return " if attack range is less than " + specifics + " for " + conTarget;
            case "Round":
                switch (specifics)
                {
                    case "Even":
                        return " every other round";
                    case "Odd":
                        return " at the start of the first round and every other round";
                }
                return " every " + specifics + " rounds";
            case "Round>":
                return " after the first " + specifics + " rounds";
            case "Round<":
                return " the first " + specifics + " rounds";
            case "RoundEquals":
                return " on round " + specifics;
            case "Passive":
                return " if the " + specifics + " passive exists for " + conTarget;
            case "Passive<>":
                return " if the " + specifics + " passive does not exists for " + conTarget;
            case "PassiveLevels>":
                return " the total passive levels are more than " + specifics + " for " + conTarget;
            case "PassiveLevels<":
                return " the total passive levels are less than " + specifics + " for " + conTarget;
            case "Counter":
                return " if the counter is greater than " + specifics + " for " + conTarget;
            case "CounterAttack":
                return " if a counter attack is available";
            case "Team":
                if (specifics == "Same")
                {
                    return " if you are on the same team";
                }
                return " if you are not on the same team";
            case "IntDirection<>":
                return " if not attacking" + RelativeDirectionDescriptions(specifics);
            case "IntDirection":
                return " if attacking" + RelativeDirectionDescriptions(specifics);
            case "Element":
                return " if " + conTarget + "'s element is " + specifics + " element";
            case "Element<>":
                return " if " + conTarget + "'s element is not " + specifics + " element";
            case "Species":
                return " if " + conTarget + "'s species is " + specifics;
            case "Species<>":
                return " if " + conTarget + "'s species is not " + specifics;
            case "Target:":
                return " if the target is targeting the attacker";
            case "Target<>":
                return " if the target is not targeting the attacker";
            case "AverageHP>":
                return " if health is greater than the average health of the battle for " + conTarget;
            case "AverageHP<":
                return " if health is less than the average health of the battle for " + conTarget;
            case "Grappling":
                return " if " + conTarget + " is grappling";
            case "Grappled":
                return " if " + conTarget + " is grappled";
            case "BadRNG":
                return " with ~" + specifics + "% chance";
            case "GoodRNG":
                return " with ~" + specifics + "% chance";
            case "HurtBy":
                return " if " + conTarget + " was hurt by " + comparedTarget;
            case "HurtBy<>":
                return " if " + conTarget + " was not hurt by " + comparedTarget;
            case "HurtMostA":
                return " if " + conTarget + " was hurt most by " + comparedTarget;
            case "HurtLeastA":
                return " if " + conTarget + " was hurt least by " + comparedTarget;
            case "LethalAttack":
                return " if the attack defeated the target";
            case "CriticalAttack":
                return " if the attack was a critical hit";
            case "CriticalAttack<>":
                return " if the attack was not a critical hit";
            case "DodgedAttack":
                return " if the attack was dodged";
            case "DodgedAttack<>":
                return " if the attack was not dodged";
            case "FirstStrike":
                return " if " + conTarget + " has not attacked yet";
            case "Moved":
                return " if " + conTarget + " moved this round";
            case "Moved<>":
                return " if " + conTarget + " did not move this round";
            case "SkillUsed":
                return " if " + conTarget + " used a skill this round";
            case "SkillUsed<>":
                return " if " + conTarget + " did not use a skill this round";
            case "Attacked":
                return " if " + conTarget + " attacked this round";
            case "Attacked<>":
                return " if " + conTarget + " did not attack this round";
            case "Defended":
                return " if " + conTarget + " was attacked this round";
            case "Defended<>":
                return " if " + conTarget + " was not attacked this round";
            case "PrevDefended":
                return " if " + conTarget + " was attacked last round";
            case "PrevDefended<>":
                return " if " + conTarget + " was not attacked last round";
            case "PrevMoved":
                return " if " + conTarget + " moved last round";
            case "PrevMoved<>":
                return " if " + conTarget + " did not move last round";
            case "PrevSkillUsed":
                return " if " + conTarget + " used a skill last round";
            case "PrevSkillUsed<>":
                return " if " + conTarget + " did not use a skill last round";
            case "PrevAttacked":
                return " if " + conTarget + " attacked last round";
            case "PrevAttacked<>":
                return " if " + conTarget + " did not attack last round";
            case "ActionCount":
                return " if " + conTarget + " used exactly " + specifics + " actions this round";
            case "ActionCount>":
                return " if " + conTarget + " used more than " + specifics + " actions this round";
            case "ActionCount<":
                return " if " + conTarget + " used less than " + specifics + " actions this round";
            case "ActionCount%":
                return " if " + conTarget + " used a multiple of " + specifics + " actions this round";
            case "PrevActionCount":
                return " if " + conTarget + " used exactly " + specifics + " actions last round";
            case "PrevActionCount>":
                return " if " + conTarget + " used more than " + specifics + " actions last round";
            case "PrevActionCount<":
                return " if " + conTarget + " used less than " + specifics + " actions last round";
            case "PrevActionCount%":
                return " if " + conTarget + " used a multiple of " + specifics + " actions last round";
            case "TotalActionCount":
                return " if " + conTarget + " used exactly " + specifics + " actions";
            case "TotalActionCount>":
                return " if " + conTarget + " used more than " + specifics + " actions";
            case "TotalActionCount<":
                return " if " + conTarget + " used less than " + specifics + " actions";
            case "TotalActionCount%":
                return " if " + conTarget + " used a multiple of " + specifics + " actions";
            case "AttackCount":
                return " if " + conTarget + " made exactly " + specifics + " attacks this round";
            case "AttackCount>":
                return " if " + conTarget + " made more than " + specifics + " attacks this round";
            case "AttackCount<":
                return " if " + conTarget + " made less than " + specifics + " attacks this round";
            case "AttackCount%":
                return " if " + conTarget + " made a multiple of " + specifics + " attacks this round";
            case "PrevAttackCount":
                return " if " + conTarget + " made exactly " + specifics + " attacks last round";
            case "PrevAttackCount>":
                return " if " + conTarget + " made more than " + specifics + " attacks last round";
            case "PrevAttackCount<":
                return " if " + conTarget + " made less than " + specifics + " attacks last round";
            case "PrevAttackCount%":
                return " if " + conTarget + " made a multiple of " + specifics + " attacks last round";
            case "TotalAttackCount":
                return " if " + conTarget + " made exactly " + specifics + " attacks";
            case "TotalAttackCount>":
                return " if " + conTarget + " made more than " + specifics + " attacks";
            case "TotalAttackCount<":
                return " if " + conTarget + " made less than " + specifics + " attacks";
            case "TotalAttackCount%":
                return " if " + conTarget + " made a multiple of " + specifics + " attacks";
            case "SkillCount":
                return " if " + conTarget + " used exactly " + specifics + " skills this round";
            case "SkillCount>":
                return " if " + conTarget + " used more than " + specifics + " skills this round";
            case "SkillCount<":
                return " if " + conTarget + " used less than " + specifics + " skills this round";
            case "SkillCount%":
                return " if " + conTarget + " used a multiple of " + specifics + " skills this round";
            case "PrevSkillCount":
                return " if " + conTarget + " used exactly " + specifics + " skills last round";
            case "PrevSkillCount>":
                return " if " + conTarget + " used more than " + specifics + " skills last round";
            case "PrevSkillCount<":
                return " if " + conTarget + " used less than " + specifics + " skills last round";
            case "PrevSkillCount%":
                return " if " + conTarget + " used a multiple of " + specifics + " skills last round";
            case "TotalSkillCount":
                return " if " + conTarget + " used exactly " + specifics + " skills";
            case "TotalSkillCount>":
                return " if " + conTarget + " used more than " + specifics + " skills";
            case "TotalSkillCount<":
                return " if " + conTarget + " used less than " + specifics + " skills";
            case "TotalSkillCount%":
                return " if " + conTarget + " used a multiple of " + specifics + " skills";
            case "PrevMoveCount":
                return " if " + conTarget + " moved exactly " + specifics + " tiles last round";
            case "PrevMoveCount>":
                return " if " + conTarget + " moved more than " + specifics + " tiles last round";
            case "PrevMoveCount<":
                return " if " + conTarget + " moved less than " + specifics + " tiles last round";
            case "PrevMoveCount%":
                return " if " + conTarget + " moved a multiple of " + specifics + " tiles last round";
            case "DefendCount":
                return " if " + conTarget + " was attacked exactly " + specifics + " time(s) this round";
            case "DefendCount>":
                return " if " + conTarget + " was attacked more than " + specifics + " time(s) this round";
            case "DefendCount<":
                return " if " + conTarget + " was attacked less than " + specifics + " time(s) this round";
            case "DefendCount%":
                return " if " + conTarget + " was attacked a multiple of " + specifics + " time(s) this round";
            case "PrevDefendCount":
                return " if " + conTarget + " was attacked exactly " + specifics + " time(s) last round";
            case "PrevDefendCount>":
                return " if " + conTarget + " was attacked more than " + specifics + " time(s) last round";
            case "PrevDefendCount<":
                return " if " + conTarget + " was attacked less than " + specifics + " time(s) last round";
            case "PrevDefendCount%":
                return " if " + conTarget + " was attacked a multiple of " + specifics + " time(s) last round";
            case "SkillName":
                return " if it is a [" + specifics + "] type";
            case "SkillType":
                return " if it is a " + specifics + " type";
            case "SkillEffect":
                return " if it has a " + specifics + " type";
            case "FirstDefend":
                return " if attacked for the first time";
            case "Damage>":
                return " if the damage is more than";
            case "Damage<":
                return " if the damage is less than";
            // AFTERSKILL CONDITIONS
            case "ActionCost>":
                return " if the skill costs more than " + specifics + " actions";
            case "ActionCost<":
                return " if the skill costs less than " + specifics + " actions";
            case "SkillUpgraded":
                return " if the skill has been upgraded";
            case "SkillModded":
                return " if the skill has been modified";
            // STS Conditions
            case "TempSkillOwnedCount<":
                return " if " + conTarget + " has less than " + specifics + " temporary skills";
            case "TempSkillOwnedCount>":
                return " if " + conTarget + " has more than " + specifics + " temporary skills";
            case "SkillOwnedCount<":
                return " if " + conTarget + " has less than " + specifics + " skills";
            case "SkillOwnedCount>":
                return " if " + conTarget + " has more than " + specifics + " skills";
        }
        return "";
    }

    protected string AdjustSpecificsText(string specifics)
    {
        string adjustedSpecifics = specifics;
        string targetString = "you";
        string multiplier = "";
        if (specifics.Contains("ScalingEquals"))
        {
            string[] scalingBasedOn = specifics.Split("Equals");
            if (scalingBasedOn.Length > 1)
            {
                adjustedSpecifics = scalingBasedOn[1];
                if (adjustedSpecifics.EndsWith("D"))
                {
                    adjustedSpecifics = adjustedSpecifics[..^1];
                    targetString = "the target";
                }
                else if (adjustedSpecifics.EndsWith("A"))
                {
                    targetString = "the attacker";
                    adjustedSpecifics = adjustedSpecifics[..^1];
                }
            }
            if (scalingBasedOn.Length > 2)
            {
                multiplier = scalingBasedOn[2] + " times ";
            }
        }
        else
        {
            return specifics;
        }
        switch (adjustedSpecifics)
        {
            default:
                return multiplier + "your " + "[" + adjustedSpecifics + "] level";
            case "DamageTaken":
                return multiplier + "the amount of damage taken";
            case "Defense":
                return multiplier + "your defense value";
            case "Attack":
                return multiplier + "your attack value";
            case "Attack/2":
                return multiplier + "half your attack value";
            case "SkillsUsed":
                return multiplier + "how many skills " + targetString + " used";
            case "TempSkillCount":
                return multiplier + "how many temporary skills " + targetString + " has";
            case "PassiveSkillCount":
                return multiplier + "how many passives " + targetString + " has";
            case "Attacks":
                return multiplier + "how many times " + targetString + " attacked";
            case "Defends":
                return multiplier + "how many times " + targetString + " was attacked";
            case "Moves":
                return multiplier + "how many times " + targetString + " moved";
            case "RoundAttacks":
                return multiplier + "how many times " + targetString + " attacked this round";
        }
    }

    protected string AffectMapText(string effect, string specifics)
    {
        switch (effect)
        {
            case "TerrainEffect":
                return " create " + specifics;
            case "Tile":
                return " create " + specifics;
            case "Spread":
                return " spread " + specifics;
            case "ChainSpread":
                return " greatly spread " + specifics;
            case "RandomTileSwap":
                return " switch your tile for a random adjacent tile";
        }
        return "";
    }

    protected string AffectSkillText(string effect, string specifics)
    {
        switch (effect)
        {
            case "OverrideA":
                return " set the action cost to " + specifics;
            case "OverrideE":
                return " set the energy cost to " + specifics;
            case "ActionCost":
                return " change the action cost by " + specifics;
            case "ActionCost%":
                return " change the action cost by " + specifics + "%";
            case "EnergyCost":
                return " change the energy cost by " + specifics;
            case "EnergyCost%":
                return " change the energy cost by " + specifics + "%";
        }
        return "";
    }

    protected string PassiveEffect(string effect, string specifics, string target)
    {
        specifics = AdjustSpecificsText(specifics);
        if (target == "Map")
        {
            return AffectMapText(effect, specifics);
        }
        if (target == "Skill")
        {
            return AffectSkillText(effect, specifics);
        }
        if (effect.EndsWith("Damage"))
        {
            return " deal " + specifics + " " + effect + " to " + target;
        }
        switch (effect)
        {
            case "Set":
            case "Increase":
            case "Increase%":
            case "Decrease":
            case "Decrease%":
                return IncreaseDecreaseTargetSpecifics(effect, specifics, target);
            case "Passive":
                return " grant " + target + " the " + specifics + " passive";
            case "PassiveAtLevel":
                string[] passiveAtLevel = specifics.Split("Equals");
                return " grant " + target + " " + passiveAtLevel[1] + " levels of the " + passiveAtLevel[0] + " passive";
            case "BaseHealth":
                return " increase maximum health of " + target + " by " + specifics;
            case "BaseHealth%":
                return " increase maximum health of " + target + " by " + specifics + "%";
            case "MaxHealth%":
                return " change maximum health of " + target + " by " + specifics + "%";
            case "CurrentHealth%":
                return " decrease current health by " + specifics + "%";
            case "BaseEnergy%":
                return " change base energy of " + target + " by " + specifics + "%";
            case "Movement":
                return " " + target + " gain " + specifics + " movement";
            case "Skill":
                return " gain the " + specifics + " skill";
            case "TemporarySkill":
                return " gain the " + specifics + " skill which can be used once";
            case "SingleTemporarySkill":
                return " gain the " + specifics + " skill which can only be used once";
            case "Spell":
                return " gain the " + specifics + " spell";
            case "TemporarySpell":
                return " gain the " + specifics + " spell once";
            case "Status":
                return " inflict " + specifics+" on " + target;
            case "Buff":
                return " give " + specifics+" to "+target;
            case "RemoveStatus":
                return " remove all " + specifics + " status effects from " + target;
            case "Health":
                return " " + target + " regain health up to " + specifics;
            case "Health%":
                return " " + target + " regain up to " + specifics + "% health";
            case "Attack%":
                return " increase attack of " + target + " by " + specifics + "%";
            case "BaseAttack%":
                return " increase base attack of " + target + " by " + specifics + "%";
            case "Defense%":
                return " increase defense of " + target + " by " + specifics + "%";
            case "MoveType":
                return " change movement type of " + target + " to " + specifics;
            case "AttackRange":
                return " increase Attack Range of " + target + " by up to " + specifics;
            case "TempRange":
                return " increase Attack Range of " + target + " by " + specifics;
            case "SetSpeed":
                return " set Base Speed of " + target + " to " + specifics;
            case "BaseSpeed":
                return " increase Base Speed of " + target + " by up to " + specifics;
            case "TempAttack%":
                return " change attack of " + target + " by " + specifics + "%, until the end of next turn";
            case "TempAttack":
                return " change attack of " + target + " by " + specifics + ", until the end of next turn";
            case "TempDefense%":
                return " change defense of " + target + " by " + specifics + "%, until the end of next turn";
            case "TempDefense":
                return " change defense of " + target + " by " + specifics + ", until the end of next turn";
            case "TempHealth%":
                return " " + target + " gain a shield that absorbs damage equal to " + specifics + "% of max health, until the end of next turn";
            case "TempHealth":
                return " " + target + " gain a shield that absorbs damage equal to " + specifics + ", until the end of next turn";
            case "MentalState":
                return " change mental state to " + specifics;
            case "Amnesia":
                return " make " + target + " forget 1 active skill";
            case "Active":
                return " use " + specifics;
            case "Death":
                return " die";
            case "CounterAttack":
                return " gain " + specifics + " counter attacks";
            case "DisableDeathActives":
                return " disable death effects";
            case "ReleaseGrapple":
                return " release the grappled target";
            case "BreakGrapple":
                return " break from any grapples";
            case "BaseDamageResistance":
                string[] bDRes = specifics.Split("Equals");
                return " increase base " + bDRes[0] + " resistance by " + bDRes[1];
            case "CurrentDamageResistance":
                string[] cDRes = specifics.Split("Equals");
                return " increase " + cDRes[0] + " resistance by " + cDRes[1];
            case "BaseElementalBonus":
                string[] bDBonus = specifics.Split("Equals");
                return " increase base " + bDBonus[0] + " damage by " + bDBonus[1];
            case "ElementalDamageBonus":
                string[] cDBonus = specifics.Split("Equals");
                return " increase " + cDBonus[0] + " damage by " + cDBonus[1];
            case "ScalingElementalBonus":
                string[] sEB = specifics.Split("Equals");
                return " increase base " + sEB[0] + " damage by " + sEB[3] + "% for each level of this passive";
            case "ScalingElementalResist":
                string[] sER = specifics.Split("Equals");
                return " increase base " + sER[0] + " resistance by " + sER[3] + "% for each level of this passive";
            case "Sleep":
                return "put " + target + " to sleep for " + specifics + " turns";
            case "Silence":
                return " disable actives of " + target + " for " + specifics + " turns";
            case "Invisible":
                return " turn invisible for " + specifics + " turns";
            case "Barricade":
                return " prevent temporary health from decaying for " + specifics + " turns";
            case "Guard":
                return " protect adjacent allies from attacks for " + specifics + " turns";
            case "GuardRange":
                return " increase the distance from which you can protected allies from attacks to up to " + specifics + " tiles.";
            case "MoveForwardRandom":
                return " move to a random forward tile";
            case "MoveBackwardRandom":
                return " move to a random backward tile";
            case "SetAttackActionCost":
                return " set the action cost of basic attacks to " + specifics;
        }
        return " increase " + effect + " of " + target + " by " + specifics;
    }

    protected string IncreaseDecreaseTargetSpecifics(string effect, string specifics, string target)
    {
        if (effect == "Set")
        {
            return " " + effect + " " + target + " to " + specifics;
        }
        switch (target)
        {
            case "MoveCost":
            switch (effect)
            {
                case "Increase":
                return " spend " + specifics + " more movement";
                case "Decrease":
                return " spend " + specifics + " less movement";
            }
            return "";
            case "AttackValue%":
            switch (effect)
            {
                case "Increase":
                return " increase attack damage by " + specifics + "%";
                case "Decrease":
                return " decrease attack damage by " + specifics + "%";
            }
            return " " + effect + " attack damage by " + specifics;
            case "Damage%":
            return " " + effect + " damage multipler by " + specifics + "%";
            case "DefenseValue%":
            switch (effect)
            {
                case "Increase":
                return " increase defense by " + specifics + "%";
                case "Decrease":
                return " ignore " + specifics + "% of defense";
            }
            return " " + effect + " defense value by " + specifics;
        }
        return " " + effect + " " + target + " by " + specifics;
    }

    protected string RelativeDirectionDescriptions(string specifics)
    {
        switch (specifics)
        {
            case "0":
                return " from the front";
            case "1":
                return " from the front right";
            case "2":
                return " from the back right";
            case "3":
                return " from the back";
            case "4":
                return " from the back left";
            case "5":
                return " from the front left";
        }
        return "";
    }
}
