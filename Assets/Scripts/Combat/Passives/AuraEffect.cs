using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// These will be generated at runtime whenever an aura is created.
// Aura list is stored in the terrain effects.
[System.Serializable]
public class AuraEffect
{
    // Auras don't store anything else, so they can use the most basic delimiters.
    public string delimiter = "|";
    public void InitializeAura(TacticActor user, int targetLocation, int newDuration, string auraData)
    {
        actor = user;
        team = actor.GetTeam();
        triggeredActors = new List<TacticActor>();
        actorAura = 1;
        location = user.GetLocation();
        duration = newDuration;
        string[] data = auraData.Split(delimiter);
        for (int i = 0; i < data.Length; i++)
        {
            LoadStat(data[i], i);
        }
        if (center != "User")
        {
            actorAura = 0;
            location = targetLocation;
        }
    }
    protected void LoadStat(string stat, int index)
    {
        switch (index)
        {
            default:
            return;
            case 0:
                auraName = stat;
                return;
            case 1:
                teamTarget = stat;
                return;
            case 2:
                center = stat;
                return;
            case 3:
                shape = stat;
                return;
            case 4:
                span = stat;
                return;
            case 5:
                trigger = int.Parse(stat);
                return;
            case 6:
                triggerType = stat;
                return;
            case 7:
                condition = stat;
                return;
            case 8:
                conditionSpecifics = stat;
                return;
            case 9:
                target = stat;
                return;
            case 10:
                effect = stat;
                return;
            case 11:
                effectSpecifics = stat;
                return;
        }
    }
    // For now only actor auras, terrain already has terrain effects.
    // Auras only affect other actors, we have passives that can do every map effect anyway.
    // Actor auras move with the actor.
    public int actorAura;
    public bool ActorAura()
    {
        return actorAura > 0;
    }
    public TacticActor actor;
    public bool AuraOwner(TacticActor nActor)
    {
        return actor == nActor;
    }
    public int team;
    public int location;
    public void UpdateAuraLocation()
    {
        if (ActorAura() && actor != null && actor.GetHealth() > 0)
        {
            location = actor.GetLocation();
        }
    }
    // Auras last for some amount of rounds/turns.
    public int duration;
    public void ActorEndsTurn(BattleMap map, TacticActor eActor)
    {
        if (actor == null || actor.GetHealth() <= 0)
        {
            map.RemoveAura(this);
        }
        if (eActor == actor && duration <= 0)
        {
            map.RemoveAura(this);
        }
    }
    public void NextRound(BattleMap map)
    {
        triggeredActors.Clear();
        duration--;
        if (actor == null || actor.GetHealth() <= 0)
        {
            map.RemoveAura(this);
        }
    }
    // Used for logging.
    public string auraName;
    public string GetAuraName(){return auraName;}
    // Ally -> same team, Enemy -> !same team
    public string teamTarget;
    public string GetTeamTarget()
    {
        // A/D is always for attacker / defender
        if (teamTarget.EndsWith("A"))
        {
            return teamTarget + "ttacker";
        }
        if (teamTarget.EndsWith("D"))
        {
            return teamTarget + "efender";
        }
        return teamTarget;
    }
    public bool TeamCheck(TacticActor target)
    {
        switch (teamTarget)
        {
            case "All":
            return true;
            case "Enemy":
            return target.GetTeam() != team;
            case "Ally":
            return target.GetTeam() == team;
        }
        return false;
    }
    public bool BattleTeamCheck(TacticActor target, TacticActor attacker, BattleMap map)
    {
        bool attackerInside = ActorInAura(attacker, map);
        bool targetInside = ActorInAura(target, map);
        if (!targetInside && !attackerInside){return false;}
        switch (teamTarget)
        {
            default:
            return false;
            case "EnemyA":
            return attacker.GetTeam() != team && attackerInside;
            case "EnemyD":
            return target.GetTeam() != team && targetInside;
            case "AllyA":
            return attacker.GetTeam() == team && attackerInside;
            case "AllyD":
            return target.GetTeam() == team && targetInside;
        }
    }
    // User/Fixed/Moving/Random?
    public string center;
    // Circle, Cone, ELine
    public string shape;
    public string span;
    public int GetSpan()
    {
        switch (span)
        {
            default:
            return int.Parse(span);
            case "AttackRange":
            return actor.GetAttackRange();
        }
    }
    public bool ActorInAura(TacticActor actor, BattleMap map)
    {
        return GetAuraTiles(map).Contains(actor.GetLocation());
    }
    // Use the map utility.
    // Also use the actor direction for cone auras.
    public List<int> GetAuraTiles(BattleMap map)
    {
        int start = map.mapUtility.PointInDirection(location, actor.GetDirection(), map.mapSize);
        switch (shape)
        {
            default:
            return map.mapUtility.GetTilesByShapeSpan(location, shape, GetSpan(), map.mapSize, start);
            case "Cone":
            return map.mapUtility.GetTilesByShapeSpan(start, shape, GetSpan(), map.mapSize, location);
        }
    }
    // Trigger auras apply whenever someone moves into them for the first time each round and meets the conditions, ex. Spirit Guardians.
    // Non trigger aura apply extra passive effects during battle if the actors meets the conditions, ex. Bernhardt.
    // Trigger - Start, End, Moving
    // Nontrigger - Attacking, Defending
    public int trigger;
    public string triggerType;
    public bool TriggerAura(string triggerTiming)
    {
        if (trigger <= 0){return false;}
        return triggerType == triggerTiming;
    }
    public bool AlreadyTriggered(TacticActor actor)
    {
        return triggeredActors.Contains(actor);
    }
    public void ActorTriggersAura(TacticActor actor)
    {
        if (!triggeredActors.Contains(actor))
        {
            triggeredActors.Add(actor);
        }
    }
    // Each actor can only be affected by each trigger aura once per round.
    public List<TacticActor> triggeredActors;
    // Regular passive stuff.
    public string condition;
    public string conditionSpecifics;
    public string target;
    public string GetTarget(){return target;}
    public string effect;
    public string GetEffect(){return effect;}
    public string effectSpecifics;
    public string GetEffectSpecifics(){return effectSpecifics;}
    public List<string> ReturnPassiveStats()
    {
        List<string> stats = new List<string>();
        stats.Add(GetAuraName());
        stats.Add(condition);
        stats.Add(conditionSpecifics);
        stats.Add(target);
        stats.Add(effect);
        stats.Add(effectSpecifics);
        return stats;
    }
}