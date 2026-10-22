using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public enum ClickTileState
{
    None,
    Move,
    Attack,
    Skill,
    Item
}
// Takes In Player Inputs And Calls Changes To Game State.
// Also Updates UI.
public class MysteryDungeonUIManager : ClickTileManager
{
    // UI Objects.
    public TMP_Text fastMoveText;
    public TMP_Text ATKText;
    public TMP_Text HITText;
    public TMP_Text CRITText;
    public TMP_Text SPDText;
    protected void UpdateAttackStats(TacticActor actor)
    {
        ATKText.text = actor.GetAttack().ToString();
        HITText.text = actor.GetHitChance() + "%";
        CRITText.text = actor.GetCritChance() + "%";
        SPDText.text = (100 + actor.GetAttackSpeed()) + "%";
    }
    public MysteryDungeonStatCardList statCards;
    void Start()
    {
        UpdateUI();
    }
    protected void UpdateUI()
    {
        utility.DisableGameObjects(actionStateObjects);
        int stateIndex = actionStateList.IndexOf(actionState);
        if (stateIndex >= 0){actionStateObjects[stateIndex].SetActive(true);}
        UpdateFastMoveText();
        statCards.UpdateTeamActors(battleManager.map.battlingActors, 0);
        UpdateAttackStats(battleManager.GetTurnActor(0));
        switch (actionState)
        {
            // None/Skill/Item
            default:
            battleManager.map.ResetHighlights();
            break;
            // Call The Manager To Highlight Tiles In Move Range.
            case ClickTileState.Move:
            battleManager.PlayerStartMoving();
            break;
            // Highlight Attack Range.
            case ClickTileState.Attack:
            battleManager.PlayerStartAttacking();
            break;
        }
    }
    public GeneralUtility utility;
    public MysteryDungeonBattleManager battleManager;
    public MysteryDungeonBattleMap map;
    public PartyDataManager partyData;
    public bool fastMove = false;
    protected void UpdateFastMoveText()
    {
        fastMoveText.text = fastMove ? "ON" : "OFF";
    }
    public void ToggleFastMove()
    {
        fastMove = !fastMove;
        UpdateUI();
    }
    public ClickTileState actionState;
    public List<ClickTileState> actionStateList;
    public List<GameObject> actionStateObjects;
    // TAKE PLAYER INPUT (END/MOVE/ATTACK/SKILL/ITEM/SPELL)
    public void ChangeState(string newState)
    {
        try
        {
            actionState = Enum.Parse<ClickTileState>(newState);
        }
        catch
        {
            actionState = ClickTileState.None;
        }
        UpdateUI();
    }
    // PLAYER CAN MOVE INTO ALLIES, SWAPPING LOCATIONS WITH THEM.
    public override void ClickOnTile(int tileNumber)
    {
        switch (actionState)
        {
            // None/Skill/Item
            default:
            break;
            case ClickTileState.Move:
            if (battleManager.PlayerMoveToTile(tileNumber))
            {
                if (battleManager.DangerNear())
                {
                    fastMove = false;
                    ChangeState("None");
                    return;
                }
                else
                {
                    battleManager.PlayerStartMoving();
                }
            }
            else
            {
                ChangeState("None");
            }
            break;
            case ClickTileState.Attack:
            if (!battleManager.PlayerAttackTile(tileNumber))
            {
                ChangeState("None");
            }
            break;
        }
        UpdateUI();
    }
    public void MoveInDirection(int direction)
    {
        if (fastMove)
        {
            map.updateMap = false;
        }
        if (fastMove && battleManager.PlayerMoveInDirection(direction))
        {
            // If In Danger Then ???
            if (battleManager.DangerNear())
            {
                fastMove = false;
                ChangeState("None");
                return;
            }
            else if (battleManager.RoomHallBorder(direction))
            {
                fastMove = false;
                UpdateFastMoveText();
                map.updateMap = true;
                map.UpdateMap();
            }
            else
            {
                MoveInDirection(direction);
            }
        }
        else if (fastMove && !battleManager.PlayerMoveInDirection(direction))
        {
            map.updateMap = true;
            map.UpdateMap();
        }
        else if (!fastMove && battleManager.PlayerMoveInDirection(direction))
        {
            battleManager.PlayerStartMoving();
        }
    }
    public void ChangeDirection(int direction)
    {
        battleManager.PlayerChangeDirection(direction);
    }
}
