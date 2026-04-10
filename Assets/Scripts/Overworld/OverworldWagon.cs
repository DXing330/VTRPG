using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OverworldWagon : MonoBehaviour
{
    public string delimiter = "|";
    public int weight;
    public int GetWeight(){return weight;}
    public int maxCarryWeight;
    public int GetCarryWeight(){return maxCarryWeight;}
    public string wheelsType;
    public string coverType;
    public int maxDurability;
    public int GetMaxDurability(){return maxDurability;}
    public int currentDurability;
    public int GetDurability(){return currentDurability;}

    protected void ResetStats()
    {
        weight = 0;
        maxCarryWeight = 0;
        maxDurability = 0;
        currentDurability = 0;
        wheelsType = "";
        coverType = "";
    }

    public void LoadAllStats(string newStats)
    {
        if (newStats.Length < 6)
        {
            ResetStats();
            return;
        }
        string[] data = newStats.Split(delimiter);
        weight = int.Parse(data[0]);
        maxCarryWeight = int.Parse(data[1]);
        maxDurability = int.Parse(data[2]);
        currentDurability = int.Parse(data[3]);
        wheelsType = data[4];
        coverType = data[5];
    }

    public string ReturnStats()
    {
        return weight+delimiter+maxCarryWeight+maxDurability+delimiter+currentDurability+delimiter+wheelsType+delimiter+coverType+delimiter;
    }
}
