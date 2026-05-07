using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Will Apply Relic Effects To Actors At Start Of Battle.
[CreateAssetMenu(fileName = "RelicBattleManager", menuName = "ScriptableObjects/BattleLogic/RelicBattleManager", order = 1)]
public class RelicBattleManager : ScriptableObject
{
    public StSState stsState;
    public PassiveSkill passive;
    public Relic relic;
    public StatDatabase relicData;
    public void ApplyBattleRelicEffects(List<TacticActor> battlingActors, DungeonBag bag)
    {
        List<string> relicNames = bag.GetRelics();
        List<string> relicCounters = bag.GetRelicCounters();
        // Determine allies vs enemy lists.
        List<TacticActor> allies = new List<TacticActor>();
        List<TacticActor> enemies = new List<TacticActor>();
        for (int i = 0; i < battlingActors.Count; i++)
        {
            if (battlingActors[i].GetTeam() == 0)
            {
                allies.Add(battlingActors[i]);
            }
            else
            {
                enemies.Add(battlingActors[i]);
            }
        }
        // Load All Relics.
        for (int i = 0; i < relicNames.Count; i++)
        {
            relic.LoadRelic(relicData.ReturnValue(relicNames[i]), relicNames[i]);
            // Apply counters for each battle counter?
            if (relic.GetCounterTiming() == "Battle")
            {
                relicCounters[i] = (int.Parse(relicCounters[i]) + 1).ToString();
            }
            // Determine which relics are battle relics.
            if (relic.BattleRelic())
            {
                // Battle Relics Either Target Allies Or Enemies
                if (relic.GetTarget() == "Allies")
                {
                    ApplyBattleRelicEffect(allies, relicCounters, i);
                }
                else
                {
                    ApplyBattleRelicEffect(enemies, relicCounters, i);
                }
            }
        }
    }
    protected void ApplyBattleRelicEffect(List<TacticActor> actors, List<string> relicCounters, int index)
    {
        // TODO check conditions.
        // Check Counter Conditions.
        if (relic.GetCondition() == "Counter")
        {
            // Enough Counters > Reset Counters.
            if (int.Parse(relicCounters[index]) >= int.Parse(relic.GetConditionSpecifics()))
            {
                relicCounters[index] = "0";
            }
            // Not enough counters > do nothing.
            else
            {
                return;
            }
        }
        // TODO Check Other Conditions, Based On Map State.
        else
        {
            if (!CheckBattleRelicCondition()){return;}
        }
        // Apply Effects To Party.
        for (int i = 0; i < actors.Count; i++)
        {
            passive.AffectActor(actors[i], relic.GetEffect(), relic.GetEffectSpecifics());
        }
    }
    protected bool CheckBattleRelicCondition()
    {
        switch (relic.GetCondition())
        {
            // No Conditions.
            default:
            return true;
            case "Type":
            return relic.GetConditionSpecifics() == stsState.GetBattleType();
            case "Act":
            return relic.GetConditionSpecifics() == stsState.GetFloor().ToString();
        }
    }
}
