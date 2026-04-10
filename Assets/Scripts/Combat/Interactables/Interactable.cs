using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interactable : MonoBehaviour
{
    public GeneralUtility utility;
    public GameObject go;
    public string delimiter = "|";
    public bool activated = false;
    public string spriteName;
    public string GetSpriteName(){return spriteName;}
    public int health;
    public int singleUse;
    public int maxState;
    public int state = 0;
    // This entire class is created so that we can have a drawbridge over deep water.
    public string trigger; // Attack / Move
    public List<string> conditions;
    public List<string> conditionSpecifics;
    public void AddCondition(string condition, string specifics)
    {
        conditions.Add(condition);
        conditionSpecifics.Add(specifics);
    }
    public void ForceCondition(string condition, string specifics)
    {
        conditions.Clear();
        conditionSpecifics.Clear();
        conditions.Add(condition);
        conditionSpecifics.Add(specifics);
    }
    public void SetConditions(List<string> newC, List<string> newS)
    {
        conditions = newC;
        conditionSpecifics = newS;
    }
    public string target;
    public List<string> effects;
    public List<string> effectSpecifics;
    public int location;
    public void SetLocation(int newInfo){location = newInfo;}
    public int GetLocation(){return location;}
    public List<int> targetLocations;
    public void AddTargetLocation(int newInfo)
    {
        targetLocations.Add(newInfo);
    }
    public void AddTargetLocations(List<int> newInfo)
    {
        for (int i = 0; i < newInfo.Count; i++)
        {
            AddTargetLocation(newInfo[i]);
        }
    }
    public void ResetStats()
    {
        spriteName = "";
        health = 1;
        singleUse = 0;
        maxState = 0;
        state = 0;
        trigger = "";
        conditions.Clear();
        conditionSpecifics.Clear();
        target = "";
        effects.Clear();
        effectSpecifics.Clear();
        location = -1;
        targetLocations.Clear();
    }
    public void SetStats(string stats)
    {
        string[] dataBlocks = stats.Split(delimiter);
        for (int i = 0; i < dataBlocks.Length; i++)
        {
            SetStat(dataBlocks[i], i);
        }
    }
    protected void SetStat(string stat, int index)
    {
        switch (index)
        {
            case 0: spriteName = stat;
                break;
            case 1: health = int.Parse(stat);
                break;
            case 2: singleUse = int.Parse(stat);
                break;
            case 3: maxState = int.Parse(stat);
                break;
            case 4: trigger = stat;
                break;
            case 5: conditions = stat.Split(",").ToList();
                break;
            case 6: conditionSpecifics = stat.Split(",").ToList();
                break;
            case 7: target = stat;
                break;
            case 8: effects = stat.Split(",").ToList();
                break;
            case 9: effectSpecifics = stat.Split(",").ToList();
                break;
            case 10: location = int.Parse(stat);
                break;
            case 11: targetLocations = utility.ConvertStringListToIntList(stat.Split(",").ToList());
                break;
        }
    }
    public string GetStat(int index)
    {
        switch (index)
        {
            default:
            return "";
            case 0: return spriteName;
            case 1: return health.ToString();
            case 2: return singleUse.ToString();
            case 3: return state.ToString();
            case 4: return trigger;
            case 5: return String.Join(",", conditions);
            case 6: return String.Join(",", conditionSpecifics);
            case 7: return target;
            case 8: return String.Join(",", effects);
            case 9: return String.Join(",", effectSpecifics);
            case 10: return location.ToString();
            case 11: return String.Join(",", targetLocations);
        }
    }
    public int statCount = 12;
    public string ReturnStatString()
    {
        string allStats = "";
        for (int i = 0; i < statCount; i++)
        {
            allStats += GetStat(i) + delimiter;
        }
        return allStats;
    }
    public void EndTrigger(BattleMap map, TacticActor triggerer)
    {
        if (trigger != "End"){return;}
        for (int i = 0; i < targetLocations.Count; i++)
        {
            // Check the conditions to activate.
            if (CheckCondition(map, triggerer, i))
            {
                // If so then activate.
                Activate(map, i);
            }
        }
        if (activated)
        {
            map.combatLog.UpdateNewestLog(triggerer.GetPersonalName() + " triggered a " + GetSpriteName() + "!");
            UpdateAfterActivation(map);
        }
    }
    public void MoveTrigger(BattleMap map, TacticActor triggerer)
    {
        if (trigger != "Move"){return;}
        for (int i = 0; i < targetLocations.Count; i++)
        {
            // Check the conditions to activate.
            if (CheckCondition(map, triggerer, i))
            {
                // If so then activate.
                Activate(map, i);
            }
        }
        if (activated)
        {
            map.combatLog.UpdateNewestLog(triggerer.GetPersonalName() + " triggered a " + GetSpriteName() + "!");
            UpdateAfterActivation(map);
        }
    }
    protected void RemoveInteractable(BattleMap map)
    {
        map.RemoveInteractable(this);
        // Need to also destroy the gameobject?
        Destroy(go);
    }
    public void AttackTrigger(BattleMap map, TacticActor triggerer)
    {
        if (trigger != "Attack")
        {
            // Attacking destroys move interactables.
            RemoveInteractable(map);
            return;
        }
        else
        {
            for (int i = 0; i < targetLocations.Count; i++)
            {
                // Check the conditions to activate.
                if (CheckCondition(map, triggerer, i))
                {
                    // If so then activate.
                    Activate(map, i);
                }
            }
        }
        if (activated)
        {
            map.combatLog.UpdateNewestLog(triggerer.GetPersonalName() + " uses " + GetSpriteName());
            UpdateAfterActivation(map);
        }
    }
    protected void UpdateAfterActivation(BattleMap map)
    {
        activated = false;
        if (singleUse > 0)
        {
            RemoveInteractable(map);
        }
        else
        {
            state = (state + 1) % maxState;
        }
    }
    protected bool CheckCondition(BattleMap map, TacticActor triggerer, int index)
    {
        switch (conditions[state%conditions.Count])
        {
            case "Tile":
                return map.mapInfo[targetLocations[index]].Contains(conditionSpecifics[state%conditionSpecifics.Count]);
            // Some interactables can only be triggered by some teams.
            case "Team":
                return triggerer.GetTeam() == int.Parse(conditionSpecifics[state%conditionSpecifics.Count]);
            // Traps can only be triggered by the opposite team.
            case "Team<>":
                return triggerer.GetTeam() != int.Parse(conditionSpecifics[state%conditionSpecifics.Count]);
        }
        return true;
    }
    protected void Activate(BattleMap map, int index)
    {
        activated = true;
        switch (target)
        {
            default:
            // Default is target map.
            // Force changes the tiles, this makes them very resistance to tile changing effects, call it magical reinforcement.
            map.ChangeTile(targetLocations[index], effects[state%effects.Count], effectSpecifics[state%effectSpecifics.Count], true);
            return;
            case "Actor":
            map.passiveEffect.AffectActor(map.GetActorOnTile(targetLocations[index]), effects[state%effects.Count], effectSpecifics[state%effectSpecifics.Count], 1, map.combatLog);
            return;
            case "MoveActor":
            map.MoveActor(map.GetActorOnTile(targetLocations[index]), effects[state%effects.Count], effectSpecifics[state%effectSpecifics.Count]);
            return;
        }
    }
}
