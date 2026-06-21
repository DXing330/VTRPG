using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    public string testPassiveNamesSingleString;
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
    [ContextMenu("Test All Passives Single String")]
    public void TestPassiveDescriptionsSingleString()
    {
        string allPassiveStrings = "";
        passiveNames = new List<string>(allPassives.keys);
        passiveInfo = new List<string>(allPassives.values);
        for (int i = 0; i < passiveNames.Count; i++)
        {
            allPassiveStrings += passiveNames[i] + "\n";
            allPassiveStrings += detailViewer.ReturnPassiveDetails(passiveInfo[i]) + "\n";
        }
        Debug.Log(allPassiveStrings);
    }
    [ContextMenu("Test All Passives Single String Reverse")]
    public void TestPassiveDescriptionsSingleStringReversed()
    {
        string allPassiveStrings = "";
        passiveNames = new List<string>(allPassives.keys);
        passiveInfo = new List<string>(allPassives.values);
        for (int i = passiveNames.Count - 1; i >= 0; i--)
        {
            allPassiveStrings += passiveNames[i] + "\n";
            allPassiveStrings += detailViewer.ReturnPassiveDetails(passiveInfo[i]) + "\n";
        }
        Debug.Log(allPassiveStrings);
    }
    [ContextMenu("Test Single Passive")]
    public void TestSinglePassiveDescription()
    {
        string testPassiveInfo = allPassives.ReturnValue(testPassiveName);
        Debug.Log(testPassiveInfo);
        Debug.Log(detailViewer.ReturnPassiveDetails(testPassiveInfo));
    }

    [ContextMenu("Test Multiple Passive Single")]
    public void TestMultiplePassiveDescriptionSingle()
    {
        testPassiveNames = testPassiveNamesSingleString.Split("|").ToList();
        string allPassiveStrings = "";
        for (int i = 0; i < testPassiveNames.Count; i++)
        {
            string testPassiveInfo = allPassives.ReturnValue(testPassiveNames[i]);
            allPassiveStrings += testPassiveNames[i] + "\n";
            allPassiveStrings += detailViewer.ReturnPassiveDetails(testPassiveInfo) + "\n";
        }
        Debug.Log(allPassiveStrings);
    }
    [ContextMenu("Test Multiple Passive")]
    public void TestMultiplePassiveDescription()
    {
        testPassiveNames = testPassiveNamesSingleString.Split("|").ToList();
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
