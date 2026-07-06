using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BattleMapUtility", menuName = "ScriptableObjects/Utility/BattleMapUtility", order = 1)]
public class BattleMapUtility : ScriptableObject
{
    public GeneralUtility utility;
    public MapUtility mapUtility;
    // Finding Actor Utilities
    public TacticActor GetActorOnTile(BattleMap map, int tileNumber, bool includeInvisible = true)
    {
        if (tileNumber < 0){ return null; }
        for (int i = 0; i < map.battlingActors.Count; i++)
        {
            if (map.battlingActors[i].GetLocation() == tileNumber && map.battlingActors[i].GetHealth() > 0)
            {
                if (!includeInvisible && map.battlingActors[i].invisible){return null;}
                return map.battlingActors[i];
            }
        }
        return null;
    }
    public List<TacticActor> GetActorsOnTiles(BattleMap map, List<int> tiles)
    {
        List<TacticActor> actors = new List<TacticActor>();
        for (int i = 0; i < tiles.Count; i++)
        {
            TacticActor testActor = GetActorOnTile(map, tiles[i]);
            if (testActor != null)
            {
                actors.Add(testActor);
            }
        }
        return actors;
    }
    public List<TacticActor> AllActorsBySprite(string spriteName, BattleMap map)
    {
        List<TacticActor> actors = new List<TacticActor>();
        for (int i = 0; i < map.battlingActors.Count; i++)
        {
            if (map.battlingActors[i].GetSpriteName().Contains(spriteName))
            {
                actors.Add(map.battlingActors[i]);
            }
        }
        return actors;
    }
    public List<TacticActor> AllActorsBySpecies(string speciesName, BattleMap map)
    {
        List<TacticActor> actors = new List<TacticActor>();
        for (int i = 0; i < map.battlingActors.Count; i++)
        {
            if (map.battlingActors[i].GetSpecies().Contains(speciesName))
            {
                actors.Add(map.battlingActors[i]);
            }
        }
        return actors;
    }
    public int GetRandomEnemyLocation(TacticActor actor, List<int> targetedTiles, BattleMap map)
    {
        List<TacticActor> enemies = GetActorsOnTiles(map, targetedTiles);
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            if (enemies[i].GetTeam() == actor.GetTeam())
            {
                enemies.RemoveAt(i);
            }
        }
        if (enemies.Count == 0)
        {
            return -1;
        }
        return enemies[UnityEngine.Random.Range(0, enemies.Count)].GetLocation();
    }
    public int GetRandomAllyLocation(TacticActor actor, List<int> targetedTiles, BattleMap map)
    {
        List<TacticActor> allies = GetActorsOnTiles(map, targetedTiles);
        for (int i = allies.Count - 1; i >= 0; i--)
        {
            if (allies[i].GetTeam() != actor.GetTeam())
            {
                allies.RemoveAt(i);
            }
        }
        if (allies.Count == 0)
        {
            return -1;
        }
        return allies[UnityEngine.Random.Range(0, allies.Count)].GetLocation();
    }
    public TacticActor GetActorOnFacingTile(TacticActor facingActor, BattleMap map)
    {
        int facingTile = mapUtility.PointInDirection(facingActor.GetLocation(), facingActor.GetDirection(), map.mapSize);
        return GetActorOnTile(map, facingTile);
    }
    // Moving Utilities
    public void MoveSkill(TacticActor mover, int targetTile, string moveDirection, int distance, BattleMap map)
    {
        int currentLocation = mover.GetLocation();
        int moveSkillDirection = mapUtility.DirectionBetweenLocations(currentLocation, targetTile, map.mapSize);
        switch (moveDirection)
        {
            case "Forward":
                break;
            case "Back":
                moveSkillDirection = (moveSkillDirection + 3) % 6;
                break;
        }
        // Get the tile to move to.
        int nextLocation = mapUtility.GetTileByDirectionDistance(currentLocation, moveSkillDirection, distance, map.mapSize);
        // Check if it is availabe.
        if (GetActorOnTile(map, nextLocation) == null)
        {
            // Move to the tile.
            map.MoveActorToTile(mover, nextLocation);
        }
    }
    public void MoveThroughSkill(TacticActor mover, int tile, BattleMap map)
    {
        int distance = mapUtility.DistanceBetweenTiles(mover.GetLocation(), tile, map.mapSize);
        int direction = mapUtility.DirectionBetweenLocations(mover.GetLocation(), tile, map.mapSize);
        int nextLocation = tile;
        for (int i = 0; i < distance; i++)
        {
            nextLocation = mapUtility.PointInDirection(nextLocation, direction, map.mapSize);
        }
        if (GetActorOnTile(map, nextLocation) == null)
        {
            map.MoveActorToTile(mover, nextLocation);
        }
    }
    public int AKRaidMove(TacticActor actor, BattleMap map)
    {
        // Get All Enemies.
        List<TacticActor> enemies = map.AllEnemies(actor);
        // Determine The Direction Of The Tile To Be Redeployed To, Relative To The Enemy.
        int oppDirection = (actor.GetDirection() + 3) % 6;
        for (int i = 0; i < enemies.Count; i++)
        {
            // Check For Each Enemy If The Tile In That Direction Is Available.
            int redeployTile = mapUtility.PointInDirection(enemies[i].GetLocation(), oppDirection, map.mapSize);
            if (GetActorOnTile(map, redeployTile) == null)
            {
                return redeployTile;
            }
        }
        return -1;
    }
    public bool TeleportToTarget(TacticActor mover, TacticActor target, string direction, BattleMap map)
    {
        int dir = -1;
        int tile = -1;
        switch (direction)
        {
            case "Behind":
                dir = target.GetDirection();
                dir = (dir + 3) % 6;
                break;
        }
        if (dir == -1) { return false; }
        tile = mapUtility.PointInDirection(target.GetLocation(), dir, map.mapSize);
        // Can't teleport is already actor there.
        if (GetActorOnTile(map, tile) != null) { return false; }
        mover.SetLocation(tile);
        return true;
    }
    public void DisplaceSkill(TacticActor displacer, List<int> targetedTiles, string displaceType, int force, BattleMap map)
    {
        int relativeForce = force;
        int elevationDifference = 0;
        TacticActor displaced = null;
        switch (displaceType)
        {
            case "Pull":
            for (int i = 0; i < targetedTiles.Count; i++)
            {
                displaced = GetActorOnTile(map, targetedTiles[i]);
                if (displaced == null){continue;}
                elevationDifference = map.ReturnElevation(displacer.GetLocation()) - map.ReturnElevation(displaced.GetLocation());
                relativeForce = force - elevationDifference + displacer.GetWeight() - displaced.GetWeight();
                DisplaceActor(displaced, map.DirectionBetweenActors(displaced, displacer), relativeForce, map);
            }
            break;
            case "Push":
            for (int i = 0; i < targetedTiles.Count; i++)
            {
                displaced = GetActorOnTile(map, targetedTiles[i]);
                if (displaced == null){continue;}
                elevationDifference = map.ReturnElevation(displacer.GetLocation()) - map.ReturnElevation(displaced.GetLocation());
                relativeForce = force + elevationDifference + displacer.GetWeight() - displaced.GetWeight();
                DisplaceActor(displaced, map.DirectionBetweenActors(displacer, displaced), relativeForce, map);
            }
                break;
            case "Flip":
                for (int i = 0; i < targetedTiles.Count; i++)
                {
                    displaced = GetActorOnTile(map, targetedTiles[i]);
                    if (displaced == null) { continue; }
                    elevationDifference = map.ReturnElevation(displacer.GetLocation()) - map.ReturnElevation(displaced.GetLocation());
                    relativeForce = force - Mathf.Abs(elevationDifference) + displacer.GetWeight() - displaced.GetWeight();
                    if (relativeForce >= 0)
                    {
                        // Get the tile that is in the opposite direction the same distance away.
                        int direction = map.DirectionBetweenActors(displaced, displacer);
                        int distance = Mathf.Max(map.DistanceBetweenActors(displacer, displaced), force);
                        int tile = displacer.GetLocation();
                        int furthestTile = displaced.GetLocation();
                        for (int j = 0; j < distance; j++)
                        {
                            int nextTile = mapUtility.PointInDirection(tile, direction, map.mapSize);
                            if (nextTile < 0)
                            {
                                break;
                            }
                            if (GetActorOnTile(map, nextTile) == null)
                            {
                                furthestTile = nextTile;
                            }
                            tile = nextTile;
                        }
                        // Check if the tile is empty.
                        if (GetActorOnTile(map, tile) == null)
                        {
                            // Move the displaced into that tile.
                            map.MoveActorToTile(displaced, tile);
                        }
                        else
                        {
                            map.MoveActorToTile(displaced, furthestTile);
                        }
                    }
                }
                break;
            case "Sideways":
                // Randomly move them in a direction that is not forward or back.
                for (int i = 0; i < targetedTiles.Count; i++)
                {
                    displaced = GetActorOnTile(map, targetedTiles[i]);
                    if (displaced == null){continue;}
                    elevationDifference = map.ReturnElevation(displacer.GetLocation()) - map.ReturnElevation(displaced.GetLocation());
                    relativeForce = force + elevationDifference + displacer.GetWeight() - displaced.GetWeight();
                    List<int> exceptDirections = new List<int>();
                    exceptDirections.Add(map.DirectionBetweenActors(displaced, displacer));
                    exceptDirections.Add(map.DirectionBetweenActors(displacer, displaced));
                    DisplaceActor(displaced, ReturnRandomDirection(exceptDirections), relativeForce, map);
                }
                break;
        }
        map.UpdateActors();
    }
    public int ReturnRandomDirection(List<int> except)
    {
        if (except.Count > 5){return -1;}
        int direction = Random.Range(0, 6);
        if (except.Contains(direction))
        {
            return ReturnRandomDirection(except);
        }
        return direction;
    }
    protected void DisplaceActor(TacticActor actor, int direction, int force, BattleMap map)
    {
        int displaceDamage = Mathf.Max(actor.GetWeight(), 1) * force * 6;
        int nextTile = actor.GetLocation();
        int initialElevation = map.GetTileElevation(nextTile);
        for (int i = 0; i < force; i++)
        {
            nextTile = mapUtility.PointInDirection(nextTile, direction, map.mapSize);
            // Can't push someone out of bounds.
            if (nextTile < 0) { break; }
            // TODO Can't push someone through a wall.
            // Need to check the border in the opposite direction of the next tile.
            // Can't push someone through a building.
            if (map.GetBuildingOnTile(nextTile) != "")
            {
                map.DisplaceDamage(actor, force, nextTile, false, null, true);
                break;
            }
            // Tiles are passable if no one is occupying them.
            if (GetActorOnTile(map, nextTile) == null)
            {
                map.MoveActorToTile(actor, nextTile);
            }
            else if (GetActorOnTile(map, nextTile) != null)
            {
                TacticActor oActor = GetActorOnTile(map, nextTile);
                // Damage both the displaced actor and actor displaced into.
                map.DisplaceDamage(actor, force, nextTile, true, oActor);
                // Chain displacement for fun.
                if (i < force - 1)
                {
                    DisplaceActor(oActor, direction, force - i - 1, map);
                }
                break;
            }
        }
        int finalElevation = map.GetTileElevation(actor.GetLocation());
        // Falling damage.
        if (finalElevation < initialElevation)
        {
            map.FallingDamage(actor, initialElevation - finalElevation);
        }
    }
    // Calculation Utilities.
    public int AverageActorHealth(BattleMap map)
    {
        int health = 0;
        int count = map.battlingActors.Count;
        for (int i = 0; i < count; i++)
        {
            health += map.battlingActors[i].GetHealth();
        }
        return health / count;
    }
    // Finding Tiles Utilities.
    public int ReturnClosestTileOfType(BattleMap map, TacticActor actor, string tileType)
    {
        List<int> tiles = new List<int>();
        for (int i = 0; i < map.mapInfo.Count; i++)
        {
            if (actor.GetMoveType() != "Flying" && map.excludedTileTypesForNonFlying.Contains(map.mapInfo[i]))
            {
                continue;
            }
            if (map.mapInfo[i].Contains(tileType) && GetActorOnTile(map, i) == null)
            {
                tiles.Add(i);
            }
        }
        return ReturnLowestMoveCostTile(map, actor, tiles);
    }
    public int ReturnLowestMoveCostTile(BattleMap map, TacticActor actor, List<int> tiles)
    {
        if (tiles == null || tiles.Count <= 0) { return -1; }
        if (map.moveManager == null)
        {
            return mapUtility.ReturnClosestTile(actor.GetLocation(), tiles, map.mapSize);
        }
        map.moveManager.GetAllMoveCosts(actor, map.battlingActors);
        return map.moveManager.GetLowestMoveCostTile(actor.GetLocation(), tiles);
    }
    public bool TileSandwiched(BattleMap map, TacticActor actor, string tileType)
    {
        if (actor.GetTarget() == null || actor.GetTarget().GetHealth() <= 0 || actor.GetTarget().invisible){return false;}
        // Already checked for alignment in earlier condition.
        // Get the tiles inbetween you and the target.
        List<int> tilesBetween = mapUtility.GetTileInLineBetweenPoints(actor.GetLocation(), actor.GetTarget().GetLocation(), map.mapSize);
        // Check if any of the tiles are of the tile type.
        for (int i = 0; i < tilesBetween.Count; i++)
        {
            if (map.mapInfo[tilesBetween[i]].Contains(tileType))
            {
                return true;
            }
        }
        return false;
    }
    public int ReturnClosestTileSandwiched(BattleMap map, TacticActor actor, string tileType)
    {
        List<int> tiles = new List<int>();
        if (actor.GetTarget() == null || actor.GetTarget().GetHealth() <= 0 || actor.GetTarget().invisible){return -1;}
        int targetLocation = actor.GetTarget().GetLocation();
        List<int> adjacentTiles = mapUtility.AdjacentTiles(targetLocation, map.mapSize);
        for (int i = 0; i < adjacentTiles.Count; i++)
        {
            // Check if the target is adjacent to any of the requested tile types.
            if (map.mapInfo[adjacentTiles[i]].Contains(tileType))
            {
                // Get the direction and check if any point in a tile is valid.
                int direction = mapUtility.DirectionBetweenLocations(targetLocation, adjacentTiles[i], map.mapSize);
                List<int> lineTiles = mapUtility.GetTilesInLineDirection(adjacentTiles[i], direction, map.mapSize, map.mapSize);
                for (int j = 0; j < lineTiles.Count; j++)
                {
                    if (lineTiles[j] < 0 || map.TileExcluded(actor, map.mapInfo[lineTiles[j]]) || GetActorOnTile(map, lineTiles[j]) != null)
                    {
                        continue;
                    }
                    tiles.Add(lineTiles[j]);
                    break;
                }
            }
        }
        return ReturnLowestMoveCostTile(map, actor, tiles);
    }
    public bool GuardCoveringAlly(BattleMap map, TacticActor guardActor)
    {
        if (map == null || guardActor == null) { return false; }
        if (!guardActor.Guarding() || guardActor.GetHealth() <= 0) { return false; }
        List<TacticActor> allies = map.AllAllies(guardActor);
        for (int i = 0; i < allies.Count; i++)
        {
            if (GuardCanCoverAlly(map, guardActor, allies[i]))
            {
                return true;
            }
        }
        return false;
    }
    public bool GuardCanCoverAlly(BattleMap map, TacticActor guardActor, TacticActor ally)
    {
        if (map == null || guardActor == null || ally == null) { return false; }
        if (guardActor == ally) { return false; }
        if (guardActor.GetTeam() != ally.GetTeam()) { return false; }
        if (guardActor.GetHealth() <= 0 || ally.GetHealth() <= 0) { return false; }
        if (!guardActor.Guarding()) { return false; }
        return map.DistanceBetweenActors(guardActor, ally) <= guardActor.GetGuardRange();
    }
    public bool TileCanGuardAnyAllyFromAnyEnemy(BattleMap map, TacticActor guardActor, int tileNumber, int guardRange = -1)
    {
        if (!ValidGuardTile(map, guardActor, tileNumber)) { return false; }
        if (guardRange < 0)
        {
            guardRange = guardActor.GetGuardRange();
        }
        if (guardRange <= 0) { return false; }
        List<TacticActor> allies = map.AllAllies(guardActor);
        List<TacticActor> enemies = map.AllEnemies(guardActor);
        for (int i = 0; i < allies.Count; i++)
        {
            TacticActor ally = allies[i];
            if (ally == null || ally == guardActor || ally.GetHealth() <= 0) { continue; }
            for (int j = 0; j < enemies.Count; j++)
            {
                TacticActor enemy = enemies[j];
                if (enemy == null || enemy.GetHealth() <= 0 || enemy.invisible) { continue; }
                if (TileCanGuardAllyFromEnemy(map, guardActor, tileNumber, ally, enemy, guardRange))
                {
                    return true;
                }
            }
        }
        return false;
    }
    public bool TileCanGuardAllyFromEnemy(BattleMap map, TacticActor guardActor, int tileNumber, TacticActor ally, TacticActor enemy, int guardRange)
    {
        if (map == null || guardActor == null || ally == null || enemy == null) { return false; }
        if (guardActor.GetTeam() != ally.GetTeam()) { return false; }
        if (guardActor.GetTeam() == enemy.GetTeam()) { return false; }
        if (guardActor == ally) { return false; }
        if (ally.GetHealth() <= 0 || enemy.GetHealth() <= 0) { return false; }
        // Check If Guard Is Close Enough To Ally To Intercept.
        if (mapUtility.DistanceBetweenTiles(tileNumber, ally.GetLocation(), map.mapSize) > guardRange)
        {
            return false;
        }
        bool meleeAttack = mapUtility.DistanceBetweenTiles(enemy.GetLocation(), ally.GetLocation(), map.mapSize) <= 1;
        // Check If Guard Is Close Enough To Enemy To Intercept.
        if (meleeAttack && mapUtility.DistanceBetweenTiles(tileNumber, enemy.GetLocation(), map.mapSize) > guardRange)
        {
            return false;
        }
        return true;
    }
    protected bool ValidGuardTile(BattleMap map, TacticActor guardActor, int tileNumber)
    {
        if (map == null || guardActor == null) { return false; }
        if (tileNumber < 0 || tileNumber >= map.mapInfo.Count) { return false; }
        if (map.TileExcluded(guardActor, map.mapInfo[tileNumber])) { return false; }
        TacticActor occupyingActor = GetActorOnTile(map, tileNumber);
        return occupyingActor == null || occupyingActor == guardActor;
    }
    public List<int> ReturnGuardTiles(BattleMap map, TacticActor guardActor)
    {
        List<int> guardTiles = new List<int>();
        if (map == null || guardActor == null) { return guardTiles; }
        for (int tile = 0; tile < map.mapInfo.Count; tile++)
        {
            if (TileCanGuardAnyAllyFromAnyEnemy(map, guardActor, tile))
            {
                guardTiles.Add(tile);
            }
        }
        return guardTiles;
    }
    public int ReturnLowestMoveCostGuardTile(BattleMap map, TacticActor guardActor)
    {
        if (map == null || guardActor == null || map.moveManager == null) { return -1; }
        List<int> guardTiles = ReturnGuardTiles(map, guardActor);
        MoveCostManager moveManager = map.moveManager;
        moveManager.GetAllMoveCosts(guardActor, map.battlingActors);
        return moveManager.GetLowestMoveCostTile(guardActor.GetLocation(), guardTiles);
    }
}
