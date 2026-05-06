using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Will Apply Relic Effects To Actors At Start Of Battle.
[CreateAssetMenu(fileName = "RelicBattleManager", menuName = "ScriptableObjects/BattleLogic/RelicBattleManager", order = 1)]
public class RelicBattleManager : ScriptableObject
{
    public PassiveSkill passive;
    public Relic relic;
    public StatDatabase relicData;
    public void ApplyBattleRelicEffects(List<TacticActor> battlingActors, List<string> relicNames, List<string> relicCounters)
    {
        for (int i = 0; i < relicNames.Count; i++)
        {
            relic.LoadRelic(relicData.ReturnValue(relicNames[i]), relicNames[i]);
            // Apply counters for each battle counter?
            // Determine which relics are battle relics.
            // Of those, check conditions.
            // Apply Effects To Party.
        }
    }
}
