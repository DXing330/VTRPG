using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainCampaignUnlocker : MonoBehaviour
{
    public GeneralUtility utility;
    public MainCampaignState campaignState;
    public List<GameObject> unlockedObjects;
    public List<int> campaignRequirements;
    void Start()
    {
        CheckLock();
    }
    public void CheckLock()
    {
        utility.DisableGameObjects(unlockedObjects);
        for (int i = 0; i < campaignRequirements.Count; i++)
        {
            if (campaignState.GetPreviousChapters().Count >= campaignRequirements[i])
            {
                unlockedObjects[i].SetActive(true);
            }
        }
    }
}
