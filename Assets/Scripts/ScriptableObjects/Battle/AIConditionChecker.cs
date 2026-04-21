using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AIConditionChecker", menuName = "ScriptableObjects/BattleLogic/AIConditionChecker", order = 1)]
public class AIConditionChecker : ScriptableObject
{
    public bool CheckConditions(string conditions, string specifics, TacticActor actor, BattleMap map)
    {
        List<string> allConditions = conditions.Split(",").ToList();
        List<string> allSpecifics = specifics.Split(",").ToList();
        for (int i = 0; i < allConditions.Count; i++)
        {
            if (!CheckCondition(allConditions[i], allSpecifics[i], actor, map))
            {
                return false;
            }
        }
        return true;
    }

    public ActiveSkill active;
    public StatDatabase activeData;
    public MagicSpell spell;
    public StatDatabase basicSpellData;

    public string GetAvailableSkillWithEffect(TacticActor actor, BattleMap map, string skillEffect)
    {
        List<string> actorActives = actor.GetActiveSkills();
        for (int i = 0; i < actorActives.Count; i++)
        {
            active.LoadSkillFromString(activeData.ReturnValue(actorActives[i]), actor);
            if (active.GetEffect() == skillEffect && active.Activatable(actor, map))
            {
                return actorActives[i];
            }
        }
        return "";
    }

    public string GetAvailableSpellWithEffect(TacticActor actor, BattleMap map, string skillEffect)
    {
        List<string> actorSpells = actor.GetSpells();
        for (int i = 0; i < actorSpells.Count; i++)
        {
            spell.LoadSkillFromString(basicSpellData.ReturnValue(actorSpells[i]));
            if (spell.GetEffect().Contains(skillEffect) && spell.Activatable(actor, map))
            {
                return actorSpells[i];
            }
        }
        return "";
    }

    protected bool CheckCondition(string condition, string specifics, TacticActor actor, BattleMap map)
    {
        switch (condition)
        {
            case "Damaged>":
                return actor.GetBaseHealth() - actor.GetHealth() > int.Parse(specifics);
            case "Damaged<":
                return actor.GetBaseHealth() - actor.GetHealth() < int.Parse(specifics);
            case "Sprite":
                return actor.GetSpriteName() == specifics;
            case "<>TempPassive":
                return !actor.tempPassives.Contains(specifics);
            case "SkillExists":
                return actor.SkillExists(specifics);
            case "TempActiveCount>":
                return actor.GetTempActives().Count > int.Parse(specifics);
            case "TempActiveCount<":
                return actor.GetTempActives().Count < int.Parse(specifics);
            case "AdjacentActorCount<":
                return map.GetAdjacentActors(actor.GetLocation()).Count < int.Parse(specifics);
            case "AdjacentActorCount>":
                return map.GetAdjacentActors(actor.GetLocation()).Count > int.Parse(specifics);
            case "AdjacentAllyCount<":
                return map.GetAdjacentAllies(actor).Count < int.Parse(specifics);
            case "AdjacentAllyCount>":
                return map.GetAdjacentAllies(actor).Count > int.Parse(specifics);
            case "AdjacentEnemyCount>":
                return map.GetAdjacentEnemies(actor).Count > int.Parse(specifics);
            case "AdjacentEnemyCount<":
                return map.GetAdjacentEnemies(actor).Count < int.Parse(specifics);
            case "AdjacentEnemyCount":
                return map.GetAdjacentEnemies(actor).Count == int.Parse(specifics);
            case "AttackableEnemyCount>":
                return map.GetAttackableEnemies(actor).Count > int.Parse(specifics);
            case "AttackableEnemyCount<":
                return map.GetAttackableEnemies(actor).Count < int.Parse(specifics);
            case "Shootable":
                return map.ShootableEnemies(actor, int.Parse(specifics));
            case "MaxEnergy":
                return actor.GetEnergy() >= actor.GetBaseEnergy();
            case "Health":
                switch (specifics)
                {
                    case "<Half":
                        return actor.GetHealth() * 2 <= actor.GetBaseHealth();
                    case ">Half":
                        return actor.GetHealth() * 2 >= actor.GetBaseHealth();
                }
                return false;
            case "Weather":
                return map.GetWeather().Contains(specifics);
            case "Weather<>":
                return !map.GetWeather().Contains(specifics);
            case "Time":
                return specifics == map.GetTime();
            case "Time<>":
                return specifics != map.GetTime();
            case "MoveType":
                return specifics == actor.GetMoveType();
            case "MoveType<>":
                return specifics != actor.GetMoveType();
            case "Energy<":
                return actor.GetEnergy() < int.Parse(specifics);
            case "Round":
                switch (specifics)
                {
                    case "Even":
                        return map.GetRound() % 2 == 0;
                    case "Odd":
                        return (map.GetRound() + 1) % 2 == 0;
                }
                return map.GetRound() % int.Parse(specifics) == 0;
            case "Counter":
            return actor.GetCounter() == int.Parse(specifics);
            case "Counter<":
            return actor.GetCounter() < int.Parse(specifics);
            case "Counter>":
            return actor.GetCounter() > int.Parse(specifics);
            case "AllyCount<":
                return map.AllAllies(actor).Count < int.Parse(specifics);
            case "AllyCount>":
                return map.AllAllies(actor).Count > int.Parse(specifics);
            case "AllyExists":
                return map.AllyExists(specifics, actor.GetTeam());
            case "AllyExists<>":
                return !map.AllyExists(specifics, actor.GetTeam());
            case "EnemyCount<":
                return map.AllEnemies(actor).Count < int.Parse(specifics);
            case "EnemyCount>":
                return map.AllEnemies(actor).Count > int.Parse(specifics);
            case "Grappling":
                return actor.Grappling();
            case "Grappling<>":
                return !actor.Grappling();
            case "Tile":
                return map.GetTileInfoOfActor(actor).Contains(specifics);
            case "Tile<>":
                return !map.GetTileInfoOfActor(actor).Contains(specifics);
            case "TileExists":
                return map.TileTypeExists(specifics);
            case "TileExists<>":
                return !map.TileTypeExists(specifics);
            case "TargetSandwiched":
                return map.TargetSandwiched(actor, specifics);
            case "TargetSandwichable":
                return map.TargetSandwichable(actor, specifics);
            case "TargetAligned":
                return map.StraightLineBetweenActors(actor, actor.GetTarget());
            case "TargetDistance<":
                return map.DistanceBetweenActors(actor, actor.GetTarget()) <= ReturnDistanceCheck(actor, specifics);
            case "TargetFacingOff":
                return map.TargetFacingActor(actor);
            case "SkillEffect":
                return GetAvailableSkillWithEffect(actor, map, specifics) != "";
            case "SpellEffect":
                return GetAvailableSpellWithEffect(actor, map, specifics) != "";
            case "SandwichedByTarget":
                return map.SandwichedByTarget(actor, specifics);
            case "TileSandwiched":
                return map.TileSandwiched(actor, specifics);
            case "TileSandwichable":
                return map.TileSandwichable(actor, specifics);
            case "TargetAdjacentAllyCount>":
                return map.GetAdjacentAllies(actor.GetTarget()).Count > int.Parse(specifics);
            case "TargetAdjacentAllyCount<":
                return map.GetAdjacentAllies(actor.GetTarget()).Count < int.Parse(specifics);
        }
        return true;
    }

    protected int ReturnDistanceCheck(TacticActor actor, string specifics)
    {
        switch (specifics)
        {
            case "Move":
                return actor.GetSpeed();
            case "Move+":
                return actor.GetSpeed() + 1;
            case "Move++":
                return actor.GetSpeed() + 2;
            case "AttackRange":
                return actor.GetAttackRange();
            case "AttackRange+":
                return actor.GetAttackRange() + 1;
            case "AttackRange++":
                return actor.GetAttackRange() + 2;
        }
        return 1;
    }
}
