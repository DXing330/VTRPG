using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CaravanMuleStatDisplay : StatDisplay
{
    public TMP_Text health;
    public TMP_Text energy;
    public TMP_Text speed;
    public TMP_Text pullLoad;
    void Start()
    {
        for (int i = 0; i < textList.Count; i++){textList[i].fontSize = textSize;}
    }
    public override void ShowStats(string stats)
    {
        string[] blocks = stats.Split("|");
        pullLoad.text = blocks[0];
        speed.text = blocks[1];
        energy.text = blocks[4]+"/"+blocks[2];
        health.text = blocks[5]+"/"+blocks[3];
    }
}
