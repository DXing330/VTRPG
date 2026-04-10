using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PassiveDetailViewerTester : MonoBehaviour
{
    public StatDatabase allPassives;
    public PassiveDetailViewer detailViewer;
    public List<string> passiveGroupNames;
    public List<string> passiveLevels;
    public string allPassiveNames;
    public List<string> passiveNames;
    public string allPassiveInfo;
    public List<string> passiveInfo;
    public string testPassiveName;
    public string testPassiveData;
    public List<string> testPassiveNames;

    [ContextMenu("Test All Passives")]
    public void TestPassiveDescriptions()
    {
        passiveNames = new List<string>(allPassives.keys);
        passiveInfo = new List<string>(allPassives.values);
        for (int i = 0; i < passiveNames.Count; i++)
        {
            Debug.Log(passiveNames[i]);
            Debug.Log(detailViewer.ReturnPassiveDetails(passiveInfo[i]));
        }
    }

    [ContextMenu("Test Single Passive")]
    public void TestSinglePassiveDescription()
    {
        string testPassiveInfo = allPassives.ReturnValue(testPassiveName);
        Debug.Log(testPassiveInfo);
        Debug.Log(detailViewer.ReturnPassiveDetails(testPassiveInfo));
    }

    [ContextMenu("Test Multiple Passive")]
    public void TestMultiplePassiveDescription()
    {
        for (int i = 0; i < testPassiveNames.Count; i++)
        {
            string testPassiveInfo = allPassives.ReturnValue(testPassiveNames[i]);
            Debug.Log(testPassiveInfo);
            Debug.Log(detailViewer.ReturnPassiveDetails(testPassiveInfo));
        }
    }

    [ContextMenu("Test Single Passive Data")]
    public void TestSinglePassiveData()
    {
        Debug.Log(testPassiveData);
        Debug.Log(detailViewer.ReturnPassiveDetails(testPassiveData));
    }
}
