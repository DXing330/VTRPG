using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatusDetailViewer : PassiveDetailViewer
{
    public StatDatabase allStatuses;
    public StatDatabase weatherPassives;
    public SelectStatTextList statusSelect;
    public PopUpMessage popUp;
    public void SelectStatus()
    {
        if (statusSelect.GetSelected() < 0){return;}
        popUp.SetMessage(ReturnStatusDetails(allStatuses.ReturnValue(statusSelect.GetSelectedStat())));
    }

    public string ReturnWeatherDetails(string weatherName)
    {
        string wDetails = weatherName + ":";
        // Kinda a pain to check delimiters every time, should get a master mapping of delimiters later.
        string[] wPassives = weatherPassives.ReturnValue(weatherName).Split("!");
        for (int i = 0; i < wPassives.Length; i++)
        {
            if (wPassives[i].Length < 6){continue;}
            wDetails += "\n";
            wDetails += ReturnPassiveDetails(wPassives[i]);
        }
        return wDetails;
    }

    [ContextMenu("View All Statuses")]
    public void ViewAllStatuses()
    {
        List<string> statusNames = allStatuses.GetAllKeys();
        for (int i = 0; i < statusNames.Count; i++)
        {
            Debug.Log(statusNames[i] + " : " + ReturnStatusDetails(allStatuses.ReturnValue(statusNames[i])));
        }
    }

    public string ReturnStatusDetails(string newInfo)
    {
        return ReturnPassiveDetails(newInfo);
    }
}
