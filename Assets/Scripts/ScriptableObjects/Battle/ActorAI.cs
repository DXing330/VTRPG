using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ActorAI", menuName = "ScriptableObjects/BattleLogic/ActorAI", order = 1)]
public class ActorAI : ScriptableObject
{
    public GeneralUtility utility;
    public AIConditionChecker conditionChecker;
    public List<string> whiteListSupportEffects;
    public bool EffectWhiteListed(string effect)
    {
        for (int i = 0; i < whiteListSupportEffects.Count; i++)
        {
            if (effect.Contains(whiteListSupportEffects[i]))
            {
                return true;
            }
        }
        return false;
    }
    public string AIType;
    public string activeSkillName;
    public string ReturnAIActiveSkill() { return activeSkillName; }
    public ActiveSkill active;
    public StatDatabase activeData;
    public StatDatabase actorAttackSkills;
    public StatDatabase actorSkillRotation;
    public StatDatabase spriteToBossRotation;
    public StatDatabase bossSkillRotation;

    public List<string> ReturnBossActions(TacticActor actor, BattleMap map)
    {
        List<string> actionsSpecifics = new List<string>();
        // Get the full rotation.
        string bossRotation = spriteToBossRotation.ReturnValue(actor.GetSpriteName());
        string[] rotationBlocks = bossSkillRotation.ReturnValue(bossRotation).Split("#");
        // Go through and determine which part of the rotation to use.
        for (int i = 0; i < rotationBlocks.Length; i++)
        {
            string[] rotationDetails = rotationBlocks[i].Split("|");
            if (conditionChecker.CheckConditions(rotationDetails[0], rotationDetails[1], actor, map))
            {
                actionsSpecifics.Add(rotationDetails[2]);
                actionsSpecifics.Add(rotationDetails[3]);
                return actionsSpecifics;
            }
        }
        actionsSpecifics.Add("Basic");
        actionsSpecifics.Add("None");
        return actionsSpecifics;
    }

    public bool BossTurn(TacticActor actor)
    {
        return actorSkillRotation.ReturnValue(actor.GetSpriteName()) == "Boss";
    }

    public bool NormalTurn(TacticActor actor, int roundIndex, BattleMap map = null, MoveCostManager moveManager = null)
    {
        string fullSkillRotation = actorSkillRotation.ReturnValue(actor.GetSpriteName());
        if (fullSkillRotation == "" || fullSkillRotation == "-1") { return true; }
        string[] skillRotation = fullSkillRotation.Split("|");
        string skillIndexString = skillRotation[(roundIndex - 1) % (skillRotation.Length)];
        // R for Random.
        int activeSkillIndex = -1;
        if (skillIndexString.Contains("R") && actor.GetActiveSkills().Count > 0)
        {
            activeSkillIndex = Random.Range(0, actor.GetActiveSkills().Count);
        }
        else
        {
            activeSkillIndex = int.Parse(skillIndexString);
        }
        if (activeSkillIndex < 0 || activeSkillIndex >= actor.GetActiveSkills().Count) { return true; }
        activeSkillName = actor.GetActiveSkill(activeSkillIndex);
        active.LoadSkillFromString(activeData.ReturnValue(activeSkillName), actor);
        return false;
    }

    public string ReturnAIAttackSkill(TacticActor actor)
    {
        string attackSkill = actorAttackSkills.ReturnValue(actor.GetSpriteName());
        if (SkillWouldHealTarget(actor, actor.GetTarget(), attackSkill)) { return ""; }
        return attackSkill;
    }

    public string ReturnSkillWithEffect(TacticActor actor, BattleMap map, string skillEffect)
    {
        return conditionChecker.GetAvailableSkillWithEffect(actor, map, skillEffect);
    }

    public bool SkillWouldHealTarget(TacticActor actor, TacticActor target, string skillName)
    {
        string skillData = activeData.ReturnValue(skillName);
        string[] skillFields = skillData.Split(active.activeSkillDelimiter);
        if (actor == null || target == null || skillName == "" || skillData == "" || skillFields.Length <= 9) { return false; }

        string[] effects = skillFields[8].Split(active.effectDelimiter);
        string[] specifics = skillFields[9].Split(active.effectDelimiter);
        for (int i = 0; i < effects.Length; i++)
        {
            if ((effects[i] == "ElementalAttack" || effects[i] == "ElementalDamage") && target.ReturnDamageResistanceOfType(i < specifics.Length ? specifics[i] : "") > 100) { return true; }
        }
        return false;
    }

    public string ReturnSpellWithEffect(TacticActor actor, BattleMap map, string spellEffect)
    {
        return conditionChecker.GetAvailableSpellWithEffect(actor, map, spellEffect);
    }

    public List<int> FindPathAwayFromTarget(TacticActor currentActor, BattleMap map, MoveCostManager moveManager)
    {
        int originalLocation = currentActor.GetLocation();
        moveManager.GetAllMoveCosts(currentActor, map.battlingActors);
        List<int> path = new List<int>();
        if (currentActor.GetTarget() == null) { return path; }
        // Find the direction to the target.
        int directionToTarget = moveManager.DirectionBetweenActors(currentActor, currentActor.GetTarget());
        // Move in the opposite direction.
        int finalDirection = (directionToTarget + 3) % 6;
        List<int> fullPath = moveManager.TilesInDirection(originalLocation, finalDirection);
        int pathCost = 0;
        for (int i = 0; i < fullPath.Count; i++)
        {
            path.Insert(0, fullPath[i]);
            pathCost += moveManager.MoveCostOfTile(fullPath[i]);
            if (pathCost > currentActor.GetMoveRange())
            {
                path.RemoveAt(0);
                pathCost -= moveManager.MoveCostOfTile(fullPath[i]);
                break;
            }
        }
        return path;
    }

    // This doesn't path to a actor, it paths to a tile.
    public List<int> FindPathToTile(TacticActor actor, BattleMap map, MoveCostManager moveManager, int tile)
    {
        int originalLocation = actor.GetLocation();
        moveManager.GetAllMoveCosts(actor, map.battlingActors);
        List<int> fullPath = moveManager.GetPrecomputedPath(originalLocation, tile);
        List<int> path = new List<int>();
        int pathCost = 0;
        for (int i = fullPath.Count - 1; i >= 0; i--)
        {
            path.Insert(0, fullPath[i]);
            pathCost += moveManager.MoveCostOfTile(fullPath[i]);
            if (pathCost > actor.GetMoveRange())
            {
                path.RemoveAt(0);
                pathCost -= moveManager.MoveCostOfTile(fullPath[i]);
                break;
            }
        }
        return path;
    }

    public List<int> FindPathToTarget(TacticActor currentActor, BattleMap map, MoveCostManager moveManager)
    {
        int originalLocation = currentActor.GetLocation();
        moveManager.GetAllMoveCosts(currentActor, map.battlingActors);
        if (currentActor.GetTarget() == null || currentActor.GetTarget().GetHealth() <= 0)
        {
            currentActor.SetTarget(GetClosestEnemy(map.battlingActors, currentActor, moveManager));
        }
        if (currentActor.GetTarget() == null)
        {
            return new List<int>();
        }
        // Should not path to the target, instead path to the closest tile adjacent to the target.
        int target = currentActor.GetTarget().GetLocation();
        if (currentActor.GetAttackRange() <= 1)
        {
            target = map.ReturnClosestTileWithinElevationDifference(currentActor.GetLocation(), target, currentActor.GetWeaponReach(), moveManager.pathCosts);
        }
        else
        {
            // Path costs already calculated from [moveManager.GetAllMoveCosts(currentActor, map.battlingActors);] above
            target = map.ReturnClosestTileWithLineOfSight(currentActor.GetLocation(), target, currentActor.GetAttackRange(), moveManager.pathCosts);      
        }
        List<int> path = new List<int>();
        int pathCost = 0;
        if (EnemyInAttackRange(currentActor, currentActor.GetTarget(), map)) { return path; }
        List<int> fullPath = moveManager.GetPrecomputedPath(originalLocation, target, true);
        for (int i = fullPath.Count - 1; i >= 0; i--)
        {
            path.Insert(0, fullPath[i]);
            pathCost += moveManager.MoveCostOfTile(fullPath[i]);
            if (pathCost > currentActor.GetMoveRange())
            {
                path.RemoveAt(0);
                pathCost -= moveManager.MoveCostOfTile(fullPath[i]);
                break;
            }
            // Stop if you can attack the enemy from the tile.
            if (EnemyAttackableFromTile(currentActor, fullPath[i], map))
            {
                break;
            }
        }
        return path;
    }

    public TacticActor GetClosestEnemy(List<TacticActor> battlingActors, TacticActor currentActor, MoveCostManager moveManager, bool rage = false)
    {
        List<TacticActor> enemies = new List<TacticActor>();
        if (!rage)
        {
            for (int i = 0; i < battlingActors.Count; i++)
            {
                // Enemies is everyone on a different team? But maybe some teams can be allied in some fights?
                if (battlingActors[i].GetTeam() != currentActor.GetTeam() && !battlingActors[i].invisible)
                {
                    enemies.Add(battlingActors[i]);
                }
            }
        }
        else
        {
            for (int i = 0; i < battlingActors.Count; i++)
            {
                // Ignore invisible enemies when selecting targets.
                if (battlingActors[i] != currentActor && !battlingActors[i].invisible)
                {
                    enemies.Add(battlingActors[i]);
                }
            }
        }
        // If there is only one enemy then thats the target.
        if (enemies.Count == 1) { return enemies[0]; }
        if (enemies.Count <= 0) { return null; }
        int distance = 9999;
        List<int> possibleIndices = new List<int>();
        for (int i = 0; i < enemies.Count; i++)
        {
            moveManager.GetAllMoveCosts(currentActor, battlingActors);
            List<int> path = moveManager.GetPrecomputedPath(currentActor.GetLocation(), enemies[i].GetLocation(), true);
            // Unable to find a path means skip.
            if (path.Count <= 0){continue;}
            if (moveManager.moveCost < distance)
            {
                distance = moveManager.moveCost;
                possibleIndices.Clear();
                possibleIndices.Add(i);
            }
            else if (moveManager.moveCost == distance)
            {
                possibleIndices.Add(i);
            }
        }
        if (possibleIndices.Count <= 0) { return null; }
        return enemies[possibleIndices[Random.Range(0, possibleIndices.Count)]];
    }

    public bool EnemyInAttackableRange(TacticActor currentActor, TacticActor target, BattleMap map, MoveCostManager moveManager)
    {
        if (target == null) { return false; }
        if (target.GetHealth() <= 0) { return false; }
        return moveManager.TileInAttackableRange(currentActor, map, target.GetLocation());
    }

    public bool EnemyAttackableFromTile(TacticActor actor, int tile, BattleMap map)
    {
        if (!actor.TargetValid()){return false;}
        List<int> attackableTiles = map.GetAttackableTiles(actor, false, tile);
        return attackableTiles.Contains(actor.GetTarget().GetLocation());
    }

    public bool EnemyInAttackRange(TacticActor currentActor, TacticActor target, BattleMap map)
    {
        if (target == null) { return false; }
        if (target.GetHealth() <= 0) { return false; }
        return map.TileInAttackRange(currentActor, target.GetLocation());
    }
    public int ChooseSkillTargetLocation(TacticActor currentActor, BattleMap map, MoveCostManager moveManager)
    {
        if (active.GetRange(currentActor) == 0)
        {
            return currentActor.GetLocation();
        }
        List<int> targetableTiles = moveManager.actorPathfinder.FindTilesInRange(currentActor.GetLocation(), active.GetRange(currentActor));
        if (targetableTiles.Count <= 0){return -1;}
        if (targetableTiles.Count == 1){return targetableTiles[0];}
        // TODO this is more complicated since move is often displacement as well.
        if (active.GetSkillType() == "Move")
        {
            targetableTiles = map.ReturnEmptyTiles(targetableTiles);
            if (targetableTiles.Count <= 0)
            {
                return -1;
            }
            return targetableTiles[Random.Range(0, targetableTiles.Count)];
        }
        // Beam skills should be fired in the direction of your target.
        if (active.GetShape() == "Beam")
        {
            // Find the direction between yourself and the target.
            int direction = -1;
            if (currentActor.GetTarget() != null && currentActor.GetTarget().GetHealth() > 0)
            {
                direction = moveManager.DirectionBetweenActors(currentActor, currentActor.GetTarget());
            }
            // Else find the direction between you and a random enemy.
            else
            {
                direction = moveManager.DirectionBetweenActors(currentActor, map.GetClosestEnemy(currentActor));
            }
            return map.mapUtility.PointInDirection(currentActor.GetLocation(), direction, map.mapSize);
        }
        if (active.GetEffect().Contains("Summon"))
        {
            // Look for an empty tile in range.
            targetableTiles = map.ReturnEmptyTiles(targetableTiles);
            if (targetableTiles.Count <= 0)
            {
                return -1;
            }
            return targetableTiles[Random.Range(0, targetableTiles.Count)];
        }
        else if (active.GetEffect().Contains("Attack"))
        {
            // Try to pick your target's tile.
            if (currentActor.GetTarget() != null && targetableTiles.Contains(currentActor.GetTarget().GetLocation()))
            {
                return currentActor.GetTarget().GetLocation();
            }
            // Else pick a random enemy.
            else
            {
                return map.GetRandomEnemyLocation(currentActor, targetableTiles);
            }
        }
        return -1;
    }
    public bool ValidSkillTargets(TacticActor currentActor, BattleMap map, ActiveManager activeManager, bool spell = false)
    {
        // Determine the type of skill being used.
        string skillType = activeManager.active.GetSkillType();
        int range = activeManager.active.GetRange(currentActor, map);
        if (spell)
        {
            skillType = activeManager.magicSpell.GetSkillType();
            range = activeManager.magicSpell.GetRange(currentActor, map);
        }
        switch (skillType)
        {
            // If the skill has no type then it's a problem, just do a normal action.
            case "":
                return false;
            case "Damage":
                // For attacking skills, make sure at least 1 enemy is in range.
                return map.EnemiesInTiles(currentActor, activeManager.targetedTiles);
            case "Support":
                // If range is 0 then it's meant to only target itself, return true.
                if (range <= 0){return true;}
                // For supporting skills, make sure at least 1 ally is in range.
                // Unless it's a summon skill then just let it through.
                // Should make an allow list of support skills that always go through.
                string effect = activeManager.active.GetEffect();
                if (spell)
                {
                    effect = activeManager.magicSpell.GetEffect();
                }
                if (EffectWhiteListed(effect))
                {
                    return true;
                }
                return map.AlliesInTiles(currentActor, activeManager.targetedTiles);
        }
        return true;
    }
    public int ChooseSpellTargetLocation(TacticActor actor, BattleMap map, MoveCostManager moveManager, MagicSpell spell)
    {
        if (spell.GetRange(actor) == 0)
        {
            return actor.GetLocation();
        }
        List<int> targetableTiles = moveManager.actorPathfinder.FindTilesInRange(actor.GetLocation(), spell.GetRange(actor));
        if (targetableTiles.Count <= 0) { return -1; }
        // Mass summoning should fall under here.
        if (targetableTiles.Count == 1) { return targetableTiles[0]; }
        // Summoning will try to look for any empty tile.
        if (spell.GetEffect().Contains("Summon"))
        {
            // Look for an empty tile in range.
            targetableTiles = map.ReturnEmptyTiles(targetableTiles);
            if (targetableTiles.Count <= 0)
            {
                return -1;
            }
            return targetableTiles[Random.Range(0, targetableTiles.Count)];
        }
        string type = spell.GetSkillType();
        switch (type)
        {
            case "Damage":
                if (actor.GetTarget() != null && targetableTiles.Contains(actor.GetTarget().GetLocation()))
                {
                    return actor.GetTarget().GetLocation();
                }
                // Else pick a random enemy.
                else
                {
                    return map.GetRandomEnemyLocation(actor, targetableTiles);
                }
            case "Support":
                // Else pick a random ally.
                // TODO improve this.
                return map.GetRandomAllyLocation(actor, targetableTiles);
        }
        return -1;
    }
}
