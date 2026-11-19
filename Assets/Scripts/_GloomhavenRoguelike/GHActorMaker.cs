using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GHActorMaker : ActorMaker
{
    public StatDatabase GHActorData;
    public TacticActor CreateGHActorByName(string actorName)
    {
        string[] actorData = GHActorData.ReturnValue(actorName).Split("|");
        if (actorData.Length < 12){return null;}
        TacticActor newActor = new();
        newActor.SetPossibleActions(actorData[0]);
        newActor.SetBaseHealth(int.Parse(actorData[1]));
        newActor.SetCurrentHealth(int.Parse(actorData[1]));
        newActor.SetBaseAttack(int.Parse(actorData[2]));
        newActor.SetBaseDefense(int.Parse(actorData[3]));
        newActor.SetAttackRange(int.Parse(actorData[4]));
        newActor.SetMoveSpeed(int.Parse(actorData[5]));
        newActor.SetPassiveSkills(actorData[6].Split(",").ToList());
        newActor.SetPassiveLevels(actorData[7].Split(",").ToList());
        newActor.SetMultiHitCount(int.Parse(actorData[8]));
        newActor.SetBuffAction(actorData[9]);
        newActor.SetStatusAction(actorData[10]);
        newActor.SetSpecialAction(actorData[11]);
        return newActor;
    }
}
