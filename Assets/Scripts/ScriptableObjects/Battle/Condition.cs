using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Condition", menuName = "ScriptableObjects/BattleLogic/Condition", order = 1)]
public class Condition : PassiveSkill
{
    List<string> statuses;
    List<string> statusInfo;

    protected bool Timing(string timing, string time)
    {
        if (timing == "ALL"){return true;}
        return timing == time;
    }

    protected bool OtherTiming(string timing)
    {
        if (timing == "ALL" || timing == "Start" || timing == "End")
        {
            return false;
        }
        return true;
    }

    protected bool ActorImmune(TacticActor actor, string status)
    {
        List<string> possibleImmunities = actor.GetStartTurnPassives();
        possibleImmunities.AddRange(actor.GetEndTurnPassives());
        for (int i = 0; i < possibleImmunities.Count; i++)
        {
            string[] details = possibleImmunities[i].Split("|");
            // If you are unconditionally immune then it does nothing.
            if (details[1] == "" && details[4] == "RemoveStatus" && details[5] == status)
            {
                return true;
            }
        }
        return false;
    }

    public void ApplyStartEndEffects(TacticActor actor, StatDatabase allData, string timing, BattleMap map)
    {
        statuses = new List<string>(actor.GetStatuses());
        for (int i = statuses.Count - 1; i >= 0; i--)
        {
            statusInfo = allData.ReturnStats(statuses[i]);
            if (!Timing(timing, statusInfo[0])){continue;}
            // If you are immune to the status then continue.
            if (ActorImmune(actor, statuses[i])){continue;}
            ApplyPassive(actor, map, allData.ReturnValue(statuses[i]));
            // Decrease duration by 1.
            actor.AdjustStatusDuration(i);
        }
    }

    public void ApplyBuffEffects(TacticActor actor, StatDatabase allData, string timing, BattleMap map)
    {
        statuses = new List<string>(actor.GetBuffs());
        for (int i = statuses.Count - 1; i >= 0; i--)
        {
            statusInfo = allData.ReturnStats(statuses[i]);
            if (!Timing(timing, statusInfo[0])){continue;}
            ApplyPassive(actor, map, allData.ReturnValue(statuses[i]));
            actor.AdjustBuffDuration(i);
        }
    }

    public void AdjustOtherTimingDurations(TacticActor actor, StatDatabase allData)
    {
        statuses = new List<string>(actor.GetBuffs());
        for (int i = statuses.Count - 1; i >= 0; i--)
        {
            statusInfo = allData.ReturnStats(statuses[i]);
            if (!OtherTiming(statusInfo[0])){continue;}
            actor.AdjustBuffDuration(i);
        }
        statuses = new List<string>(actor.GetStatuses());
        for (int i = statuses.Count - 1; i >= 0; i--)
        {
            statusInfo = allData.ReturnStats(statuses[i]);
            if (!OtherTiming(statusInfo[0])){continue;}
            actor.AdjustStatusDuration(i);
        }
    }
}
