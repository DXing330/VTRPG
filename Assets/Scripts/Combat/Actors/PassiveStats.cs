using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PassiveStats
{
    public string delimiter = "|";
    public string timing;
    public void SetTiming(string newInfo){timing = newInfo;}
    public string condition;
    public void SetConditionAndSpecifics(string newInfo, string delimiter = " ")
    {
        string[] data = newInfo.Split(delimiter);
        condition = data[0];
        if (data.Length < 2)
        {
            conditionSpecifics = "";
        }
        else
        {
            conditionSpecifics = data[1];
        }
    }
    public string conditionSpecifics;
    public string target;
    public string effect;
    public string effectSpecifics;
    public void SetTargetEffectAndSpecifics(string newInfo, string delimiter = " ", int multiplier = 1)
    {
        string[] data = newInfo.Split(delimiter);
        target = data[0];
        if (data.Length < 2)
        {
            effect = "";
            effectSpecifics = "";
        }
        else
        {
            effect = data[1];
        }
        if (data.Length < 3)
        {
            effectSpecifics = "";
        }
        else
        {
            try
            {
                int multiplied = int.Parse(data[2]) * multiplier;
                effectSpecifics = multiplied.ToString();
            }
            catch
            {
                effectSpecifics = data[2];
            }
        }
    }
    public void ResetStats()
    {
        timing = "";
        condition = "";
        conditionSpecifics = "";
        target = "";
        effect = "";
        effectSpecifics = "";
    }
    public string ReturnStats()
    {
        return timing + delimiter + condition + delimiter + conditionSpecifics + delimiter + target + delimiter + effect + delimiter + effectSpecifics;
    }
}
