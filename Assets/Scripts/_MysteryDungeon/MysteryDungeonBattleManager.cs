using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// You Control The Party Leader.
// Other Party Members Are Adjustable AI (Select AI + Usable Skills/Spells).
// Enemy Are Nonadjustable AI.
public class MysteryDungeonBattleManager : ClickTileManager
{
    public MysteryDungeonManager MDGeneralManager;
    public PartyDataManager partyData;
    public CharacterList playerParty;
    public MysteryDungeonBattleMap map;
    public AttackManager attackManager;
    public ActiveManager activeManager;
    public EffectManager effectManager;
    public MoveCostManager moveManager;
    // Add The Party To The Map Battling Actors.
    public void Initialize()
    {
        SpawnParty();
        // TODO Randomly Spawn Some Enemies Based On Various Factors?
        NewRound();
    }
    public void InitializePartyLocations(List<TacticActor> actors, int spawnCount)
    {
        // Need To Set Location Of Leader, Then Set Location Of Other Party Members Close To Leader On Valid Tiles.
        List<int> emptyAdjacent = map.MDMapUtility.GetEmptyTilesInCircleRange(map.playerLocation, map);
        int span = 1;
        int maxSpan = map.mapSize / 2;
        while (emptyAdjacent.Count < spawnCount - 1)
        {
            span++;
            if (span >= maxSpan){break;}
            emptyAdjacent = map.MDMapUtility.GetEmptyTilesInCircleRange(map.playerLocation, map, span);
        }
        if (emptyAdjacent.Count < spawnCount - 1)
        {
            Debug.Log($"Not enough valid spawn tiles. Required: {spawnCount - 1}, Available: {emptyAdjacent.Count}. ");
            // TODO Move To Dungeon Defeat Scene.
            return;
        }
        int leaderID = partyData.dungeonState.leaderID;
        int leaderIndex = actors.FindIndex(actor => actor.GetID() == leaderID);
        if (leaderIndex > 0)
        {
            (actors[0], actors[leaderIndex]) = (actors[leaderIndex], actors[0]);
        }
        else if (leaderIndex < 0)
        {
            Debug.Log("No Leader, Exiting.");
            // TODO Move To Dungeon Defeat Scene.
            return;
        }
        actors[0].SetLocation(map.playerLocation);
        actors[0].LoadPersistentStats(partyData.dungeonState.GetCurrentStats(actors[0].GetID()));
        int count = 0;
        for (int i = 0; i < spawnCount; i++)
        {
            if (actors[i].GetID() == leaderID){continue;}
            actors[i].SetLocation(emptyAdjacent[count]);
            count++;
            actors[i].LoadPersistentStats(partyData.dungeonState.GetCurrentStats(actors[i].GetID()));
        }
    }
    protected void SpawnParty()
    {
        int partySizeCap = map.MapMaxPartyCapacity();
        List<TacticActor> actors = new List<TacticActor>();
        actors = map.actorMaker.SpawnTeam(0, playerParty.characters, playerParty.stats, playerParty.characterNames, playerParty.equipment, playerParty.characterIDs);
        map.actorMaker.ApplyBattleModifiers(actors, playerParty.GetBattleModifiers());
        int spawnCount = Mathf.Min(partySizeCap, actors.Count);
        for (int i = 0; i < spawnCount; i++)
        {
            map.AddActorToBattle(actors[i]);
            effectManager.StartBattle(actors[i], map);
        }
        InitializePartyLocations(actors, spawnCount);
    }
    protected bool dangerNear = false;
    public bool DangerNear(){return dangerNear;}
    public void UpdateDanger()
    {
        // Check If There Are Any Enemies Visible Nearby.
        map.UpdateVisibleTiles();
        List<TacticActor> visibleActors = map.GetActorsOnTiles(map.GetVisibleTiles());
        dangerNear = false;
        for (int i = 0; i < visibleActors.Count; i++)
        {
            if (visibleActors[i].GetTeam() != 0)
            {
                dangerNear = true;
                return;
            }
        }
    }
    public List<TacticActor> roundActors;
    public int currentTurn = 0;
    public void NewRound()
    {
        roundActors.Clear();
        List<TacticActor> currentRoundActors = new List<TacticActor>(map.battlingActors);
        // Make Sure The Leader Goes First, Then Sort By Initiative.
        for (int i = 0; i < currentRoundActors.Count; i++)
        {
            if (currentRoundActors[i].GetID() == partyData.dungeonState.leaderID)
            {
                roundActors.Insert(0, currentRoundActors[i]);
                continue;
            }
            roundActors.Add(currentRoundActors[i]);
        }
        currentTurn = 0;
        roundActors[currentTurn].StartTurn();
        effectManager.StartTurn(roundActors[currentTurn], map);
        map.UpdateMap();
        // Wait For Player To Finish Before Doing Anything.
        // TODO StartTurn effects for leader.
    }
    // TODO Add The Player Action Stuff.
    // Track If An Action Is Successful.
    protected List<int> GetTurnActorReachableTiles()
    {
        // This Already Updates Path Costs.
        return moveManager.GetAllReachableTiles(roundActors[currentTurn], map.battlingActors, true);
    }
    public void PlayerStartMoving()
    {
        if (currentTurn != 0){return;}
        map.UpdateMap();
        map.ResetHighlights();
        map.UpdateMovingHighlights(roundActors[currentTurn], moveManager);
        GetTurnActorReachableTiles();
    }
    // Clicking On A Direction Arrow.
    public bool PlayerMoveInDirection(int direction)
    {
        // Only On Player's Turn.
        if (currentTurn != 0){return false;}
        int currentLocation = map.GetPlayerLocation();
        int targetLocation = map.mapUtility.PointInDirection(currentLocation, direction, map.mapSize);
        if (map.mapUtility.DistanceBetweenTiles(currentLocation, targetLocation, map.mapSize) > 1){return false;}
        // You Can Switch Places With Allies (You Will Die If In DeepWater/Pit And Not A Flier).
        TacticActor actorOnTile = map.GetActorOnTile(targetLocation);
        if (actorOnTile != null && actorOnTile.GetTeam() == 0)
        {
            MoveActorToTile(targetLocation);
            map.UpdatePlayerLocation(targetLocation);
            MoveActorToTile(currentLocation, actorOnTile);
            EndPlayerTurn();
            return true;
        }
        List<int> reachableTiles = GetTurnActorReachableTiles();
        if (reachableTiles.Contains(targetLocation))
        {
            MoveActorToTile(targetLocation);
            map.UpdatePlayerLocation(targetLocation);
            EndPlayerTurn();
            return true;
        }
        return false;
    }
    // Clicking On A Tile.
    public bool PlayerMoveToTile(int tileNumber)
    {
        if (currentTurn != 0){return false;}
        int clickedTileNumber = map.currentTiles[tileNumber];
        if (clickedTileNumber < 0 || clickedTileNumber > map.mapInfo.Count){return false;}
        int currentLocation = map.GetPlayerLocation();
        // Check If The Tile Is In Move Range.
        List<int> reachableTiles = GetTurnActorReachableTiles();
        if (reachableTiles.Contains(clickedTileNumber))
        {
            // Find The Path To The Tile.
            List<int> path = moveManager.GetPrecomputedPath(currentLocation, clickedTileNumber);
            MoveAlongPath(roundActors[currentTurn], path);
            map.UpdatePlayerLocation(clickedTileNumber);
            EndPlayerTurn();
            return true;
        }
        return false;
    }
    protected void EndPlayerTurn()
    {
        if (currentTurn != 0){return;}
        effectManager.EndTurn(roundActors[currentTurn], map);
        UpdateDanger();
        NextTurn();
    }
    protected bool TurnActorAlive()
    {
        return roundActors[currentTurn].GetHealth() > 0;
    }
    public void NextTurn()
    {
        currentTurn++;
        if (currentTurn >= roundActors.Count)
        {
            NewRound();
            return;
        }
        roundActors[currentTurn].StartTurn();
        effectManager.StartTurn(roundActors[currentTurn], map);
        if (roundActors[currentTurn].GetTeam() == 0)
        {
            AllyTurn();
        }
        else
        {
            EnemyTurn();
        }
        if (TurnActorAlive())
        {
            effectManager.EndTurn(roundActors[currentTurn], map);
        }
        else
        {
            // TODO Apply Death Skills.
        }
        NextTurn();
    }
    protected void FollowTheLeader()
    {
        int targetLocation = -1;
        int targetDisance = 999;
        int targetMoveCost = 999;
        int currentLocation = roundActors[currentTurn].GetLocation();
        // Path To The Closest Tile You Can Reach.
        List<int> reachableTiles = GetTurnActorReachableTiles();
        int leaderLocation = map.GetPlayerLocation();
        for (int i = 0; i < reachableTiles.Count; i++)
        {
            List<int> testPath = moveManager.GetPrecomputedPath(currentLocation, reachableTiles[i]);
            int distance = map.mapUtility.DistanceBetweenTiles(leaderLocation, reachableTiles[i], map.mapSize);
            int moveCost = moveManager.moveCost;
            if (distance < targetDisance)
            {
                targetLocation = reachableTiles[i];
                targetDisance = distance;
                targetMoveCost = moveCost;
            }
            else if (distance == targetDisance && moveCost < targetMoveCost)
            {
                targetLocation = reachableTiles[i];
                targetMoveCost = moveCost;
            }
        }
        if (targetLocation < 0){return;}
        List<int> path = moveManager.GetPrecomputedPath(currentLocation, targetLocation);
        MoveAlongPath(roundActors[currentTurn], path);
    }
    public void AllyTurn()
    {
        // DO ALLY TURNS BASED ON SET AI (Stick Together/Go After Foes/Go Other Way/Explore)
        // For Now Simple, Follow The Leader.
        FollowTheLeader();
    }
    // DO ENEMY TURNS.
    public void EnemyTurn()
    {
        // Move Randomly In A Single Direction.
        // If In Range Then Become Regular AI.
    }
    protected void MoveAlongPath(TacticActor actor, List<int> path)
    {
        if (path.Count <= 0){return;}
        for (int i = path.Count - 1; i >= 0; i--)
        {
            int prevLoc = actor.GetLocation();
            moveManager.MoveActorToTile(actor, path[i], map);
            actor.PayMoveCost(moveManager.MoveCostOfTile(path[i]));
            if (actor.GetMovement() < 0){break;}
        }
    }
    public void MoveActorToTile(int tileNumber, TacticActor actor = null)
    {
        if (actor == null)
        {
            actor = roundActors[currentTurn];
        }
        map.MoveActorToTile(actor, tileNumber);
    }
}
