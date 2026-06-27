using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoChessFactionIconToolTip : ToolTipTarget
{
    public AutoChessFactionDisplay factionDisplay;
    public void ClickButton()
    {
        factionDisplay.ClickFactionToolTipButton(tooltipIndex);
    }
}
