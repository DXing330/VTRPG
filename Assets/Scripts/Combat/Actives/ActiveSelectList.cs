using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ActiveSelectList : SelectList
{
    public BattleManager battle;
    public ActiveManager activeManager;
    public ActiveDescriptionViewer descriptionViewer;
    public TMP_Text activeDescription;
    public int state;
    public string spellState = "Spell";
    public Inventory inventory;
    public List<string> useableItems;
    // This needs to be updated on a per actor basis.
    public void UpdateUseableItems()
    {
        useableItems.Clear();
        string ID = battle.GetTurnActor().GetID().ToString();
        for (int i = 0; i < inventory.items.Count; i++)
        {
            if (inventory.GetAssignedActorIDFromIndex(i) == ID && activeManager.activeData.KeyExists(inventory.items[i]))
            {
                useableItems.Add(inventory.items[i]);
            }
        }
    }
    public GameObject mainPanel;
    public List<GameObject> statePanels;
    protected void ResetPanels()
    {
        utility.DisableGameObjects(statePanels);
    }

    public void SetState(int newState)
    {
        ResetPanels();
        state = newState;
        statePanels[state].SetActive(true);
    }

    public override void Select(int index)
    {
        base.Select(index);
        if (battle.GetState() == spellState)
        {
            SelectSpell();
            return;
        }
        IncrementState();
        ShowSelected();
        activeManager.SetSkillFromName(selected, battle.GetTurnActor());
        activeDescription.text = descriptionViewer.ReturnActiveDescription(activeManager.active, activeManager.skillUser, battle.map);
        if (battle.GetTurnActor().GetSilenced())
        {
            ErrorMessage("Can't use skills while silenced.");
            ResetState();
            return;
        }
        activeManager.GetTargetableTiles(battle.GetTurnActor().GetLocation(), battle.moveManager.actorPathfinder);
        activeManager.ResetTargetedTiles();
        activeManager.CheckIfSingleTargetableTile();
        battle.map.UpdateHighlights(activeManager.ReturnTargetableTiles());
    }

    public string selectedSpell;
    public void SetSelectedSpell(string newInfo){selectedSpell = newInfo;}
    public string GetSelectedSpell(){return selectedSpell;}

    protected void SelectSpell()
    {
        IncrementState();
        // Show the spell by name.
        SetSelectedSpell(battle.GetTurnActor().GetSpells()[selectedIndex]);
        activeManager.SetSpell(battle.GetTurnActor().GetSpells()[selectedIndex]);
        UpdateSelectedText(activeManager.magicSpell.GetSkillName());
        if (battle.GetTurnActor().GetSilenced())
        {
            ErrorMessage("Can't use spells while silenced.");
            ResetState();
            return;
        }
        activeDescription.text = descriptionViewer.ReturnSpellDescription(activeManager.magicSpell, activeManager.skillUser);
        activeManager.GetTargetableTiles(battle.GetTurnActor().GetLocation(), battle.moveManager.actorPathfinder, true);
        activeManager.ResetTargetedTiles();
        activeManager.CheckIfSingleTargetableTile();
        battle.map.UpdateHighlights(activeManager.ReturnTargetableTiles());
    }

    public void Deselect()
    {
        DecrementState();
        battle.map.ResetHighlights();
        //StartingPage();
    }

    protected void DecrementState()
    {
        int newState = Mathf.Max(0, state - 1);
        SetState(newState);
    }

    protected void IncrementState()
    {
        int newState = Mathf.Min(statePanels.Count, state + 1);
        SetState(newState);
    }

    public void ResetState()
    {
        battle.ResetState();
        SetState(0);
    }

    public void StartSelecting()
    {
        if (battle.GetTurnActor().ActiveSkillCount() <= 0)
        {
            errorMsgPanel.SetMessage("You currently don't have any skills that can be used.");
            ResetSelectables();
            return;
        }
        else if (battle.GetTurnActor().GetActions() <= 0) { return; }
        SetSelectables(battle.GetTurnActor().GetActiveSkills());
        activeManager.SetSkillUser(battle.GetTurnActor());
        activeManager.ResetTargetedTiles();
        StartingPage();
    }

    public void StartSelectingItems()
    {
        UpdateUseableItems();
        if (useableItems.Count <= 0)
        {
            errorMsgPanel.SetMessage("You currently don't have any items that can be used.");
            ResetSelectables();
            return;
        }
        // Some items can be used with 0 actions. 
        //else if (battle.GetTurnActor().GetActions() <= 0) { return; }
        SetSelectables(useableItems);
        activeManager.SetSkillUser(battle.GetTurnActor());
        activeManager.ResetTargetedTiles();
        StartingPage();
    }

    public void StartSelectingSpells()
    {
        if (battle.GetTurnActor().SpellCount() <= 0)
        {
            errorMsgPanel.SetMessage("You currently don't know any spells that can be used.");
            ResetSelectables();
            return;
        }
        else if (battle.GetTurnActor().GetActions() <= 0) { return; }
        SetSelectables(battle.GetTurnActor().GetSpellNames());
        activeManager.SetSkillUser(battle.GetTurnActor());
        activeManager.ResetTargetedTiles();
        StartingPage();
    }

    public void ActivateSkill()
    {
        // Check cost here.
        if (!activeManager.CheckSkillCost(battle.map))
        {
            // Show an error message instead of just returning?
            ErrorMessage("Not enough resources to use this skill.");
            return;
        }
        // Don't do anything unless a target has been selected.
        if (!activeManager.ExistTargetedTiles())
        {
            ErrorMessage(errorMessages[2]);
            return;
        }
        // Its a spell then do something a little different
        if (battle.GetState() == spellState)
        {
            ActivateSpell();
            return;
        }
        battle.ActivateSkill(selected);
        battle.turnNumber = battle.map.RemoveActorsFromBattle(battle.GetTurnIndex());
        battle.UI.battleStats.UpdateStats();
        // Deal with stealing items later.
        // Check if the skill you just used was an item.
        inventory.ActorUsesItem(selected, battle.GetTurnActor().GetID());
        ResetState();
        mainPanel.SetActive(false);
    }

    public void ActivateSpell()
    {
        if (!activeManager.CheckSpellCost(battle.map))
        {
            // Show an error message instead of just returning?
            ErrorMessage("Not enough resources to cast this spell.");
            return;
        }
        battle.ActivateSpell();
        ResetState();
        mainPanel.SetActive(false);
    }
}
