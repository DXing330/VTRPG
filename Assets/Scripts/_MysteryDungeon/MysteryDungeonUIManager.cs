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
public class MysteryDungeonUIManager : ClickTileManager
{
    // UI Objects.
    public TMP_Text fastMoveText;
    void Start()
    {
        UpdateUI();
    }
    protected void UpdateUI()
    {
        utility.DisableGameObjects(actionStateObjects);
        int stateIndex = actionStateList.IndexOf(actionState);
        if (stateIndex >= 0){actionStateObjects[stateIndex].SetActive(true);}
        fastMoveText.text = fastMove ? "ON" : "OFF";
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
            break;
        }
    }
    public GeneralUtility utility;
    public MysteryDungeonBattleManager battleManager;
    public PartyDataManager partyData;
    public bool fastMove = false;
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
            break;
            // Highlight Attack Range.
            case ClickTileState.Attack:
            break;
        }
    }
    public void MoveInDirection(int direction)
    {
        if (fastMove && battleManager.PlayerMoveInDirection(direction))
        {
            // If In Danger Then ???
            if (battleManager.DangerNear())
            {
                fastMove = false;
                ChangeState("None");
                return;
            }
            else
            {
                MoveInDirection(direction);
            }
        }
        else if (!fastMove && battleManager.PlayerMoveInDirection(direction))
        {
            battleManager.PlayerStartMoving();
        }
    }
}
