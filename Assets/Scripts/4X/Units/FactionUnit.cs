using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class FactionUnit
{
    // Stores actors and equipment which already use "|", "+" and "," delimiters.
    protected string delimiter = "#";
    protected string delimiterTwo = "&";
    public int location;
    public void SetLocation(int newInfo){location = newInfo;}
    // Used if pushed out of the current location?
    public int previousLocation;
    public void SetPreviousLocation(int newInfo){previousLocation = newInfo;}
    public string faction;
    public void SetFaction(string newInfo){faction = newInfo;}
    public int level;
    public void SetLevel(int newInfo){level = newInfo;}
    public int exp;
    public void SetExp(int newInfo){exp = newInfo;}
    public List<string> unitActorSprites;
    public List<int> unitActorSquadSize;
    public List<string> unitActorStats;
    public List<string> unitActorEquipment;

    public void ResetStats()
    {
        location = -1;
        previousLocation = -1;
        faction = "";
        level = 1;
        exp = 0;
        unitActorSprites.Clear();
        unitActorSquadSize.Clear();
        unitActorStats.Clear();
        unitActorEquipment.Clear();
    }

    public string GetStats()
    {
        string stats = "";
        stats += "Location="+location + delimiter;
        stats += "PreviousLocation="+previousLocation + delimiter;
        stats += "Faction="+faction + delimiter;
        stats += "Level="+level + delimiter;
        stats += "Exp="+exp + delimiter;
        stats += "Sprites="+String.Join(delimiterTwo, unitActorSprites) + delimiter;
        stats += "SquadSizes="+String.Join(delimiterTwo, unitActorSquadSize) + delimiter;
        stats += "Stats="+String.Join(delimiterTwo, unitActorStats) + delimiter;
        stats += "Equipment="+String.Join(delimiterTwo, unitActorEquipment) + delimiter;
        return stats;
    }

    public void SetStats(string newInfo)
    {
        string[] blocks = newInfo.Split(delimiter);
        for (int i = 0; i < blocks.Length; i++)
        {
            SetStat(blocks[i]);
        }
    }

    protected void SetStat(string newStat)
    {
        string[] blocks = newStat.Split("=");
        if (blocks.Length < 2){return;}
        string value = blocks[1];
        switch (blocks[0])
        {
            case "Location":
                location = int.Parse(value);
                break;
            case "PreviousLocation":
                previousLocation = int.Parse(value);
                break;
            case "Faction":
                faction = value;
                break;
            case "Level":
                level = int.Parse(value);
                break;
            case "Exp":
                exp = int.Parse(value);
                break;
            case "Sprites":
                unitActorSprites = value.Split(delimiterTwo).ToList();
                break;
            case "SquadSizes":
                unitActorSquadSize = value.Split(delimiterTwo).Select(int.Parse).ToList();
                break;
            case "Stats":
                unitActorStats = value.Split(delimiterTwo).ToList();
                break;
            case "Equipment":
                unitActorEquipment = value.Split(delimiterTwo).ToList();
                break;
            default:
                break;
        }
    }
}
