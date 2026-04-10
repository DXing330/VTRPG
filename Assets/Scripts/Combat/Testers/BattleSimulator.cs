using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleSimulator : MonoBehaviour
{
    // DEBUG STUFF
    [ContextMenu("Simulator State Save")]
    public void DebugSaveState()
    {
        simulatorState.Save();
    }
    [ContextMenu("Simulator State New Game")]
    public void DebugNewState()
    {
        simulatorState.NewGame();
    }
    void Start()
    {
        partyOneList.ResetLists();
        partyTwoList.ResetLists();
        simulatorState.Load();
        partyOneSelect.RefreshData();
        partyTwoSelect.RefreshData();
        selectedActorName = "";
        actorSelect.SetData(actorStats.GetAllKeys(), actorStats.GetAllKeys(), actorStats.GetAllValues());
        if (simulatorState.MultiBattleEnabled() && !simulatorState.MultiBattleFinished())
        {
            simulatorState.IncrementMultiBattle();
            StartBattle();
        }
        else if (simulatorState.MultiBattlePreviouslyEnabled())
        {
            simulatorState.EnableMultiBattle();
        }
    }
    public BattleSimulatorState simulatorState;
    // BATTLESIM SETTINGS
    public void EnableMultiBattle()
    {
        simulatorState.EnableMultiBattle();
    }
    // Determine the characters.
    public StatDatabase actorStats;
    public ActorSpriteHPList actorSelect;
    public string selectedActorName;
    public ActorSpriteHPList partyOneSelect;
    public ActorSpriteHPList partyTwoSelect;
    public CharacterList partyOneList;
    public CharacterList partyTwoList;
    public BattleManager battleManager;
    public BattleStatsTrackerSaving battleStatsTrackerSaving;
    public GameObject battleManagerObject;
    public GameObject simulatorPanel;
    public void StartBattle()
    {
        // Don't start unless there are members on both sides.
        simulatorState.Save();
        if (partyOneList.characters.Count <= 0 || partyTwoList.characters.Count <= 0)
        {
            return;
        }
        battleManager.SetAutoBattle(simulatorState.AutoBattleEnabled());
        battleManager.SetControlAI(simulatorState.ControlAIEnabled());
        // If you're starting a multibattle for the first time then reset the tracker.
        if (simulatorState.MultiBattleEnabled() && simulatorState.GetCurrentMultiBattleIteration() == 0)
        {
            battleStatsTrackerSaving.NewGame();
            simulatorState.IncrementMultiBattle();
        }
        simulatorPanel.SetActive(false);
        simulatorState.SetTerrainType();
        // These are copied from the state upon starting, make sure the manager is pointing to the right state.
        battleManager.map.SetTime(simulatorState.GetTime());
        battleManager.map.SetWeather(simulatorState.GetWeather());
        simulatorState.ApplyBattleModifiers();
        battleManagerObject.SetActive(true);
    }
    public void RemoveFromPartyOne()
    {
        if (partyOneSelect.GetSelected() < 0)
        {
            return;
        }
        partyOneList.RemoveFromParty(partyOneSelect.GetSelected());
        partyOneSelect.RefreshData();
    }
    public void ClearParty(int index)
    {
        if (index == 0)
        {
            partyOneList.ResetLists();
            partyOneSelect.RefreshData();
        }
        else
        {
            partyTwoList.ResetLists();
            partyTwoSelect.RefreshData();
        }
    }
    public void RemoveFromPartyTwo()
    {
        if (partyTwoSelect.GetSelected() < 0)
        {
            return;
        }
        partyTwoList.RemoveFromParty(partyTwoSelect.GetSelected());
        partyTwoSelect.RefreshData();
        // Disable what is needed.
    }
    public void SelectActorToAdd()
    {
        if (actorSelect.GetSelected() < 0)
        {
            selectedActorName = "";
            return;
        }
        selectedActorName = actorSelect.GetSelectedName();
    }
    public void AddToPartyOne()
    {
        if (selectedActorName.Length <= 0)
        {
            return;
        }
        string stats = actorStats.ReturnValue(selectedActorName);
        string ID = Random.Range(1, 999).ToString();
        partyOneList.AddMemberToParty(selectedActorName + " " + ID, stats, selectedActorName, ID);
        partyOneSelect.RefreshData();
    }
    public void AddToPartyTwo()
    {
        if (selectedActorName.Length <= 0)
        {
            return;
        }
        string stats = actorStats.ReturnValue(selectedActorName);
        string ID = Random.Range(1, 999).ToString();
        partyTwoList.AddMemberToParty(selectedActorName + " " + ID, stats, selectedActorName, ID);
        partyTwoSelect.RefreshData();
    }
    public NameRater filter; 
    public void ResetFilter()
    {
        filter.ResetNewName();
        actorSelect.SetData(actorStats.GetAllKeys(), actorStats.GetAllKeys(), actorStats.GetAllValues());
    }
    public void FilterActorSelect()
    {
        actorSelect.ResetSelected();
        List<string> filters = new List<string>();
        filters.Add(filter.ReturnNameWithFirstCharUpperCase());
        filters.Add(filter.ConfirmName().ToLower());
        actorSelect.SetData(actorStats.GetFilteredKeys(filters), actorStats.GetFilteredKeys(filters), actorStats.GetFilteredValues(filters));
    }
}
