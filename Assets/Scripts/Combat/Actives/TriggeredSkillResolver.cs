using System.Collections.Generic;
using UnityEngine;

public class TriggeredSkillResolver : MonoBehaviour
{
    public string triggerSkillDelimiter = "::triggerSkillDelimiter::";
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
        string[] triggerDetails = triggerData.Split(new string[] { triggerSkillDelimiter }, System.StringSplitOptions.None);
        if (triggerDetails.Length < 2){return false;}
        string skillName = triggerDetails[0];
        string targetMode = triggerDetails[1];
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
        DebugResolvedCast("Self", resolvedCast, battle);
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
        DebugResolvedCast("RandomEnemy", resolvedCast, battle);
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
