using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CombatLogLarge : MonoBehaviour
{
    void Start()
    {
        UpdateAllLogs();
    }
    public GeneralUtility utility;
    public CombatLog combatLog;
    public List<GameObject> logDisplayObjects;
    public void ResetPage()
    {
        utility.DisableGameObjects(logDisplayObjects);
    }
    public List<GameObject> changePageObjects;
    public List<CombatLogSingle> logDisplay;
    public List<int> roundTracker;
    public List<int> turnTracker;
    public List<string> allLogs;
    public List<int> fullRoundTracker;
    public List<int> fullTurnTracker;
    public List<int> indexTracker;
    public List<string> fullLogs;
    protected void UpdateFullLogs()
    {
        fullRoundTracker.Clear();
        fullTurnTracker.Clear();
        indexTracker.Clear();
        fullLogs.Clear();
        int index = 0;
        for (int i = 0; i < allLogs.Count; i++)
        {
            index = 0;
            string[] splitLogs = allLogs[i].Split("|");
            for (int j = 0; j < splitLogs.Length; j++)
            {
                fullRoundTracker.Add(roundTracker[i]);
                fullTurnTracker.Add(turnTracker[i]);
                indexTracker.Add(index);
                index++;
                fullLogs.Add(splitLogs[j]);
            }
        }
    }
    public void UpdateAllLogs()
    {
        // Not setting these to new, since we want it to always copy the combat.
        allLogs = combatLog.allLogs;
        roundTracker = combatLog.combatRoundTracker;
        turnTracker = combatLog.combatTurnTracker;
        page = 0;
        UpdateFullLogs();
        UpdatePage();
    }
    public PopUpMessage detailsPopUp;
    public int page;
    public void ChangePage(bool right = true)
    {
        page = utility.ChangePage(page, right, changePageObjects, fullLogs);
        if (page < 0){page = 0;}
        UpdatePage();
    }
    public void UpdatePage()
    {
        ResetPage();
        // Determine the log indexes based on the page.
        int startIndex = logDisplayObjects.Count * page;
        // Iterate through the logs.
        for (int i = 0; i < logDisplayObjects.Count; i++)
        {
            if (startIndex + i >= fullLogs.Count){return;}
            logDisplayObjects[i].SetActive(true);
            logDisplay[i].UpdateLog(fullRoundTracker[i + startIndex].ToString(), fullTurnTracker[i + startIndex].ToString(), fullLogs[i + startIndex]);
        }
    }
    public void ClickOnLog(int index)
    {
        int trueIndex = index + (logDisplayObjects.Count * page);
        string details = combatLog.ReturnDetailedLog(fullRoundTracker[trueIndex], fullTurnTracker[trueIndex], indexTracker[trueIndex]);
        if (details.Length < 6){return;}
        detailsPopUp.SetMessage(details);
    }
}
