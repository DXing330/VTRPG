using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolTipTarget : MonoBehaviour
{
    public ToolTipPopUp tooltipPopup;
    public int tooltipIndex;
    public void SetToolTipIndex(int newIndex)
    {
        tooltipIndex = newIndex;
    }
    public string tooltipText;
    public RectTransform anchor;
    public void ShowTooltip(string newText = "")
    {
        if (newText != "")
        {
            tooltipText = newText;
        }
        tooltipPopup.ShowTooltip(tooltipText, anchor);
    }
}
