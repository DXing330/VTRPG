using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActorStats : ActorInitialStats
{
    public int ID = -1;
    public void ResetID(){ID = -1;}
    public void SetID(int newID){ID = newID;}
    public int GetID(){return ID;}
    public void ReloadPassives()
    {
        SetInitialStat("Passives", String.Join(passiveDelimiter, GetPassiveSkills()));
        SetInitialStat("PassiveLevels", String.Join(passiveDelimiter, GetPassiveSkills()));
        GetInitialStats();
    }
    public void CopyBaseStats(ActorStats newStats)
    {
        SetBaseHealth(newStats.GetBaseHealth());
        SetCurrentHealth(GetBaseHealth());
        SetBaseAttack(newStats.GetBaseAttack());
        SetBaseDefense(newStats.GetBaseDefense());
    }
    public void ChangeFormFromString(string newStats)
    {
        ChangeForm(newStats.Split(delimiter).ToList());
    }
    public void ChangeForm(List<string> newStats)
    {
        // Save the current health when changing forms.
        int prevHealth = GetHealth();
        ResetPassives();
        EndTurnResetStats();
        stats = newStats;
        for (int i = 0; i < stats.Count; i++)
        {
            SetInitialStat(stats[i], changeFormStatNames[i]);
        }
        currentEnergy = baseEnergy;
        currentAttack = baseAttack;
        currentDefense = baseDefense;
        currentSpeed = moveSpeed;
        currentHealth = prevHealth;
    }
    public void ResetStatsBeforeLoading()
    {
        for (int i = 0; i < statNames.Count; i++)
        {
            SetInitialStat("", statNames[i]);
        }
        // TempAtk/Crit/Dodge/Etc.
        ResetStats();
        EndTurnResetStats();
        // Silence/Invis/Guard/Etc.
        ResetUniqueEffects();
    }
    // Start of Turn
    public virtual void ResetStats()
    {
        currentAttack = baseAttack;
        currentDefense = baseDefense;
        currentSpeed = moveSpeed;
        currentWeight = weight;
        currentDodge = baseDodge;
        ResetResistances();
        ResetDamageBonuses();
        // Initiative is used to determine your turn in the round.
        // At the start of your turn during the round reset it.
        ResetTempInitiative();
        CheckStartUniqueEffects();
    }
    // End of Turn
    protected virtual void EndTurnResetStats()
    {
        ResetTempAttack();
        ResetTempDefense();
        ResetTempHealth();
        ResetBonusAttackRange();
        CheckEndUniqueEffects();
        tempWeight = 0;
        currentCritDamage = baseCritDamage;
        currentCrit = baseCrit;
        currentHitChance = baseHitChance;
    }
    public List<string> ReturnStats()
    {
        List<string> stats = new List<string>();
        if (GetTempHealth() > 0)
        {
            stats.Add(GetHealth()+"+("+GetTempHealth()+")");
        }
        else
        {
            stats.Add(GetHealth().ToString());
        }
        stats.Add(GetAttack().ToString());
        stats.Add(GetDefense().ToString());
        stats.Add(GetAttackRange().ToString());
        return stats;
    }
    public void HalfRestore()
    {
        SetCurrentHealth((GetBaseHealth() + GetHealth()) / 2);
    }
    public void FullRestore()
    {
        SetCurrentHealth(GetBaseHealth());
        ClearStatuses();
    }
    public void NearDeath()
    {
        SetCurrentHealth(1);
    }
    // CURRENT/TEMP STATS
    public int currentWeight;
    public void UpdateWeight(int changeAmount)
    {
        currentWeight += changeAmount;
    }
    public int tempWeight;
    public void UpdateTempWeight(int changeAmount)
    {
        tempWeight += changeAmount;
    }
    public int GetWeight()
    {
        return currentWeight + tempWeight;
    }
    public int tempInitiative;
    public void ResetTempInitiative()
    {
        tempInitiative = 0;
    }
    public void UpdateTempInitiative(int amount)
    {
        tempInitiative += amount;
    }
    public int GetCurrentInitiative()
    {
        return initiative + tempInitiative;
    }
    public void ChangeInitiative(int change) { initiative += change; }
    public int tempHealth;
    // You can keep a little bit of temphealth to buff temphealth as a stat.
    public void ResetTempHealth()
    {
        // Barricade means you don't lose temp health except by damage.
        if (barricade){return;}
        tempHealth = tempHealth / 2;
    }
    public void UpdateTempHealth(int changeAmount) { tempHealth += changeAmount; }
    public int GetTempHealth() { return tempHealth; }
    public void Heal(int amount)
    {
        if (amount < 0)
        {
            amount = Mathf.Abs(amount);
        }
        currentHealth += amount;
        if (currentHealth > GetBaseHealth()) { currentHealth = GetBaseHealth(); }
    }
    public void Hurt(int amount)
    {
        if (amount < 0)
        {
            amount = Mathf.Abs(amount);
        }
        currentHealth -= amount;
    }
    public void UpdateHealth(int changeAmount, bool decrease = true)
    {
        if (decrease)
        {
            if (tempHealth > 0)
            {
                int temp = tempHealth;
                tempHealth -= changeAmount;
                if (tempHealth < 0) { tempHealth = 0; }
                changeAmount -= temp;
            }
            if (changeAmount < 0) { return; }
            currentHealth -= changeAmount;
        }
        else { currentHealth += changeAmount; }
        if (currentHealth > GetBaseHealth()) { currentHealth = GetBaseHealth(); }
    }
    public int TakeEffectDamage(int damage, string type = "Physical")
    {
        if (type == "Physical")
        {
            damage -= GetDefense();
        }
        if (damage < 0)
        {
            return 0;
        }
        damage = TakeDamage(damage, type);
        return damage;
    }
    public List<string> bonusDmgTypes;
    // Deal bonus damage of type.
    public List<int> baseDmgBonuses;
    public List<int> currentDmgBonuses;
    public int ReturnDamageBonusOfType(string type)
    {
        int indexOf = bonusDmgTypes.IndexOf(type);
        if (indexOf < 0){return 0;}
        return currentDmgBonuses[indexOf];
    }
    public void ResetDamageBonuses()
    {
        for (int i = 0; i < bonusDmgTypes.Count; i++)
        {
            currentDmgBonuses[i] = baseDmgBonuses[i];
        }
    }
    public void UpdateElementalDamageBonus(string type, int amount)
    {
        int indexOf = bonusDmgTypes.IndexOf(type);
        if (indexOf < 0)
        {
            bonusDmgTypes.Add(type);
            baseDmgBonuses.Add(amount);
            currentDmgBonuses.Add(amount);
        }
        else
        {
            baseDmgBonuses[indexOf] += amount;
        }
    }
    public void UpdateCurrentElementalDamageBonus(string type, int amount)
    {
        int indexOf = bonusDmgTypes.IndexOf(type);
        if (indexOf < 0)
        {
            bonusDmgTypes.Add(type);
            baseDmgBonuses.Add(0);
            currentDmgBonuses.Add(amount);
        }
        else
        {
            currentDmgBonuses[indexOf] += amount;
        }
    }
    public List<string> resistDmgTypes;
    // Resist damage.
    public List<int> baseDmgResists;
    public List<int> currentDmgResists;
    public int ReturnDamageResistanceOfType(string type)
    {
        int indexOf = resistDmgTypes.IndexOf(type);
        if (indexOf < 0){return 0;}
        return currentDmgResists[indexOf];
    }
    public void ResetResistances()
    {
        for (int i = 0; i < resistDmgTypes.Count; i++)
        {
            currentDmgResists[i] = baseDmgResists[i];
        }
    }
    public void UpdateBaseDamageResist(string type, int amount)
    {
        int indexOf = resistDmgTypes.IndexOf(type);
        if (indexOf < 0)
        {
            resistDmgTypes.Add(type);
            baseDmgResists.Add(amount);
            currentDmgResists.Add(amount);
        }
        else
        {
            baseDmgResists[indexOf] += amount;
        }
    }
    public void UpdateCurrentDamageResist(string type, int amount)
    {
        int indexOf = resistDmgTypes.IndexOf(type);
        if (indexOf < 0)
        {
            resistDmgTypes.Add(type);
            baseDmgResists.Add(0);
            currentDmgResists.Add(amount);
        }
        else
        {
            currentDmgResists[indexOf] += amount;
        }
    }
    public virtual int TakeDamage(int damage, string type = "Physical")
    {
        // Intangible reduces damage to 1.
        if (intangible)
        {
            damage = 1;
        }
        // Buffer stacks prevent damage.
        if (ConsumeBufferStack()){return 0;}
        WakeUp();
        int resistance = ReturnDamageResistanceOfType(type);
        if (resistance != 0)
        {
            damage = damage * (100 - resistance) / 100;
        }
        if (damage < 0)
        {
            if (resistance > 100)
            {
                Heal(damage);
                return damage;
            }
            damage = 0;
        }
        UpdateHealth(damage);
        return damage;
    }
    public int currentEnergy;
    public void UpdateEnergy(int changeAmount, bool decrease = false)
    {
        if (decrease) { LoseEnergy(changeAmount); }
        else { currentEnergy += changeAmount; }
        if (currentEnergy > GetBaseEnergy()) { currentEnergy = GetBaseEnergy(); }
        if (currentEnergy < 0)
        {
            currentEnergy = 0;
        }
    }
    public void LoseEnergy(int amount)
    {
        currentEnergy -= amount;
        if (currentEnergy < 0) { currentEnergy = 0; }
    }
    public int GetEnergy() { return currentEnergy; }
    public bool SpendEnergy(int energyCost)
    {
        if (GetEnergy() >= energyCost)
        {
            LoseEnergy(energyCost);
            return true;
        }
        return false;
    }
    public int tempAttack; // Used specifically for end of turn attack buffs.
    public void ResetTempAttack() { tempAttack = 0; }
    public void UpdateTempAttack(int changeAmount) { tempAttack += changeAmount; }
    public int currentAttack;
    public int GetAttack() { return currentAttack + tempAttack; }
    public void UpdateAttack(int changeAmount) { currentAttack += changeAmount; }
    public int tempDefense; // Used specifically for end of turn attack buffs.
    public void ResetTempDefense() { tempDefense = 0; }
    public void UpdateTempDefense(int changeAmount) { tempDefense += changeAmount; }
    public int currentDefense;
    public int GetDefense() { return currentDefense + tempDefense; }
    public void UpdateDefense(int changeAmount) { currentDefense += changeAmount; }
    public int currentSpeed;
    public int GetSpeed() { return currentSpeed; }
    public override void SetMoveSpeed(int newMoveSpeed)
    {
        moveSpeed = newMoveSpeed;
        currentSpeed = moveSpeed;
    }
    public void UpdateSpeed(int changeAmount)
    {
        currentSpeed += changeAmount;
        if (currentSpeed < 0)
        {
            currentSpeed = 0;
        }
    }
    // Stats that are not stored in the stat string.
    // Call this before making an actor to ensure that the actor has a fresh start.
    public virtual void InitializeStats()
    {
        ClearStatuses();
        ClearBuffs();
        resistDmgTypes.Clear();
        baseDmgResists.Clear();
        currentDmgResists.Clear();
        bonusDmgTypes.Clear();
        baseDmgBonuses.Clear();
        currentDmgBonuses.Clear();
        ResetStats();
        EndTurnResetStats();
        ResetUniqueEffects();
        ResetBufferStacks();
        ResetArtifactStacks();
    }
    public void UpdateBaseHitChance(int amount){baseHitChance += amount;}
    public int currentHitChance;
    public int GetHitChance(){return currentHitChance;}
    public void UpdateHitChance(int amount){currentHitChance += amount;}
    public void UpdateBaseDodge(int amount){baseDodge += amount;}
    public int currentDodge;
    public int GetDodgeChance(){return currentDodge;}
    public void UpdateDodgeChance(int amount){currentDodge += amount;}
    public void UpdateBaseCritChance(int amount){baseCrit += amount;}
    public int currentCrit;
    public int GetCritChance(){return currentCrit;}
    public void UpdateCritChance(int amount){currentCrit += amount;}
    public void UpdateBaseCritDamage(int amount){baseCritDamage += amount;}
    public int currentCritDamage;
    public int GetCritDamage(){return currentCritDamage;}
    public void UpdateCritDamage(int amount){currentCritDamage += amount;}
    public string ReturnRandomActiveSkill()
    {
        if (activeSkills.Count <= 0){return "";}
        return activeSkills[UnityEngine.Random.Range(0, activeSkills.Count)];
    }
    public bool SkillExists(string skillName)
    {
        for (int i = 0; i < activeSkills.Count; i++)
        {
            if (activeSkills[i].Contains(skillName)){return true;}
        }
        for (int i = 0; i < tempActives.Count; i++)
        {
            if (tempActives[i].Contains(skillName)){return true;}
        }
        return false;
    }
    public string ReturnSkillContainingName(string skillName)
    {
        for (int i = 0; i < activeSkills.Count; i++)
        {
            if (activeSkills[i].Contains(skillName)){return activeSkills[i];}
        }
        for (int i = 0; i < tempActives.Count; i++)
        {
            if (tempActives[i].Contains(skillName)){return tempActives[i];}
        }
        return "";
    }
    public void RemoveActiveSkill(int index)
    {
        activeSkills.RemoveAt(index);
    }
    public bool RemoveActiveSkillByName(string skillName)
    {
        int indexOf = activeSkills.IndexOf(skillName);
        if (indexOf < 0){return false;}
        RemoveActiveSkill(indexOf);
        return true;
    }
    public void RemoveRandomActiveSkill()
    {
        if (tempActives.Count <= 0)
        {
            // Remove a regular active skill only if there are no temp actives to remove.
            if (activeSkills.Count <= 0) { return; }
            int index = UnityEngine.Random.Range(0, activeSkills.Count);
            RemoveActiveSkill(index);
            return;
        }
        int tempIndex = UnityEngine.Random.Range(0, tempActives.Count);
        tempActives.RemoveAt(tempIndex);
    }

    public void AddActiveSkill(string skillName)
    {
        if (skillName.Length <= 1) { return; }
        if (activeSkills.Contains(skillName)) { return; }
        activeSkills.Add(skillName);
    }
    public void LearnRandomActive(ActorStats otherActor)
    {
        AddActiveSkill(otherActor.ReturnRandomActiveSkill());
    }
    public void TeachRandomActive(ActorStats otherActor)
    {
        otherActor.LearnRandomActive(this);
    }
    public List<string> tempActives;
    public List<string> GetTempActives(){return tempActives;}
    public void AddTempActive(string skillName)
    {
        if (skillName.Length <= 1) { return; }
        tempActives.Add(skillName);
    }
    public bool TempActiveExists(string skillName)
    {
        return tempActives.Contains(skillName);
    }
    public int ActiveSkillCount()
    {
        int count = 0;
        for (int i = 0; i < activeSkills.Count; i++)
        {
            if (activeSkills[i].Length <= 0) { continue; }
            count++;
        }
        count += tempActives.Count;
        return count;
    }
    public string GetActiveSkill(int index)
    {
        if (index < 0 || index >= activeSkills.Count) { return ""; }
        return activeSkills[index];
    }
    public List<string> GetActiveSkills()
    {
        List<string> allActives = new List<string>(activeSkills);
        allActives.AddRange(tempActives);
        return allActives;
    }
    public List<string> tempSpells;
    public void ResetSpells()
    {
        spells.Clear();
        tempSpells.Clear();
    }
    public void LearnSpell(string newInfo)
    {
        if (newInfo.Length < 6){return;}
        if (spells.Contains(newInfo)){return;}
        spells.Add(newInfo);
    }
    public void LearnTempSpell(string newInfo)
    {
        if (newInfo.Length < 6){return;}
        if (spells.Contains(newInfo)){return;}
        tempSpells.Add(newInfo);
    }
    public void RemoveTempSpell(string spellInfo)
    {
        int indexOf = tempSpells.IndexOf(spellInfo);
        if (indexOf >= 0)
        {
            tempSpells.RemoveAt(indexOf);
        }
    }
    public List<string> GetSpells()
    {
        List<string> allSpells = new List<string>(spells);
        allSpells.AddRange(tempSpells);
        return allSpells;
    }
    public List<string> GetSpellNames()
    {
        List<string> spellNames = new List<string>();
        if (SpellCount() <= 0) { return spellNames; }
        spellNames.AddRange(spells);
        spellNames.AddRange(tempSpells);
        return spellNames;
    }
    public int SpellCount()
    {
        return spells.Count + tempSpells.Count;
    }
    public List<string> GetAttackingPassives(StatDatabase buffDB)
    {
        List<string> aPassives = new List<string>(attackingPassives);
        aPassives.AddRange(GetBattleBuffsAndStatuses(buffDB, "Attack"));
        return aPassives;
    }
    public List<string> GetDefendingPassives(StatDatabase buffDB)
    {
        List<string> dPassives = new List<string>(defendingPassives);
        dPassives.AddRange(GetBattleBuffsAndStatuses(buffDB, "Defend"));
        return dPassives;
    }
    public List<string> buffs;
    public List<string> GetBattleBuffsAndStatuses(StatDatabase buffDB, string timing)
    {
        List<string> battleBS = new List<string>();
        for (int i = 0; i < buffs.Count; i++)
        {
            string data = buffDB.ReturnValue(buffs[i]);
            string[] splitData = data.Split("|");
            if (splitData[0] == timing)
            {
                battleBS.Add(data);
            }
        }
        for (int i = 0; i < statuses.Count; i++)
        {
            string data = buffDB.ReturnValue(statuses[i]);
            string[] splitData = data.Split("|");
            if (splitData[0] == timing)
            {
                battleBS.Add(data);
            }
        }
        return battleBS;
    }
    public int defaultBuffDuration = 3;
    public List<int> buffDurations;
    public List<string> GetBuffs(){return buffs;}
    public List<int> GetBuffDurations(){return buffDurations;}
    public void AddBuff(string newCondition, int duration)
    {
        if (newCondition.Length <= 0) { return; }
        if (duration < 0)
        {
            duration = defaultBuffDuration;
        }
        int indexOf = buffs.IndexOf(newCondition);
        if (indexOf < 0)
        {
            buffs.Add(newCondition);
            buffDurations.Add(duration);
        }
        else
        {
            buffDurations[indexOf] = buffDurations[indexOf] + duration;
        }
    }
    public void ClearBuffs(string specifics = "*")
    {
        if (specifics == "*")
        {
            buffs.Clear();
            buffDurations.Clear();
            return;
        }
        RemoveBuff(specifics);
    }
    public void RemoveBuff(string buffName)
    {
        if (buffName == "All")
        {
            buffs.Clear();
            buffDurations.Clear();
            return;
        }
        for (int i = buffs.Count - 1; i >= 0; i--)
        {
            if (buffs[i] == buffName)
            {
                buffs.RemoveAt(i);
                buffDurations.RemoveAt(i);
            }
        }
    }
    public bool BuffExists(string buffName)
    {
        return buffs.Contains(buffName);
    }
    public void AdjustBuffDuration(int index, int amount = -1)
    {
        buffDurations[index] = buffDurations[index] + amount;
    }
    public void CheckBuffDuration()
    {
        for (int i = buffs.Count - 1; i >= 0; i--)
        {
            if (buffDurations[i] == 0)
            {
                buffs.RemoveAt(i);
                buffDurations.RemoveAt(i);
            }
        }
    }
    public List<string> GetUniqueStatusAndBuffs()
    {
        List<string> SB = GetUniqueStatuses();
        SB.AddRange(GetBuffs());
        return SB;
    }
    public List<string> GetUnqiueSBDurations()
    {
        List<string> SBD = GetUniqueStatusDurationsAndStacks();
        for (int i = 0; i < buffDurations.Count; i++)
        {
            SBD.Add(buffDurations[i].ToString());
        }
        return SBD;
    }
    public override void AddStatus(string newCondition, int duration)
    {
        if (newCondition.Length <= 1 || newCondition.Trim().Length <= 1) { return; }
        // Luck gives you a chance to ignore statuses.
        int luckRoll = UnityEngine.Random.Range(0, 100);
        if (luckRoll < GetLuck()){return;}
        // Artifact stacks block new statuses.
        // Can this be gamed to prevent curses? Maybe.
        if (ConsumeArtifactStack()){return;}
        // Permanent statuses can stack up infinitely and are a win condition.
        if (duration < 0)
        {
            statuses.Add(newCondition);
            statusDurations.Add(duration);
            return;
        }
        int indexOf = statuses.IndexOf(newCondition);
        if (indexOf < 0)
        {
            statuses.Add(newCondition);
            statusDurations.Add(duration);
        }
        else
        {
            statusDurations[indexOf] = statusDurations[indexOf] + duration;
        }
    }
    public List<string> GetUniqueStatuses()
    {
        List<string> unique = new List<string>(statuses.Distinct());
        return unique;
    }
    public List<string> GetUniqueStatusStacks()
    {
        List<string> uniqueCount = new List<string>();
        List<string> unique = GetUniqueStatuses();
        for (int i = 0; i < unique.Count; i++)
        {
            uniqueCount.Add(utility.CountStringsInList(statuses, unique[i]).ToString());
        }
        return uniqueCount;
    }
    public List<string> GetUniqueStatusDurationsAndStacks()
    {
        List<string> uniqueCount = new List<string>();
        List<string> unique = GetUniqueStatuses();
        int count = -1;
        for (int i = 0; i < unique.Count; i++)
        {
            count = utility.CountStringsInList(statuses, unique[i]);
            if (count > 1)
            {
                uniqueCount.Add("-"+count);
            }
            else
            {
                int duration = ReturnStatusDuration(unique[i]);
                if (duration < 0)
                {
                    uniqueCount.Add("-1");
                }
                else
                {
                    uniqueCount.Add(duration.ToString());
                }
            }
        }
        return uniqueCount;
    }
    public bool AnyStatusExists(List<string> names = null)
    {
        if (names == null)
        {
            return statuses.Count > 0;
        }
        for (int i = 0; i < names.Count; i++)
        {
            if (StatusExists(names[i])){return true;}
        }
        return false;
    }
    public bool StatusExists(string statusName)
    {
        return statuses.Contains(statusName);
    }
    public int StatusStacks(string statusName)
    {
        return utility.CountStringsInList(statuses, statusName);
    }
    public int ReturnStatusDuration(string statusName)
    {
        int indexOf = statuses.IndexOf(statusName);
        if (indexOf < 0)
        {
            return 0;
        }
        return statusDurations[indexOf];
    }
    public void AdjustStatusDuration(int index, int amount = -1)
    {
        statusDurations[index] = statusDurations[index] + amount;
    }
    public void CheckStatusDuration()
    {
        for (int i = statuses.Count - 1; i >= 0; i--)
        {
            if (statusDurations[i] == 0)
            {
                statuses.RemoveAt(i);
                statusDurations.RemoveAt(i);
            }
        }
    }
    // UNIQUE STATUSES //
    public void ResetUniqueEffects()
    {
        Unsilence();
        WakeUp();
        RemoveInvisibility();
        RemoveBarricade();
        RemoveGuard();
    }
    public void CheckStartUniqueEffects()
    {
        CheckInvisibility();
        CheckBarricade();
        CheckGuard();
    }
    public void CheckEndUniqueEffects()
    {
        CheckSilence();
        CheckSleeping();
    }
    public bool silenced = false;
    public bool GetSilenced(){return silenced;}
    public int silenceDuration;
    public void CheckSilence()
    {
        (silenced, silenceDuration) = utility.DecrementBoolDuration(silenced, silenceDuration);
    }
    public void Silence(int duration)
    {
        silenced = true;
        if (silenceDuration < duration)
        {
            silenceDuration = duration;
        }
    }
    public void Unsilence()
    {
        silenced = false;
        silenceDuration = 0;
    }
    public bool sleeping = false;
    public bool GetSleeping(){return sleeping;}
    public int sleepDuration;
    public void CheckSleeping()
    {
        (sleeping, sleepDuration) = utility.DecrementBoolDuration(sleeping, sleepDuration);
    }
    public void Sleep(int duration)
    {
        sleeping = true;
        if (sleepDuration < duration)
        {
            sleepDuration = duration;
        }
    }
    public void WakeUp()
    {
        sleeping = false;
        sleepDuration = 0;
    }
    public bool invisible = false;
    public int invisibleDuration;
    public void TurnInvisible(int duration)
    {
        invisible = true;
        if (invisibleDuration < duration)
        {
            invisibleDuration = duration;
        }
    }
    public void CheckInvisibility()
    {
        (invisible, invisibleDuration) = utility.DecrementBoolDuration(invisible, invisibleDuration);
    }
    public void RemoveInvisibility()
    {
        invisible = false;
        invisibleDuration = 0;
    }
    public bool barricade = false;
    public bool GetBarricade(){return barricade;}
    public int barricadeDuration;
    public void GainBarricade(int duration)
    {
        barricade = true;
        if (barricadeDuration < duration)
        {
            barricadeDuration = duration;
        }
    }
    public void CheckBarricade()
    {
        (barricade, barricadeDuration) = utility.DecrementBoolDuration(barricade, barricadeDuration);
    }
    public void RemoveBarricade()
    {
        barricade = false;
        barricadeDuration = 0;
    }
    public bool guarding = false;
    public bool Guarding(){return guarding;}
    public int guardRange;
    public void SetGuardRange(int newInfo)
    {
        guardRange = Mathf.Max(newInfo, guardRange);
    }
    public int GetGuardRange(){return guardRange;}
    public int guardDuration;
    public void GainGuard(int duration, int range = 1)
    {
        guarding = true;
        if (guardDuration == 0 || guardRange < range)
        {
            guardRange = range;
        }
        if (guardDuration < duration)
        {
            guardDuration = duration;
        }
    }
    public void CheckGuard()
    {
        (guarding, guardDuration) = utility.DecrementBoolDuration(guarding, guardDuration);
        if (!guarding)
        {
            guardRange = 0;
        }
    }
    public void RemoveGuard()
    {
        guarding = false;
        guardDuration = 0;
        guardRange = 0;
    }
    // NULLIFY DAMAGE/STATUS/ETC Stacks
    public bool intangible = false;
    public bool GetIntangible(){return intangible;}
    public int intangibleDuration;
    public void GainIntangible(int duration)
    {
        intangible = true;
        if (intangibleDuration < duration)
        {
            intangibleDuration = duration;
        }
    }
    public void CheckIntangible()
    {
        (intangible, intangibleDuration) = utility.DecrementBoolDuration(intangible, intangibleDuration);
    }
    public void RemoveIntangible()
    {
        intangible = false;
        intangibleDuration = 0;
    }
    public int bufferStacks;
    public void GainBufferStack(int amount = 1)
    {
        bufferStacks += amount;
    }
    public void ResetBufferStacks()
    {
        bufferStacks = 0;
    }
    public bool ConsumeBufferStack()
    {
        if (bufferStacks <= 0){return false;}
        bufferStacks--;
        return true;
    }
    public int artifactStacks;
    public void GainArtifactStack(int amount = 1)
    {
        artifactStacks += amount;
    }
    public void ResetArtifactStacks()
    {
        artifactStacks = 0;
    }
    public bool ConsumeArtifactStack()
    {
        if (artifactStacks <= 0){return false;}
        artifactStacks--;
        return true;
    }
}
