using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleUIManager : MonoBehaviour
{
    public bool debug = false;
    public GeneralUtility utility;
    public BattleManager battleManager;
    public int state;
    public void SetState(int newState)
    {
        state = Mathf.Max(0, newState);
        utility.DisableGameObjects(stateObjects);
        stateObjects[state].SetActive(true);
        if (state == 0)
        {
            UpdatePinnedView();
        }
    }
    public List<GameObject> stateObjects;
    // Prestate.
    public GameObject adjustStartingPositionsPanel;
    public void AdjustStartingPositions()
    {
        adjustStartingPositionsPanel.SetActive(true);
        utility.DisableGameObjects(playerChoiceActions);
    }
    public void FinishSettingStartingPositions()
    {
        adjustStartingPositionsPanel.SetActive(false);
        utility.EnableGameObjects(playerChoiceActions);
        //SetState(0);
    }
    public TMP_Text winConText;
    public void UpdateWinConString()
    {
        winConText.text = battleManager.battleState.AltWinConString();
    }
    // State 0 - Player Stats + Actions.
    // Don't have to view the battle stats all the time, can also view other things.
    public int pinnedView = 0;
    public void ChangePinnedView(bool right = true)
    {
        pinnedView = utility.ChangeIndex(pinnedView, right, pinnedViewObjects.Count - 1, 0);
        UpdatePinnedView();
    }
    public void UpdatePinnedView()
    {
        utility.DisableGameObjects(pinnedViewObjects);
        pinnedViewObjects[pinnedView].SetActive(true);
        pinnedViewTitle.text = pinnedViewTitles[pinnedView];
        pinnedUIs[pinnedView].SetActor(battleManager.GetSelectedActor());
        pinnedUIs[pinnedView].UpdateUI();
    }
    public List<string> pinnedViewTitles;
    public TMP_Text pinnedViewTitle;
    public List<GameObject> pinnedViewObjects;
    public List<BattleUIBaseClass> pinnedUIs;
    public BattleStats battleStats;
    public GameObject playerChoicesPanel;
    public List<GameObject> playerChoiceActions;
    public ActiveSelectList activeSelectList;
    public SelectStatTextList statusSelect;
    public void ViewActorStatuses()
    {
        TacticActor viewedActor = battleManager.GetSelectedActor();
        if (viewedActor == null){return;}
        statusSelect.SetStatsAndData(viewedActor.GetUniqueStatusAndBuffs(), viewedActor.GetUnqiueSBDurations());
    }
    public SelectStatTextList passiveSelect;
    public PassiveDetailViewer passiveViewer;
    public void ViewActorPassives()
    {
        TacticActor viewedActor = battleManager.GetSelectedActor();
        if (viewedActor == null){return;}
        passiveSelect.SetStatsAndData(viewedActor.GetPassiveSkills(), viewedActor.GetPassiveLevels());
    }
    public void ViewActorCustomPassives()
    {
        TacticActor viewedActor = battleManager.GetSelectedActor();
        if (viewedActor == null){return;}
        passiveViewer.ViewCustomPassives(viewedActor);
    }
    public void ViewActorRunePassives()
    {
        TacticActor viewedActor = battleManager.GetSelectedActor();
        if (viewedActor == null){return;}
        passiveViewer.ViewRunePassives(viewedActor);
    }
    public void NPCTurn()
    {
        if (debug)
        {
            return;
        }
        utility.DisableGameObjects(playerChoiceActions);
    }
    public void PlayerTurn()
    {
        playerChoicesPanel.SetActive(true);
        utility.EnableGameObjects(playerChoiceActions);
    }
    public void ResetActiveSelectList()
    {
        activeSelectList.SetState(0);
    }
    // Battle Stats Preview
    public TMP_Text attackerATKText;
    public TMP_Text targetHPText;
    public TMP_Text targetDEFText;
    public void PreviewBattleStats(TacticActor attacker, TacticActor target = null)
    {
        attackerATKText.text = attacker.GetAttack().ToString();
        if (target == null || target.invisible)
        {
            targetHPText.text = "";
            targetDEFText.text = "";
            return;
        }
        targetHPText.text = target.GetHealth().ToString();
        targetDEFText.text = target.GetDefense().ToString();
    }
    // Target Stats Preview
    public TMP_Text targetedName;
    public TMP_Text targetedHealth;
    public TMP_Text targetedTeam;
    public void PreviewTarget(TacticActor target = null)
    {
        if (target == null || target.invisible)
        {
            targetedName.text = "";
            targetedHealth.text = "";
            targetedTeam.text = "";
            return;
        }
        targetedName.text = target.GetPersonalName();
        targetedHealth.text = target.GetHealth().ToString();
        targetedTeam.text = target.GetTeam().ToString();
    }
    // TODO
    public void UpdateStatSheet(TacticActor actor)
    {

    }
    // State 1 - UI Choices
    // State 2 - Battle Log
    public DisplayTurnOrder turnOrder;

    public void UpdateTurnOrder()
    {
        turnOrder.UpdateTurnOrder(battleManager.map.battlingActors, battleManager.GetTurnIndex());
    }
    // State 3 - Map Details.
    public int mapDetailState = 3;
    public bool ViewingDetails()
    {
        return mapDetailState == state;
    }
    public PopUpMessage mapPassives;
    public void ViewMapPassives(BattleMap map, int tileNumber)
    {
        string mPassives = passiveViewer.MapTEffectPassives(map, tileNumber);
        if (mPassives == "")
        {
            mPassives = passiveViewer.MapTilePassives(map, tileNumber);
        }
        mapPassives.SetMessage(mPassives);
    }
}
