using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class WagonStatDisplay : StatDisplay
{
    public TMP_Text baseWeight;
    public TMP_Text carryWeight;
    public TMP_Text durability;
    public override void ShowStats(string stats)
    {
        string[] data = stats.Split("|");
        baseWeight.text = data[0];
        carryWeight.text = data[1];
        durability.text = data[3]+"/"+data[2];
    }
}
