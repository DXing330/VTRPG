using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TriggeredSkillResolver : MonoBehaviour
{
    public string triggerSkillDelimiter = "::trig::";
    public string legacyTriggerSkillDelimiter = "::triggerSkillDelimiter::";
    public string triggerSelectorDelimiter = "::sel::";
    public bool debugTriggeredSkillTargets = true;
    protected List<string> debugMessages = new List<string>();

    public void ClearDebugMessages()
    {
        debugMessages.Clear();
    }

    public List<string> GetDebugMessages()
    {
        return new List<string>(debugMessages);
    }

    public void AddDebugMessage(string message)
    {
        if (!debugTriggeredSkillTargets){return;}
        debugMessages.Add(message);
    }

    public class TriggeredSkillCast
    {
        public string skillName;
        public int selectedTile;
        public List<int> targetedTiles;

        public TriggeredSkillCast(string newSkillName, int newSelectedTile, List<int> newTargetedTiles)
        {
            skillName = newSkillName;
            selectedTile = newSelectedTile;
            targetedTiles = newTargetedTiles;
        }
    }

    public bool TryResolve(string triggerData, TacticActor caster, ActiveManager activeManager, BattleManager battle, out TriggeredSkillCast resolvedCast)
    {
        resolvedCast = null;
        if (string.IsNullOrEmpty(triggerData) || caster == null || activeManager == null || battle == null){return false;}
        string[] triggerDetails = SplitTriggerData(triggerData);
        if (triggerDetails.Length < 2){return false;}
        string selectorData = triggerDetails[0];
        string targetMode = triggerDetails[1];
        List<string> candidateSkills = ResolveSkillCandidates(selectorData, caster, activeManager);
        AddDebugMessage("TriggeredSkill Selector"
            + " | Selector=" + selectorData
            + " | TargetMode=" + targetMode
            + " | Candidates=" + string.Join(",", candidateSkills.ToArray()));
        List<TriggeredSkillCast> legalCasts = new List<TriggeredSkillCast>();
        for (int i = 0; i < candidateSkills.Count; i++)
        {
            TriggeredSkillCast candidateCast;
            if (TryResolveCandidate(candidateSkills[i], targetMode, caster, activeManager, battle, out candidateCast))
            {
                legalCasts.Add(candidateCast);
            }
        }
        AddDebugMessage("TriggeredSkill Selector"
            + " | Selector=" + selectorData
            + " | Valid=" + string.Join(",", CastNames(legalCasts).ToArray()));
        if (legalCasts.Count <= 0){return false;}
        resolvedCast = legalCasts[Random.Range(0, legalCasts.Count)];
        activeManager.SetSkillFromName(resolvedCast.skillName, caster);
        DebugResolvedCast(targetMode, resolvedCast, battle);
        return true;
    }

    protected string[] SplitTriggerData(string triggerData)
    {
        string[] triggerDetails = triggerData.Split(new string[] { triggerSkillDelimiter }, System.StringSplitOptions.None);
        if (triggerDetails.Length >= 2){return triggerDetails;}
        return triggerData.Split(new string[] { legacyTriggerSkillDelimiter }, System.StringSplitOptions.None);
    }

    protected List<string> ResolveSkillCandidates(string selectorData, TacticActor caster, ActiveManager activeManager)
    {
        List<string> candidates = new List<string>();
        string currentSkill = activeManager.active.GetSkillName();
        string selectorType = selectorData;
        string selectorSpecifics = "";
        string[] selectorDetails = selectorData.Split(new string[] { triggerSelectorDelimiter }, System.StringSplitOptions.None);
        if (selectorDetails.Length >= 2)
        {
            selectorType = selectorDetails[0];
            selectorSpecifics = selectorDetails[1];
        }
        else if (activeManager.SkillExists(selectorData))
        {
            selectorType = "Fixed";
            selectorSpecifics = selectorData;
        }

        switch (selectorType)
        {
            case "Fixed":
                AddCandidate(candidates, selectorSpecifics, activeManager);
                break;
            case "LastUsed":
                AddCandidate(candidates, ReturnMostRecentSkill(caster, currentSkill), activeManager);
                break;
            case "MostUsed":
                AddCandidate(candidates, ReturnMostUsedSkill(caster, currentSkill), activeManager);
                break;
            case "RandomKnown":
                AddKnownCandidates(candidates, caster, activeManager, currentSkill);
                break;
            case "RandomType":
                AddKnownCandidatesByType(candidates, caster, activeManager, selectorSpecifics, currentSkill);
                break;
            default:
                AddCandidate(candidates, selectorData, activeManager);
                break;
        }
        return candidates;
    }

    protected string ReturnMostRecentSkill(TacticActor caster, string excludedSkill)
    {
        if (caster.skillsUsed == null || caster.skillsUsed.Count <= 0){return "";}
        for (int i = caster.skillsUsed.Count - 1; i >= 0; i--)
        {
            if (caster.skillsUsed[i] == excludedSkill){continue;}
            return caster.skillsUsed[i];
        }
        return "";
    }

    protected string ReturnMostUsedSkill(TacticActor caster, string excludedSkill)
    {
        if (caster.skillsUsed == null || caster.skillsUsed.Count <= 0){return "";}
        List<string> countedSkills = new List<string>();
        for (int i = 0; i < caster.skillsUsed.Count; i++)
        {
            if (caster.skillsUsed[i] == excludedSkill){continue;}
            countedSkills.Add(caster.skillsUsed[i]);
        }
        if (countedSkills.Count <= 0){return "";}
        return countedSkills.GroupBy(s => s).OrderByDescending(g => g.Count()).First().Key;
    }

    protected void AddCandidate(List<string> candidates, string skillName, ActiveManager activeManager)
    {
        if (string.IsNullOrEmpty(skillName)){return;}
        if (!activeManager.SkillExists(skillName)){return;}
        if (candidates.Contains(skillName)){return;}
        candidates.Add(skillName);
    }

    protected void AddKnownCandidates(List<string> candidates, TacticActor caster, ActiveManager activeManager, string excludedSkill)
    {
        List<string> knownSkills = caster.GetActiveSkills();
        for (int i = 0; i < knownSkills.Count; i++)
        {
            if (knownSkills[i] == excludedSkill){continue;}
            AddCandidate(candidates, knownSkills[i], activeManager);
        }
    }

    protected void AddKnownCandidatesByType(List<string> candidates, TacticActor caster, ActiveManager activeManager, string skillType, string excludedSkill)
    {
        List<string> knownSkills = caster.GetActiveSkills();
        for (int i = 0; i < knownSkills.Count; i++)
        {
            if (knownSkills[i] == excludedSkill){continue;}
            if (!activeManager.SkillExists(knownSkills[i])){continue;}
            activeManager.SetSkillFromName(knownSkills[i], caster);
            if (activeManager.active.GetSkillType() != skillType){continue;}
            AddCandidate(candidates, knownSkills[i], activeManager);
        }
    }

    protected List<string> CastNames(List<TriggeredSkillCast> casts)
    {
        List<string> names = new List<string>();
        for (int i = 0; i < casts.Count; i++)
        {
            names.Add(casts[i].skillName);
        }
        return names;
    }

    protected bool TryResolveCandidate(string skillName, string targetMode, TacticActor caster, ActiveManager activeManager, BattleManager battle, out TriggeredSkillCast resolvedCast)
    {
        resolvedCast = null;
        if (!activeManager.SkillExists(skillName)){return false;}
        activeManager.SetSkillFromName(skillName, caster);
        if (!activeManager.CheckTriggeredSkillCost(battle.map)){return false;}

        switch (targetMode)
        {
            case "Self":
            return TryResolveSelf(skillName, caster, activeManager, battle, out resolvedCast);
            case "RandomEnemy":
            return TryResolveRandomEnemy(skillName, caster, activeManager, battle, out resolvedCast);
        }
        return false;
    }

    protected bool TryResolveSelf(string skillName, TacticActor caster, ActiveManager activeManager, BattleManager battle, out TriggeredSkillCast resolvedCast)
    {
        int selectedTile = caster.GetLocation();
        List<int> targetedTiles = activeManager.GetTargetedTiles(selectedTile, battle.moveManager.actorPathfinder);
        if (!targetedTiles.Contains(selectedTile))
        {
            targetedTiles.Add(selectedTile);
        }
        resolvedCast = new TriggeredSkillCast(skillName, selectedTile, new List<int>(targetedTiles));
        return true;
    }

    protected bool TryResolveRandomEnemy(string skillName, TacticActor caster, ActiveManager activeManager, BattleManager battle, out TriggeredSkillCast resolvedCast)
    {
        resolvedCast = null;
        List<TriggeredSkillCast> legalCasts = new List<TriggeredSkillCast>();
        List<int> targetableTiles = activeManager.GetTargetableTiles(caster.GetLocation(), battle.moveManager.actorPathfinder);
        List<TacticActor> enemies = battle.map.AllEnemies(caster);
        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] == null || enemies[i].GetHealth() <= 0){continue;}
            int enemyTile = enemies[i].GetLocation();
            if (!targetableTiles.Contains(enemyTile)){continue;}
            List<int> targetedTiles = activeManager.GetTargetedTiles(enemyTile, battle.moveManager.actorPathfinder);
            List<TacticActor> targetedActors = battle.map.GetActorsOnTiles(targetedTiles);
            if (!targetedActors.Contains(enemies[i])){continue;}
            legalCasts.Add(new TriggeredSkillCast(skillName, enemyTile, new List<int>(targetedTiles)));
        }
        if (legalCasts.Count <= 0){return false;}
        resolvedCast = legalCasts[Random.Range(0, legalCasts.Count)];
        return true;
    }

    protected void DebugResolvedCast(string targetMode, TriggeredSkillCast resolvedCast, BattleManager battle)
    {
        if (!debugTriggeredSkillTargets){return;}
        List<TacticActor> targetedActors = battle.map.GetActorsOnTiles(resolvedCast.targetedTiles);
        List<string> actorDetails = new List<string>();
        for (int i = 0; i < targetedActors.Count; i++)
        {
            actorDetails.Add(targetedActors[i].GetPersonalName() + "@" + targetedActors[i].GetLocation());
        }
        string message = "TriggeredSkillResolver"
            + " | Mode=" + targetMode
            + " | Skill=" + resolvedCast.skillName
            + " | SelectedTile=" + resolvedCast.selectedTile
            + " | TargetedTiles=" + string.Join(",", resolvedCast.targetedTiles.ToArray())
            + " | TargetedActors=" + string.Join(",", actorDetails.ToArray());
        debugMessages.Add(message);
    }
}
