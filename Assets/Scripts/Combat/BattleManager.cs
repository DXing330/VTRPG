using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    public PartyDataManager partyData;
    public BattleState battleState;
    public BattleMap map;
    public ActorAI actorAI;
    public bool setStartingPositions = false;
    public bool autoBattle = false;
    public bool controlAI = false;
    public void SetControlAI(bool newInfo)
    {
        controlAI = newInfo;
    }
    public void SetAutoBattle(bool newInfo)
    {
        autoBattle = newInfo;
    }
    public bool pause = false;
    public void PauseButton()
    {
        if (pause)
        {
            int winningTeam = FindWinningTeam();
            if (winningTeam >= 0)
            {
                return;
            }
            pause = false;
            NextTurn();
        }
        else
        {
            pause = true;
        }
    }
    public ActorMaker actorMaker;
    public InteractableMaker interactableMaker;
    public BattleMapFeatures battleMapFeatures;
    public InitiativeTracker initiativeTracker;
    public TurnOrderManager turnOrderManager;
    public CombatLog combatLog;
    public BattleStatsTracker battleStatsTracker;
    public PopUpMessage popUpMessage;
    public CharacterList playerParty;
    public CharacterList enemyParty;
    public EffectManager effectManager;
    public ActiveManager activeManager;
    public MoveCostManager moveManager;
    public AttackManager attackManager;
    public BattleEndManager battleEndManager;
    public int FindWinningTeam()
    {
        return battleEndManager.FindWinningTeam(map, battleState);
    }
    public GameObject autoWinButton;
    protected void EndBattle(int winningTeam, bool autoWin = false)
    {
        autoWinButton.SetActive(true);
        pause = true;
        StopAllCoroutines();
        if (autoWin)
        {
            combatLog.UpdateNewestLog("Automatically Ending The Battle");
            battleEndManager.EndBattle(winningTeam);
            return;
        }
        if (!battleEndManager.test)
        {
            battleState.SetWinningTeam(winningTeam);
        }
        combatLog.UpdateNewestLog("Team "+winningTeam+" wins.");
        battleEndManager.UpdatePartyAfterBattle(map, winningTeam);
        combatLog.UpdateNewestLog("Finished updating team stats.");
        battleEndManager.EndBattle(winningTeam);
    }
    public BattleUIManager UI;
    public void RefreshUI()
    {
        UI.UpdatePinnedView();
        UI.UpdateTurnOrder();
    }
    public void ForceStart()
    {
        Start();
    }
    public BattleStartManager startManager;
    public bool roguelikeBattle = false;
    protected void Start()
    {
        // Initialize Map/Teams
        startManager.InitializeMap(map, this, roguelikeBattle);
        battleStatsTracker.InitializeTracker(map.battlingActors);
        // Apply start of battle passives.
        for (int i = 0; i < map.battlingActors.Count; i++)
        {
            effectManager.StartBattle(map.battlingActors[i]);
        }
        // If the battle state records a win, then auto end the battle.
        if (battleState.GetWinningTeam() >= 0 && !battleEndManager.test)
        {
            EndBattle(battleState.GetWinningTeam(), true);
            return;
        }
        // Start the combat.
        if (!setStartingPositions)
        {
            map.RandomAllyStartingPositions(battleState.GetAllySpawnPattern());
            NextRound();
            ChangeTurn();
            if (autoBattle) { NPCTurn(); }
            else if (turnActor.GetTeam() > 0 && !controlAI) { NPCTurn(); }
        }
        else
        {
            // Update the UI so that you can start the battle after you finish setting positions.
            UI.AdjustStartingPositions();
            map.UpdateStartingPositionTiles(battleState.GetAllySpawnPattern());
        }
    }
    public void FinishSettingStartingPositions()
    {
        setStartingPositions = false;
        UI.FinishSettingStartingPositions();
        map.ResetHighlights();
        NextRound();
        ChangeTurn();
        if (autoBattle) { NPCTurn(); }
        else if (turnActor.GetTeam() > 0 && !controlAI) { NPCTurn(); }
    }
    public void SpawnAndAddActor(int location, string actorName, int team = 0, TacticActor summoner = null)
    {
        TacticActor newActor = actorMaker.SpawnActor(location, actorName, team);
        map.AddActorToBattle(newActor);
        ApplyBattleModifiersToActor(newActor);
        effectManager.SummonedStartBattle(newActor);
        if (summoner != null)
        {
            summoner.AddSummonedActor(newActor);
        }
    }
    protected void ApplyBattleModifiersToActor(TacticActor actor)
    {
        List<TacticActor> actors = new List<TacticActor>();
        actors.Add(actor);
        int team = actor.GetTeam();
        if (team > 0)
        {
            actorMaker.ApplyBattleModifiers(actors, enemyParty.GetBattleModifiers());
        }
        else
        {
            actorMaker.ApplyBattleModifiers(actors, playerParty.GetBattleModifiers());
        }
    }
    public bool interactable = true;
    public float delayTime = 0.1f;
    public int roundNumber;
    public int GetRoundNumber(){return roundNumber;}
    public int turnNumber;
    public int GetTurnIndex(){return turnNumber;}
    // Snapshot of actors eligible to act this round; map.battlingActors remains the live roster.
    public List<TacticActor> roundTurnOrder = new List<TacticActor>();
    public List<TacticActor> GetRoundTurnOrder(){return roundTurnOrder;}
    public TacticActor turnActor;
    public TacticActor GetTurnActor(){return turnActor;}
    public bool ValidTurnActor(TacticActor actor)
    {
        return actor != null
            && actor.GetHealth() > 0
            && map.battlingActors.Contains(actor);
    }
    protected bool TryFindNextValidTurn()
    {
        for (int i = turnNumber; i < roundTurnOrder.Count; i++)
        {
            if (!ValidTurnActor(roundTurnOrder[i])){continue;}
            turnNumber = i;
            return true;
        }
        return false;
    }
    protected void NextRound()
    {
        combatLog.AddNewLog();
        // Update terrain effects/weather interactions/delayed/etc.
        map.NextRound();
        map.RemoveActorsFromBattle();
        int winningTeam = FindWinningTeam();
        if (winningTeam >= 0)
        {
            combatLog.UpdateNewestLog("Ending Battle By Map Effects");
            EndBattle(winningTeam);
            return;
        }
        turnNumber = 0;
        selectedActor = null;
        roundNumber++;
        map.SetRound(roundNumber);
        // Copy at round start so summons/revivals wait until the next round.
        roundTurnOrder = initiativeTracker.SortActors(new List<TacticActor>(map.battlingActors));
    }
    // Updates stats UI inbetween turns.
    // Also applies new turn effects to the next actor.
    protected void ChangeTurn()
    {
        endingTurn = false;
        combatLog.AddNewLog();
        if (map.battlingActors.Count <= 0 && roundNumber > 1)
        {
            combatLog.UpdateNewestLog("Everyone is Dead");
            // End the battle immediately.
            int winningTeam = FindWinningTeam();
            EndBattle(winningTeam);
            return;
        }
        if (!TryFindNextValidTurn())
        {
            NextRound();
            if (FindWinningTeam() >= 0)
            {
                return;
            }
            if (!TryFindNextValidTurn())
            {
                combatLog.UpdateNewestLog("Everyone is Dead");
                int winningTeam = FindWinningTeam();
                EndBattle(winningTeam);
                return;
            }
        }
        turnActor = roundTurnOrder[turnNumber];
        turnActor.NewTurn();
        combatLog.UpdateNewestLog(turnActor.GetPersonalName() + "'s Turn");
        // Apply Conditions/Passives.
        effectManager.StartTurn(turnActor, map);
        RefreshUI();
        if (turnActor.GetHealth() <= 0)
        {
            ActiveDeathPassives(turnActor);
            NextTurn();
            return;
        }
    }
    protected float clickNextTurnTime;
    protected float enemyTurnMaxDurations = 3f;
    public void ManualNextTurn()
    {
        if (pause)
        {
            return;
        }
        // Can't go to next turn if not all actions are spent?
        if (turnActor.GetTeam() != 0 && turnActor.GetActions() > 0 && clickNextTurnTime < 0f)
        {
            clickNextTurnTime = Time.time;
            return;
        }
        else if (turnActor.GetTeam() != 0 && turnActor.GetActions() > 0 && clickNextTurnTime > 0f)
        {
            if (Time.time > clickNextTurnTime + enemyTurnMaxDurations)
            {
                EndTurn();
                return;                
            }
        }
        NextTurn();
    }
    public void NextTurn()
    {
        clickNextTurnTime = -1f;
        int winningTeam = FindWinningTeam();
        if (winningTeam >= 0)
        {
            combatLog.UpdateNewestLog("Ending Battle By Clicking Next Turn");
            EndBattle(winningTeam);
            return;
        }
        turnActor.EndTurn(); // End turn first before passives apply, so that end of turn buffs can stick around til the next round.
        effectManager.EndTurn(turnActor, map);
        // This allows for a one turn grace period for immunities to have a chance.
        map.ApplyEndTerrainEffect(turnActor);
        // Remove dead actors.
        map.RemoveActorsFromBattle();
        winningTeam = FindWinningTeam();
        if (winningTeam >= 0)
        {
            combatLog.UpdateNewestLog("Ending Battle By Clicking Next Turn");
            EndBattle(winningTeam);
            return;
        }
        turnNumber++;
        if (!TryFindNextValidTurn())
        {
            NextRound();
            if (FindWinningTeam() >= 0)
            {
                return;
            }
        }
        ChangeTurn();
        ResetState();
        UI.PlayerTurn();
        // Check for mental conditions.
        string mentalState = turnActor.GetMentalState();
        switch (mentalState)
        {
            case "Terrified":
                combatLog.UpdateNewestLog(turnActor.GetPersonalName() + " is Terrified.");
                UI.NPCTurn();
                TerrifiedTurn(turnActor.GetActions());
                return;
            case "Enraged":
                combatLog.UpdateNewestLog(turnActor.GetPersonalName() + " is Enraged.");
                UI.NPCTurn();
                EnragedTurn(turnActor.GetActions());
                return;
            case "Charmed":
                combatLog.UpdateNewestLog(turnActor.GetPersonalName() + " is Charmed.");
                UI.NPCTurn();
                CharmedTurn(turnActor.GetActions());
                return;
            case "Taunted":
                combatLog.UpdateNewestLog(turnActor.GetPersonalName() + " is Taunted.");
                UI.NPCTurn();
                TauntedTurn(turnActor.GetActions());
                return;
            case "Confused":
                combatLog.UpdateNewestLog(turnActor.GetPersonalName() + " is Confused.");
                UI.NPCTurn();
                ConfusedTurn(turnActor.GetActions());
                return;
        }
        if (autoBattle || turnActor.UncontrolledSummon() || turnActor.GetTeam() > 0 && !controlAI){NPCTurn();}
    }
    protected void NPCTurn()
    {
        UI.NPCTurn();
        int actionsLeft = turnActor.GetActions();
        if (actionsLeft <= 0 || turnActor.GetHealth() <= 0)
        {
            EndTurn();
            return;
        }
        else if (actorAI.BossTurn(turnActor))
        {
            BossTurn(actionsLeft);
        }
        // This always calls an end turn.
        else if (!actorAI.NormalTurn(turnActor, roundNumber, map, moveManager))
        {
            NPCSkillAction(actionsLeft);
        }
        // This always calls an end turn.
        else
        {
            BasicNPCAction();
        }
    }
    bool endingTurn = false;
    protected void EndTurn()
    {
        if (endingTurn)
        {
            return;
        }
        endingTurn = true;
        ResetState();
        NextTurn();
    }
    // None, Move, Attack, SkillSelect, SkillTargeting, Viewing
    public string selectedState;
    public string GetState(){ return selectedState; }
    public void SetState(string newState)
    {
        map.UpdateMap();
        // Only some states reset when double clicked.
        if (newState == selectedState && selectedState == "Move")
        {
            ResetState();
            return;
        }
        // The only action you can take without actionsleft is moving, assuming you have movement remaining.
        else if (newState != "Move" && !turnActor.ActionsLeft())
        {
            ResetState();
            return;
        }
        selectedState = newState;
        switch (selectedState)
        {
            case "Move":
                if (turnActor.GetSpeed() <= 0)
                {
                    popUpMessage.SetMessage("No Movespeed");
                    ResetState();
                    return;
                }
                StartMoving();
                break;
            case "Attack":
                StartAttacking();
                break;
            case "Target":
                StartTargeting();
                break;
            // Spells/Items/Actives are the same.
            default:
                map.ResetHighlights();
                break;
        }
    }
    public int selectedTile;
    public TacticActor selectedActor;
    public TacticActor GetSelectedActor()
    {
        if (selectedActor == null)
        {
            return turnActor;
        }
        return selectedActor;
    }

    public void ResetState()
    {
        selectedState = "";
        selectedTile = -1;
        selectedActor = null;
        map.ResetHighlights();
        map.UpdateMap();
        UI.ResetActiveSelectList();
        ResetConfirmPanels();
        RefreshUI();
    }

    public int prevStartingPosition = -1;
    protected void AdjustStartingPosition(int tileNumber)
    {
        // Can only set the actors in the first few columns.
        if (!map.ValidStartingTile(battleState.GetAllySpawnPattern(), tileNumber))
        {
            return;
        }
        // Start selecting.
        if (prevStartingPosition < 0 && map.TileNotEmpty(tileNumber))
        {
            prevStartingPosition = tileNumber;
            selectedActor = map.GetActorOnTile(tileNumber);
            UI.UpdatePinnedView();
        }
        // Move a selected actor into a new location.
        else if (prevStartingPosition >= 0)
        {
            // Move the actor.
            map.ChangeActorsLocation(prevStartingPosition, tileNumber);
            prevStartingPosition = -1;
        }
    }

    public void ResetConfirmPanels()
    {
        confirmMovePanel.SetActive(false);
        confirmAttackPanel.SetActive(false);
        confirmTargetPanel.SetActive(false);
    }
    public GameObject confirmMovePanel;
    public GameObject confirmAttackPanel;
    public GameObject confirmTargetPanel;
    public void ConfirmMovementToTile()
    {
        if (selectedState != "Move"){return;}
        if (selectedTile < 0){return;}
        MoveToTile(selectedTile);
    }
    public void ConfirmAttack()
    {
        if (selectedState != "Attack"){return;}
        if (selectedTile < 0){return;}
        AttackTile(selectedTile);
        if (selectedState == "Attack")
        {
            StartAttacking();
        }
    }
    public void ConfirmTarget()
    {
        if (selectedState != "Target"){return;}
        if (selectedTile < 0){return;}
        turnActor.SetTarget(map.GetActorOnTile(selectedTile));
        ResetState();
    }
    public void ClickOnTile(int tileNumber)
    {
        // UI takes priority over gameplay.
        if (UI.ViewingDetails())
        {
            UI.ViewMapPassives(map, tileNumber);
            return;
        }
        if (setStartingPositions)
        {
            AdjustStartingPosition(tileNumber);
            map.UpdateStartingPositionTiles(battleState.GetAllySpawnPattern(), prevStartingPosition);
            return;
        }
        if (!interactable){return;}
        selectedTile = map.currentTiles[tileNumber];
        // First make sure your tile is a valid tile.
        if (selectedTile < 0)
        {
            ResetState();
            return;
        }
        // Then do something depending on the state.
        switch (selectedState)
        {
            case "Move":
            // Confirm movement through a menu first, this will eliminate misclicks.
            int indexOf = moveManager.reachableTiles.IndexOf(selectedTile);
            if (indexOf < 0){return;}
            map.UpdateMovingPath(turnActor, moveManager, selectedTile);
            break;
            case "Attack":
            if (!turnActor.ActionsLeft()){break;}
            // Highlight the selected tile.
            map.UpdateSelectedAttackTile(turnActor, selectedTile);
            // Preview the battle details.
            UI.PreviewBattleStats(turnActor, map.GetActorOnTile(selectedTile));
            break;
            case "Target":
            map.UpdateSelectedAttackTile(turnActor, selectedTile);
            UI.PreviewTarget(map.GetActorOnTile(selectedTile));
            break;
            case "":
            ViewActorOnTile(selectedTile);
            return;
            case "View":
            ViewActorOnTile(selectedTile);
            return;
            case "Skill":
            // Target the tile and update the targeted tiles.
            if (!activeManager.ReturnTargetableTiles().Contains(selectedTile)){return;}
            activeManager.GetTargetedTiles(selectedTile, moveManager.actorPathfinder);
            map.UpdateHighlights(activeManager.targetedTiles, "Attack", 4);
            break;
            case "Spell":
            // Target the tile and update the targeted tiles.
            if (!activeManager.ReturnTargetableTiles().Contains(selectedTile)){return;}
            activeManager.GetTargetedTiles(selectedTile, moveManager.actorPathfinder, true);
            map.UpdateHighlights(activeManager.targetedTiles, "Attack", 4);
            break;
            case "Item":
            // Target the tile and update the targeted tiles.
            if (!activeManager.ReturnTargetableTiles().Contains(selectedTile)){return;}
            activeManager.GetTargetedTiles(selectedTile, moveManager.actorPathfinder);
            map.UpdateHighlights(activeManager.targetedTiles, "Attack", 4);
            break;
        }
        RefreshUI();
    }

    public void ViewActorFromTurnOrder(int index)
    {
        selectedActor = map.GetActorByIndex(index + turnNumber);
        if (selectedActor == null){return;}
        ViewActorOnTile(selectedActor.GetLocation());
    }

    public void ViewTargetedActorFromTurnOrder(int index)
    {
        selectedActor = map.GetActorByIndex(index + turnNumber).GetTarget();
        if (selectedActor == null){return;}
        ViewActorOnTile(selectedActor.GetLocation());
    }

    protected void ViewActorOnTile(int tileNumber)
    {
        selectedActor = map.GetActorOnTile(tileNumber);
        if (selectedActor == null){return;}
        else
        {
            RefreshUI();
            map.UpdateMovingHighlights(selectedActor, moveManager, selectedActor == turnActor);
        }
    }

    protected void StartTargeting()
    {
        map.ResetHighlights();
        UI.PreviewTarget();
        map.UpdateSelectedAttackTile(turnActor, selectedTile);
    }

    protected void StartAttacking()
    {
        map.ResetHighlights();
        UI.PreviewBattleStats(turnActor);
        map.UpdateSelectedAttackTile(turnActor, selectedTile);
    }

    protected void AttackTile(int tileNumber)
    {
        List<int> attackableTiles = map.GetAttackableTiles(turnActor);
        int indexOf = attackableTiles.IndexOf(tileNumber);
        // Don't pay if you didn't select a tile.
        if (indexOf < 0)
        {
            ResetState();
            return;
        }
        selectedActor = map.GetActorOnTile(tileNumber);
        if (selectedActor == null && map.InteractableOnTile(tileNumber))
        {
            map.AttackInteractable(tileNumber, turnActor);
            if (AdjustTurnNumber())
            {
                return;
            }
            map.UpdateMap();
            return;
        }
        else if (selectedActor == null && !map.InteractableOnTile(tileNumber))
        {
            // Pay the cost even if nothing is there, since you have to confirm the attack tile.
            turnActor.PayAttackCost();
            ResetState();
            return;
        }
        ActorAttacksActor(turnActor, selectedActor);
    }

    public void PublicAAA(TacticActor attacker, TacticActor defender, bool payCost = true)
    {
        ActorAttacksActor(attacker, defender, payCost);
    }

    // ALL/Only Basic Attacks Go Through Here, Skills Go Through Active Manager
    protected void ActorAttacksActor(TacticActor attacker, TacticActor defender, bool payCost = true)
    {
        if (payCost)
        {
            if (!attacker.AttackActionsLeft()){return;}
            attacker.PayAttackCost();
        }
        combatLog.UpdateNewestLog(attacker.GetPersonalName() + " attacks " + defender.GetPersonalName() + ".");
        attackManager.ActorAttacksActor(attacker, defender, map, attacker.GetBasicAttackMultiplier());
        if (AdjustTurnNumber())
        {
            return;
        }
        // After you finish attacking reset the selected actor.
        selectedActor = null;
        if (selectedState == "Attack" && attacker.GetActions() <= 0 && attacker.GetTeam() == 0)
        {
            ResetState();
        }
        map.UpdateMap();
        RefreshUI();
    }
    
    protected void StartMoving()
    {
        map.ResetHighlights();
        map.UpdateMovingHighlights(turnActor, moveManager);
        moveManager.GetAllReachableTiles(turnActor, map.battlingActors);
    }

    protected void MoveToTile(int tileNumber)
    {
        int indexOf = moveManager.reachableTiles.IndexOf(tileNumber);
        // For now you can't move into other actors.
        if (indexOf < 0 || map.GetActorOnTile(tileNumber) != null)
        {
            ResetState();
            return;
        }
        else
        {
            List<int> path = moveManager.GetPrecomputedPath(turnActor.GetLocation(), tileNumber);
            // Need to change the character's direction.
            MoveAlongPath(turnActor, path);
        }
    }

    protected void BossTurn(int actionsLeft, string turnAction = "", string turnSpecifics = "")
    {
        List<string> turnDetails = actorAI.ReturnBossActions(turnActor, map);
        string chosenAction = turnDetails[0];
        string chosenSpecifics = turnDetails[1];
        if (turnAction != "")
        {
            chosenAction = turnAction;
        }
        if (turnSpecifics != "")
        {
            chosenSpecifics = turnSpecifics;
        }
        // Some things you can do without actions.
        switch (chosenAction)
        {
            // LOOP and try to find the next rotation.
            case "Change Form":
                // Update base stats based on new form.
                actorMaker.ChangeActorForm(turnActor, chosenSpecifics);
                BossTurn(actionsLeft);
                // This will always take all your actions.
                //turnActor.ResetActions();
                return;
            case "One Time Spell":
                turnActor.IncrementCounter();
                NPCSpellAction(actionsLeft, chosenSpecifics);
                return;            
            case "One Time Skill":
                turnActor.IncrementCounter();
                NPCSkillAction(actionsLeft, chosenSpecifics);
                return;
            case "One Time Chain Skill":
                turnActor.IncrementCounter();
                string[] OTchainSkills = chosenSpecifics.Split(",");
                NPCChainSkillActions(OTchainSkills);
                return;
            case "Spell":
                if (!TryNPCSpellOnce(chosenSpecifics))
                {
                    BasicNPCAction();
                    return;
                }
                if (turnActor.GetActions() > 0 && turnActor.GetHealth() > 0)
                {
                    BossTurn(turnActor.GetActions());
                    return;
                }
                EndTurn();
                return;
            case "Skill":
                if (!TryNPCSkillOnce(chosenSpecifics))
                {
                    BasicNPCAction();
                    return;
                }
                if (turnActor.GetActions() > 0 && turnActor.GetHealth() > 0)
                {
                    BossTurn(turnActor.GetActions());
                    return;
                }
                EndTurn();
                return;
            case "Summon Skill":
                string summonSkill = actorAI.ReturnSkillWithEffect(turnActor, map, "Summon");
                if (summonSkill == "")
                {
                    BasicNPCAction();
                    return;
                }
                BossTurn(actionsLeft, "Skill", summonSkill);
                return;
            case "Summon Spell":
                string summonSpell = actorAI.ReturnSpellWithEffect(turnActor, map, "Summon");
                if (summonSpell == "")
                {
                    BasicNPCAction();
                    return;
                }
                BossTurn(actionsLeft, "Spell", summonSpell);
                return;
            case "MoveToTile":
                // Move to the closest tile of type.
                int tile = map.ReturnClosestTileOfType(turnActor, chosenSpecifics);
                if (tile < 0)
                {
                    BasicNPCAction();
                    return;
                }
                else
                {
                    List<int> path = actorAI.FindPathToTile(turnActor, map, moveManager, tile);
                    MoveAlongPath(turnActor, path);
                    if (turnActor.GetActions() > 0)
                    {
                        BossTurn(turnActor.GetActions());
                        return;
                    }
                    else
                    {
                        break;
                    }
                }
            case "MoveToSandwichTarget":
                int sandwichingTile = map.ReturnClosestSandwichTargetBetweenTileOfType(turnActor, chosenSpecifics);
                if (sandwichingTile < 0)
                {
                    BasicNPCAction();
                    return;
                }
                else
                {
                    List<int> path = actorAI.FindPathToTile(turnActor, map, moveManager, sandwichingTile);
                    MoveAlongPath(turnActor, path);
                    if (turnActor.GetActions() > 0)
                    {
                        BossTurn(turnActor.GetActions());
                        return;
                    }
                    else
                    {
                        break;
                    }
                }
            case "MoveToSandwichTile":
                int sandwichedTile = map.ReturnClosestTileSandwiched(turnActor, chosenSpecifics);
                if (sandwichedTile < 0)
                {
                    BasicNPCAction();
                    return;
                }
                else
                {
                    List<int> path = actorAI.FindPathToTile(turnActor, map, moveManager, sandwichedTile);
                    MoveAlongPath(turnActor, path);
                    if (turnActor.GetActions() > 0)
                    {
                        BossTurn(turnActor.GetActions());
                        return;
                    }
                    else
                    {
                        break;
                    }
                }
            // STOP And Basic For The Rest Of The Turn.
            case "Split":
                // Change base health to be the same as current health.
                turnActor.SetBaseHealth(turnActor.GetHealth());
                // Check if there are any empty adjacent tiles.
                int splitTile = map.ReturnRandomAdjacentEmptyTile(turnActor.GetLocation());
                // If can't split then just do normal turn.
                if (splitTile < 0)
                {
                    break;
                }
                // Create a copy in a random adjacent empty tile.
                TacticActor clonedActor = actorMaker.CloneActor(turnActor, splitTile);
                map.AddActorToBattle(clonedActor);
                ApplyBattleModifiersToActor(clonedActor);
                // This will always take all your actions.
                turnActor.ResetActions();
                break;


            case "Chain Skill":
                string[] chainSkills = chosenSpecifics.Split(",");
                NPCChainSkillActions(chainSkills);
                return;
            case "Random Skill":
                string[] skills = chosenSpecifics.Split(",");
                string chosenSkill = skills[Random.Range(0, skills.Length)];
                if (chosenSkill == "None")
                {
                    BasicNPCAction();
                }
                else
                {
                    NPCSkillAction(actionsLeft, chosenSkill);
                }
                return;
            
            case "Basic":
                BasicNPCAction();
                return;
        }
        EndTurn();
    }

    protected void NPCChainSkillActions(string[] skills)
    {
        for (int i = 0; i < skills.Length; i++)
        {
            activeManager.SetSkillFromName(skills[i], turnActor);
            int targetedTile = actorAI.ChooseSkillTargetLocation(turnActor, map, moveManager);
            if (targetedTile == -1 || !activeManager.CheckSkillCost(map))
            {
                BasicNPCAction();
                return;
            }
            activeManager.GetTargetedTiles(targetedTile, moveManager.actorPathfinder);
            // If the skill has no valid targets in the case of an AOE, then just do a normal action.
            if (!actorAI.ValidSkillTargets(turnActor, map, activeManager))
            {
                BasicNPCAction();
                return;
            }
            ActivateSkill(skills[i]);
            if (AdjustTurnNumber())
            {
                return;
            }
        }
        EndTurn();
    }

    protected bool TryNPCSpellOnce(string spell)
    {
        activeManager.SetSkillUser(turnActor);
        activeManager.SetSpell(spell);
        int targetedTile = actorAI.ChooseSpellTargetLocation(turnActor, map, moveManager, activeManager.magicSpell);
        if (targetedTile == -1 || !activeManager.CheckSpellCost(map))
        {
            return false;
        }
        activeManager.GetTargetedTiles(targetedTile, moveManager.actorPathfinder, true);
        if (!actorAI.ValidSkillTargets(turnActor, map, activeManager, true))
        {
            return false;
        }
        ActivateSpell();
        AdjustTurnNumber();
        return true;
    }

    protected void NPCSpellAction(int actionsLeft, string spell)
    {
        for (int i = 0; i < actionsLeft; i++)
        {
            if (!TryNPCSpellOnce(spell))
            {
                BasicNPCAction();
                return;
            }
            if (turnActor.GetActions() <= 0 || turnActor.GetHealth() <= 0)
            {
                EndTurn();
                return;
            }
        }
        EndTurn();
    }

    protected bool TryNPCSkillOnce(string skill)
    {
        // Get the active and the targeted tile.
        string skillToUse = skill;
        if (skillToUse == "")
        {
            skillToUse = actorAI.ReturnAIActiveSkill();
        }
        activeManager.SetSkillFromName(skillToUse, turnActor);
        // Choose Target.
        int targetedTile = actorAI.ChooseSkillTargetLocation(turnActor, map, moveManager);
        // Check Cost & Target.
        if (targetedTile == -1 || !activeManager.CheckSkillCost(map))
        {
            return false;
        }
        // Check Target Appropriate
        activeManager.GetTargetedTiles(targetedTile, moveManager.actorPathfinder);
        // If the skill has no valid targets in the case of an AOE, let caller decide fallback.
        if (!actorAI.ValidSkillTargets(turnActor, map, activeManager))
        {
            return false;
        }
        ActivateSkill(skillToUse);
        // Preserve the same battle-end / actor-death handling order.
        AdjustTurnNumber();
        return true;
    }

    protected void NPCSkillAction(int actionsLeft, string skill = "")
    {
        for (int i = 0; i < actionsLeft; i++)
        {
            // If you can't use the skill then just do a basic action.
            if (!TryNPCSkillOnce(skill))
            {
                BasicNPCAction();
                return;
            }
            if (turnActor.GetActions() <= 0 || turnActor.GetHealth() <= 0)
            {
                EndTurn();
                return;
            }
        }
        EndTurn();
    }

    protected void TerrifiedTurn(int actionsLeft)
    {
        for (int i = 0; i < actionsLeft; i++)
        {
            // Get the closest enemy.
            moveManager.GetAllMoveCosts(turnActor, map.battlingActors);
            TacticActor closestEnemy = actorAI.GetClosestEnemy(map.battlingActors, turnActor, moveManager);
            if (closestEnemy == null)
            {
                // No more enemies, just end turn.
                EndTurn();
                return;
            }
            turnActor.SetTarget(closestEnemy);
            // Move away from them the target.
            moveManager.GetAllMoveCosts(turnActor, map.battlingActors);
            List<int> path = actorAI.FindPathAwayFromTarget(turnActor, map, moveManager);
            MoveAlongPath(turnActor, path);
            if (turnActor.GetActions() <= 0 || turnActor.GetHealth() <= 0)
            {
                EndTurn();
                return;
            }
        }
        EndTurn();
    }

    protected void EnragedTurn(int actionsLeft)
    {
        for (int i = 0; i < actionsLeft; i++)
        {
            // Pick the closest target no matter what.
            moveManager.GetAllMoveCosts(turnActor, map.battlingActors);
            TacticActor newTarget = actorAI.GetClosestEnemy(map.battlingActors, turnActor, moveManager, true);
            if (newTarget == null)
            {
                // No more enemies, just end turn.
                EndTurn();
                return;
            }
            turnActor.SetTarget(newTarget);
            // Attack them if you can.
            // You can use a random skill if possible.
            if (actorAI.EnemyInAttackRange(turnActor, turnActor.GetTarget(), map)) { NPCAttackAction(true); }
            // Else move towards the target.
            else
            {
                if (AIPathToTarget())
                {
                    EndTurn();
                    return;
                }
            }
            if (turnActor.GetActions() <= 0 || turnActor.GetHealth() <= 0)
            {
                EndTurn();
                return;
            }
        }
        // Reset targets after you stop raging.
        turnActor.ResetTarget();
        EndTurn();
    }

    protected void CharmedTurn(int actionsLeft)
    {
        for (int i = 0; i < actionsLeft; i++)
        {
            if (turnActor.GetTarget() == null || turnActor.GetTarget().GetHealth() <= 0)
            {
                moveManager.GetAllMoveCosts(turnActor, map.battlingActors);
                TacticActor closestEnemy = actorAI.GetClosestEnemy(map.battlingActors, turnActor, moveManager);
                if (closestEnemy == null)
                {
                    // No more enemies, just end turn.
                    EndTurn();
                    return;
                }
                turnActor.SetTarget(closestEnemy);
            }
            // Move towards the target.
            moveManager.GetAllMoveCosts(turnActor, map.battlingActors);
            if (AIPathToTarget())
            {
                EndTurn();
                return;
            }
            if (turnActor.GetActions() <= 0 || turnActor.GetHealth() <= 0)
            {
                EndTurn();
                return;
            }
        }
        EndTurn();
    }

    protected void TauntedTurn(int actionsLeft)
    {
        for (int i = 0; i < actionsLeft; i++)
        {
            moveManager.GetAllMoveCosts(turnActor, map.battlingActors);
            // Find a new target if needed.
            if (turnActor.GetTarget() == null || turnActor.GetTarget().GetHealth() <= 0)
            {
                TacticActor closestEnemy = actorAI.GetClosestEnemy(map.battlingActors, turnActor, moveManager);
                if (closestEnemy == null)
                {
                    // No more enemies, just end turn.
                    EndTurn();
                    return;
                }
                turnActor.SetTarget(closestEnemy);
            }
            // If they can be attacked without moving then attack.
            if (actorAI.EnemyInAttackRange(turnActor, turnActor.GetTarget(), map))
            {
                ActorAttacksActor(turnActor, turnActor.GetTarget());
            }
            // Otherwise move.
            else
            {
                moveManager.GetAllMoveCosts(turnActor, map.battlingActors);
                if (AIPathToTarget())
                {
                    EndTurn();
                    return;
                }
            }
            if (turnActor.GetActions() <= 0 || turnActor.GetHealth() <= 0)
            {
                EndTurn();
                return;
            }
        }
        EndTurn();
    }

    protected void ConfusedTurn(int actionsLeft)
    {
        // Pretend to be a random other status.
        int randomTurnType = Random.Range(0, 3);
        switch (randomTurnType)
        {
            case 0:
                TerrifiedTurn(actionsLeft);
                return;
            case 1:
                EnragedTurn(actionsLeft);
                return;
            case 2:
                CharmedTurn(actionsLeft);
                return;
        }
        EndTurn();
    }

    protected void BasicNPCAction()
    {
        // Summons break command on their first turn, then act normally.
        if (turnActor.summoned && turnActor.summonedBy != null)
        {
            turnActor.ResetSummonedBy();
        }
        // This will make sure the path costs are up to date for AI action calculations.
        moveManager.GetAllMoveCosts(turnActor, map.battlingActors);
        // Find a new target if needed.
        // Don't hit your allies even if they hit you.
        if (turnActor.GetTarget() == null || turnActor.GetTarget().GetHealth() <= 0 || turnActor.GetTarget().invisible || turnActor.GetTarget().GetTeam() == turnActor.GetTeam())
        {
            TacticActor closestEnemy = actorAI.GetClosestEnemy(map.battlingActors, turnActor, moveManager);
            if (closestEnemy == null)
            {
                // No more enemies, just end turn.
                EndTurn();
                return;
            }
            turnActor.SetTarget(closestEnemy);
        }
        // If they can be attacked without moving then attack.
        if (actorAI.EnemyInAttackRange(turnActor, turnActor.GetTarget(), map) && turnActor.AttackActionsLeft())
        {
            NPCAttackAction();
        }
        // Else Check If Target In Move + Attack Range.
        else if (actorAI.EnemyInAttackableRange(turnActor, turnActor.GetTarget(), map, moveManager))
        {
            if (AIPathToTarget())
            {
                EndTurn();
                return;
            }
            NPCAttackAction();
        }
        // Else Try To Stay Out Of Enemy Attack Range.
        else
        {
            AIPathTowardTarget();
        }
        // Always end turn at the end.
        EndTurn();
    }

    protected void NPCAttackAction(bool randomSkill = false)
    {
        string attackActive = actorAI.ReturnAIAttackSkill(turnActor);
        if (randomSkill && turnActor.GetActiveSkills().Count > 0)
        {
            List<string> turnActorSkills = turnActor.GetActiveSkills();
            string rSkill = turnActorSkills[Random.Range(0, turnActorSkills.Count)];
            if (activeManager.SkillExists(rSkill))
            {
                activeManager.SetSkillFromName(rSkill, turnActor);
                if (activeManager.active.GetSkillType() == "Damage")
                {
                    attackActive = rSkill;
                }
            }
        }
        if (activeManager.SkillExists(attackActive))
        {
            activeManager.SetSkillFromName(attackActive, turnActor);
            if (activeManager.CheckSkillCost(map))
            {
                int targetTile = turnActor.GetTarget().GetLocation();
                if (activeManager.active.GetRange() == 0)
                {
                    targetTile = turnActor.GetLocation();
                }
                activeManager.GetTargetedTiles(targetTile, moveManager.actorPathfinder);
                // Turn to face the target in case the skill is not a real attack or an AOE.
                turnActor.SetDirection(moveManager.DirectionBetweenActors(turnActor, turnActor.GetTarget()));
                ActivateSkill(attackActive);
                if (AdjustTurnNumber())
                {
                    return;
                }
            }
            else { ActorAttacksActor(turnActor, turnActor.GetTarget()); }
        }
        else { ActorAttacksActor(turnActor, turnActor.GetTarget()); }
        // If you can attack again then do so.
        if (turnActor.AttackActionsLeft() && turnActor.TargetValid())
        {
            NPCAttackAction(randomSkill);
        }
    }

    protected void DragGrappledActor(TacticActor grappled, int tile)
    {
        int prevLoc = grappled.GetLocation();
        moveManager.MoveActorToTile(grappled, tile, map);
        // Don't do this if two actors are grappling each other.
        if (grappled.Grappling() && grappled.GetGrappledActor() != grappled.GetGrappledByActor())
        {
            DragGrappledActor(grappled.GetGrappledActor(), prevLoc);
        }
    }

    protected void AIPathTowardTarget()
    {
        List<int> path = actorAI.FindPathToTarget(turnActor, map, moveManager);
        MoveTowardTarget(turnActor, path);
    }

    protected bool AIPathToTarget()
    {
        List<int> path = actorAI.FindPathToTarget(turnActor, map, moveManager);
        // End turn if invalid path.
        if (path.Count <= 0)
        {
            turnActor.ResetActions();
            return true;
        }
        MoveAlongPath(turnActor, path);
        return false;
    }

    protected void MoveTowardTarget(TacticActor actor, List<int> path)
    {
        // If you're already in attack range then just move along the path.
        List<int> targetAttackableTiles = map.GetAttackableTiles(actor.GetTarget());
        if (targetAttackableTiles.Contains(actor.GetLocation()))
        {
            MoveAlongPath(actor, path);
            return;
        }
        if (path.Count <= 0){return;}
        for (int i = path.Count - 1; i >= 0; i--)
        {
            if (targetAttackableTiles.Contains(path[i])){return;}
            int prevLoc = actor.GetLocation();
            actor.SetDirection(moveManager.DirectionBetweenLocations(prevLoc, path[i]));
            moveManager.MoveActorToTile(actor, path[i], map);
            actor.PayMoveCost(moveManager.MoveCostOfTile(path[i]));
            // A Grappled Actor Can't Start The Drag Chain
            if (actor.Grappling() && !actor.Grappled())
            {
                DragGrappledActor(actor.GetGrappledActor(), prevLoc);
            }
            if (actor.GetMovement() < 0)
            {
                break;
            }
        }
        map.UpdateActors();
        ResetState();
    }

    protected void MoveAlongPath(TacticActor actor, List<int> path)
    {
        if (path.Count <= 0){return;}
        for (int i = path.Count - 1; i >= 0; i--)
        {
            int prevLoc = actor.GetLocation();
            actor.SetDirection(moveManager.DirectionBetweenLocations(prevLoc, path[i]));
            // This can decrease movement based on some tile passives.
            moveManager.MoveActorToTile(actor, path[i], map);
            // Only pay the move cost here if you're moving along a path.
            actor.PayMoveCost(moveManager.MoveCostOfTile(path[i]));
            // A Grappled Actor Can't Start The Drag Chain
            if (actor.Grappling() && !actor.Grappled())
            {
                DragGrappledActor(actor.GetGrappledActor(), prevLoc);
            }
            if (actor.GetMovement() < 0)
            {
                // Stop if you run out of movement.
                break;
            }
        }
        map.UpdateActors();
        ResetState();
    }

    public bool AdjustTurnNumber()
    {
        map.RemoveActorsFromBattle();
        int winningTeam = FindWinningTeam();
        if (winningTeam >= 0)
        {
            EndBattle(winningTeam);
            return true;
        }
        // End the turn and stop anything if the turn actor died or was removed in any way.
        if (turnActor == null || turnActor.GetHealth() <= 0|| !map.battlingActors.Contains(turnActor))
        {
            EndTurn();
            return true;
        }
        return false;
    }

    public void ActivateSkill(string skillName, TacticActor actor = null)
    {
        ResetState();
        if (actor == null){actor = turnActor;}
        combatLog.UpdateNewestLog(actor.GetPersonalName() + " uses " + skillName + ".");
        activeManager.ActivateSkill(this);
        map.UpdateMap();
    }

    public void ActivateSpell(TacticActor actor = null)
    {
        ResetState();
        if (actor == null) { actor = turnActor; }
        activeManager.ActivateSpell(this);
        map.UpdateMap();
        combatLog.UpdateNewestLog(actor.GetPersonalName()+" casts  " + activeManager.magicSpell.GetSkillName());
    }

    public void ActiveDeathPassives(TacticActor actor)
    {
        List<string> deathActives = new List<string>(actor.GetDeathActives());
        for (int i = 0; i < deathActives.Count; i++)
        {
            if (deathActives[i].Length <= 0) { continue; }
            activeManager.SetSkillFromName(deathActives[i], actor);
            activeManager.GetTargetedTiles(actor.GetLocation(), moveManager.actorPathfinder);
            ActivateSkill(deathActives[i], actor);
        }
    }

    public void DEBUGAUTOWIN()
    {
        EndBattle(0, true);
    }

    public void Forfeit()
    {
        EndBattle(1, true);
    }
}
