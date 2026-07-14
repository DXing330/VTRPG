using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoChessLogDisplay : TextList
{
    public AutoChessLogDataManager logData;
    public void UpdateDisplay()
    {
        allText = logData.GetLogs();
        page = 0;
        UpdatePage();
    }
}
