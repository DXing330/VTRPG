using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AttackManager", menuName = "ScriptableObjects/BattleLogic/AttackManager", order = 1)]
public class AttackManager : ScriptableObject
{
    public PassiveSkill passive;
    public StatDatabase passiveData;
    public StatDatabase buffStatusData;
    public TerrainPassivesList terrainPassives;
    public TerrainPassivesList tEffectPassives;
    public TerrainPassivesList weatherPassives;
    public TerrainPassivesList borderPassives;
    public TerrainPassivesList buildingPassives;
    public int stabMultiplier = 150;
    public int baseMultiplier;
    protected string damageRolls;
    protected string passiveEffectString;
    protected string finalDamageCalculation;
    // Track if the attack is a counter attack.
    public bool counterAttack = false;
    // Based on attacker/defender.
    protected int advantage;
    protected int baseDamage;
    protected int damageMultiplier;
    protected int attackDamageMultiplier;
    protected int bonusDamage;
    protected int defenseMultiplier;
    protected int bonusDefense;
    // Based on defender.
    protected int dodgeChance;
    protected int defenseValue;
    // Based on attacker.
    protected int attackValue;
    protected int hitChance;
    protected int critDamage;
    protected int critChance;

    // TESTED IN BATTLETESTER
    public bool GuardActive(TacticActor defender, TacticActor attacker, BattleMap map)
    {
        return map.GetGuardingAlly(defender, attacker) != null;
    }
    // TESTED IN BATTLETESTER
    public TacticActor GetGuard(TacticActor defender, TacticActor attacker, BattleMap map)
    {
        TacticActor guard = map.GetGuardingAlly(defender, attacker);
        if (guard == null){return defender;}
        return guard;
    }

    public bool RollToHit(TacticActor attacker, TacticActor defender, BattleMap map, bool showLog = true)
    {
        int hitRoll = Random.Range(0, 100);
        hitRoll += defender.GetLuck();
        if (hitRoll >= (hitChance - dodgeChance))
        {
            if (showLog)
            {
                map.combatLog.UpdateNewestLog("The attack misses!");
                map.combatLog.AddDetailedLogs(passiveEffectString);
            }
            return false;
        }
        return true;
    }
    public int STAB(TacticActor attacker, int damage, string type, bool showLog = true)
    {
        if (attacker.SameElement(type))
        {
            if (showLog)
            {
                finalDamageCalculation += "STAB: " + damage + " * " + stabMultiplier + "% = ";
            }
            damage = damage * stabMultiplier / baseMultiplier;
            if (showLog)
            {
                finalDamageCalculation += damage + "\n";
            }
        }
        return damage;
    }
    public int ElementalMastery(TacticActor attacker, int damage, string type, bool showLog = true)
    {
        // Check for bonus damage.
        int elementBonus = attacker.ReturnDamageBonusOfType(type);
        if (elementBonus != 0)
        {
            if (showLog)
            {
                finalDamageCalculation += type + " Mastery = +" + elementBonus + "% Damage";
                finalDamageCalculation += "\n" + damage + " * " + (100 + elementBonus) + "% = " + (damage * (100 + elementBonus) / 100) + "\n";
            }
            return damage * (100 + elementBonus) / 100;
        }
        return damage;
    }
    public void ElementalResistance(TacticActor defender, int damage, string type, bool showLog = true)
    {
        int resistance = defender.ReturnDamageResistanceOfType(type);
        if (resistance != 0)
        {
            if (showLog)
            {
                finalDamageCalculation += "\n" + type + " Resistance = " + resistance + "%";
                finalDamageCalculation += "\n" + baseDamage + " * " + (100 - resistance) + "% = " + (baseDamage * (100 - resistance) / 100);
            }
        }
    }
    public bool CritRoll(TacticActor attacker, int damage, bool showLog = true)
    {
        int critRoll = Random.Range(0, 100);
        critRoll -= attacker.GetLuck();
        if (critRoll < critChance)
        {
            if (showLog)
            {
                finalDamageCalculation += "CRITICAL HIT: " + damage + " * " + critDamage + "% = ";
            }
            return true;
        }
        return false;
    }
    public int CritDamage(int damage, bool showLog = true)
    {
        damage = damage * critDamage / baseMultiplier;
        if (showLog)
        {
            finalDamageCalculation += damage + "\n";
        }
        return damage;
    }
    public void AttackDefenseMultipliersBonuses(bool attack = true, bool defense = true, bool showLog = true)
    {
        if (showLog)
        {
            finalDamageCalculation += "Attack Bonus: " + baseDamage + " * " + attackDamageMultiplier + "% + " + bonusDamage + " = ";
        }
        baseDamage = (baseDamage * attackDamageMultiplier / baseMultiplier) + bonusDamage;
        if (showLog)
        {
            finalDamageCalculation += baseDamage + "\n";

            finalDamageCalculation += "Defense Bonus: " + defenseValue + " * " + defenseMultiplier + "% + " + bonusDefense + " = ";
        }
        defenseValue = (defenseValue * defenseMultiplier / baseMultiplier) + bonusDefense;
        if (showLog)
        {
            finalDamageCalculation += defenseValue + "\n";
        }
    }
    // Simplified since it's just bonus damage.
    protected void ElementalBonusDamage(TacticActor dealer, TacticActor receiver, int damage, string element, BattleMap map = null)
    {
        damage = STAB(dealer, damage, element, false);
        damage = ElementalMastery(dealer, damage, element, false);
        ElementalResistance(receiver, damage, element, false);
        damage = receiver.TakeDamage(damage, element);
        if (map != null)
        {
            map.combatLog.UpdateNewestLog(receiver.GetPersonalName() + " takes " + damage + " " + element + " damage.");
        }
    }
    // Used for most spell effects.
    public void ElementalFlatDamage(TacticActor attacker, TacticActor defender, BattleMap map, int damage, string element)
    {
        attacker.SetDirection(map.DirectionBetweenActors(attacker, defender));
        if (!RollToHit(attacker, defender, map)){return;}
        baseDamage = damage;
        finalDamageCalculation = "";
        // Stab
        baseDamage = STAB(attacker, baseDamage, element);
        // Elemental Mastery
        baseDamage = ElementalMastery(attacker, baseDamage, element);
        // Crit
        if (CritRoll(attacker, baseDamage))
        {
            baseDamage = CritDamage(baseDamage);
        }
        // Resistance
        ElementalResistance(defender, baseDamage, element);
        baseDamage = defender.TakeDamage(baseDamage, element);
        defender.HurtBy(attacker, baseDamage);
        if (defender.GetHurtBy() == attacker)
        {
            defender.SetTarget(attacker);
        }
        map.combatLog.UpdateNewestLog(defender.GetPersonalName() + " takes " + baseDamage + " damage.");
        map.damageTracker.UpdateDamageStat(attacker, defender, baseDamage);
        map.combatLog.AddDetailedLogs(finalDamageCalculation);
    }
    protected void CheckMapPassives(TacticActor attacker, TacticActor defender, BattleMap map, int defenderTile, bool forAttacker = true, bool forDefender = true)
    {
        // Track the defenders tile, since if even someone blocks the combat still happens on that tile.
        CheckAuraEffects(defender, attacker, map);
        CheckTerrainPassives(defender, attacker, map, defenderTile, forAttacker, forDefender);
        CheckTEffectPassives(defender, attacker, map, defenderTile, forAttacker, forDefender);
        CheckBorderPassives(defender, attacker, map, defenderTile, forAttacker, forDefender);
        CheckBuildingPassives(defender, attacker, map, defenderTile, forAttacker, forDefender);
        CheckWeatherPassives(defender, attacker, map, forAttacker, forDefender);
    }
    // Basically used for guns/cannons other non scaling damage
    public void FlatDamageAttack(TacticActor attacker, TacticActor target, BattleMap map, int damage)
    {
        bool guard = GuardActive(target, attacker, map);
        attacker.SetDirection(map.DirectionBetweenActors(attacker, target));
        TacticActor attackTarget = target;
        if (guard)
        {
            attackTarget = GetGuard(target, attacker, map);
            map.combatLog.UpdateNewestLog(attackTarget.GetPersonalName() + " defends " + target.GetPersonalName() + " from the attack.");
        }
        UpdateBattleStats(attacker, attackTarget);
        advantage = 0;
        damageMultiplier = baseMultiplier;
        baseDamage = damage;
        damageRolls = "Damage Rolls: ";
        passiveEffectString = "Applied Passives: ";
        finalDamageCalculation = "";
        // Only the defender gets passive bonuses(?) from tile/teffects
        CheckMapPassives(attacker, attackTarget, map, target.GetLocation(), false, true);
        CheckPassives(attackTarget.GetDefendingPassives(), attackTarget, attacker, map);
        if (!RollToHit(attacker, attackTarget, map)){return;}
        baseDamage = Advantage(baseDamage, advantage);
        finalDamageCalculation += "Subtract Defense: " + baseDamage + " - " + attackTarget.GetDefense() + " = ";
        baseDamage = Mathf.Max(0, baseDamage - attackTarget.GetDefense());
        finalDamageCalculation += baseDamage;
        if (damageMultiplier < 0) { damageMultiplier = 0; }
        finalDamageCalculation += "\n" + "Damage Multiplier: " + baseDamage + " * " + damageMultiplier + "% = ";
        baseDamage = damageMultiplier * baseDamage / baseMultiplier;
        finalDamageCalculation += baseDamage;
        // Flat damage is always physical type.
        baseDamage = attackTarget.TakeDamage(baseDamage);
        attackTarget.HurtBy(attacker, baseDamage);
        if (attackTarget.GetHurtBy() == attacker)
        {
            attackTarget.SetTarget(attacker);
        }
        map.combatLog.UpdateNewestLog(attackTarget.GetPersonalName() + " takes " + baseDamage + " damage.");
        map.combatLog.AddDetailedLogs(passiveEffectString);
        map.combatLog.AddDetailedLogs(damageRolls);
        map.combatLog.AddDetailedLogs(finalDamageCalculation);
        map.damageTracker.UpdateDamageStat(attacker, attackTarget, baseDamage);
    }
    public void TrueDamageAttack(TacticActor attacker, TacticActor defender, BattleMap map, int attackMultiplier = -1, string type = "Attack")
    {
        // True damage ignores guarding, because it should be as OP as possible.
        // True damage cannot be countered.
        counterAttack = false;
        bool critHit = false;
        attacker.SetDirection(map.DirectionBetweenActors(attacker, defender));
        UpdateBattleStats(attacker, defender);
        baseDamage = attackValue;
        // True damage ignores defender/terrain passives, making it even stronger than it should be.
        CheckMapPassives(attacker, defender, map, defender.GetLocation(), true, false);
        CheckPassives(attacker.GetAttackingPassives(buffStatusData), defender, attacker, map);
        switch (type)
        {
            case "Health":
                baseDamage = attacker.GetHealth();
                break;
            case "Defense":
                baseDamage = attacker.GetDefense();
                break;
        }
        if (advantage < 0){advantage = 0;}
        baseDamage = Advantage(baseDamage, advantage);
        if (CritRoll(attacker, baseDamage))
        {
            critHit = true;
            baseDamage = CritDamage(baseDamage);
        }
        baseDamage = defender.TakeDamage(baseDamage, "True");
        // No damage/attack/defense multiplier, true damage can't be increased or decreased besides crit/advantage.
        defender.HurtBy(attacker, baseDamage);
        if (defender.GetHurtBy() == attacker)
        {
            defender.SetTarget(attacker);
        }
        map.combatLog.UpdateNewestLog(defender.GetPersonalName() + " takes " + baseDamage + " damage.");
        passive.ApplyAfterAttackPassives(attacker, defender, baseDamage, map, true, critHit, counterAttack);
        map.damageTracker.UpdateDamageStat(attacker, defender, baseDamage);
    }
    protected void UpdateBattleStats(TacticActor attacker, TacticActor defender)
    {
        dodgeChance = defender.GetDodgeChance();
        defenseValue = defender.GetDefense();
        attackValue = attacker.GetAttack();
        hitChance = attacker.GetHitChance();
        critChance = attacker.GetCritChance();
        critDamage = attacker.GetCritDamage();
        attackDamageMultiplier = 100;
        defenseMultiplier = 100;
        bonusDamage = 0;
        bonusDefense = 0;
    }
    // Basic attack damage calculation
    // All Basic Attacks Should Pass Through Here.
    public void ActorAttacksActor(TacticActor attacker, TacticActor target, BattleMap map, int attackMultiplier = -1, string type = "Physical")
    {
        // If the target is already dead then stop.
        if (target.GetHealth() <= 0){return;}
        // Track some things for after attack passives.
        bool hit = true;
        bool critHit = false;
        bool guard = GuardActive(target, attacker, map);
        attacker.SetDirection(map.DirectionBetweenActors(attacker, target));
        //attacker.UpdateTempInitiative(-1);
        attacker.UpdateRoundAttackTracker();
        TacticActor attackTarget = target;
        if (guard)
        {
            attackTarget = GetGuard(target, attacker, map);
            map.combatLog.UpdateNewestLog(attackTarget.GetPersonalName() + " defends " + target.GetPersonalName() + " from the attack.");
        }
        attackTarget.UpdateRoundDefendTracker();
        advantage = 0;
        if (attackMultiplier < 0) { damageMultiplier = baseMultiplier; }
        else { damageMultiplier = attackMultiplier; }
        UpdateBattleStats(attacker, attackTarget);
        baseDamage = attackValue;
        damageRolls = "Damage Rolls: ";
        passiveEffectString = "Applied Passives: ";
        finalDamageCalculation = "";
        CheckMapPassives(attacker, attackTarget, map, target.GetLocation(), true, true);
        // Bonus damage can be calculated here and triggers regardless of hit/miss.
        CheckPassives(attacker.GetAttackingPassives(buffStatusData), attackTarget, attacker, map);
        CheckPassives(attackTarget.GetDefendingPassives(buffStatusData), attackTarget, attacker, map);
        // TODO Check any buffs/statuses that apply. 
        // Determine if you miss or not.
        if (!RollToHit(attacker, attackTarget, map))
        {
            hit = false;
            passive.ApplyAfterAttackPassives(attacker, attackTarget, 0, map, hit, critHit, counterAttack);
            passive.ApplyAfterDefendPassives(attacker, attackTarget, 0, map, hit, critHit, counterAttack);
            return;
        }
        baseDamage = Advantage(baseDamage, advantage);
        // Check for stab.
        baseDamage = STAB(attacker, baseDamage, type);
        // Check for bonus damage.
        baseDamage = ElementalMastery(attacker, baseDamage, type);
        // Check for a critical hit.
        if (CritRoll(attacker, baseDamage))
        {
            critHit = true;
            baseDamage = CritDamage(baseDamage);
        }
        // Apply Attack / Defense Multipliers/Bonuses.
        AttackDefenseMultipliersBonuses();
        // Deal Damage To Any Buildings Supporting The Target.
        map.DamageActorBuilding(target.GetLocation(), attacker, baseDamage);
        // Ranged attacks get elevation bonus/penalties.
        if (attacker.GetAttackRange() > 1)
        {
            int elvDiff = map.ReturnPosNegElvDiff(attacker.GetLocation(), target.GetLocation());
            if (elvDiff != 0)
            {
                finalDamageCalculation += "Elevation Difference: " + elvDiff;
                finalDamageCalculation += "\n" + "Damage Multiplier: " + damageMultiplier + " -> ";
                damageMultiplier += 10 * elvDiff;
                finalDamageCalculation += damageMultiplier + "\n";
            }
        }
        // First subtract defense.
        finalDamageCalculation += "Subtract Defense: " + baseDamage + " - " + defenseValue + " = ";
        baseDamage = baseDamage - defenseValue;
        if (baseDamage < 0){ baseDamage = 0; }
        finalDamageCalculation += baseDamage;
        // Then multiply by damage multiplier.
        if (damageMultiplier < 0) { damageMultiplier = 0; }
        finalDamageCalculation += "\n" + "Damage Multiplier: " + baseDamage + " * " + damageMultiplier + "% = ";
        baseDamage = damageMultiplier * baseDamage / baseMultiplier;
        finalDamageCalculation += baseDamage;
        // Check if the passive affects damage.
        baseDamage = CheckTakeDamagePassives(attackTarget.GetTakeDamagePassives(), baseDamage, type, attacker, attackTarget);
        // Show the resistance calculation.
        ElementalResistance(attackTarget, baseDamage, type);
        baseDamage = attackTarget.TakeDamage(baseDamage, type);
        attackTarget.HurtBy(attacker, baseDamage);
        if (attackTarget.GetHurtBy() == attacker)
        {
            attackTarget.SetTarget(attacker);
        }
        map.combatLog.UpdateNewestLog(attackTarget.GetPersonalName() + " takes " + baseDamage + " damage.");
        map.damageTracker.UpdateDamageStat(attacker, attackTarget, baseDamage);
        map.combatLog.AddDetailedLogs(passiveEffectString);
        map.combatLog.AddDetailedLogs("Damage Calculations:");
        map.combatLog.AddDetailedLogs(damageRolls);
        map.combatLog.AddDetailedLogs(finalDamageCalculation);
        passive.ApplyAfterAttackPassives(attacker, attackTarget, baseDamage, map, hit, critHit, counterAttack);
        passive.ApplyAfterDefendPassives(attacker, attackTarget, baseDamage, map, hit, critHit, counterAttack);
        // Check if the defender is alive, has counter attacks available and is in range.
        if (attackTarget.GetHealth() > 0 && attackTarget.CounterAttackAvailable() && map.DistanceBetweenActors(attackTarget, attacker) <= attackTarget.GetAttackRange())
        {
            attackTarget.UseCounterAttack();
            // Set Counter Attack Equal To True Only Right Before The Counter Attack Hits.
            counterAttack = true;
            map.combatLog.UpdateNewestLog(attackTarget.GetPersonalName() + " counter attacks " + attacker.GetPersonalName());
            ActorAttacksActor(attackTarget, attacker, map);
        }
        counterAttack = false;
        // TODO Trigger After Attack Auras Here.
        map.ApplyAuraEffects(attacker, "Attack");
    }

    protected int RollAttackDamage(int baseAttack)
    {
        int roll = baseAttack + Random.Range(-baseAttack/3, baseAttack/3);
        damageRolls += " "+roll+" ";
        return roll;
    }

    protected int Advantage(int baseAttack, int advantage)
    {
        if (advantage < 0)
        {
            return Disadvantage(baseAttack, Mathf.Abs(advantage));
        }
        if (advantage == 0)
        {
            return RollAttackDamage(baseAttack);
        }
        damageRolls += "Max(";
        int damage = RollAttackDamage(baseAttack);
        for (int i = 0; i < advantage; i++)
        {
            damage = Mathf.Max(damage, RollAttackDamage(baseAttack));
        }
        damageRolls += ") + "+advantage.ToString();
        return damage + advantage;
    }

    protected int Disadvantage(int baseAttack, int disadvantage)
    {
        damageRolls += "Min(";
        int damage = RollAttackDamage(baseAttack);
        for (int i = 0; i < disadvantage; i++)
        {
            damage = Mathf.Min(damage, RollAttackDamage(baseAttack));
        }
        damageRolls += ") - "+disadvantage.ToString();
        return damage - disadvantage;
    }

    protected void CheckPassives(List<string> characterPassives, TacticActor target, TacticActor attacker, BattleMap map)
    {
        for (int i = 0; i < characterPassives.Count; i++)
        {
            ApplyPassiveEffect(characterPassives[i], target, attacker, map);
        }
    }

    protected void ActivatePassiveEffect(string passiveName, List<string> passiveStats, TacticActor target, TacticActor attacker, BattleMap map)
    {
        switch (passiveStats[3])
        {
            case "Advantage":
            passiveEffectString += "\n";
            passiveEffectString += passiveName + " : " + passiveStats[3] + " : " + advantage+"->";
            advantage = passive.AffectInt(advantage, passiveStats[4], passiveStats[5]);
            passiveEffectString += advantage;
            break;
            case "Damage%":
            passiveEffectString += "\n";
            passiveEffectString += passiveName + " : " + passiveStats[3] + " : " + damageMultiplier + "->";
            damageMultiplier = passive.AffectInt(damageMultiplier, passiveStats[4], passiveStats[5]);
            if (damageMultiplier < 0)
            {
                damageMultiplier = 0;
            }
            passiveEffectString += damageMultiplier;
            break;
            case "AttackValue":
            passiveEffectString += "\n";
            passiveEffectString += passiveName + " : " + "Bonus Damage" + " : " + bonusDamage + "->";
            bonusDamage = passive.AffectInt(bonusDamage, passiveStats[4], passiveStats[5], attacker, target);
            passiveEffectString += bonusDamage;
            break;
            case "DefenseValue":
            passiveEffectString += "\n";
            passiveEffectString += passiveName + " : " + "Bonus Defense" + " : " + bonusDefense + "->";
            bonusDefense = passive.AffectInt(bonusDefense, passiveStats[4], passiveStats[5], attacker, target);
            passiveEffectString += bonusDefense;
            break;
            case "AttackValue%":
            passiveEffectString += "\n";
            passiveEffectString += passiveName + " : " + "Attack Multiplier" + " : " + attackDamageMultiplier + "->";
            attackDamageMultiplier = passive.AffectInt(attackDamageMultiplier, passiveStats[4], passiveStats[5]);
            passiveEffectString += attackDamageMultiplier;
            break;
            case "DefenseValue%":
            passiveEffectString += "\n";
            passiveEffectString += passiveName + " : " + "Defense Multiplier" + " : " + defenseMultiplier + "->";
            defenseMultiplier = passive.AffectInt(defenseMultiplier, passiveStats[4], passiveStats[5]);
            passiveEffectString += defenseMultiplier;
            break;
            case "HitChance":
            passiveEffectString += "\n";
            passiveEffectString += passiveName + " : " + passiveStats[3] + " : " + hitChance+"->";
            hitChance = passive.AffectInt(hitChance, passiveStats[4], passiveStats[5], attacker, target);
            passiveEffectString += hitChance;
            break;
            case "Dodge":
            passiveEffectString += "\n";
            passiveEffectString += passiveName + " : " + passiveStats[3] + " : " + dodgeChance+"->";
            dodgeChance = passive.AffectInt(dodgeChance, passiveStats[4], passiveStats[5], attacker, target);
            passiveEffectString += dodgeChance;
            break;
            case "CritChance":
            passiveEffectString += "\n";
            passiveEffectString += passiveName + " : " + passiveStats[3] + " : " + critChance+"->";
            critChance = passive.AffectInt(critChance, passiveStats[4], passiveStats[5], attacker, target);
            passiveEffectString += critChance;
            break;
            case "CritDamage":
            passiveEffectString += "\n";
            passiveEffectString += passiveName + " : " + passiveStats[3] + " : " + critDamage+"->";
            critDamage = passive.AffectInt(critDamage, passiveStats[4], passiveStats[5]);
            passiveEffectString += critDamage;
            break;
            case "Target":
            passive.AffectActor(target, passiveStats[4], passiveStats[5]);
            break;
            case "Attacker":
            passive.AffectActor(attacker, passiveStats[4], passiveStats[5]);
            break;
            case "Map":
            map.ChangeTile(target.GetLocation(), passiveStats[4], passiveStats[5]);
            break;
            case "ElementalBonusDamage":
            string[] eBD = passiveStats[5].Split(">>");
            ElementalBonusDamage(attacker, target, int.Parse(eBD[1]), eBD[0], map);
            break;
            case "ElementalReflectDamage":
            string[] eRD = passiveStats[5].Split(">>");
            ElementalBonusDamage(target, attacker, int.Parse(eRD[1]), eRD[0], map);
            break;
        }
    }

    protected void ApplyPassiveEffect(string pData, TacticActor target, TacticActor attacker, BattleMap map)
    {
        if (pData.Length < 6 || !pData.Contains("|"))
        {
            // Try to get the data from the name.
            pData = passiveData.ReturnValue(pData);
            if (pData.Length < 6 || !pData.Contains("|"))
            {
                return;
            }
        }
        List<string> pStats = pData.Split("|").ToList();
        string passiveName = passiveData.ReturnKeyFromValue(pData);
        if (passive.CheckBattleConditions(pStats[1], pStats[2], target, attacker, map))
        {
            ActivatePassiveEffect(passiveName, pStats, target, attacker, map);
        }
    }

    protected void ApplyAuraEffect(AuraEffect aura, TacticActor target, TacticActor attacker, BattleMap map)
    {
        List<string> pStats = aura.ReturnPassiveStats();
        if (passive.CheckBattleConditions(pStats[1], pStats[2], target, attacker, map))
        {
            ActivatePassiveEffect(pStats[0], pStats, target, attacker, map);
        }
    }

    protected int CheckTakeDamagePassives(List<string> passives, int damage, string damageType, TacticActor attacker, TacticActor target)
    {
        int originalDamage = damage;
        for (int i = 0; i < passives.Count; i++)
        {
            List<string> passiveStats = passives[i].Split("|").ToList();
            if (passive.CheckTakeDamageCondition(passiveStats[1], passiveStats[2], originalDamage, damageType, attacker, target))
            {
                string passiveName = passiveData.ReturnKeyFromValue(passives[i]);
                finalDamageCalculation += "\n" + passiveName + "; Damage : " + damage;
                damage = passive.AffectInt(damage, passiveStats[4], passiveStats[5]);
                finalDamageCalculation += " > " + damage;
            }
        }
        return damage;
    }

    protected void CheckWeatherPassives(TacticActor target, TacticActor attacker, BattleMap map, bool forAttacker = true, bool forTarget = true)
    {
        string weather = map.GetWeather();
        string defendingPassive = weatherPassives.ReturnDefendingPassive(weather);
        string attackingPassive = weatherPassives.ReturnAttackingPassive(weather);
        if (defendingPassive.Length > 1 && forTarget)
        {
            ApplyPassiveEffect(defendingPassive, target, attacker, map);
        }
        if (attackingPassive.Length > 1 && forAttacker)
        {
            ApplyPassiveEffect(attackingPassive, target, attacker, map);
        }
    }

    protected void CheckTEffectPassives(TacticActor target, TacticActor attacker, BattleMap map, int targetTileNumber, bool forAttacker = true, bool forTarget = true)
    {
        string targetTile = map.terrainEffectTiles[targetTileNumber];
        string attackingTile = map.terrainEffectTiles[attacker.GetLocation()];
        string defendingPassive = tEffectPassives.ReturnDefendingPassive(targetTile);
        if (defendingPassive.Length > 1 && forTarget)
        {
            ApplyPassiveEffect(defendingPassive, target, attacker, map);
        }
        string attackingPassive = tEffectPassives.ReturnAttackingPassive(attackingTile);
        if (attackingPassive.Length > 1 && forAttacker)
        {
            ApplyPassiveEffect(attackingPassive, target, attacker, map);
        }
    }

    protected void CheckTerrainPassives(TacticActor target, TacticActor attacker, BattleMap map, int targetTileNumber, bool forAttacker = true, bool forTarget = true)
    {
        string targetTile = map.mapInfo[targetTileNumber];
        string attackingTile = map.mapInfo[attacker.GetLocation()];
        string defendingPassive = terrainPassives.ReturnDefendingPassive(targetTile);
        if (defendingPassive.Length > 1 && forTarget)
        {
            ApplyPassiveEffect(defendingPassive, target, attacker, map);
        }
        string attackingPassive = terrainPassives.ReturnAttackingPassive(attackingTile);
        if (attackingPassive.Length > 1 && forAttacker)
        {
            ApplyPassiveEffect(attackingPassive, target, attacker, map);
        }
    }

    protected void CheckBorderPassives(TacticActor target, TacticActor attacker, BattleMap map, int targetTile, bool forAttacker = true, bool forTarget = true)
    {
        // Get the direction from the attacker to the direction.
        int attackerToTargetDir = map.DirectionBetweenActors(attacker, target);
        int targetToAttackerDir = (attackerToTargetDir + 3) % 6;
        // Check if there is a border in the attacker direction on the attacker tile.
        if (forAttacker)
        {
            string attackerBorder = map.ReturnBorderFromTileDirection(attacker.GetLocation(), attackerToTargetDir);
            string attackingPassive = borderPassives.ReturnAttackingPassive(attackerBorder);
            ApplyPassiveEffect(attackingPassive, target, attacker, map);
        }
        // Check if the defender has a border.
        if (forTarget)
        {
            string targetBorder = map.ReturnBorderFromTileDirection(targetTile, targetToAttackerDir);
            string defendingPassive = borderPassives.ReturnDefendingPassive(targetBorder);
            ApplyPassiveEffect(defendingPassive, target, attacker, map);
        }
    }

    protected void CheckBuildingPassives(TacticActor target, TacticActor attacker, BattleMap map, int targetTileNumber, bool forAttacker = true, bool forTarget = true)
    {
        // Check if any buildings are on the tiles.
        if (forAttacker)
        {
            string attackerBuilding = map.GetBuildingOnTile(attacker.GetLocation());
            string attackingPassive = buildingPassives.ReturnAttackingPassive(attackerBuilding);
            ApplyPassiveEffect(attackingPassive, target, attacker, map);
        }
        if (forTarget)
        {
            string defenderBuilding = map.GetBuildingOnTile(targetTileNumber);
            string defendingPassive = buildingPassives.ReturnDefendingPassive(defenderBuilding);
            ApplyPassiveEffect(defendingPassive, target, attacker, map);
        }
    }

    // Always check for both.
    protected void CheckAuraEffects(TacticActor target, TacticActor attacker, BattleMap map)
    {
        // Iterate through the auras.
        for (int i = 0; i < map.auras.Count; i++)
        {
            if (map.auras[i].BattleTeamCheck(target, attacker, map))
            {
                ApplyAuraEffect(map.auras[i], target, attacker, map);
            }
        }
    }
}
