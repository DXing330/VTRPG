using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TacticActor : ActorStats
{
    // Skill modifiers.
    public List<string> turnSkillMods = new List<string>();
    public void AddTurnSkillMod(string mod)
    {
        // Can't stack same mod infinitely.
        if (turnSkillMods.Contains(mod)){return;}
        turnSkillMods.Add(mod);
    }
    public List<string> GetTurnSkillMods()
    {
        return turnSkillMods;
    }
    public void ClearTurnSkillMods()
    {
        turnSkillMods = new List<string>();
    }
    public bool HasTurnSkillMods()
    {
        return turnSkillMods != null && turnSkillMods.Count > 0;
    }
    public List<string> nextSkillMods = new List<string>();
    public void AddNextSkillMod(string mod)
    {
        // Can't stack same mod infinitely.
        if (nextSkillMods.Contains(mod)){return;}
        nextSkillMods.Add(mod);
    }
    public List<string> GetNextSkillMods()
    {
        return nextSkillMods;
    }
    public void ClearNextSkillMods()
    {
        nextSkillMods = new List<string>();
    }
    public bool HasNextSkillMods()
    {
        return nextSkillMods != null && nextSkillMods.Count > 0;
    }
    // Equipment.
    public void ResetEquipment()
    {
        ResetWeapon();
        ResetArmor();
        ResetEquipmentSkillsAndSpells();
    }
    public string weaponType;
    public void ResetWeapon()
    {
        weaponType = "";
        weaponName = "";
        weaponStats = "";
        weaponReach = 1;
    }
    public string Disarm()
    {
        // Remove weapon passives, that can be handled separately.
        string stats = weaponStats;
        ResetWeapon();
        return stats;
    }
    public void SetWeaponType(string newWeapon){weaponType = newWeapon;}
    public string GetWeaponType(){return weaponType;}
    public bool NoWeapon()
    {
        return weaponType == "";
    }
    public string weaponName;
    public void SetWeaponName(string newInfo){weaponName = newInfo;}
    public string GetWeaponName(){return weaponName;}
    public string weaponStats;
    public void SetWeaponStats(string newInfo){weaponStats = newInfo;}
    public string GetWeaponStats(){return weaponStats;}
    public int weaponReach = 1;
    public int GetWeaponReach()
    {
        if (GetAttackRange() > 1){return 999;}
        // Bigger things have bigger reach.
        return Mathf.Max(weaponReach, GetBaseWeight());
    }
    public void SetWeaponReach(int newInfo){weaponReach = newInfo;}
    public void ResetArmor()
    {
        armorName = "";
        armorStats = "";
    }
    public string armorName;
    public void SetArmorName(string newInfo){armorName = newInfo;}
    public string GetArmorName(){return armorName;}
    public string armorStats;
    public void SetArmorStats(string newInfo){armorStats = newInfo;}
    public string GetArmorStats(){return armorStats;}
    public List<string> equipmentSkills;
    public void AddEquipmentSkill(string newSkill)
    {
        if (equipmentSkills.Contains(newSkill)){return;}
        equipmentSkills.Add(newSkill);
    }
    public List<string> equipmentSpells;
    public void AddEquipmentSpell(string newSkill)
    {
        if (equipmentSpells.Contains(newSkill)){return;}
        equipmentSpells.Add(newSkill);
    }
    public void ResetEquipmentSkillsAndSpells()
    {
        equipmentSkills.Clear();
        equipmentSpells.Clear();
    }
    public override List<string> GetActiveSkills()
    {
        List<string> allActives = new List<string>(activeSkills);
        RefreshTempActives();
        allActives.AddRange(tempActives);
        allActives.AddRange(equipmentSkills);
        return allActives;
    }
    public override List<string> GetSpells()
    {
        List<string> allSpells = new List<string>(spells);
        allSpells.AddRange(tempSpells);
        allSpells.AddRange(equipmentSpells);
        return allSpells;
    }
    // Actor identity and battle membership.
    public GameObject actorObject;
    public void DestroyActor(){DestroyImmediate(actorObject);}
    protected bool sacrificed;
    public void MarkSacrificed(){sacrificed = true;}
    public bool WasSacrificed(){return sacrificed;}
    public void ResetSacrificed(){sacrificed = false;}
    public int team;
    public int GetTeam(){return team;}
    public void SetTeam(int newTeam){team = newTeam;}
    public string personalName;
    public void SetPersonalName(string newName){personalName = newName;}
    public string GetPersonalName()
    {
        if (personalName.Length <= 0){return GetSpriteName();}
        return personalName;
    }
    // Used mainly for enemy AI.
    public int counter = 0;
    public void ResetCounter(){counter = 0;}
    public void UpdateCounter(int changeAmount)
    {
        counter += changeAmount;
    }
    public void IncrementCounter(){counter++;}
    public void SetCounter(int newInfo){counter = newInfo;}
    public int GetCounter(){return counter;}

    // Actions.
    public int baseActions = 4;
    public void UpdateBaseActions(int amount){baseActions += amount;}
    public int GetBaseActions() { return baseActions; }
    public void SetBaseActions(int newActions){baseActions = newActions;}
    public int actions;
    public void SetActions(int newActions){actions = newActions;}
    public void ResetActions(){actions = 0;}
    public void AdjustActionAmount(int change){actions += change;}
    public void SpendAction(int actionCost = 1)
    {
        UpdateRoundActionTracker(actionCost);
        if (bonusActions > 0)
        {
            bonusActions -= actionCost;
            if (bonusActions < 0)
            {
                actions += bonusActions;
                bonusActions = 0;
            }
            return;
        }
        actions -= actionCost;
    }
    public int bonusActions;
    public void ResetBonusActions(){bonusActions = 0;}
    public void GainBonusActions(int amount){bonusActions += amount;}
    public int GetActions(){return actions + bonusActions;}
    public int basicAttackMultiplier = 100;
    public void SetBasicAttackMultiplier(int newAmount)
    {
        basicAttackMultiplier = newAmount;
    }
    public int GetBasicAttackMultiplier()
    {
        return basicAttackMultiplier;
    }
    public void ChangeBasicAttackMultiplier(int newAmount)
    {
        basicAttackMultiplier += newAmount;
    }
    public int attackActionCost = 2;
    public void SetAttackActionCost(int newCost)
    {
        attackActionCost = Mathf.Max(1, newCost);
    }
    public bool AttackActionsLeft()
    {
        return actions >= attackActionCost;
    }
    public void PayAttackCost()
    {
        SpendAction(attackActionCost);
    }
    public bool ActionsLeft(){return actions > 0;}

    // Movement.
    public int movement;
    public int GetMovement(){return movement + tempMovement;}
    protected void MoveAction()
    {
        SpendAction();
        movement += GetSpeed();
    }
    public void GainMovement(int amount){movement += amount;}
    public int tempMovement;
    public void GainTempMovement(int amount)
    {
        tempMovement += amount;
    }
    public void ResetTempMovement(){tempMovement = 0;}
    public void PayMoveCost(int cost)
    {
        if (tempMovement > 0)
        {
            tempMovement -= cost;
            if (tempMovement < 0)
            {
                movement += tempMovement;
                tempMovement = 0;
            }
        }
        else if (tempMovement <= 0)
        {
            movement -= cost;
        }
        if (movement < 0)
        {
            int maxActions = actions;
            for (int i = 0; i < maxActions; i++)
            {
                MoveAction();
                if (movement >= 0){break;}
            }
        }
    }

    // Counterattacks.
    public int counterAttacks;
    public void GainCounterAttacks(int amount = 1)
    {
        counterAttacks += amount;
    }
    public bool CounterAttackAvailable()
    {
        return counterAttacks > 0;
    }
    public void UseCounterAttack()
    {
        counterAttacks--;
    }

    // Turn lifecycle.
    public override void InitializeStats()
    {
        base.InitializeStats();
        currentEnergy = GetBaseEnergy();
        ClearNextSkillMods();
        ResetMentalState();
        ResetTarget();
        ResetHurtBy();
        ResetRoundTrackers();
        ResetSummonTrackers();
        ResetSacrificed();
    }
    public override void ResetStats()
    {
        base.ResetStats();
        movement = 0;
    }
    // Start of Turn
    public void NewTurn()
    {
        // Default is two actions.
        counterAttacks = 0;
        // Update all the turn trackers.
        // This is before passives obviously.
        // Some passives use previous round, some use recent round.
        AddToRoundTrackers();
        ResetStats();
        if (sleeping)
        {
            actions = 0;
        }
        else
        {
            actions = Mathf.Max(actions, baseActions);
        }
        // Pay for any summons. Pay for auras?
        ControlSummonedActors();
    }
    protected override void EndTurnResetStats()
    {
        base.EndTurnResetStats();
        ResetTempMovement();
        ResetBonusActions();
        ResetEquipmentSkillsAndSpells();
        ClearTurnSkillMods();
    }
    protected void TrackEndTurnRemainingStats()
    {
        if (remainingActionsEachRound == null || remainingActionsEachRound.Count <= 0){return;}
        remainingActionsEachRound[remainingActionsEachRound.Count - 1] = GetActions();
        remainingMovementEachRound[remainingMovementEachRound.Count - 1] = GetMovement();
    }
    public void EndTurn()
    {
        TrackEndTurnRemainingStats();
        tempMovement = 0;
        // Allow some slight turn manipulation by saving your actions.
        /*if (actions > 0)
        {
            UpdateTempInitiative(actions * 2);
            ResetActions();
        }*/
        EndTurnResetStats();
        UpdateMentalState();
        CheckBuffDuration();
        CheckStatusDuration();
    }
    public int GetMoveRangeBasedOnActions(int actionCount)
    {
        return (movement + (GetSpeed() * actionCount));
    }
    public int GetMaxMoveRange()
    {
        // Max of current / base speed and base / current actions.
        return Mathf.Max(GetSpeed(), GetMoveSpeed()) * Mathf.Max(2, actions) + Mathf.Max(0, GetMovement());
    }
    public int GetMoveRange(bool current = true)
    {
        if (current)
        {
            return (movement + (GetSpeed() * actions));
        }
        // Default is two actions.
        return GetMoveSpeed() * 2;
    }
    public int GetMoveRangeWhileAttacking(bool current = true)
    {
        if (current)
        {
            return (movement + (GetSpeed() * (actions - attackActionCost)));
        }
        return (movement + (GetMoveSpeed() * (baseActions - attackActionCost)));
    }

    // Map position and facing.
    public int location;
    public void SetLocation(int newLocation){location = newLocation;}
    public int GetLocation(){return location;}
    public int direction;
    public int GetDirection(){return direction;}
    public void SetDirection(int newDirection){direction = newDirection;}

    // Mental state.
    protected string immuneMentalState = "Calm";
    public string mentalState;
    public int mentalStateDuration = 0;
    public void UpdateMentalState()
    {
        if (mentalStateDuration > 0)
        {
            mentalStateDuration--;
            if (mentalStateDuration <= 0)
            {
                ResetMentalState();
            }
        }
    }
    public void ResetMentalState()
    {
        mentalState = "";
        mentalStateDuration = 0;
    }
    public void SetMentalState(string newInfo, int duration = 1)
    {
        if (duration < 1){duration = 1;}
        // Can't change to a weaker mental state.
        if (duration < mentalStateDuration){return;}
        // Can change to immune no matter what.
        if (newInfo == immuneMentalState)
        {
            mentalState = immuneMentalState;
            if (duration > mentalStateDuration)
            {
                mentalStateDuration = duration;
            }
            return;
        }
        // Immunity blocks one negative change.
        if (mentalState == immuneMentalState)
        {
            mentalState = "";
            mentalStateDuration = 0;
            return;
        }
        mentalState = newInfo;
        mentalStateDuration = duration;
    }
    public string GetMentalState(){ return mentalState; }

    // Keep track of skills/spells used, movement and attacks.
    protected void ResetRoundTrackers()
    {
        ResetRoundActionTracker();
        ResetRoundRemainingActionTracker();
        ResetRoundAttackTracker();
        ResetRoundDefendTracker();
        ResetRoundSkillTracker();
        ResetRoundSpellTracker();
        ResetRoundMoveTracker();
        ResetRoundItemTracker();
        ResetRoundRemainingMovementTracker();
        ResetLocationTracker();
        ResetHealthTracker();
    }
    protected void AddToRoundTrackers()
    {
        actionsEachRound.Add(0);
        attacksEachRound.Add(0);
        defendsEachRound.Add(0);
        skillsEachRound.Add(0);
        spellsEachRound.Add(0);
        movesEachRound.Add(0);
        itemsEachRound.Add(0);
        remainingActionsEachRound.Add(0);
        remainingMovementEachRound.Add(0);
        locationsEachRound.Add(GetLocation());
        healthEachRound.Add(GetHealth());
    }

    // Location and health history.
    public List<int> locationsEachRound;
    public void ResetLocationTracker()
    {
        locationsEachRound.Clear();
    }
    public int GetLatestLocation()
    {
        return locationsEachRound[locationsEachRound.Count - 1];
    }
    public int GetPreviousLocation()
    {
        if (locationsEachRound.Count <= 1){return -1;}
        return locationsEachRound[locationsEachRound.Count - 2];
    }
    public int GetInitialLocation()
    {
        return locationsEachRound[0];
    }
    public List<int> healthEachRound;
    public void ResetHealthTracker()
    {
        healthEachRound.Clear();
    }
    public int GetLatestHealth()
    {
        return healthEachRound[healthEachRound.Count - 1];
    }
    public int GetPreviousHealth()
    {
        if (healthEachRound.Count <= 1){return -1;}
        return healthEachRound[healthEachRound.Count - 2];
    }
    public int GetInitialHealth()
    {
        return healthEachRound[0];
    }

    // Action history.
    public List<int> actionsEachRound;
    public void ResetRoundActionTracker()
    {
        actionsEachRound.Clear();
    }
    public void UpdateRoundActionTracker(int amount = 1)
    {
        if (actionsEachRound == null || actionsEachRound.Count == 0)
        {
            actionsEachRound = new List<int>();
            actionsEachRound.Add(amount);
            return;
        }
        actionsEachRound[actionsEachRound.Count - 1] += amount;
    }
    public int ReturnCurrentRoundActions()
    {
        if (actionsEachRound == null || actionsEachRound.Count <= 0)
        {
            return -1;
        }
        return actionsEachRound[actionsEachRound.Count - 1];
    }
    public int ReturnPreviousRoundActions()
    {
        if (actionsEachRound == null || actionsEachRound.Count <= 1)
        {
            return -1;
        }
        return actionsEachRound[actionsEachRound.Count - 2];
    }
    public int ReturnTotalRoundActions()
    {
        if (actionsEachRound == null)
        {
            return 0;
        }
        return actionsEachRound.Sum();
    }
    public List<int> remainingActionsEachRound;
    public void ResetRoundRemainingActionTracker()
    {
        remainingActionsEachRound.Clear();
    }
    public int ReturnCurrentRoundRemainingActions()
    {
        if (remainingActionsEachRound == null || remainingActionsEachRound.Count <= 0)
        {
            return -1;
        }
        return remainingActionsEachRound[remainingActionsEachRound.Count - 1];
    }
    public int ReturnPreviousRoundRemainingActions()
    {
        if (remainingActionsEachRound == null || remainingActionsEachRound.Count <= 1)
        {
            return -1;
        }
        return remainingActionsEachRound[remainingActionsEachRound.Count - 2];
    }
    // Attack history.
    public List<int> attacksEachRound;
    public void ResetRoundAttackTracker()
    {
        attacksEachRound.Clear();
    }
    public void UpdateRoundAttackTracker()
    {
        if (attacksEachRound == null || attacksEachRound.Count == 0)
        {
            attacksEachRound = new List<int>();
            attacksEachRound.Add(1);
            return;
        }
        attacksEachRound[attacksEachRound.Count - 1]++;
    }
    public int ReturnCurrentRoundAttacks()
    {
        if (attacksEachRound.Count <= 0){return -1;}
        return attacksEachRound[attacksEachRound.Count - 1];
    }
    public int ReturnPreviousRoundAttacks()
    {
        if (attacksEachRound.Count <= 1){return -1;}
        return attacksEachRound[attacksEachRound.Count - 2];
    }
    public int ReturnTotalRoundAttacks()
    {
        return attacksEachRound.Sum();
    }
    // Defense history.
    public List<int> defendsEachRound;
    public void ResetRoundDefendTracker()
    {
        defendsEachRound.Clear();
    }
    public void UpdateRoundDefendTracker()
    {
        if (defendsEachRound == null || defendsEachRound.Count == 0)
        {
            defendsEachRound = new List<int>();
            defendsEachRound.Add(1);
            return;
        }
        defendsEachRound[defendsEachRound.Count - 1]++;
    }
    public int ReturnCurrentRoundDefends()
    {
        if (defendsEachRound.Count <= 0){return -1;}
        return defendsEachRound[defendsEachRound.Count - 1];
    }
    public int ReturnPreviousRoundDefends()
    {
        if (defendsEachRound.Count <= 1){return -1;}
        return defendsEachRound[defendsEachRound.Count - 2];
    }
    public int ReturnTotalRoundDefends()
    {
        return defendsEachRound.Sum();
    }
    // Skill history.
    public List<int> skillsEachRound;
    public List<string> skillsUsed;
    public bool SkillUsedAlready(string skillName)
    {
        return skillsUsed.Contains(skillName);
    }
    public void RemoveRecentActiveSkill()
    {
        // Check the latest used skill.
        if (skillsUsed.Count <= 0)
        {
            RemoveRandomActiveSkill();
            return;
        }
        string skillName = skillsUsed[skillsUsed.Count - 1];
        // Try to remove it.
        if (!RemoveActiveSkillByName(skillName))
        {
            // Else remove a random skill.
            RemoveRandomActiveSkill();
        }
    }
    public List<string> tempSkillsUsed;
    public bool RemoveTempActive(string skillName)
    {
        int indexOf = tempActives.IndexOf(skillName);
        if (indexOf >= 0)
        {
            tempActives.RemoveAt(indexOf);
            tempSkillsUsed.Add(skillName);
            return true;
        }
        return false;
    }
    public string ReturnMostUsedSkill()
    {
        if (skillsUsed == null || skillsUsed.Count <= 0){return "";}
        return skillsUsed.GroupBy(s => s).OrderByDescending(g => g.Count()).First().Key;
    }
    public string ReturnMostRecentSkill()
    {
        if (skillsUsed.Count < 1){return "";}
        return skillsUsed[skillsUsed.Count - 1];
    }
    public void ResetRoundSkillTracker()
    {
        skillsEachRound.Clear();
        skillsUsed.Clear();
        tempSkillsUsed.Clear();
    }
    public void UpdateRoundSkillTracker(string skillName)
    {
        if (skillsEachRound == null || skillsEachRound.Count == 0)
        {
            skillsEachRound = new List<int>();
            skillsEachRound.Add(1);
            return;
        }
        skillsEachRound[skillsEachRound.Count - 1]++;
        skillsUsed.Add(skillName);
    }
    public int ReturnCurrentRoundSkills()
    {
        if (skillsEachRound.Count <= 0){return -1;}
        return skillsEachRound[skillsEachRound.Count - 1];
    }
    public int ReturnPreviousRoundSkills()
    {
        if (skillsEachRound.Count <= 1){return -1;}
        return skillsEachRound[skillsEachRound.Count - 2];
    }
    public int ReturnTotalRoundSkills()
    {
        return skillsEachRound.Sum();
    }
    // Spell history.
    public List<int> spellsEachRound;
    public List<string> spellsUsed;
    public List<string> tempSpellsUsed;
    public void RemoveTempSpellActive(string spellName)
    {
        int indexOf = tempSpells.IndexOf(spellName);
        if (indexOf >= 0)
        {
            tempSpells.RemoveAt(indexOf);
            tempSpellsUsed.Add(spellName);
        }
    }
    public string ReturnMostUsedSpell()
    {
        if (spellsUsed == null || spellsUsed.Count == 0){return "";}
        return spellsUsed.GroupBy(s => s).OrderByDescending(g => g.Count()).First().Key;
    }
    public string ReturnMostRecentSpell()
    {
        if (spellsUsed == null || spellsUsed.Count < 1){return "";}
        return spellsUsed[spellsUsed.Count - 1];
    }
    public void ResetRoundSpellTracker()
    {
        spellsEachRound.Clear();
        spellsUsed.Clear();
        tempSpellsUsed.Clear();
    }
    public void UpdateRoundSpellTracker(string spellName)
    {
        if (spellsEachRound == null || spellsEachRound.Count == 0)
        {
            spellsEachRound = new List<int>();
            spellsEachRound.Add(1);
        }
        else
        {
            spellsEachRound[spellsEachRound.Count - 1]++;
        }
        spellsUsed.Add(spellName);
    }
    public int ReturnCurrentRoundSpells()
    {
        if (spellsEachRound == null || spellsEachRound.Count <= 0){return 0;}
        return spellsEachRound[spellsEachRound.Count - 1];
    }
    public int ReturnPreviousRoundSpells()
    {
        if (spellsEachRound == null || spellsEachRound.Count <= 1){return 0;}
        return spellsEachRound[spellsEachRound.Count - 2];
    }
    public int ReturnTotalRoundSpells()
    {
        if (spellsEachRound == null){return 0;}
        return spellsEachRound.Sum();
    }
    // Item history.
    public List<int> itemsEachRound;
    public List<string> itemsUsed;
    public bool ItemUsedAlready(string itemName)
    {
        return itemsUsed.Contains(itemName);
    }
    public string ReturnMostUsedItem()
    {
        if (itemsUsed == null || itemsUsed.Count <= 0){return "";}
        return itemsUsed.GroupBy(s => s).OrderByDescending(g => g.Count()).First().Key;
    }
    public string ReturnMostRecentItem()
    {
        if (itemsUsed.Count < 1){return "";}
        return itemsUsed[itemsUsed.Count - 1];
    }
    public void ResetRoundItemTracker()
    {
        itemsEachRound.Clear();
        itemsUsed.Clear();
    }
    public void UpdateRoundItemTracker(string itemName)
    {
        if (itemsEachRound == null || itemsEachRound.Count == 0)
        {
            itemsEachRound = new List<int>();
            itemsEachRound.Add(1);
            itemsUsed.Add(itemName);
            return;
        }
        itemsEachRound[itemsEachRound.Count - 1]++;
        itemsUsed.Add(itemName);
    }
    public int ReturnCurrentRoundItems()
    {
        if (itemsEachRound.Count <= 0){return -1;}
        return itemsEachRound[itemsEachRound.Count - 1];
    }
    public int ReturnPreviousRoundItems()
    {
        if (itemsEachRound.Count <= 1){return -1;}
        return itemsEachRound[itemsEachRound.Count - 2];
    }
    public int ReturnTotalRoundItems()
    {
        return itemsEachRound.Sum();
    }
    // Move history.
    public List<int> movesEachRound;
    public void ResetRoundMoveTracker()
    {
        movesEachRound.Clear();
    }
    public void UpdateRoundMoveTracker()
    {
        if (movesEachRound == null || movesEachRound.Count == 0)
        {
            movesEachRound = new List<int>();
            movesEachRound.Add(1);
            return;
        }
        movesEachRound[movesEachRound.Count - 1]++;
    }
    public int ReturnCurrentRoundMoves()
    {
        if (movesEachRound.Count <= 0) { return -1; }
        return movesEachRound[movesEachRound.Count - 1];
    }
    public int ReturnPreviousRoundMoves()
    {
        if (movesEachRound.Count <= 1) { return -1; }
        return movesEachRound[movesEachRound.Count - 2];
    }
    public int ReturnTotalRoundMoves()
    {
        return movesEachRound.Sum();
    }
    public List<int> remainingMovementEachRound;
    public void ResetRoundRemainingMovementTracker()
    {
        remainingMovementEachRound.Clear();
    }
    public int ReturnCurrentRoundRemainingMovement()
    {
        if (remainingMovementEachRound == null || remainingMovementEachRound.Count <= 0)
        {
            return -1;
        }
        return remainingMovementEachRound[remainingMovementEachRound.Count - 1];
    }
    public int ReturnPreviousRoundRemainingMovement()
    {
        if (remainingMovementEachRound == null || remainingMovementEachRound.Count <= 1)
        {
            return -1;
        }
        return remainingMovementEachRound[remainingMovementEachRound.Count - 2];
    }
    // Keep track of who hurt you, how many times and how much.
    public List<TacticActor> hurtByList;
    public List<int> hurtCount;
    public List<int> hurtAmount;
    public void ResetHurtBy()
    {
        hurtByList.Clear();
        hurtCount.Clear();
        hurtAmount.Clear();
    }
    public void RemoveHurtByAtIndex(int index)
    {
        if (index < 0 || index >= hurtByList.Count){return;}
        hurtByList.RemoveAt(index);
        hurtCount.RemoveAt(index);
        hurtAmount.RemoveAt(index);
    }
    public void HurtBy(TacticActor actor, int amount)
    {
        // Ignore those who don't deal any damage.
        if (amount <= 0){return;}
        int indexOf = hurtByList.IndexOf(actor);
        if (indexOf < 0)
        {
            hurtByList.Add(actor);
            hurtCount.Add(1);
            hurtAmount.Add(amount);
        }
        else
        {
            hurtCount[indexOf]++;
            hurtAmount[indexOf] += amount;
        }
    }
    // Don't hold grudges against the dead or your teammates.
    public void RefreshHurtBy()
    {
        for (int i = hurtByList.Count - 1; i >= 0; i--)
        {
            if (hurtByList[i].GetHealth() <= 0 || hurtByList[i].GetTeam() == GetTeam())
            {
                RemoveHurtByAtIndex(i);
            }
        }
    }
    public bool WasHurtByActor(TacticActor actor)
    {
        return hurtByList.Contains(actor);
    }
    public TacticActor GetHurtBy(bool most = true)
    {
        RefreshHurtBy();
        if (hurtByList.Count <= 0){return null;}
        if (hurtByList.Count == 1){return hurtByList[0];}
        int amount = 0;
        if (!most)
        {
            amount = 999;
        }
        int index = 0;
        for (int i = 0; i < hurtAmount.Count; i++)
        {
            if (most && hurtAmount[i] > amount)
            {
                amount = hurtAmount[i];
                index = i;
            }
            else if (!most && hurtAmount[i] < amount)
            {
                amount = hurtAmount[i];
                index = i;
            }
        }
        return hurtByList[index];
    }
    public TacticActor GetHurtCount(bool most = true)
    {
        RefreshHurtBy();
        if (hurtByList.Count <= 0){return null;}
        if (hurtByList.Count == 1){return hurtByList[0];}
        int amount = 0;
        if (!most)
        {
            amount = 999;
        }
        int index = 0;
        for (int i = 0; i < hurtCount.Count; i++)
        {
            if (most && hurtCount[i] > amount)
            {
                amount = hurtCount[i];
                index = i;
            }
            else if (!most && hurtCount[i] < amount)
            {
                amount = hurtCount[i];
                index = i;
            }
        }
        return hurtByList[index];
    }
    // Targeting.
    public TacticActor target;
    public void ResetTarget(){ target = null; }
    public void SetTarget(TacticActor newTarget) { target = newTarget; }
    public TacticActor GetTarget()
    {
        if (!TargetAlive()){ResetTarget();}
        return target;
    }
    public bool TargetAlive()
    {
        if (target == null){return false;}
        return target.GetHealth() > 0;
    }
    public bool TargetValid()
    {
        if (!TargetAlive()){return false;}
        if (target.invisible){return false;}
        return true;
    }
    // Grappling.
    public TacticActor grappledActor;
    public TacticActor GetGrappledActor(){return grappledActor;}
    public void ResetGrappledActor(){grappledActor = null;}
    public void ReleaseGrapple()
    {
        if (grappledActor != null)
        {
            grappledActor.ResetGrappledByActor();
            ResetGrappledActor();
        }
    }
    public void GrappleActor(TacticActor newGrapple)
    {
        // Break any grapple you have to try to grapple someone else.
        ReleaseGrapple();
        // Can't grabble someone that is already grappled.
        if (newGrapple.Grappled()){return;}
        grappledActor = newGrapple;
        grappledActor.SetGrappledByActor(this);
    }
    public TacticActor grappledByActor;
    public void SetGrappledByActor(TacticActor actor){grappledByActor = actor;}
    public TacticActor GetGrappledByActor(){return grappledByActor;}
    public void ResetGrappledByActor(){grappledByActor = null;}
    public void BreakGrapple()
    {
        if (grappledByActor != null)
        {
            grappledByActor.ResetGrappledActor();
            ResetGrappledByActor();
        }
    }
    public bool Grappled(BattleMap map = null)
    {
        
        if (grappledByActor != null)
        {
            // Dead.
            if (grappledByActor.GetHealth() <= 0)
            {
                BreakGrapple();
                return false;
            }
            // Too heavy to be grappled.
            if (grappledByActor.GetWeight() < GetWeight() - 1)
            {
                BreakGrapple();
                return false;
            }
            // Out of range.
            if (map != null && map.DistanceBetweenActors(this, grappledByActor) > 1)
            {
                BreakGrapple();
                return false;
            }
            return true;
        }
        return false;
    }
    public bool Grappling(BattleMap map = null)
    {
        if (grappledActor != null)
        {
            if (grappledActor.GetHealth() <= 0)
            {
                ReleaseGrapple();
                return false;
            }
            if (grappledActor.GetWeight() > GetWeight() + 1)
            {
                ReleaseGrapple();
                return false;
            }
            if (map != null && map.DistanceBetweenActors(this, grappledActor) > 1)
            {
                ReleaseGrapple();
                return false;
            }
            return true;
        }
        return false;
    }
    // Summons.
    protected void ResetSummonTrackers()
    {
        summoned = false;
        summonedBy = null;
        summonedActors.Clear();
    }
    public bool summoned = false;
    public bool UncontrolledSummon()
    {
        return (summoned && summonedBy == null);
    }
    public TacticActor summonedBy;
    public void SetSummonedBy(TacticActor summonedByActor)
    {
        summonedBy = summonedByActor;
    }
    // Breaks the connection between the summoned and summoner.
    public void ResetSummonedBy()
    {
        summonedBy.ReleaseSummonedActor(this);
        summonedBy = null;
    }
    public List<TacticActor> summonedActors;
    public void AddSummonedActor(TacticActor newActor)
    {
        summonedActors.Add(newActor);
        newActor.summoned = true;
        newActor.SetSummonedBy(this);
    }
    public void ReleaseSummonedActor(TacticActor actor)
    {
        summonedActors.Remove(actor);
    }
    // Pay 1 action each to control summoned units.
    public void ControlSummonedActors()
    {
        for (int i = summonedActors.Count - 1; i >= 0; i--)
        {
            if (actions > 0)
            {
                actions--;
                continue;
            }
            // If you can't pay then release them.
            else
            {
                summonedActors[i].ResetSummonedBy();
            }
        }
    }
    // Save/load stat summaries.
    public List<string> ReturnSpendableStats()
    {
        List<string> stats = new List<string>();
        stats.Add(GetActions().ToString());
        stats.Add(GetEnergy().ToString());
        stats.Add(GetMovement().ToString());
        stats.Add(GetMana().ToString());
        return stats;
    }
    public string ReturnPersistentStats(string delimiter = "|")
    {
        string healthString = GetHealth().ToString();
        string curses = GetCurseString();
        string mana = GetMana().ToString();
        return healthString + delimiter + curses + delimiter + mana;
    }
}
