using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RelicButtonTooltip : ToolTipTarget
{
    public GameObject go;
    public void DisableGO()
    {
        go.SetActive(false);
    }
    public void EnableGO()
    {
        go.SetActive(true);
    }
    public Image relicImage;
    public Image GetImage(){return relicImage;}
    public PartyAndRelicFrame partyFrame;
    public void ClickButton()
    {
        
    }
}
