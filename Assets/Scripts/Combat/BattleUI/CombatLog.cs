using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CombatLog : MonoBehaviour
{
    protected void ResetLists()
    {
        combatRoundTracker.Clear();
        combatTurnTracker.Clear();
        allLogs.Clear();
        currentLogs.Clear();
        detailKeys.Clear();
        detailedLogs.Clear();
    }
    public void ForceStart()
    {
        ResetLists();
    }
    public BattleManager battleManager;
    public GeneralUtility utility;
    // Change rounds.
    public int round;
    public void ChangeRound(bool increase = true)
    {
        turn = 0;
        int maxRound = GetLatestRound();
        if (increase)
        {
            if (maxRound > round){round++;}
            else {round = 1;}
        }
        else
        {
            if (round > 1){round--;}
            else {round = maxRound;}
        }
        UpdateLog();
    }
    public int turn;
    public int DetermineTurnsInRound()
    {
        int maxTurn = 0;
        for (int i = 0; i < combatRoundTracker.Count; i++)
        {
            if (combatRoundTracker[i] == round)
            {
                if (combatTurnTracker[i] > maxTurn){maxTurn = combatTurnTracker[i];}
            }
        }
        return maxTurn;
    }
    public void ChangeTurn(bool increase = true)
    {
        // Need to determine the max number of turns in the current round.
        int maxTurn = DetermineTurnsInRound();
        if (increase)
        {
            if (maxTurn > turn){turn++;}
            // If you go too far then just go to the next turn.
            else {ChangeRound(increase);}
        }
        else
        {
            if (turn >= 0){turn--;}
            else
            {
                ChangeRound(increase);
                // If you go down in rounds then you need to start at the max turn.
                maxTurn = DetermineTurnsInRound();
                turn = maxTurn;
            }
        }
        UpdateLog();
    }
    public List<int> combatRoundTracker;
    public int GetLatestRound()
    {
        if (combatRoundTracker.Count == 0){return 0;}
        return combatRoundTracker[combatRoundTracker.Count - 1];
    }
    public List<int> combatTurnTracker;
    public int GetLatestTurn()
    {
        if (combatTurnTracker.Count == 0){return 0;}
        return combatTurnTracker[combatTurnTracker.Count - 1];
    }
    public List<string> allLogs;
    public List<string> currentLogs;
    public List<string> detailKeys;
    public List<string> detailedLogs;
    public void DebugLatestDetailsLog()
    {
        Debug.Log(detailedLogs[detailedLogs.Count - 1]);
    }
    public void AddDetailedLogs(string newDetail)
    {
        string key = GetLatestRound() + "|" + GetLatestTurn() + "|" + GetLatestLogCount();
        int indexOf = detailKeys.IndexOf(key);
        if (indexOf == -1)
        {
            detailedLogs.Add(newDetail);
            detailKeys.Add(key);
        }
        else
        {
            if (newDetail == ""){return;}
            detailedLogs[indexOf] += "\n"+newDetail;
        }
    }
    public string ReturnDetailedLog(int round, int turn, int index)
    {
        string key = round+"|"+turn+"|"+index;
        int indexOf = detailKeys.IndexOf(key);
        if (indexOf == -1){return "";}
        return detailedLogs[indexOf];
    }
    public void AddNewLog()
    {
        round = battleManager.GetRoundNumber();
        if (round == GetLatestRound())
        {
            turn = GetLatestTurn() + 1;
        }
        else{turn = 0;}
        combatRoundTracker.Add(round);
        combatTurnTracker.Add(turn);
        allLogs.Add("");
    }
    public void UpdateNewestLog(string newText)
    {
        if (allLogs[allLogs.Count - 1] == "")
        {
            allLogs[allLogs.Count - 1] = newText;
        }
        else
        {
            allLogs[allLogs.Count - 1] = allLogs[allLogs.Count - 1]+"|"+newText;
        }
        UpdateLog(false);
        //if (round == battleManager.GetRoundNumber() && turn == battleManager.GetTurnIndex()){UpdateLog(false);}
    }
    public string GetLatestLog()
    {
        return allLogs[allLogs.Count - 1];
    }
    public int GetLatestLogCount()
    {
        return (allLogs[allLogs.Count - 1].Split("|").Length - 1);
    }
    public TMP_Text roundTrackerText;
    public TMP_Text turnTrackerText;
    public GameObject detailLogObject;
    public TMP_Text eventLog;
    public SelectList eventLogs;
    public void UpdateLog(bool manual = true)
    {
        detailLogObject.SetActive(false);
        eventLogs.StartingPage();
        for (int i = 0; i < combatRoundTracker.Count; i++)
        {
            if (combatRoundTracker[i] == round && combatTurnTracker[i] == turn)
            {
                roundTrackerText.text = "Round "+round;
                turnTrackerText.text = "Turn "+(turn+1);
                if (manual)
                {
                    eventLogs.SetSelectables(allLogs[i].Split("|").ToList());
                }
                else{eventLogs.UpdateSelectables(allLogs[i].Split("|").ToList());}
            }
        }
    }
    public void ClickOnLog()
    {
        string details = ReturnDetailedLog(round,turn,eventLogs.GetSelected());
        if (details.Length <= 1){return;}
        detailLogObject.SetActive(true);
        eventLog.text = details;
    }
    public void ClickOnDetails()
    {
        detailLogObject.SetActive(false);
    }
}
