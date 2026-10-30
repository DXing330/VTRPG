using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// Like The PMD, New Logs Will Appear At The Bottom, Pushing Old Logs Up.
// TESTED 10/27/26
public class MysteryDungeonCombatLog : MonoBehaviour
{
    public GeneralUtility utility;
    public MysteryDungeonLog logData;
    public List<GameObject> currentPageObjects;
    public List<TMP_Text> currentPageLogs;
    public PopUpMessage popUpDetails;
    protected List<string> allLogs;
    public void UpdateAllLogs()
    {
        allLogs = logData.GetMDLogs();
    }
    // 0 Means The Newest Log Is At The Bottom, 1 Means Second Newest, Etc.
    public int index = 0;
    // By +- 1
    // TESTED 10/27/26
    public void ChangeIndex(int change)
    {
        UpdateAllLogs();
        int maxIndex = Mathf.Max(0, allLogs.Count - currentPageObjects.Count);
        index = Mathf.Clamp(index + change, 0, maxIndex);
        UpdateCurrentPage();
    }
    // TESTED 10/27/26
    public void JumpToBottom()
    {
        index = 0; // Newest logs
        UpdateCurrentPage();
    }
    // TESTED 10/27/26
    public void JumpToTop()
    {
        UpdateAllLogs();
        index = Mathf.Max(0, allLogs.Count - currentPageObjects.Count);
        UpdateCurrentPage();
    }
    // Start With The Oldest Logs First.
    public void UpdateCurrentPage()
    {
        UpdateAllLogs();
        utility.DisableGameObjects(currentPageObjects);
        for (int i = 0; i < currentPageObjects.Count; i++)
        {
            int logIndex = index + i;
            if (logIndex >= allLogs.Count)
                break;
            int dataIndex = allLogs.Count - 1 - logIndex;
            currentPageObjects[i].SetActive(true);
            currentPageLogs[i].text = allLogs[dataIndex];
        }
    }
    // TESTED 10/27/26
    public void ShowDetailedLog(int clickedIndex)
    {
        UpdateAllLogs();
        int dataIndex = allLogs.Count - 1 - clickedIndex - index;
        string details = logData.GetDetailedLogAtIndex(dataIndex);
        if (details.Length <= 0){return;}
        popUpDetails.SetMessage(details);
    }
}
