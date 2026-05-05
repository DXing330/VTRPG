using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolTipTarget : MonoBehaviour
{
    public ToolTipPopUp tooltipPopup;
    public string tooltipText;
    public RectTransform anchor;
    public void ShowTooltip()
    {
        tooltipPopup.ShowTooltip(tooltipText, anchor);
    }
}
