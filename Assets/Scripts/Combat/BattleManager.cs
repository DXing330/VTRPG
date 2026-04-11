using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    protected bool loadedCustomBattleMap = false;
    protected List<int> customEnemyStartingLocations = new List<int>();
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
    protected void Start()
    {
        // Get a new battle map.
        map.ForceStart();
        combatLog.ForceStart();
        int partySizeCap = map.MapMaxPartyCapacity();
        combatLog.AddNewLog();
        UI.UpdateWinConString();
        map.SetWeather(battleState.GetWeather());
        combatLog.UpdateNewestLog("The weather is " + map.GetWeather());
        map.SetTime(battleState.GetTime());
        combatLog.UpdateNewestLog("The time is " + map.GetTime());
        loadedCustomBattleMap = TryLoadCustomBattleMap();
        if (!loadedCustomBattleMap)
        {
            // Always plains default map tile.
            map.GetNewMapFeatures(battleMapFeatures.CurrentMapFeatures());
            map.GetNewTerrainEffects(battleMapFeatures.CurrentMapTerrainFeatures());
            interactableMaker.GetNewInteractables(map, battleMapFeatures.CurrentMapInteractables());
            map.InitializeElevations();
        }
        moveManager.UpdateInfoFromBattleMap(map);
        actorMaker.SetMapSize(map.mapSize);
        // Spawn actors in patterns based on teams.
        List<TacticActor> actors = new List<TacticActor>();
        actors = actorMaker.SpawnTeamInPattern(battleState.GetAllySpawnPattern(), 0, playerParty.characters, playerParty.stats, playerParty.characterNames, playerParty.equipment, playerParty.characterIDs);
        actorMaker.ApplyBattleModifiers(actors, playerParty.GetBattleModifiers());
        for (int i = 0; i < Mathf.Min(partySizeCap, actors.Count); i++) { map.AddActorToBattle(actors[i]); }
        actors = new List<TacticActor>();
        actors = actorMaker.SpawnTeamInPattern(battleState.GetEnemySpawnPattern(), 1, enemyParty.characters, enemyParty.stats, enemyParty.characterNames, enemyParty.equipment, enemyParty.characterIDs);
        actorMaker.ApplyBattleModifiers(actors, enemyParty.GetBattleModifiers());
        for (int i = 0; i < Mathf.Min(partySizeCap, actors.Count); i++) { map.AddActorToBattle(actors[i]); }
        // Apply relics/ascension/etc. battle modifier effects here.
        // Use condition, effect, specifics for battle modifiers.
        // Condition will include team.
        // Get the modifiers from the battle state.
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
        if (loadedCustomBattleMap)
        {
            ApplyCustomEnemyStartingPositions();
        }
        else
        {
            map.RandomEnemyStartingPositions(battleState.GetEnemySpawnPattern());
        }
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
    public bool longDelays = true;
    public float longDelayTime = 0.1f;
    public float shortDelayTime = 0.01f;
    public int roundNumber;
    public int GetRoundNumber(){return roundNumber;}
    public int turnNumber;
    public int GetTurnIndex(){return turnNumber;}
    public TacticActor turnActor;
    public TacticActor GetTurnActor(){return turnActor;}
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
        // Get initiative order.
        map.battlingActors = initiativeTracker.SortActors(map.battlingActors);
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
        turnActor = map.battlingActors[turnNumber];
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
        turnNumber = map.RemoveActorsFromBattle(turnNumber);
        winningTeam = FindWinningTeam();
        if (winningTeam >= 0)
        {
            combatLog.UpdateNewestLog("Ending Battle By Clicking Next Turn");
            EndBattle(winningTeam);
            return;
        }
        turnNumber++;
        if (turnNumber >= map.battlingActors.Count){NextRound();}
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
        else if (!actorAI.NormalTurn(turnActor, roundNumber))
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

    protected void ActorAttacksActor(TacticActor attacker, TacticActor defender, bool payCost = true)
    {
        if (payCost)
        {
            if (!attacker.AttackActionsLeft()){return;}
            attacker.PayAttackCost();
        }
        combatLog.UpdateNewestLog(attacker.GetPersonalName() + " attacks " + defender.GetPersonalName() + ".");
        attackManager.ActorAttacksActor(attacker, defender, map);
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

    protected void BossTurn(int actionsLeft)
    {
        List<string> turnDetails = actorAI.ReturnBossActions(turnActor, map);
        // Some things you can do without actions.
        switch (turnDetails[0])
        {
            case "Change Form":
                // Update base stats based on new form.
                actorMaker.ChangeActorForm(turnActor, turnDetails[1]);
                BossTurn(actionsLeft);
                // This will always take all your actions.
                //turnActor.ResetActions();
                return;
            case "Split":
                // Change base health to be the same as current health.
                turnActor.SetBaseHealth(turnActor.GetHealth());
                // Check if there are any empty adjacent tiles.
                int splitTile = map.ReturnRandomAdjacentEmptyTile(turnActor.GetLocation());
                // Create a copy in a random adjacent empty tile.
                // Or this is a special case where you can stack actors?
                // This will always take all your actions.
                TacticActor clonedActor = actorMaker.CloneActor(turnActor, splitTile);
                map.AddActorToBattle(clonedActor);
                ApplyBattleModifiersToActor(clonedActor);
                turnActor.ResetActions();
                break;
            case "Skill":
                NPCSkillAction(actionsLeft, turnDetails[1]);
                return;
            case "Summon Skill":
                NPCSkillAction(actionsLeft, actorAI.ReturnSkillWithEffect(turnActor, map, "Summon"));
                return;
            case "Spell":
                NPCSpellAction(actionsLeft, turnDetails[1]);
                return;
            case "One Time Spell":
                turnActor.IncrementCounter();
                NPCSpellAction(actionsLeft, turnDetails[1]);
                return;
            case "Summon Spell":
                NPCSpellAction(actionsLeft, actorAI.ReturnSpellWithEffect(turnActor, map, "Summon"));
                return;
            case "One Time Skill":
                turnActor.IncrementCounter();
                NPCSkillAction(actionsLeft, turnDetails[1]);
                return;
            case "Chain Skill":
                string[] chainSkills = turnDetails[1].Split(",");
                NPCChainSkillActions(chainSkills);
                return;
            case "One Time Chain Skill":
                turnActor.IncrementCounter();
                string[] OTchainSkills = turnDetails[1].Split(",");
                NPCChainSkillActions(OTchainSkills);
                return;
            case "Random Skill":
                string[] skills = turnDetails[1].Split(",");
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
            case "MoveToTile":
                // Move to the closest tile of type.
                int tile = map.ReturnClosestTileOfType(turnActor, turnDetails[1]);
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
                        BasicNPCAction();
                        return;
                    }
                    else
                    {
                        break;
                    }
                }
            case "MoveToSandwichTarget":
                int sandwichingTile = map.ReturnClosestSandwichTargetBetweenTileOfType(turnActor, turnDetails[1]);
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
                int sandwichedTile = map.ReturnClosestTileSandwiched(turnActor, turnDetails[1]);
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

    protected void NPCSpellAction(int actionsLeft, string spell)
    {
        for (int i = 0; i < actionsLeft; i++)
        {
            // Get the active and the targeted tile.
            activeManager.SetSkillUser(turnActor);
            activeManager.SetSpell(spell);
            int targetedTile = actorAI.ChooseSpellTargetLocation(turnActor, map, moveManager, activeManager.magicSpell);
            // If you can't find a target or cast the skill or are silenced then just do a regular action.
            if (targetedTile == -1 || !activeManager.CheckSpellCost(map))
            {
                BasicNPCAction();
                return;
            }
            activeManager.GetTargetedTiles(targetedTile, moveManager.actorPathfinder, true);
            // Bool = TRUE for spells.
            if (!actorAI.ValidSkillTargets(turnActor, map, activeManager, true))
            {
                BasicNPCAction();
                return;
            }
            ActivateSpell();
            if (AdjustTurnNumber())
            {
                return;
            }
            if (turnActor.GetActions() <= 0 || turnActor.GetHealth() < 0)
            {
                EndTurn();
                return;
            }
        }
        EndTurn();
    }

    protected void NPCSkillAction(int actionsLeft, string skill = "")
    {
        for (int i = 0; i < actionsLeft; i++)
        {
            // Get the active and the targeted tile.
            if (skill == "")
            {
                activeManager.SetSkillFromName(actorAI.ReturnAIActiveSkill(), turnActor);
            }
            else
            {
                activeManager.SetSkillFromName(skill, turnActor);
            }
            int targetedTile = actorAI.ChooseSkillTargetLocation(turnActor, map, moveManager);
            // If you can't find a target or cast the skill or are silenced then just do a regular action.
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
            if (skill == "")
            {
                ActivateSkill(actorAI.ReturnAIActiveSkill());
                if (AdjustTurnNumber())
                {
                    return;
                }
            }
            else
            {
                ActivateSkill(skill);
                if (AdjustTurnNumber())
                {
                    return;
                }
            }
            if (turnActor.GetActions() <= 0 || turnActor.GetHealth() < 0)
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
            if (actorAI.EnemyInAttackRange(turnActor, turnActor.GetTarget(), map)) { ActorAttacksActor(turnActor, turnActor.GetTarget()); }
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
        // Summoned enemies have summoning sickness.
        if (turnActor.summoned && turnActor.summonedBy != null)
        {
            turnActor.ResetSummonedBy();
            EndTurn();
            return;
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
            if (actor.Grappling())
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
            if (actor.Grappling())
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
        turnNumber = map.RemoveActorsFromBattle(turnNumber);
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
    }

    public void ActivateSpell(TacticActor actor = null)
    {
        ResetState();
        if (actor == null) { actor = turnActor; }
        activeManager.ActivateSpell(this);
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

    protected bool TryLoadCustomBattleMap()
    {
        customEnemyStartingLocations.Clear();
        if (!battleState.UsingCustomBattle() || battleState.savedBattles == null)
        {
            Debug.Log("TryLoadCustomBattleMap aborted. UsingCustomBattle=" + battleState.UsingCustomBattle() + " savedBattlesNull=" + (battleState.savedBattles == null));
            return false;
        }
        List<string> savedMapInfo;
        List<string> savedTerrainEffects;
        List<int> savedElevations;
        List<string> savedBorders;
        List<string> savedBuildings;
        List<string> savedEnemies;
        List<int> savedEnemyLocations;
        string savedWeather;
        string savedTime;
        if (!battleState.savedBattles.TryLoadBattleData(battleState.GetCustomBattleName(), out savedMapInfo, out savedTerrainEffects, out savedElevations, out savedBorders, out savedBuildings, out savedEnemies, out savedEnemyLocations, out savedWeather, out savedTime))
        {
            Debug.Log("TryLoadCustomBattleMap failed to load saved data for: " + battleState.GetCustomBattleName());
            return false;
        }
        map.SetMapInfo(savedMapInfo);
        map.terrainEffectTiles = new List<string>(savedTerrainEffects);
        map.mapElevations = new List<int>(savedElevations);
        for (int i = 0; i < Mathf.Min(map.mapTiles.Count, savedBorders.Count); i++)
        {
            map.mapTiles[i].SetBorders(savedBorders[i].Split("|").ToList());
            map.UpdateTileBorderSprites(i);
        }
        for (int i = 0; i < Mathf.Min(map.mapTiles.Count, savedElevations.Count); i++)
        {
            map.ChangeTileElevation(i, savedElevations[i]);
        }
        for (int i = 0; i < savedBuildings.Count; i++)
        {
            if (savedBuildings[i].Length <= 0){continue;}
            map.AddBuilding(savedBuildings[i], i);
        }
        enemyParty.ResetLists();
        enemyParty.AddCharacters(savedEnemies);
        battleState.SetEnemyNames(savedEnemies);
        customEnemyStartingLocations = new List<int>(savedEnemyLocations);
        map.UpdateMap();
        return true;
    }

    protected void ApplyCustomEnemyStartingPositions()
    {
        List<TacticActor> enemyTeam = map.AllTeamMembers(1);
        if (customEnemyStartingLocations.Count <= 0)
        {
            map.RandomEnemyStartingPositions(battleState.GetEnemySpawnPattern());
            return;
        }
        for (int i = 0; i < Mathf.Min(enemyTeam.Count, customEnemyStartingLocations.Count); i++)
        {
            enemyTeam[i].SetLocation(customEnemyStartingLocations[i]);
        }
        map.UpdateMap();
    }
}
