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
    public PassiveDetailViewerSwitch detailViewerData;
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
    // For a list of passive groups and levels, returns passive data strings.
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
    // For a list of passive groups and levels, returns passive description strings.
    // Return passive description strings, for all passives of a passive group, up to a specific level.
    public List<string> ReturnSpecificPassiveUpToLevelEffects(string group, int level)
    {
        List<string> specificsUpToLevel = new List<string>();
        for (int i = 1; i < level + 1; i++)
        {
            specificsUpToLevel.Add(ReturnSpecificPassiveLevelEffect(group, i));
        }
        return specificsUpToLevel;
    }
    // For a specific level of a passive group.
    public string ReturnSpecificPassiveLevelEffect(string group, int level)
    {
        string passiveName = passiveNameLevels.GetMultiKeyValue(group, (level).ToString());
        return ReturnPassiveDetails(allPassives.ReturnValue(passiveName));
    }
    // For a passive group name, returning all sub names and effects.
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
        description += detailViewerData.PassiveEffect(aura.effect, aura.effectSpecifics, aura.target);
        description += detailViewerData.PassiveConditionText(aura.condition, aura.conditionSpecifics);
        return description;
    }
    // For the actual name of the passive, ie a single level (name) of a passive group.
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
    // For the actual data of the passive, ie a single level of a passive group.
    public string ReturnPassiveDetails(string newInfo)
    {
        return detailViewerData.ReturnPassiveDetails(newInfo);
    }
}
