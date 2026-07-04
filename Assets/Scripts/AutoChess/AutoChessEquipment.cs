using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AutoChessEquipment
{
    public string equipmentName;
    public string GetName(){return equipmentName;}
    public string timing;
    public string GetTiming(){return timing;}
    public string target;
    public string GetTarget(){return target;}
    public string effect;
    public string GetEffect(){return effect;}
    public string specifics;
    public string GetSpecifics(){return specifics;}
    public void LoadAutoChessEquipStats(string newName, string newData)
    {
        string[] blocks = newData.Split("|");
        equipmentName = newName;
        timing = blocks[0];
        target = blocks[1];
        effect = blocks[2];
        specifics = blocks[3];
    }
}
