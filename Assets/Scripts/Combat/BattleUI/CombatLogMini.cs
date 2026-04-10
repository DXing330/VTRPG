using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CombatLogMini : BattleUIBaseClass
{
    public CombatLog combatLog;
    public string latestLog;
    public List<string> latestLogs;
    public List<TMP_Text> latestLogTexts;
    public override void ResetUI()
    {
        for (int i = 0; i < latestLogTexts.Count; i++)
        {
            latestLogTexts[i].text = "";
        }
    }
    public override void UpdateUI()
    {
        ResetUI();
        latestLog = combatLog.GetLatestLog();
        latestLogs = latestLog.Split("|").ToList();
        int index = 0;
        for (int i = latestLogs.Count - 1; i >= 0; i--)
        {
            if (index >= latestLogTexts.Count){return;}
            latestLogTexts[index].text = latestLogs[i];
            index++;
        }
    }
}
