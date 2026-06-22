using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatDisplayList : MonoBehaviour
{
    public List<GameObject> objects;
    public List<GameObject> changePageObjects;
    public void ChangePage(bool right = true)
    {
        page = utility.ChangePage(page, right, objects, statsToDisplay);
        UpdateDisplay();
    }
    public List<StatDisplay> statDisplays;
    public List<string> statsToDisplay;
    public void SetStatsToDisplay(List<string> newStats)
    {
        statsToDisplay = new List<string>(newStats);
        page = 0;
        UpdateDisplay();
    }
    public List<string> currentStats;
    public GeneralUtility utility;
    public ColorDictionary colors;
    public int page = 0;
    protected void DisableChangePage(){utility.DisableGameObjects(changePageObjects);}
    protected void EnableChangePage(){utility.EnableGameObjects(changePageObjects);}
    public void UpdateDisplay()
    {
        utility.DisableGameObjects(objects);
        DisableChangePage();
        currentStats = utility.GetCurrentPageStrings(page, objects, statsToDisplay);
        for (int i = 0; i < currentStats.Count; i++)
        {
            objects[i].SetActive(true);
            statDisplays[i].ShowStats(currentStats[i]);
        }
        if (statsToDisplay.Count > objects.Count)
        {
            EnableChangePage();
        }
    }
}
