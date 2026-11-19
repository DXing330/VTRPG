using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GHActorAI", menuName = "ScriptableObjects/Gloomhaven/GHActorAI", order = 1)]
public class GHActorAI : ActorAI
{
    public StatDatabase GHActorData;
    public void DetermineGHIntent(TacticActor actor, GHBattleMap map)
    {
        string[] possibleIntentions = GHActorData.ReturnValue(actor.GetSpriteName()).Split("|")[0].Split(",");
    }
}
