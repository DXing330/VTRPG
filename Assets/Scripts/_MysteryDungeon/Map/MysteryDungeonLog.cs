using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MysteryDungeonLog : CombatLog
{
    // Simplified CombatLog.
    protected override void ResetLists()
    {
        MDLogs.Clear();
        MDDetails.Clear();
        MDDetailKeys.Clear();
    }
    public List<string> MDLogs;
    public List<string> MDDetails;
    public List<int> MDDetailKeys;
    public override string GetLatestLog()
    {
        if (MDLogs.Count <= 0){return "";}
        return MDLogs[MDLogs.Count - 1];
    }
    public override string ReturnDetailedLog(int round, int turn, int index)
    {
        int indexOf = MDDetailKeys.IndexOf(index);
        if (indexOf == -1){return "";}
        return MDDetails[indexOf];
    }
    public override void UpdateNewestLog(string newLog)
    {
        MDLogs.Add(newLog);
    }
    public override void AddDetailedLogs(string newDetail)
    {
        if (newDetail == ""){return;}
        int key = MDLogs.Count - 1;
        int indexOf = MDDetailKeys.IndexOf(key);
        if (indexOf == -1)
        {
            MDDetails.Add(newDetail);
            MDDetailKeys.Add(key);
        }
        else
        {
            MDDetails[indexOf] += "\n" + newDetail;
        }
    }
}
