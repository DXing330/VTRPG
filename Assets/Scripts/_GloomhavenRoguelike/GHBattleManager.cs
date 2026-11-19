using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Handles Player/Enemy Turn LifeCycle
public class GHBattleManager : BasicBattleManager<GHBattleMap, GHActorAI>
{
    public List<GHCard> cardQueue;
    public List<string> effectQueue;
    public List<string> specificsQueue;
    public void EnqueueCard(GHCard playedCard)
    {
        if (playedCard == null){return;}
        if (playedCard.effects == null || playedCard.specifics == null){return;}
        for (int i = 0; i < playedCard.effects.Count; i++)
        {
            cardQueue.Add(playedCard);
            effectQueue.Add(playedCard.effects[i]);
            specificsQueue.Add(playedCard.specifics[i]);
        }
    }
    // Do The First Effect In Queue, Some Effects Require Player Input, Pause The Queue Until Player Acts.
    public string currentEffect = "";
    public string currentSpecifics = "";
    public List<int> targetableTiles = new();
    public int targetedTile = -1;
    public List<int> targetedTiles = new();
    public void UpdateTargetedTile(int newTile)
    {
        if (!targetableTiles.Contains(newTile)){return;}
        targetedTile = newTile;
        // Update Highlights.
    }
    public ClickTileState actionState;
    protected void StartEffect()
    {
        currentEffect = effectQueue[0];
        currentSpecifics = specificsQueue[0];
        targetableTiles = new();
        targetedTiles = new();
        targetedTile = -1;
        actionState = ClickTileState.None;
        switch (currentEffect)
        {
            // Some Effects Don't Require Player Input Or Do Nothing?
            default:
            effectManager.passive.AffectActor(player, currentEffect, currentSpecifics);
            NextEffect();
            break;
            case "Move":
            // Show Moveable Tiles.
            targetableTiles = moveManager.GetAllReachableTilesWithFixedMovement(player, map.battlingActors, int.Parse(currentSpecifics) + player.GetSpeed());
            map.ResetHighlights();
            map.UpdateHighlights(targetableTiles);
            actionState = ClickTileState.Move;
            break;
            case "Status":
            case "Attack":
            // Show Attackable Tiles.
            // Click With Show Targeted Based On Shape/Span.
            actionState = ClickTileState.Attack;
            break;
            case "Defend":
            // Gain Block.
            player.UpdateTempHealth(int.Parse(currentSpecifics));
            NextEffect();
            break;
        }
    }
    // Only For Attack/Move/Etc.
    protected void ActivateEffect()
    {
        if (!targetableTiles.Contains(targetedTile)){return;}
        switch (currentEffect)
        {
            case "Move":
            // Path To Selected Tile.
            break;
            case "Status":
            case "Attack":
            // Get Targets + Attack/Status Them.
            break;
        }
        NextEffect();
    }
    // Remove The Effect That Was Just Done And Start The Next Effect.
    protected void NextEffect()
    {
        cardQueue.RemoveAt(0);
        effectQueue.RemoveAt(0);
        specificsQueue.RemoveAt(0);
        if (effectQueue.Count > 0){StartEffect();}
    }
    // Can easily be expanded into a list for multiple players.
    public GHDeck deck;
    public TacticActor player;
    public void PlayerPlaysCard(GHCard card)
    {
        // Can't Play Cards While Queue Is Working.
        if (effectQueue.Count > 0){return;}
        // Check If The Player Can Afford To Play The Card.
        //if (card.cost > player.GetActions()){return;}
        EnqueueCard(card);
        StartEffect();
        deck.PlayCard(card);
    }
    public override void NewRound()
    {
        // Get The Living Actors From The BattleMap.
        base.NewRound();
        // Check Player Alive.
        if (player.GetHealth() <= 0)
        {
            // Check If Resurrect/Potions/Relics, Else EndGame. 
        }
        // Determine + Display Enemy Intents.
        // Make Sure The Player Is First.
        // Player Start Turn.
    }
    public override void NextTurn()
    {
        base.NextTurn();
    }
    public override void EnemyTurn()
    {
        // Act Based On Intent.
    }
}