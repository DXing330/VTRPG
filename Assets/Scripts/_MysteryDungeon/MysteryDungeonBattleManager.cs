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
    public MysteryDungeonActorAI actorAI;
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
        int leaderID = partyData.dungeonState.GetLeaderID();
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
        actors[0].SetMaxFullness(partyData.dungeonState.GetMaxFullness());
        actors[0].SetCurrentFullness(partyData.dungeonState.GetCurrentFullness());
        int count = 0;
        for (int i = 0; i < spawnCount; i++)
        {
            if (actors[i].GetID() == leaderID){continue;}
            actors[i].SetLocation(emptyAdjacent[count]);
            count++;
            actors[i].LoadPersistentStats(partyData.dungeonState.GetCurrentStats(actors[i].GetID()));
        }
        map.UpdateMap();
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
            // MD Mode Has Less Actions Per Turn, In General Either 1 Move/Attack/Skill.
            actors[i].SetBaseActions(2);
            map.AddActorToBattle(actors[i]);
            effectManager.StartBattle(actors[i], map);
        }
        InitializePartyLocations(actors, spawnCount);
    }
    protected bool fastMove = false;
    public void UpdateFastMove(bool newInfo){fastMove = newInfo;}
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
                fastMove = false;
                return;
            }
        }
    }
    public bool RoomHallBorder(int direction)
    {
        int currentLocation = map.GetPlayerLocation();
        int targetLocation = map.mapUtility.PointInDirection(currentLocation, direction, map.mapSize);
        int currentRoom = map.GetRoomOfTile(currentLocation);
        int nextRoom = map.GetRoomOfTile(targetLocation);
        // If in a hallway and the next tile is a room.
        if (currentRoom < 0 && nextRoom >= 0){return true;}
        // Or if in a room and the next tile is a hallway.
        if (currentRoom >= 0 && nextRoom < 0){return true;}
        return false;
    }
    public List<TacticActor> roundActors;
    public int currentTurn = 0;
    public TacticActor GetTurnActor(int turn = -1)
    {
        if (turn < 0){turn = currentTurn;}
        return roundActors[turn];
    }
    public void NewRound()
    {
        map.UpdateEnemySpawning();
        roundActors.Clear();
        for (int i = map.battlingActors.Count - 1; i >= 0; i--)
        {
            ActivateDeathPassive(map.battlingActors[i]);
        }
        List<TacticActor> currentRoundActors = new List<TacticActor>(map.battlingActors);
        // Make Sure The Leader Goes First.
        for (int i = 0; i < currentRoundActors.Count; i++)
        {
            if (currentRoundActors[i].GetID() == partyData.dungeonState.GetLeaderID())
            {
                roundActors.Insert(0, currentRoundActors[i]);
                continue;
            }
            roundActors.Add(currentRoundActors[i]);
        }
        currentTurn = 0;
        roundActors[currentTurn].StartTurn();
        effectManager.StartTurn(roundActors[currentTurn], map);
        if (roundActors[currentTurn].GetHealth() <= 0)
        {
            // Leader Dead, Owari Da, Check For Reviver Seeds?
            // Return In Defeat.
            MDGeneralManager.CompleteDungeon(false);
            return;
        }
        if (!fastMove){map.updateMap = true;}
        map.RefreshPlayerLocation();
        map.UpdateMap();
        // Wait For Player To Finish Before Doing Anything.
        if (actorAI.MentalStateTurn(roundActors[currentTurn], map))
        {
            EndActorTurn();
        }
    }
    // Track If An Action Is Successful.
    protected List<int> GetTurnActorAttackableTiles()
    {
        List<int> allAttackable = map.GetAttackableTiles(roundActors[currentTurn]);
        if (currentTurn == 0)
        {
            allAttackable = allAttackable.Intersect(map.GetVisibleTiles()).ToList();
        }
        return allAttackable;
    }
    public void PlayerStartAttacking()
    {
        if (currentTurn != 0){return;}
        map.UpdateMap();
        map.ResetHighlights();
        map.UpdateHighlights(GetTurnActorAttackableTiles(), "Red");
    }
    public bool PlayerAttackTile(int tileNumber)
    {
        if (currentTurn != 0){return false;}
        int clickedTileNumber = map.currentTiles[tileNumber];
        if (clickedTileNumber < 0 || clickedTileNumber > map.mapInfo.Count){return false;}
        // Check If The Tile Is In Move Range.
        List<int> attackableTiles = GetTurnActorAttackableTiles();
        if (!attackableTiles.Contains(clickedTileNumber)){return false;}
        ActorBasicAttacksTile(roundActors[currentTurn], clickedTileNumber);
        // End The Turn If The Actor Can't Attack Anymore.
        if (roundActors[currentTurn].GetActions() < roundActors[currentTurn].GetAttackActionCost())
        {
            EndActorTurn();
        }
        return true;
    }
    public void PlayerChangeDirection(int direction)
    {
        if (currentTurn != 0){return;}
        roundActors[currentTurn].SetDirection(direction);
        map.UpdateMap();
    }
    protected List<int> GetTurnActorReachableTiles()
    {
        // This Already Updates Path Costs.
        List<int> allReachable = new List<int>(moveManager.GetAllReachableTiles(roundActors[currentTurn], map.battlingActors, true));
        if (currentTurn == 0)
        {
            allReachable = allReachable.Intersect(map.GetVisibleTiles()).ToList();
        }
        return allReachable;
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
        TacticActor playerActor = roundActors[currentTurn];
        int currentLocation = map.GetPlayerLocation();
        int targetLocation = map.mapUtility.PointInDirection(currentLocation, direction, map.mapSize);
        if (map.mapUtility.DistanceBetweenTiles(currentLocation, targetLocation, map.mapSize) > 1){return false;}
        // You Can Switch Places With Allies If You Can Move Onto The Tile.
        TacticActor actorOnTile = map.GetActorOnTile(targetLocation);
        if (actorOnTile != null && actorOnTile.GetTeam() == 0 && moveManager.ReturnBaseMoveCostOfTile(playerActor, targetLocation) <= (playerActor.GetActions() * playerActor.GetSpeed()))
        {
            MoveActorToTile(targetLocation);
            MoveActorToTile(currentLocation, actorOnTile);
            map.RefreshPlayerLocation();
            EndActorTurn();
            return true;
        }
        List<int> reachableTiles = GetTurnActorReachableTiles();
        if (reachableTiles.Contains(targetLocation))
        {
            MoveActorToTile(targetLocation);
            map.RefreshPlayerLocation();
            EndActorTurn();
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
            map.RefreshPlayerLocation();
            EndActorTurn();
            return true;
        }
        return false;
    }
    protected void PlayerHunger()
    {
        if (currentTurn == 0)
        {
            int hungerDamage = roundActors[currentTurn].Hunger();
            if (hungerDamage > 0)
            {
                map.UpdateCombatLog(roundActors[currentTurn].GetPersonalName() + " takes " + hungerDamage + " hunger damage.");
            }
            else if (roundActors[currentTurn].GetStomachPercentFull() < 10)
            {
                map.UpdateCombatLog("Feeling Hungry");
            }
        }
    }
    public MysteryDungeonItemEffect itemEffect;
    public void ActorUseItem(string itemName, TacticActor actor = null)
    {
        if (actor == null)
        {
            actor = roundActors[currentTurn];
        }
        itemEffect.UseItem(actor, map, itemName, partyData);
        if (actor.GetTeam() == 0)
        {
            // Remove The Item.
            partyData.dungeonBag.UseItem(itemName);
        }
        EndActorTurn();
    }
    public void ActorThrowItem(string itemName, TacticActor actor = null)
    {
        if (actor == null)
        {
            actor = roundActors[currentTurn];
        }
        int thrownToTile = map.MDMapUtility.GetThrownToTile(actor, map);
        // If No Actor Then It Hits A Wall And Lands In Adjacent Empty Tile.
        if (map.GetActorOnTile(thrownToTile) == null)
        {
            // TESTED 10/26/26
            map.AddItemToLocation(itemName, thrownToTile);
        }
        else
        {
            itemEffect.ThrowItem(map.GetActorOnTile(thrownToTile), map, itemName, partyData);
        }
        if (actor.GetTeam() == 0)
        {
            // Remove The Item.
            partyData.dungeonBag.UseItem(itemName);
        }
        EndActorTurn();
    }
    public void ActorDropItem(string itemName, TacticActor actor = null)
    {
        if (actor == null)
        {
            actor = roundActors[currentTurn];
        }
        map.AddItemToLocation(itemName, actor.GetLocation());
        NextTurn();
    }
    protected void EndActorTurn()
    {
        // Track Hunger For The Leader At The End Of Turn.
        PlayerHunger();
        effectManager.EndTurn(roundActors[currentTurn], map);
        roundActors[currentTurn].EndTurn();
        map.ApplyEndTerrainEffect(roundActors[currentTurn]);
        UpdateDanger();
        NextTurn();
    }
    protected bool TurnActorAlive()
    {
        return roundActors[currentTurn].GetHealth() > 0;
    }
    public void NextTurn()
    {
        map.updateMap = false;
        currentTurn++;
        if (currentTurn >= roundActors.Count)
        {
            NewRound();
            return;
        }
        roundActors[currentTurn].StartTurn();
        effectManager.StartTurn(roundActors[currentTurn], map);
        // Skip Dead/Stunned/Etc Do Nothing.
        if (roundActors[currentTurn].GetHealth() <= 0 || roundActors[currentTurn].GetActions() <= 0)
        {
        }
        // Next Priority Is Mental States, Again Skipped Since Actor AI Will Handle It.
        else if (actorAI.MentalStateTurn(roundActors[currentTurn], map))
        {
        }
        // Then Team Checks.
        else if (roundActors[currentTurn].GetTeam() == 0)
        {
            AllyTurn();
        }
        else if (roundActors[currentTurn].GetTeam() != 0)
        {
            EnemyTurn();
        }
        // Always Reach Here.
        if (TurnActorAlive())
        {
            // This Calls NextTurn();
            EndActorTurn();
        }
        else
        {
            ActivateDeathPassive(roundActors[currentTurn]);
            NextTurn();
        }
    }
    public void AllyTurn()
    {
        // DO ALLY TURNS BASED ON SET AI (Stick Together/Go After Foes/Go Other Way/Explore)
        actorAI.MysteryDungeonAllyTurn(roundActors[currentTurn], this, map);
    }
    // DO ENEMY TURNS.
    public void EnemyTurn()
    {
        actorAI.MysteryDungeonEnemyTurn(roundActors[currentTurn], this, map);
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
    public void ActorAttacksTarget(TacticActor actor)
    {
        if (!actor.TargetValid()){return;}
        ActorAttacksActor(actor, actor.GetTarget());
    }
    public void ActorBasicAttacksTile(TacticActor actor, int tileNumber)
    {
        TacticActor target = map.GetActorOnTile(tileNumber);
        if (target == null){return;}
        ActorAttacksActor(actor, target);
    }
    protected void ActorAttacksActor(TacticActor attacker, TacticActor defender)
    {
        if (!attacker.AttackActionsLeft()){return;}
        attacker.PayAttackCost();
        map.UpdateCombatLog(attacker.GetPersonalName() + " attacks " + defender.GetPersonalName() + ".");
        attackManager.ActorAttacksActorWithAttackSpeed(attacker, defender, map, attacker.GetBasicAttackMultiplier(), attacker.GetBasicAttackDamageType());
        List<int> basicAttackTargetTiles = map.mapUtility.GetTilesByShapeSpan(defender.GetLocation(), attacker.GetBasicAttackShape(), attacker.GetBasicAttackSpan(), map.mapSize, attacker.GetLocation());
        List<TacticActor> targetActors = map.GetActorsOnTiles(basicAttackTargetTiles);
        for (int i = 0; i < targetActors.Count; i++)
        {
            // Don't Hit Teammates Or The Target Twice.
            if (targetActors[i].GetTeam() == attacker.GetTeam() || targetActors[i] == defender){continue;}
            map.UpdateCombatLog(attacker.GetPersonalName() + " hits " + targetActors[i].GetPersonalName() + ".");
            attackManager.ActorAttacksActorWithAttackSpeed(attacker, targetActors[i], map, attacker.GetBasicAttackMultiplier(), attacker.GetBasicAttackDamageType());
        }
        map.UpdateMap();
    }
    public void ActivateSkill(string skillName, TacticActor actor, int targetTile)
    {
        activeManager.SetSkillFromName(skillName, actor);
        activeManager.GetTargetedTiles(targetTile);
        map.UpdateCombatLog(actor.GetPersonalName() + " uses " + skillName + ".");
        activeManager.ActivateSkill(this);
    }
    public void ActivateSkill(string skillName, TacticActor actor = null)
    {
        if (actor == null){actor = roundActors[currentTurn];}
        activeManager.ActivateSkill(this);
    }
    // Checks And Activates Death Effects.
    public void ActivateDeathPassive(TacticActor actor)
    {
        if (actor.GetHealth() > 0){return;}
        if (actor.Resurrect())
        {
            actor.FullRestore();
            map.ResurrectActor(actor);
            return;
        }
        if (!map.RemoveActorFromBattle(actor)){return;}
        List<string> deathActives = new List<string>(actor.GetDeathActives());
        for (int i = 0; i < deathActives.Count; i++)
        {
            if (deathActives[i].Length <= 0) { continue; }
            activeManager.SetSkillFromName(deathActives[i], actor);
            activeManager.GetTargetedTiles(actor.GetLocation());
            ActivateSkill(deathActives[i], actor);
        }
    }
}
