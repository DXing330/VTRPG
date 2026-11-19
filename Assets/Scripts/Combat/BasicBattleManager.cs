using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicBattleManager<T, U> : ClickTileManager
    where T : BattleMap
    where U : ActorAI
{
    public T map;
    public U actorAI;
    public List<TacticActor> roundActors;
    public int currentTurn = 0;
    public AttackManager attackManager;
    public void ActorAttacksTarget(TacticActor actor)
    {
        if (!actor.TargetValid()){return;}
        ActorAttacksActor(actor, actor.GetTarget());
    }
    protected void ActorAttacksActor(TacticActor attacker, TacticActor defender)
    {
        if (!attacker.AttackActionsLeft()){return;}
        attacker.PayAttackCost();
        map.UpdateCombatLog(attacker.GetPersonalName() + " attacks " + defender.GetPersonalName() + ".");
        attackManager.ActorAttacksActorWithAttackSpeed(attacker, defender, map, attacker.GetBasicAttackMultiplier(), attacker.GetBasicAttackDamageType());
        List<int> basicAttackTargetTiles = map.mapUtility.GetTilesByShapeSpan(defender.GetLocation(), attacker.GetBasicAttackShape(), attacker.GetBasicAttackSpan(), map.mapSize, attacker.GetLocation());
        List<TacticActor> targetActors = map.GetActorsOnTiles(basicAttackTargetTiles);
        for (int i = 0; i < targetActors.Count; i++)
        {
            // Don't Hit Teammates Or The Target Twice.
            if (targetActors[i].GetTeam() == attacker.GetTeam() || targetActors[i] == defender){continue;}
            map.UpdateCombatLog(attacker.GetPersonalName() + " hits " + targetActors[i].GetPersonalName() + ".");
            attackManager.ActorAttacksActorWithAttackSpeed(attacker, targetActors[i], map, attacker.GetBasicAttackMultiplier(), attacker.GetBasicAttackDamageType());
        }
        map.UpdateMap();
    }
    public ActiveManager activeManager;
    public MoveCostManager moveManager;
    public EffectManager effectManager;
    public virtual void NewRound()
    {
        roundActors.Clear();
        for (int i = map.battlingActors.Count - 1; i >= 0; i--)
        {
            ActivateDeathPassive(map.battlingActors[i]);
        }
        List<TacticActor> currentRoundActors = new List<TacticActor>(map.battlingActors);
    }
    protected virtual void StartTurn()
    {
        roundActors[currentTurn].StartTurn();
        effectManager.StartTurn(roundActors[currentTurn], map);
    }
    public virtual void NextTurn()
    {
        currentTurn++;
        if (currentTurn >= roundActors.Count)
        {
            NewRound();
            return;
        }
        StartTurn();
        // Skip Dead/Stunned/Etc Do Nothing.
        if (roundActors[currentTurn].GetHealth() <= 0 || roundActors[currentTurn].GetActions() <= 0)
        {
        }
        else if (actorAI.MentalStateTurn(roundActors[currentTurn], map))
        {
        }
        // Then Team Checks.
        else if (roundActors[currentTurn].GetTeam() == 0)
        {
            AllyTurn();
        }
        else if (roundActors[currentTurn].GetTeam() != 0)
        {
            EnemyTurn();
        }
        // Always Reach Here.
        if (roundActors[currentTurn].GetHealth() > 0)
        {
            // This Calls NextTurn();
            EndActorTurn();
        }
        else
        {
            ActivateDeathPassive(roundActors[currentTurn]);
            NextTurn();
        }
    }
    public virtual void AllyTurn()
    {
    }
    public virtual void EnemyTurn()
    {
    }
    protected virtual void EndActorTurn()
    {
        effectManager.EndTurn(roundActors[currentTurn], map);
        roundActors[currentTurn].EndTurn();
        map.ApplyEndTerrainEffect(roundActors[currentTurn]);
        NextTurn();
    }
    protected void MoveAlongPath(TacticActor actor, List<int> path)
    {
        if (path.Count <= 0){return;}
        for (int i = path.Count - 1; i >= 0; i--)
        {
            int prevLoc = actor.GetLocation();
            moveManager.MoveActorToTile(actor, path[i], map);
            actor.PayMoveCost(moveManager.MoveCostOfTile(path[i]));
            if (actor.GetMovement() < 0 || actor.MovementStopped()){break;}
        }
    }
    public void ActivateDeathPassive(TacticActor actor)
    {
        if (actor.GetHealth() > 0){return;}
        if (actor.Resurrect())
        {
            actor.FullRestore();
            map.ResurrectActor(actor);
            return;
        }
        if (!map.RemoveActorFromBattle(actor)){return;}
        List<string> deathActives = new List<string>(actor.GetDeathActives());
        for (int i = 0; i < deathActives.Count; i++)
        {
            if (deathActives[i].Length <= 0) { continue; }
            activeManager.SetSkillFromName(deathActives[i], actor);
            activeManager.GetTargetedTiles(actor.GetLocation());
            ActivateSkill(deathActives[i], actor);
        }
    }
    public void ActivateSkill(string skillName, TacticActor actor = null)
    {
        if (actor == null){actor = roundActors[currentTurn];}
        activeManager.ActivateSkill(this);
    }
    public void ActivateSkill(string skillName, TacticActor actor, int targetTile)
    {
        activeManager.SetSkillFromName(skillName, actor);
        activeManager.GetTargetedTiles(targetTile);
        map.UpdateCombatLog(actor.GetPersonalName() + " uses " + skillName + ".");
        activeManager.ActivateSkill(this);
    }
}