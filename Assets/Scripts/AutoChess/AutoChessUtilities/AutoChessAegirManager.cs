using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoChessAegirManager : MonoBehaviour
{
    public int CONSUMEHPLOSS = 200;
    public List<TacticActor> aegirActors;
    public List<TacticActor> consumedActors;
    public List<TacticActor> consumingActors;
    protected BattleMap map;
    protected AutoBattleManager manager;
    int aegirTeam = -1;
    public void ApplyAegirFactionEffect(List<TacticActor> allActors, BattleMap newMap, AutoBattleManager newManager, int team = 0)
    {
        aegirTeam = team;
        aegirActors.Clear();
        consumedActors.Clear();
        consumingActors.Clear();
        map = newMap;
        manager = newManager;
        // Get The Aegir Actors.
        for (int i = 0; i < allActors.Count; i++)
        {
            if (allActors[i].GetTeam() != aegirTeam){continue;}
            if (allActors[i].AutoChessFaction("Aegir"))
            {
                aegirActors.Add(allActors[i]);
            }
        }
        // Flip Then Flip Back.
        if (team != 0)
        {
            for (int i = 0; i < allActors.Count; i++)
            {
                if (allActors[i].GetTeam() != aegirTeam){continue;}
                allActors[i].SetLocation(map.mapUtility.HorizontalReflectTile(allActors[i].GetLocation(), map.mapSize));
                allActors[i].FlipDirection();
            }
        }
        // Determine Consumed/Consuming.
        for (int i = 0; i < aegirActors.Count; i++)
        {
            TacticActor consumed = map.GetActorInFrontActor(aegirActors[i], true);
            // Only Consume Allies
            if (consumed != null && consumed.GetTeam() == aegirTeam)
            {
                consumedActors.Add(consumed);
                consumingActors.Add(aegirActors[i]);
            }
        }
        // Iterate Through Consumed Actors Until It's Empty.
        int consumedCount = consumedActors.Count;
        for (int i = 0; i < consumedCount; i++)
        {
            bool didConsume = false;
            // Each Loop Removed One Consumed Actor.
            for (int j = 0; j < consumedActors.Count; j++)
            {
                // Skip Any Actors That Are Also Consuming.
                if (!CanConsume(j))
                {
                    continue;
                }
                Consume(j);
                didConsume = true;
                break;
            }
            if (!didConsume){break;}
        }
        // This Should Only Happen If There Is A Loop.
        if (consumedActors.Count > 0)
        {
            int remaining = consumedActors.Count;
            for (int pass = 0; pass < remaining; pass++)
            {
                int consumedIndex = 0;
                for (int i = 1; i < consumingActors.Count; i++)
                {
                    if (consumingActors[i].GetLocation() < consumingActors[consumedIndex].GetLocation())
                    {
                        consumedIndex = i;
                    }
                }
                Consume(consumedIndex);
            }
        }
        if (team != 0)
        {
            for (int i = 0; i < allActors.Count; i++)
            {
                if (allActors[i].GetTeam() != aegirTeam){continue;}
                allActors[i].SetLocation(map.mapUtility.HorizontalReflectTile(allActors[i].GetLocation(), map.mapSize));
                allActors[i].FlipDirection();
            }
        }
    }
    protected bool CanConsume(int index)
    {
        return !consumingActors.Contains(consumedActors[index]);
    }
    protected void Consume(int index)
    {
        int attackGain = consumedActors[index].GetBaseAttack();
        int originalAttack = consumingActors[index].GetBaseAttack();
        consumingActors[index].UpdateBaseAttack(attackGain);
        consumedActors[index].UpdateHealth(CONSUMEHPLOSS);
        map.UpdateCombatLog(consumingActors[index].GetSpriteName() + " consumes " + consumedActors[index].GetSpriteName() + " and gains " + attackGain + " attack. (" + originalAttack + "->" + consumingActors[index].GetBaseAttack() + ")");
        consumedActors.RemoveAt(index);
        consumingActors.RemoveAt(index);
        manager.GainFactionStacks("Aegir", 2, aegirTeam);
    }
}
