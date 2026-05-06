using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SkillEffect", menuName = "ScriptableObjects/BattleLogic/SkillEffect", order = 1)]
public class SkillEffect : ScriptableObject
{
    public GeneralUtility utility;
    public PassiveOrganizer passiveOrganizer;
    public StatDatabase buffsAndStatus;
    public StatDatabase standardSpells;
    public int basicDenominator = 100;
    public int baseStatusDuration = 3;
    public void AffectActor(TacticActor target, string effect, string effectSpecifics, int level = 1, CombatLog combatLog = null)
    {
        if (target == null){return;}
        int changeAmount = 0;
        switch (effect)
        {
            case "Passive":
                target.AddPassiveSkill(effectSpecifics, "1");
                int newPassiveLevel = target.GetLevelFromPassive(effectSpecifics);
                passiveOrganizer.AddSortedPassiveNewLevel(target, effectSpecifics, newPassiveLevel);
                break;
            case "PassiveAtLevel":
                string[] passiveAtLevel = effectSpecifics.Split("Equals");
                target.AddPassiveSkill(passiveAtLevel[0], passiveAtLevel[1]);
                int newPassiveAtLevel = target.GetLevelFromPassive(effectSpecifics);
                passiveOrganizer.AddSortedPassiveNewLevel(target, effectSpecifics, newPassiveAtLevel);
                break;
            case "TemporaryPassive":
                if (target.AddTempPassive(effectSpecifics, level))
                {
                    passiveOrganizer.AddSortedPassive(target, effectSpecifics);
                }
                break;
            case "NextSkillMod":
                target.AddNextSkillMod(effectSpecifics);
                break;
            case "Status":
                int duration = level;
                if (level <= baseStatusDuration && level >= 0) { duration = baseStatusDuration; }
                // Some statuses don't naturally wear off and are permanent or immediately take effect.
                switch (effectSpecifics)
                {
                    case "Poison":
                        duration = -1;
                        break;
                    case "Burn":
                        duration = -1;
                        break;
                    case "Bleed":
                        duration = -1;
                        break;
                    case "Stun":
                        target.ResetActions();
                        break;
                }
                target.AddStatus(effectSpecifics, duration);
                break;
            case "Statuses":
                int durations = level;
                if (level <= baseStatusDuration && level >= 0) { durations = baseStatusDuration; }
                string[] statuses = effectSpecifics.Split(",");
                for (int i = 0; i < statuses.Length; i++)
                {
                    AffectActor(target, "Status", statuses[i], durations);
                }
                break;
            case "Buff":
                // If it's a new buff, then immediately apply it's effects.
                int bDuration = level;
                if (level <= baseStatusDuration && level >= 0) { bDuration = baseStatusDuration; }
                if (!target.BuffExists(effectSpecifics))
                {
                    string[] buffEffects = buffsAndStatus.ReturnValue(effectSpecifics).Split("|");
                    AffectActor(target, buffEffects[1], buffEffects[2]);
                }
                target.AddBuff(effectSpecifics, bDuration);
                break;
            case "RemoveBuff":
                target.RemoveBuff(effectSpecifics);
                break;
            case "RemoveStatus":
                target.RemoveStatus(effectSpecifics);
                break;
            case "RemoveStatuses":
                string[] removedStatuses = effectSpecifics.Split(",");
                for (int i = 0; i < removedStatuses.Length; i++)
                {
                    target.RemoveStatus(removedStatuses[i]);
                }
                break;
            // Temp health is always a shield.
            case "TempHealth":
                target.UpdateTempHealth(int.Parse(effectSpecifics));
                break;
            // Default is increasing health.
            case "Health":
                target.UpdateHealth(int.Parse(effectSpecifics) * level, false);
                break;
            case "Damage":
                int effectDamage = int.Parse(effectSpecifics) * level;
                effectDamage = target.TakeEffectDamage(effectDamage);
                if (combatLog != null)
                {
                    combatLog.UpdateNewestLog(target.GetPersonalName() + " takes " + effectDamage + " damage.");
                }
                break;
            case "TrueDamage":
                target.currentHealth -= int.Parse(effectSpecifics) * level;
                if (combatLog != null)
                {
                    combatLog.UpdateNewestLog(target.GetPersonalName() + " takes " + int.Parse(effectSpecifics) * level + " damage.");
                }
                break;
            // ALL the elemental damage types go here as well.
            case "LightningDamage":
                int lightningDamage = int.Parse(effectSpecifics) * level;
                // Double Damage + Bonus Damage If Wet.
                if (target.StatusExists("Wet"))
                {
                    lightningDamage *= 2;
                    lightningDamage += target.ReturnStatusDuration("Wet");
                }
                lightningDamage = target.ApplyMagicResist(lightningDamage);
                lightningDamage = target.TakeEffectDamage(lightningDamage, "Lightning");
                if (combatLog != null && lightningDamage > 0)
                {
                    combatLog.UpdateNewestLog(target.GetPersonalName() + " takes " + lightningDamage + " lightning damage.");
                }
                break;
            case "FireDamage":
                // Fire removes bleeds/freeze.
                target.RemoveStatus("Bleed");
                target.RemoveStatus("Frozen");
                int fireDamage = int.Parse(effectSpecifics) * level;
                fireDamage = target.ApplyMagicResist(fireDamage);
                // Bonus Damage For Each Burn Stack, Penetrates Magic Resist.
                fireDamage += target.StatusStacks("Burn");
                fireDamage = target.TakeEffectDamage(fireDamage, "Fire");
                if (combatLog != null && fireDamage > 0)
                {
                    combatLog.UpdateNewestLog(target.GetPersonalName() + " takes " + fireDamage + " fire damage.");
                }
                break;
            case "WaterDamage":
                int waterDamage = int.Parse(effectSpecifics) * level;
                waterDamage = target.ApplyMagicResist(waterDamage);
                // Bonus Damage Based On Wet Duration, Penetrates Magic Resist.
                waterDamage += target.ReturnStatusDuration("Wet");
                waterDamage = target.TakeEffectDamage(waterDamage, "Water");
                if (combatLog != null && waterDamage > 0)
                {
                    combatLog.UpdateNewestLog(target.GetPersonalName() + " takes " + waterDamage + " water damage.");
                }
                break;
            case "IceDamage":
                int iceDamage = int.Parse(effectSpecifics) * level;
                // Ice removes bleeds.
                target.RemoveStatus("Bleed");
                // Freeze If Wet.
                if (target.StatusExists("Wet"))
                {
                    int freezeDuration = target.ReturnStatusDuration("Wet");
                    target.RemoveStatus("Wet");
                    target.AddStatus("Frozen", freezeDuration);
                }
                iceDamage = target.ApplyMagicResist(iceDamage);
                iceDamage = target.TakeEffectDamage(iceDamage, "Ice");
                if (combatLog != null && iceDamage > 0)
                {
                    combatLog.UpdateNewestLog(target.GetPersonalName() + " takes " + iceDamage + " ice damage.");
                }
                break;
            case "EarthDamage":
                int earthDamage = int.Parse(effectSpecifics) * level;
                // Multiplied By Weight.
                earthDamage *= Mathf.Max(1, target.GetWeight());
                earthDamage = target.ApplyMagicResist(earthDamage);
                earthDamage = target.TakeEffectDamage(earthDamage, "Earth");
                if (combatLog != null && earthDamage > 0)
                {
                    combatLog.UpdateNewestLog(target.GetPersonalName() + " takes " + earthDamage + " earth damage.");
                }
                break;
            case "LightDamage":
                int lightDamage = int.Parse(effectSpecifics) * level;
                // Reveal invisibility.
                target.RemoveInvisibility();
                lightDamage = target.ApplyMagicResist(lightDamage);
                lightDamage = target.TakeEffectDamage(lightDamage, "Light");
                if (combatLog != null && lightDamage > 0)
                {
                    combatLog.UpdateNewestLog(target.GetPersonalName() + " takes " + lightDamage + " light damage.");
                }
                break;
            case "DarkDamage":
                int darkDamage = int.Parse(effectSpecifics) * level;
                darkDamage = target.ApplyMagicResist(darkDamage);
                // Bonus Damage For Each Unique Status, Penetrates Magic Resist.
                darkDamage += target.GetUniqueStatuses().Count;
                darkDamage = target.TakeEffectDamage(darkDamage, "Dark");
                if (combatLog != null && darkDamage > 0)
                {
                    combatLog.UpdateNewestLog(target.GetPersonalName() + " takes " + darkDamage + " dark damage.");
                }
                break;
            case "AirDamage":
                // Decrease weight.
                target.UpdateWeight(-1);
                int airDamage = int.Parse(effectSpecifics) * level;
                airDamage = target.ApplyMagicResist(airDamage);
                airDamage = target.TakeEffectDamage(airDamage, "Air");
                if (combatLog != null && airDamage > 0)
                {
                    combatLog.UpdateNewestLog(target.GetPersonalName() + " takes " + airDamage + " air damage.");
                }
                break;
            case "Energy":
                target.UpdateEnergy(int.Parse(effectSpecifics) * level);
                break;
            case "TempAttack":
                target.UpdateTempAttack(int.Parse(effectSpecifics));
                break;
            case "Attack":
                target.UpdateAttack(int.Parse(effectSpecifics) * level);
                break;
            case "TempDefense":
                target.UpdateTempDefense(int.Parse(effectSpecifics));
                break;
            case "Defense":
                target.UpdateDefense(int.Parse(effectSpecifics) * level);
                break;
            case "AllStats":
                int allStatChange = int.Parse(effectSpecifics) * level;
                target.UpdateHealth(allStatChange, false);
                target.UpdateAttack(allStatChange);
                target.UpdateDefense(allStatChange);
                break;
            case "AllStats%":
                AffectActor(target, "BaseHealth%", effectSpecifics, level);
                AffectActor(target, "BaseAttack%", effectSpecifics, level);
                AffectActor(target, "BaseDefense%", effectSpecifics, level);
                break;
            case "RandomBaseStat":
                // Health / Attack / Defense
                int baseStatRoll = Random.Range(0, 3);
                switch (baseStatRoll)
                {
                    case 0:
                    AffectActor(target, "BaseHealth", effectSpecifics, level);
                    break;
                    case 1:
                    AffectActor(target, "BaseAttack", effectSpecifics, level);
                    break;
                    case 2:
                    AffectActor(target, "BaseDefense", effectSpecifics, level);
                    break;
                }
                break;
            case "CurrentHealth%":
                int currentHealth = target.GetHealth();
                target.UpdateHealth(int.Parse(effectSpecifics) * currentHealth / basicDenominator);
                break;
            case "BaseHealth":
                target.UpdateBaseHealth(int.Parse(effectSpecifics) * level, false);
                target.UpdateHealth(int.Parse(effectSpecifics) * level, false);
                break;
            case "BaseHealth%":
                target.UpdateBaseHealth(int.Parse(effectSpecifics) * level * target.GetBaseHealth() / basicDenominator, false);
                target.UpdateHealth(int.Parse(effectSpecifics) * level * target.GetBaseHealth() / basicDenominator, false);
                break;
            case "MaxHealth%":
                target.UpdateBaseHealth(int.Parse(effectSpecifics) * level * target.GetBaseHealth() / basicDenominator, false);
                if (target.GetHealth() > target.GetBaseHealth())
                {
                    target.SetCurrentHealth(target.GetBaseHealth());
                }
                break;
            case "BaseEnergy":
                target.UpdateBaseEnergy(int.Parse(effectSpecifics) * level);
                target.UpdateEnergy(int.Parse(effectSpecifics) * level);
                break;
            case "BaseEnergy%":
                int bEnergyChange = int.Parse(effectSpecifics) * level * target.GetBaseEnergy() / basicDenominator;
                if (bEnergyChange < 1 && int.Parse(effectSpecifics) > 0)
                {
                    bEnergyChange = 1;
                }
                target.UpdateBaseEnergy(bEnergyChange);
                target.UpdateEnergy(bEnergyChange);
                break;
            case "BaseAttack":
                int bAtkChange = int.Parse(effectSpecifics) * level;
                target.UpdateBaseAttack(bAtkChange);
                target.UpdateAttack(bAtkChange);
                break;
            case "BaseAttack%":
                int bAtkPChange = level * (int.Parse(effectSpecifics) * target.GetBaseAttack()) / basicDenominator;
                if (int.Parse(effectSpecifics) > 0)
                {
                    bAtkPChange = Mathf.Max(1, bAtkPChange);
                }
                target.UpdateBaseAttack(bAtkPChange);
                target.UpdateAttack(bAtkPChange);
                break;
            case "BaseDefense":
                int bDefChange = int.Parse(effectSpecifics) * level;
                target.UpdateBaseDefense(bDefChange);
                target.UpdateDefense(bDefChange);
                break;
            case "BaseDefense%":
                int bDefPChange = level * (int.Parse(effectSpecifics) * target.GetBaseDefense()) / basicDenominator;
                // Positive boosts always increase stat by at least 1.
                if (int.Parse(effectSpecifics) > 0)
                {
                    bDefChange = Mathf.Max(1, bDefPChange);
                }
                target.UpdateBaseDefense(bDefPChange);
                target.UpdateDefense(bDefPChange);
                break;
            case "AttackRange":
                target.SetAttackRangeMax(int.Parse(effectSpecifics));
                break;
            case "SetAttackActionCost":
                target.SetAttackActionCost(int.Parse(effectSpecifics));
                break;
            case "BasicAttackDamage":
                target.ChangeBasicAttackMultiplier(int.Parse(effectSpecifics));
                break;
            case "TempRange":
                target.UpdateBonusAttackRange(int.Parse(effectSpecifics));
                break;
            case "TempHealth%":
                changeAmount = int.Parse(effectSpecifics) * target.GetBaseHealth() / basicDenominator;
                if (Mathf.Abs(changeAmount) < Mathf.Abs(int.Parse(effectSpecifics)))
                {
                    changeAmount = int.Parse(effectSpecifics);
                }
                target.UpdateTempHealth(changeAmount);
                break;
            case "Health%":
                // % changes should not go below a minimum amount or else low stat characters are effectively immune.
                changeAmount = level * int.Parse(effectSpecifics) * target.GetBaseHealth() / basicDenominator;
                if (Mathf.Abs(changeAmount) < Mathf.Abs(int.Parse(effectSpecifics) * level))
                {
                    changeAmount = int.Parse(effectSpecifics);
                }
                target.UpdateHealth((changeAmount), false);
                break;
            case "TempAttack%":
                target.UpdateTempAttack((int.Parse(effectSpecifics) * target.GetBaseAttack()) / basicDenominator);
                break;
            case "Attack%":
                target.UpdateAttack(level * (int.Parse(effectSpecifics) * target.GetBaseAttack()) / basicDenominator);
                break;
            case "TempDefense%":
                int tDefPChange = level * (int.Parse(effectSpecifics) * target.GetBaseDefense()) / basicDenominator;
                // Positive boosts always increase stat by at least 1.
                if (int.Parse(effectSpecifics) > 0)
                {
                    tDefPChange = Mathf.Max(1, tDefPChange);
                }
                target.UpdateTempDefense(tDefPChange);
                break;
            case "Defense%":
                int defPChange = level * (int.Parse(effectSpecifics) * target.GetBaseDefense()) / basicDenominator;
                // Positive boosts always increase stat by at least 1.
                if (int.Parse(effectSpecifics) > 0)
                {
                    defPChange = Mathf.Max(1, defPChange);
                }
                target.UpdateDefense(defPChange);
                break;
            case "Skill":
                // Add an active skill.
                target.AddActiveSkill(effectSpecifics);
                break;
            case "TemporarySkill":
                target.AddTempActive(effectSpecifics);
                break;
            case "SingleTemporarySkill":
                // If the target has it then do nothing.
                if (!target.TempActiveExists(effectSpecifics))
                {
                    target.AddTempActive(effectSpecifics);
                }
                break;
            case "Spell":
                target.LearnSpell(effectSpecifics);
                break;
            case "TemporarySpell":
                target.LearnTempSpell(effectSpecifics);
                break;
            case "SetSpeed":
                target.SetMoveSpeed(int.Parse(effectSpecifics));
                break;
            case "Speed":
                target.UpdateSpeed(int.Parse(effectSpecifics) * level);
                break;
            case "BaseSpeed":
                target.SetMoveSpeedMax(int.Parse(effectSpecifics));
                break;
            case "Movement":
                AffectActorMovement(target, effectSpecifics, level);
                break;
            case "TempMovement":
                target.GainTempMovement(level * int.Parse(effectSpecifics));
                break;
            case "BaseActions":
                target.UpdateBaseActions(level * int.Parse(effectSpecifics));
                break;
            case "Actions":
                target.AdjustActionAmount(level * int.Parse(effectSpecifics));
                break;
            // Ice Cream isn't really the same since bonus actions are reset before passives are calculated. Maybe this is fine since storing up infinite actions doesn't make much sense physically.
            case "BonusActions":
                target.GainBonusActions(int.Parse(effectSpecifics));
                break;
            case "MoveType":
                target.SetMoveType(effectSpecifics);
                break;
            case "Initiative":
                target.ChangeInitiative(int.Parse(effectSpecifics));
                break;
            case "TempInitiative":
                target.UpdateTempInitiative(int.Parse(effectSpecifics));
                break;
            case "Weight":
                target.UpdateWeight(int.Parse(effectSpecifics));
                break;
            case "TempWeight":
                target.UpdateTempWeight(int.Parse(effectSpecifics));
                break;
            case "Death":
                target.SetCurrentHealth(0);
                target.ResetActions();
                break;
            case "MentalState":
                target.SetMentalState(effectSpecifics, level);
                break;
            case "Amnesia":
                target.RemoveRandomActiveSkill();
                break;
            case "Seal":
                target.RemoveRecentActiveSkill();
                break;
            case "Counter":
                target.UpdateCounter(int.Parse(effectSpecifics));
                break;
            case "CounterAttack":
                target.GainCounterAttacks(int.Parse(effectSpecifics));
                break;
            case "BaseHitChance":
                target.UpdateBaseHitChance(int.Parse(effectSpecifics));
                break;
            case "HitChance":
                target.UpdateHitChance(int.Parse(effectSpecifics));
                break;
            case "BaseDodge":
                target.UpdateBaseDodge(int.Parse(effectSpecifics));
                break;
            case "Dodge":
                target.UpdateDodgeChance(int.Parse(effectSpecifics));
                break;
            case "BaseCritChance":
                target.UpdateBaseCritChance(int.Parse(effectSpecifics));
                break;
            case "CritChance":
                target.UpdateCritChance(int.Parse(effectSpecifics));
                break;
            case "BaseCritDamage":
                target.UpdateBaseCritDamage(int.Parse(effectSpecifics));
                break;
            case "CritDamage":
                target.UpdateCritDamage(int.Parse(effectSpecifics));
                break;
            case "DisableDeathActives":
                target.DisableDeathActives();
                break;
            case "ReleaseGrapple":
                target.ReleaseGrapple();
                break;
            case "BreakGrapple":
                target.BreakGrapple();
                break;
            // Current And Base Are Simple Enough.
            case "BaseDamageResistance":
                string[] baseResist = effectSpecifics.Split("Equals");
                target.UpdateBaseDamageResist(baseResist[0], SafeParseInt(baseResist[1]));
                break;
            case "CurrentDamageResistance":
                string[] cResist = effectSpecifics.Split("Equals");
                target.UpdateCurrentDamageResist(cResist[0], SafeParseInt(cResist[1]));
                break;
            case "BaseElementalBonus":
                string[] baseBonus = effectSpecifics.Split("Equals");
                target.UpdateElementalDamageBonus(baseBonus[0], SafeParseInt(baseBonus[1]));
                break;
            case "ElementalDamageBonus":
                string[] cBonus = effectSpecifics.Split("Equals");
                target.UpdateCurrentElementalDamageBonus(cBonus[0], SafeParseInt(cBonus[1]));
                break;
            // TODO Scaling Is Twice As Complex For No Reason?
            case "ScalingElementalBonus":
                string[] scalingEB = effectSpecifics.Split("Equals");
                target.UpdateElementalDamageBonus(scalingEB[0], GetScalingInt(target, scalingEB[1], scalingEB[2], scalingEB[3]));
                break;
            case "ScalingElementalResist":
                string[] scalingER = effectSpecifics.Split("Equals");
                target.UpdateBaseDamageResist(scalingER[0], GetScalingInt(target, scalingER[1], scalingER[2], scalingER[3]));
                break;
            case "ManaEfficiency":
                target.IncreaseManaEfficiency(int.Parse(effectSpecifics));
                break;
            case "Mana":
                target.RestoreMana(int.Parse(effectSpecifics));
                break;
            case "Silence":
                target.Silence(int.Parse(effectSpecifics));
                break;
            case "Sleep":
                target.Sleep(int.Parse(effectSpecifics));
                break;
            case "Invisible":
                target.TurnInvisible(int.Parse(effectSpecifics));
                break;
            case "Barricade":
                target.GainBarricade(int.Parse(effectSpecifics));
                break;
            case "MinTempHealth":
                target.ChangeMinTempHealth(int.Parse(effectSpecifics));
                break;
            case "TempHealthDecay":
                target.ChangeTempHealthDecay(int.Parse(effectSpecifics));
                break;
            case "Guard":
                target.GainGuard(int.Parse(effectSpecifics));
                break;
            case "GuardRange":
                target.SetGuardRange(int.Parse(effectSpecifics));
                break;
            case "Disarm":
                string disarmedWeapon = target.Disarm();
                // TODO Try to remove any passives that the weapon granted and refresh the target's passives.
                break;
            // Power word kill.
            case "Kill":
                if (target.GetHealth() < int.Parse(effectSpecifics))
                {
                    AffectActor(target, "Death", effectSpecifics, level);
                }
                break;
            case "MagicPower":
                target.GainMagicPower(int.Parse(effectSpecifics));
                break;
            case "MagicResist":
                target.GainMagicResist(int.Parse(effectSpecifics));
                break;
            case "Artifact":
                target.GainArtifactStack(int.Parse(effectSpecifics));
                break;
            case "Buffer":
                target.GainBufferStack(int.Parse(effectSpecifics));
                break;
            case "Intangible":
                target.GainIntangible(int.Parse(effectSpecifics));
                break;
        }
    }

    protected int GetScalingInt(TacticActor target, string scaling, string scalingSpecifics, string scalingMultiplier)
    {
        switch (scaling)
        {
            case "PLevel":
            return GetTargetPassiveLevel(target, scalingSpecifics) * SafeParseInt(scalingMultiplier);
        }
        return 1;
    }

    protected int GetTargetPassiveLevel(TacticActor target, string passiveName)
    {
        return target.GetLevelFromPassive(passiveName);
    }

    protected int SafeParseInt(string intString, int defaultValue = 1)
    {
        try
        {
            return int.Parse(intString);
        }
        catch
        {
            return defaultValue;
        }
    }

    protected void AffectActorMovement(TacticActor target, string effectSpecifics, int power = 1)
    {
        int amount = SafeParseInt(effectSpecifics, -1);
        if (amount > 0)
        {
            target.GainMovement(power * amount);
            return;
        }
        switch (effectSpecifics)
        {
            case "Speed":
                amount = target.GetSpeed();
                break;
        }
        // You never lose movement, except by moving, if you want someone to stop moving you affect their speed, not their movement.
        if (amount <= 0) { amount = 1; }
        target.GainMovement(power * amount);
    }
}
