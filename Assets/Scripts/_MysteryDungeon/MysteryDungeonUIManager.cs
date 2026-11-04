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
public enum MysteryDungeonUIState
{
    None,
    Team,
    Map,
    Log
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
    void Start()
    {
        UpdateUI();
    }
    protected void UpdateActionUI()
    {
        utility.DisableGameObjects(actionStateObjects);
        int stateIndex = actionStateList.IndexOf(actionState);
        if (stateIndex >= 0){actionStateObjects[stateIndex].SetActive(true);}
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
            case ClickTileState.Item:
            selectItemList.SetSelectables(partyData.dungeonBag.GetItems());
            useItemObject.SetActive(false);
            break;
            case ClickTileState.Skill:
            selectActiveList.SetSelectables(map.GetPartyLeader().GetActiveSkills());
            useActiveObject.SetActive(false);
            break;
        }
    }
    protected void UpdateUI()
    {
        int stateIndex = actionStateList.IndexOf(actionState);
        if (stateIndex >= 0){actionStateObjects[stateIndex].SetActive(true);}
        UpdateFastMoveText();
        UpdateAttackStats(battleManager.GetTurnActor(0));
        UpdateActionUI();
        UpdateViewUI();
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
        if (!fastMove)
        {
            battleManager.UpdateFastMove(fastMove);
            map.updateMap = true;
            map.RefreshPlayerLocation();
            map.UpdateMap();
        }
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
        map.ResetHighlights();
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
            case ClickTileState.Skill:
            if (useActiveObject.activeSelf)
            {
                // TODO Call The BattleManager To Check That Kind Of Stuff.
                battleManager.PlayerSelectSkillTarget(tileNumber);
                return;
            }
            else
            {
                ChangeState("None");
            }
            break;
        }
        UpdateUI();
    }
    protected bool isMoving = false;
    protected float nextMoveInputTime = 0f;
    protected float moveInputCooldown = 0.15f;
    public void MoveInDirection(int direction)
    {
        // Prevent rapid external clicks.
        if (!fastMove)
        {
            if (Time.unscaledTime < nextMoveInputTime){return;}
            nextMoveInputTime = Time.unscaledTime + moveInputCooldown;
        }
        if (isMoving){return;}
        isMoving = true;
        if (fastMove)
        {
            map.updateMap = false;
        }
        bool moved = battleManager.PlayerMoveInDirection(direction);
        if (fastMove && moved)
        {
            // If In Danger Then Stop Moving
            if (battleManager.DangerNear())
            {
                ToggleFastMove();
                ChangeState("None");
                isMoving = false;
                return;
            }
            else if (battleManager.RoomHallBorder(direction))
            {
                ToggleFastMove();
                battleManager.PlayerStartMoving();
            }
            else
            {
                isMoving = false;
                MoveInDirection(direction);
            }
        }
        else if (fastMove && !moved)
        {
            ToggleFastMove();
        }
        else if (!fastMove && moved)
        {
            battleManager.PlayerStartMoving();
        }
        isMoving = false;
    }
    public void ChangeDirection(int direction)
    {
        battleManager.PlayerChangeDirection(direction);
    }
    public SelectList selectItemList;
    public GameObject useItemObject;
    public TMP_Text useItemDescription;
    public MysteryDungeonItemDetailSwitch itemDescriptions;
    public void SelectItem()
    {
        useItemObject.SetActive(true);
        UpdateItemDescription(selectItemList.GetSelectedString());
    }
    public void UpdateItemDescription(string itemName)
    {
        useItemDescription.text = itemDescriptions.ReturnItemDescriptionFromName(itemName);
    }
    public void UseItem()
    {
        battleManager.ActorUseItem(selectItemList.GetSelectedString());
        ChangeState("Item");
    }
    // Removes The Item.
    public void DropItem()
    {
        battleManager.ActorDropItem(selectItemList.GetSelectedString());
        partyData.dungeonBag.UseItem(selectItemList.GetSelectedString());
        ChangeState("Item");
    }
    // If Throwable?
    public void ThrowItem()
    {
        battleManager.ActorThrowItem(selectItemList.GetSelectedString());
        ChangeState("Item");
    }
    public SelectList selectActiveList;
    public GameObject useActiveObject;
    public TMP_Text useActiveDescription;
    public ActiveDescriptionViewer activeDescriptions;
    public void SelectSkill()
    {
        useActiveObject.SetActive(true);
        string selectedActive = selectActiveList.GetSelectedString();
        battleManager.PlayerSelectSkill(selectedActive);
        useActiveDescription.text = activeDescriptions.ReturnActiveDescriptionFromName(selectedActive, map.GetPartyLeader());
    }
    public void DeselectSkill()
    {
        useActiveObject.SetActive(false);
        selectActiveList.ResetSelected();
        map.ResetHighlights();
    }
    public void ActivateSkill()
    {
        if (battleManager.PlayerActivateSkill())
        {
            ChangeState("Skill");
        }
        else
        {
            return;
        }
    }
    public MysteryDungeonUIState UIState;
    public List<MysteryDungeonUIState> UIStateList;
    public List<TMP_Text> UIStateSelectTexts;
    public List<GameObject> UIStateObjects;
    public MysteryDungeonStatCardList statCards;
    public MysteryDungeonCombatLog combatLog;
    public void ChangeUIState(string newState)
    {
        try
        {
            UIState = Enum.Parse<MysteryDungeonUIState>(newState);
        }
        catch
        {
            UIState = MysteryDungeonUIState.None;
        }
        UpdateUI();
    }
    protected void UpdateViewUI()
    {
        utility.DisableGameObjects(UIStateObjects);
        int stateIndex = UIStateList.IndexOf(UIState);
        if (stateIndex >= 0)
        {
            UIStateObjects[stateIndex].SetActive(true);
        }
        for (int i = 0; i < UIStateSelectTexts.Count; i++)
        {
            UIStateSelectTexts[i].fontStyle = FontStyles.Normal;
            if (i == stateIndex)
            {
                UIStateSelectTexts[i].fontStyle = FontStyles.Underline;
            }
        }
        switch (UIState)
        {
            case MysteryDungeonUIState.Team:
            statCards.UpdateTeamActors(battleManager.map.battlingActors, 0);
            break;
            case MysteryDungeonUIState.Log:
            combatLog.UpdateCurrentPage();
            break;
        }
    }
}
