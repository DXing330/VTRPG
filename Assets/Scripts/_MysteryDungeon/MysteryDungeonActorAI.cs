using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MysteryDungeonActorAI", menuName = "ScriptableObjects/MysteryDungeon/MysteryDungeonActorAI", order = 1)]
public class MysteryDungeonActorAI : ActorAI
{
    protected void FollowTheLeader(TacticActor actor, MysteryDungeonBattleMap map)
    {
        int targetDistance = 999;
        int targetMoveCost = 999;
        int currentLocation = actor.GetLocation();
        int leaderLocation = map.GetPlayerLocation();
        if (map.mapUtility.DistanceBetweenTiles(leaderLocation, currentLocation, map.mapSize) <= 1){return;}
        // Path To The Closest Tile You Can Reach.
        List<int> bestPath = null;
        List<int> reachableTiles = map.moveManager.GetAllReachableTiles(actor, map.battlingActors, true);
        for (int i = 0; i < reachableTiles.Count; i++)
        {
            List<int> path = map.moveManager.GetPrecomputedPath(currentLocation, reachableTiles[i]);
            int distance = map.mapUtility.DistanceBetweenTiles(leaderLocation, reachableTiles[i], map.mapSize);
            int moveCost = map.moveManager.moveCost;
            if (distance < targetDistance || (distance == targetDistance && moveCost < targetMoveCost))
            {
                targetDistance = distance;
                targetMoveCost = moveCost;
                bestPath = path;
            }
        }
        if (bestPath == null){return;}
        MoveAlongPath(actor, map, bestPath);
    }
    protected bool TileAvailableAndPassable(TacticActor actor, MysteryDungeonBattleMap map, MoveCostManager moveManager, int tileNumber)
    {
        if (tileNumber < 0){return false;}
        bool passable = moveManager.MoveCostOfTile(tileNumber) <= (actor.GetSpeed() * actor.GetActions());
        bool available = map.GetActorOnTile(tileNumber) == null;
        return (passable && available);
    }
    protected List<int> GetAvailableAdjacentTileMinimizingTurns(TacticActor actor, MysteryDungeonBattleMap map, MoveCostManager moveManager)
    {
        int currentDirection = actor.GetDirection();
        int lowestTurningAmount = 6;
        List<int> availableTiles = new List<int>();
        for (int i = 0; i < 6; i++)
        {
            int difference = Mathf.Abs(currentDirection - i);
            difference = Mathf.Min(difference, 6 - difference);
            int nextTile = map.mapUtility.PointInDirection(actor.GetLocation(), i, map.mapSize);
            bool available = TileAvailableAndPassable(actor, map, moveManager, nextTile);
            if (available && difference < lowestTurningAmount)
            {
                availableTiles.Clear();
                lowestTurningAmount = difference;
                availableTiles.Add(nextTile);
            }
            else if (available && difference == lowestTurningAmount)
            {
                availableTiles.Add(nextTile);
            }
        }
        return availableTiles;
    }
    protected int GetAvailableAdjacentTileTowardTarget(TacticActor actor, MysteryDungeonBattleMap map, int targetTile)
    {
        int currentLocation = actor.GetLocation();
        int currentDistance = map.mapUtility.DistanceBetweenTiles(currentLocation, targetTile, map.mapSize);
        List<int> allAdjacent = map.mapUtility.AdjacentTiles(currentLocation, map.mapSize);
        int targetLocation = -1;
        List<int> possibleChoices = new List<int>();
        for (int i = 0; i < allAdjacent.Count; i++)
        {
            if (!TileAvailableAndPassable(actor, map, map.moveManager, allAdjacent[i])){continue;}
            int newDist = map.mapUtility.DistanceBetweenTiles(allAdjacent[i], targetTile, map.mapSize);
            if (newDist <= currentDistance)
            {
                if (newDist < currentDistance)
                {
                    possibleChoices.Clear();
                    currentDistance = newDist;
                }
                possibleChoices.Add(allAdjacent[i]);
            }
        }
        if (possibleChoices.Count > 0)
        {
            targetLocation = possibleChoices[Random.Range(0, possibleChoices.Count)];
        }
        return targetLocation;
    }
    public void MysteryDungeonEnemyTurn(TacticActor actor, MysteryDungeonBattleManager battleManager, MysteryDungeonBattleMap map)
    {
        int currentLocation = actor.GetLocation();
        map.moveManager.GetAllMoveCosts(actor, map.battlingActors);
        // Same Room As Player/Player Ally OR Very Close (Calculate Enemy Visible Tiles) > Target Them.
        if (!actor.TargetValid())
        {
            TacticActor target = map.MDMapUtility.GetClosestEnemy(actor, map);
            if (target != null)
            {
                actor.SetTarget(target);
            }
        }
        if (actor.TargetValid())
        {
            (List<string> possibleActions, List<List<int>> possibleTargets) = ReturnPossibleActions(actor, map);
            // Target In Range > Attack.
            if (possibleActions.Count > 0)
            {
                // Select and execute an action.
                ExecutePossibleAction(actor, battleManager, map, possibleActions, possibleTargets);
                return;
            }
            // Target Out Of Range > Move.
            else
            {
                List<int> path = FindPathToTarget(actor, map, map.moveManager);
                // Target Very Far > Lose Target.
                if (path.Count > map.mapSize / 3)
                {
                    actor.ResetTarget();
                    return;
                }
                MoveAlongPath(actor, map, path);
                return;
            }
        }
        // Room > Move Straight Til Wall (Prevents Always Entering Same Hallway), Then Along Edge Until Hallway Is Adjacent.
        int room = map.GetRoomOfTile(currentLocation);
        // Move Forward If Possible.
        int forwardTile = map.mapUtility.PointInDirection(currentLocation, actor.GetDirection(), map.mapSize);
        if (TileAvailableAndPassable(actor, map, map.moveManager, forwardTile))
        {
            map.MoveActorToTile(actor, forwardTile);
            return;
        }
        List<int> adjacentTiles = map.mapUtility.AdjacentTiles(currentLocation, map.mapSize);
        List<int> availableTiles = GetAvailableAdjacentTileMinimizingTurns(actor, map, map.moveManager);
        // Nowhere To Go.
        if (availableTiles.Count <= 0){return;}
        // Room And Forward Tile Not Available.
        if (room >= 0)
        {
            // If There Is A Hallway Adjacent, Go For It.
            for (int i = 0; i < adjacentTiles.Count; i++)
            {
                if (map.GetRoomOfTile(adjacentTiles[i]) < 0 && TileAvailableAndPassable(actor, map, map.moveManager, adjacentTiles[i]))
                {
                    map.MoveActorToTile(actor, adjacentTiles[i]);
                    return;
                }
            }
        }
        // Search For Empty Adjacent Tile, Minimizing Turning.
        map.MoveActorToTile(actor, availableTiles[UnityEngine.Random.Range(0, availableTiles.Count)]);
    }
    public void MysteryDungeonAllyTurn(TacticActor actor, MysteryDungeonBattleManager battleManager, MysteryDungeonBattleMap map)
    {
        map.moveManager.GetAllMoveCosts(actor, map.battlingActors);
        TacticActor target = map.MDMapUtility.GetClosestEnemy(actor, map);
        // Enemy Is Visible.
        if (target != null)
        {
            actor.SetTarget(target);
            (List<string> possibleActions, List<List<int>> possibleTargets) = ReturnPossibleActions(actor, map);
            if (possibleActions.Count > 0)
            {
                // Select and execute an action.
                ExecutePossibleAction(actor, battleManager, map, possibleActions, possibleTargets);
                return;
            }
            // Check Adjacent Tiles.
            else
            {
                int moveTo = GetAvailableAdjacentTileTowardTarget(actor, map, target.GetLocation());
                if (moveTo < 0){return;}
                map.MoveActorToTile(actor, moveTo);
                return;
            }
        }
        // Else Follow The Leader.
        else
        {
            actor.ResetTarget();
            FollowTheLeader(actor, map);
        }
    }
    protected void ExecutePossibleAction(TacticActor actor, MysteryDungeonBattleManager battleManager, MysteryDungeonBattleMap map, List<string> possibleActions, List<List<int>> possibleTargets)
    {
        // Select The Action Randomly.
        int selectedIndex = Random.Range(0, possibleActions.Count);
        string selectedAction = possibleActions[selectedIndex];
        // Just In Case.
        if (string.IsNullOrEmpty(selectedAction) ||
        possibleTargets[selectedIndex].Count <= 0)
        {
            Debug.Log(possibleActions.Count);
            Debug.Log("Somehow An Invalid Action Got Through");
            return;
        }
        int selectedLocation = possibleTargets[selectedIndex][Random.Range(0, possibleTargets[selectedIndex].Count)];
        // If Attack Exists Then The Target Is Attackable
        if (selectedAction == "Attack")
        {
            battleManager.ActorAttacksTarget(actor);
            return;
        }
        // Else Do The Skill.
        battleManager.ActivateSkill(selectedAction, actor, selectedLocation);
    }
    // This Implies That A Target Exists For The Actor.
    protected (List<string>, List<List<int>>) ReturnPossibleActions(TacticActor actor, MysteryDungeonBattleMap map)
    {
        // SkillEqualsSkillName OR Attack OR Item
        // TODO Track Items Later.
        // Get All Actives.
        List<string> possibleChoices = new List<string>();
        List<List<int>> possibleTargets = new List<List<int>>();
        if (ReturnActionPossible(actor, map, "Attack").Count > 0)
        {
            possibleChoices.Add("Attack");
            possibleTargets.Add(new List<int>{actor.GetTarget().GetLocation()});
        }
        List<string> allSkills = actor.GetActiveSkills();
        for (int i = 0; i < allSkills.Count; i++)
        {
            // TODO Get The Skill Type From The Skill Name
            active.LoadSkillFromString(activeData.ReturnValue(allSkills[i]), actor);
            string skillType = active.GetSkillType();
            List<int> targets = ReturnActionPossible(actor, map, skillType);
            if (targets.Count > 0)
            {
                possibleChoices.Add(allSkills[i]);
                possibleTargets.Add(targets);
            }
        }
        return (possibleChoices, possibleTargets);
    }
    // Check Is Buff/Debuff/Cleanse/Heal/Strip/Etc Are Useful.
    protected bool SkillEffectUseful(TacticActor target, string effect, string specifics)
    {
        switch (effect)
        {
            // Most Effects Naturally Scale/Stack.
            default:
            return true;
            case "Heal":
            return target.GetHealth() < target.GetBaseHealth();
            case "Manaize":
            return target.GetMana() < target.GetMaxMana();
            case "Buff":
            return !target.BuffExists(specifics);
            case "Cleanse":
            return target.GetStatuses().Count > 0;
            case "Unhealable":
            return !target.GetUnhealable();
            case "Status":
            switch (specifics)
            {
                default:
                return !target.StatusExists(specifics);
                case "Poison":
                case "Burn":
                case "Bleed":
                return true;
            }
        }
    }
    protected List<int> ReturnActionPossible(TacticActor actor, MysteryDungeonBattleMap map, string actionType)
    {
        List<int> possibleTargets = new List<int>();
        // Stricter Action Type Enforcement Than Regular Gamemode.
        // Basic Attack: Check Enemy Targetable Within Basic Attack Range.
        if (actionType == "Attack")
        {
            if (TargetInAttackRange(actor, map))
            {
                possibleTargets.Add(actor.GetTarget().GetLocation());
            }
            return possibleTargets;
        }
        // Check The Cost + Silenced + Etc.
        if (actor.GetSilenced()){return possibleTargets;}
        if (actor.GetEnergy() < active.GetEnergyCost(actor, map)){return possibleTargets;}
        if (actor.GetActions() < active.GetActionCost(actor, map)){return possibleTargets;}
        int range = active.GetRange(actor, map);
        string effect = active.GetEffect();
        string specifics = active.GetSpecifics();
        List<int> targetable = map.mapUtility.GetTilesByShapeSpan(actor.GetLocation(), active.GetRangeShape(), range, map.mapSize, actor.GetLocation());
        // Check The Tiles That Are Actually Targeted, Since They Are Always The Same Times For Zero Range Skills.
        if (range <= 0)
        {
            targetable = map.mapUtility.GetTilesByShapeSpan(actor.GetLocation(), active.GetShape(), active.GetSpan(actor, map), map.mapSize, actor.GetLocation());
        }
        targetable = map.FilterTilesForLineOfSight(actor.GetLocation(), targetable);
        List<TacticActor> targetableEnemies = map.ReturnEnemiesInTiles(actor, targetable);
        List<TacticActor> targetableAllies = map.ReturnAlliesInTiles(actor, targetable);
        // [Skill Types]
        switch (actionType)
        {
            default:
            break;
            // Check Enemy Targetable Within Skill Range.
            case "Damage":
            // Range Zero Damage Is AOE/Room/Etc. Needs To Check The Span Not.
            for (int i = 0; i < targetableEnemies.Count; i++)
            {
                possibleTargets.Add(targetableEnemies[i].GetLocation());
            }
            break;
            // Check Enemy Targetable Within Skill Range And Effect Useful.
            case "Sabotage":
            for (int i = 0; i < targetableEnemies.Count; i++)
            {
                if (SkillEffectUseful(targetableEnemies[i], effect, specifics)){possibleTargets.Add(targetableEnemies[i].GetLocation());}
            }
            break;
            // Check Ally Targetable Within Skill Range And Effect Useful.
            case "Support":
            for (int i = 0; i < targetableAllies.Count; i++)
            {
                if (SkillEffectUseful(targetableAllies[i], effect, specifics)){possibleTargets.Add(targetableAllies[i].GetLocation());}
            }
            break;
            // Check Empty Tile Available.
            case "Summon":
            possibleTargets = map.ReturnEmptyTiles(targetable);
            break;
        }
        if (range <= 0 && possibleTargets.Count > 0)
        {
            possibleTargets.Clear();
            possibleTargets.Add(actor.GetLocation());
        }
        return possibleTargets;
    }
}
