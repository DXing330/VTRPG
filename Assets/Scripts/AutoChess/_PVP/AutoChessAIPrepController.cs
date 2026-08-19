using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoChessAIPrepController : MonoBehaviour
{
    // For greater details during player matches.
    public bool recordAIDecisions = false;
    // Load In Genome To Make Choices.
    public AutoChessPVPGenome genome;
    public void DefaultGenome()
    {
        genome = new AutoChessPVPGenome();
        genome.ResetToDefault();
    }
    public AutoChessPrepManager prepManager;
    public AutoChessAIPrepAegirPlacementController aegirPlacement;
    public AutoChessAIPrepEquipmentController equipmentController;
    public AutoChessFactionManager factionManager;
    Dictionary<string, float> stackCache = new();
    float GetStacks(string factionName)
    {
        if(stackCache.TryGetValue(factionName, out float value))
        {
            return value;
        }
        return 0f;
    }
    void BuildStackCache()
    {
        stackCache.Clear();
        foreach (string faction in factionManager.factionData.GetAllFactions())
        {
            stackCache[faction] = int.Parse(factionManager.factionData.GetStacksOfFaction(faction));
        }
    }
    // Only changes when buy/sell/swap a unit, all those actions already clear the cache. No benefit of caching bench/field separately on top of the above.
    Dictionary<AutoActorRollUpData, float> keepCache = new();
    Dictionary<AutoActorRollUpData, float> buyCache = new();
    void LogUnitAndFactionScores()
    {
        string scores = "Field~";
        for (int i = 0; i < prepManager.fieldSlots.Count; i++)
        {
            scores += $"{prepManager.fieldSlots[i].GetName()}:{KeepScore(prepManager.fieldSlots[i], false)}";
            if (i < prepManager.fieldSlots.Count - 1){scores += ",";}
        }
        prepManager.dataManager.AddLog(scores);
        scores = "Bench~";
        for (int i = 0; i < prepManager.benchSlots.Count; i++)
        {
            scores += $"{prepManager.benchSlots[i].GetName()}:{KeepScore(prepManager.benchSlots[i])}";
            if (i < prepManager.benchSlots.Count - 1){scores += ",";}
        }
        prepManager.dataManager.AddLog(scores);
        scores = "Stacks~" + factionManager.factionData.GetAllFactionStacksString();
        prepManager.dataManager.AddLog(scores);
    }
    void LogShopScores()
    {
        string scores = "Shop~";
        for (int i = 0; i < shopManager.shopActors.Count; i++)
        {
            shopManager.Select(i);
            var shopActor = shopManager.GetSelectedActor();
            if (shopActor == null) continue;
            scores += $"{shopActor.GetName()}:{BuyScore(shopActor)}";
            if (i < shopManager.shopActors.Count - 1)
            {
                scores += ",";
            }
        }
        prepManager.dataManager.AddLog(scores);
    }
    void ClearScoreCaches()
    {
        keepCache.Clear();
        buyCache.Clear();
    }
    public AutoChessShopManager shopManager;
    public StatDatabase unitRarity;
    public StatDatabase factionThresholds;
    public void AIPrepPhase(AutoChessDataManager dataManager)
    {
        prepManager.SetDataManager(dataManager);
        prepManager.DisableUI();
        shopManager.shopData.GeneratePVPCurrentListing();
        shopManager.PVPRefreshData();
        BuildStackCache();
        ClearScoreCaches();
        if (recordAIDecisions)
        {
            dataManager.AddLog(dataManager.GetEconState());
            LogUnitAndFactionScores();
            LogShopScores();
        }
        // Log Economy State + Choice + Choice Option Weights.
        EconomyAction(dataManager);
        // Log Each Shop Score + Choice.
        AcquireLoop(dataManager);
        // Log Each Choice.
        AutoPlaceFieldUnits(dataManager);
        // Log Each Equipment Score + Choice.
        equipmentController.AutoPlaceEquipment(this);
        aegirPlacement.AegirPlaceFieldUnits();
        if (recordAIDecisions)
        {
            dataManager.AddLog(dataManager.GetEconState());
            LogUnitAndFactionScores();
        }
        prepManager.StartBattle();
    }
    // ========================================================
    // 1. ECONOMY
    // ========================================================
    void EconomyAction(AutoChessDataManager dataManager)
    {
        int gold = dataManager.GetGold();
        int level = dataManager.GetLevel();
        int hp = dataManager.GetHealth();
        int winStreak = dataManager.GetWinStreak();
        int lossStreak = dataManager.GetLossStreak();
        float levelScore = genome.GetByName("W_ECON_LEVEL_TIMING") * LevelUrgency(dataManager);
        float saveScore = genome.GetByName("W_ECON_INTEREST") * (Mathf.Min(gold / 10, 5));
        float hpUrgencyScore = genome.GetByName("W_ECON_HP_URGENCY") * (100 - hp);
        float rerollScore = genome.GetByName("W_ECON_REROLL") * ShopQualityScore();
        float winStreakScore = genome.GetByName("W_ECON_STREAK_WIN") * (Mathf.Min(winStreak, 5));
        float lossStreakScore = genome.GetByName("W_ECON_STREAK_LOSS") * (Mathf.Min(lossStreak, 5));
        // If Losing And High HP Then Keep Saving To Lose More For Econ.
        if (lossStreakScore > hpUrgencyScore && lossStreakScore > 0f)
        {
            saveScore += (lossStreakScore - hpUrgencyScore);
        }
        // If Winning Then Level More.
        if (winStreakScore > 0f)
        {
            levelScore += winStreakScore;
        }
        if (recordAIDecisions)
        {
            dataManager.AddLog($"EconFinalScores~" + $"Level:{levelScore:F2}," + $"Save:{saveScore:F2}," + $"Reroll:{rerollScore:F2}");
        }
        // Priority: Level up if urgent and affordable, else reroll if shop is weak and gold is high, else save
        if (!dataManager.MaxLevel() && gold >= 4 && levelScore > saveScore && levelScore > rerollScore)
        {
            prepManager.BuyExp();
            if (recordAIDecisions)
            {
                dataManager.AddLog("EconDecision~Level");
            }
            EconomyAction(dataManager);
        }
        else if (gold >= 1 && rerollScore > saveScore && rerollScore > levelScore)
        {
            prepManager.PVPRerollShop();
            if (recordAIDecisions)
            {
                dataManager.AddLog("EconDecision~Reroll");
                LogShopScores();
            }
            EconomyAction(dataManager);
        }
        // Else Save Money -> Do Nothing.
        else
        {
            if (recordAIDecisions)
            {
                dataManager.AddLog("EconDecision~Save");
            }
        }
    }
    float LevelUrgency(AutoChessDataManager dataManager)
    {
        int level = dataManager.GetLevel();
        int round = dataManager.GetRound();
        float expectedLevel = 2 + (round * 0.3f);
        return Mathf.Max(0, expectedLevel - level);
    }
    float ShopQualityScore()
    {
        float total = 0f;
        int validSlots = 0;
        for (int i = 0; i < shopManager.shopActors.Count; i++)
        {
            shopManager.Select(i);
            var actor = shopManager.GetSelectedActor();
            if (actor == null) continue;
            total += BuyScore(actor);
            validSlots++;
        }
        if (validSlots == 0) return -5f; // Empty shop = terrible
        return total / validSlots;
    }
    // ========================================================
    // 2. ACQUIRE (Shop + Bench merged)
    // ========================================================
    void AcquireLoop(AutoChessDataManager dataManager)
    {
        bool actionTaken = true;
        while (actionTaken && dataManager.GetGold() > 0)
        {
            actionTaken = false;
            int bestSlot = -1;
            float bestScore = 0;
            for (int i = 0; i < shopManager.shopActors.Count; i++)
            {
                shopManager.Select(i);
                var shopActor = shopManager.GetSelectedActor();
                if (shopActor == null) continue;
                float score = BuyScore(shopActor);
                int cost = shopManager.SelectedCost();
                score += genome.GetByName("W_BUY_ECON_STRETCH") * ((float)cost / Mathf.Max(1, dataManager.GetGold()));
                if (score > bestScore)
                {
                    bestScore = score;
                    bestSlot = i;
                }
            }
            if (bestSlot < 0) break;
            shopManager.Select(bestSlot);
            int buyCost = shopManager.SelectedCost();
            // Bench full? Sell worst unit to make room
            if (prepManager.AvailableBenchSlot() < 0)
            {
                int worstIndex = FindWorstBenchUnit();
                if (worstIndex < 0) break;
                float worstKeep = KeepScore(prepManager.benchSlots[worstIndex]);
                if (bestScore <= worstKeep + genome.GetByName("W_SELL_MARGIN")){break;} // margin threshold
                if (recordAIDecisions)
                {
                    dataManager.AddLog($"Sell:{prepManager.benchSlots[worstIndex].GetName()},KeepScore:{worstKeep:F2},BuyScore:{bestScore:F2}");
                }
                prepManager.FastSellSelectedActor(prepManager.benchSlots[worstIndex]);
                ClearScoreCaches();
            }
            if (dataManager.GetGold() >= buyCost)
            {
                if (recordAIDecisions)
                {
                    dataManager.AddLog($"Buy:{shopManager.GetSelectedActor().GetName()},BuyScore:{bestScore:F2},Cost:{buyCost}");
                }
                prepManager.PVPBuySelectedActor(buyCost);
                ClearScoreCaches();
                actionTaken = true;
            }
        }
    }
    float BuyScore(AutoActorRollUpData unit)
    {
        if (unit == null){return -999f;}
        if (buyCache.TryGetValue(unit, out float cached))
        {
            return cached;
        }
        string name = unit.GetName();
        int tier = GetUnitTier(name);
        int cost = GetUnitCost(name);
        float score = 0;
        score += genome.GetByName("W_BUY_TIER") * tier;
        score += genome.GetByName("W_BUY_SYNERGY") * SynergyValue(unit);
        score += genome.GetByName("W_BUY_DUPLICATE") * DuplicateValue(name);
        score += genome.GetByName("W_UNIT_STACK_GENERATION") * StackGainValue(unit);
        score += genome.GetByName("W_UNIT_STACK_SCALING") * StackScalingValue(unit);
        score += genome.GetByName("W_UNIT_CYCLE") * CycleValue(unit);
        score += genome.GetByName("W_UNIT_CYCLE_SCALING") * CycleScalingValue(unit);
        List<string> factions = unit.GetFactions();
        for (int i = 0; i < factions.Count; i++)
        {
            score += EvaluateFaction(factions[i]);
        }
        buyCache[unit] = score;
        return score;
    }
    public float KeepScore(AutoActorRollUpData unit, bool bench = true)
    {
        if (unit == null){return float.MinValue;}
        if (keepCache.TryGetValue(unit, out float cached))
        {
            return cached;
        }
        string name = unit.GetName();
        int tier = GetUnitTier(name);
        int star = unit.GetLevel();
        float score = 0;
        score += genome.GetByName("W_KEEP_TIER") * tier;
        // Score Increased Based On How Many Synergies Are Lost
        float keepSynergyScore = 0f;
        // For Bench Units.
        if (bench)
        {
            for (int i = 0; i < unit.GetFactions().Count; i++)
            {
                keepSynergyScore += SynergyLossIfRemoved(unit.GetFactions()[i]);
            }
            score += genome.GetByName("W_KEEP_SYNERGY") * keepSynergyScore;
        }
        // For Field Units.
        else
        {
            for (int i = 0; i < unit.GetFactions().Count; i++)
            {
                keepSynergyScore += SynergyLossIfRemoved(unit.GetFactions()[i], false);
            }
            score += genome.GetByName("W_KEEP_SYNERGY") * keepSynergyScore;
        }
        score += genome.GetByName("W_KEEP_DUPLICATE") * DuplicatePotential(name);
        score += genome.GetByName("W_KEEP_STAR_LEVEL") * star;
        score += genome.GetByName("W_UNIT_STACK_GENERATION") * StackGainValue(unit, false);
        score += genome.GetByName("W_UNIT_STACK_SCALING") * StackScalingValue(unit);
        score += genome.GetByName("W_UNIT_CYCLE") * CycleValue(unit, false);
        score += genome.GetByName("W_UNIT_CYCLE_SCALING") * CycleScalingValue(unit, bench);
        List<string> factions = unit.GetFactions();
        for (int i = 0; i < factions.Count; i++)
        {
            score += EvaluateFaction(factions[i]);
        }
        keepCache[unit] = score;
        return score;
    }
    int FindWorstBenchUnit()
    {
        int worstIndex = -1;
        float worstScore = 9999f;
        for (int i = 0; i < prepManager.benchSlots.Count; i++)
        {
            float score = KeepScore(prepManager.benchSlots[i]);
            if (score < worstScore)
            {
                worstScore = score;
                worstIndex = i;
            }
        }
        return worstIndex;
    }
    int GetBestBenchIndex()
    {
        if (prepManager.benchSlots.Count <= 0){return -1;}
        float bestScore = float.MinValue;
        int index = 0;
        for (int i = 0; i < prepManager.benchSlots.Count; i++)
        {
            float benchScore = KeepScore(prepManager.benchSlots[i]);
            if (benchScore > bestScore)
            {
                bestScore = benchScore;
                index = i;
            }
        }
        return index;
    }
    // ======================================================
    // 3. BOARD PLACEMENT
    // ======================================================
    int GetWorstFieldIndex()
    {
        if (prepManager.fieldSlots.Count <= 0){return -1;}
        // If duplicates on the field, then return lowest rarity duplicate first.
        float worstScore = 999f;
        int index = -1;
        for (int i = 0; i < prepManager.fieldSlots.Count; i++)
        {
            float fieldScore = KeepScore(prepManager.fieldSlots[i], false);
            if (fieldScore < worstScore)
            {
                worstScore = fieldScore;
                index = i;
            }
        }
        return index;
    }
    void CheckAllSwaps(int totalSwaps)
    {
        List<AutoActorRollUpData> allBench = prepManager.benchSlots;
        List<AutoActorRollUpData> allField = prepManager.fieldSlots;
        // Don't Swap Infinitely, Pass Through Thrice At Most.
        if (totalSwaps > 2){return;}
        int benchIndex = -1;
        int fieldIndex = -1;
        float bestSwapValue = float.MinValue;
        for (int i = 0; i < allBench.Count; i++)
        {
            for (int j = 0; j < allField.Count; j++)
            {
                float swapValue = KeepScore(allBench[i]) - KeepScore(allField[j], false) + FindActiveFactionDifferences(allBench[i], allField[j]);
                if (swapValue > bestSwapValue)
                {
                    benchIndex = i;
                    fieldIndex = j;
                    bestSwapValue = swapValue;
                }
            }
        }
        if (bestSwapValue >= genome.GetByName("W_PLACE_SWAP_THRESH") && benchIndex >= 0 && fieldIndex >= 0)
        {
            int benchLocation = allBench[benchIndex].GetLocation();
            int bestTile = aegirPlacement.FindBestTileForRole(allBench[benchIndex]);
            // No Open Spots.
            if (bestTile < 0){return;}
            if (recordAIDecisions)
            {
                prepManager.dataManager.AddLog($"Swap:{allField[fieldIndex].GetName()}->{allBench[benchIndex].GetName()},SwapScore:{bestSwapValue:F2},OldScore:{KeepScore(allField[fieldIndex], false)},NewScore:{KeepScore(allBench[benchIndex])}");
            }
            allBench[benchIndex].SetLocation(bestTile);
            allBench[benchIndex].SetDirection(1);
            allField[fieldIndex].SetLocation(benchLocation);
            prepManager.MoveFromFieldToBench(allField[fieldIndex]);
            prepManager.MoveFromBenchToField(allBench[benchIndex]);
            prepManager.SaveToDataManager();
            ClearScoreCaches();
            // Good Swap Means Try Again.
            CheckAllSwaps(totalSwaps + 1);
        }
    }
    void AutoPlaceFieldUnits(AutoChessDataManager dataManager)
    {
        // Place The Best Units If There Is Space.
        if (prepManager.fieldSlots.Count < prepManager.GetMaxFieldSlots())
        {
            int bestBenchIndex = GetBestBenchIndex();
            if (bestBenchIndex < 0)
            {
                // Go Buy Something, Anything Is Better Than Nothing.
                AcquireLoop(dataManager);
                if (prepManager.benchSlots.Count > 0)
                {
                    AutoPlaceFieldUnits(dataManager);
                }
                return;
            }
            AutoActorRollUpData bestBenchActor = prepManager.benchSlots[bestBenchIndex];
            int bestTile = aegirPlacement.FindBestTileForRole(bestBenchActor);
            // No Open Spots.
            if (bestTile < 0){return;}
            if (recordAIDecisions)
            {
                dataManager.AddLog($"Place:{bestBenchActor.GetName()},Score:{KeepScore(bestBenchActor):F2}");
            }
            bestBenchActor.SetLocation(bestTile);
            bestBenchActor.SetDirection(1);
            prepManager.MoveFromBenchToField(bestBenchActor);
            prepManager.SaveToDataManager();
            AutoPlaceFieldUnits(dataManager);
            return;
        }
        // Determine The Weakest Field Unit And The Best Bench And See If They Should Be Swapped.
        else
        {
            CheckAllSwaps(0);
        }
    }
    // Check If You're Losing/Gaining Any Thresholds During A Swap
    float FindActiveFactionDifferences(AutoActorRollUpData benchUnit, AutoActorRollUpData fieldUnit)
    {
        float totalDifference = 0f;
        // Adding a copy of a bench unit doesn't improve synergy, it can only decrease it.
        List<string> fieldFactions = fieldUnit.RAWGetFactions();
        if (prepManager.UnitExists(benchUnit, false) && benchUnit.GetName() != fieldUnit.GetName())
        {
            for (int i = 0; i < fieldFactions.Count; i++)
            {
                totalDifference -= SynergyLossIfRemoved(fieldFactions[i], false);
            }
            return totalDifference;
        }
        List<string> benchFactions = benchUnit.RAWGetFactions();
        // Factions lost by removing the field unit.
        for (int i = 0; i < fieldFactions.Count; i++)
        {
            string fieldFaction = fieldFactions[i];
            bool shared = false;
            for (int j = 0; j < benchFactions.Count; j++)
            {
                if (benchFactions[j] == fieldFaction)
                {
                    shared = true;
                    break;
                }
            }
            if (!shared)
            {
                totalDifference -= SynergyLossIfRemoved(fieldFaction, false);
            }
        }
        // Factions gained by adding the bench unit.
        for (int i = 0; i < benchFactions.Count; i++)
        {
            string benchFaction = benchFactions[i];
            bool shared = false;
            for (int j = 0; j < fieldFactions.Count; j++)
            {
                if (fieldFactions[j] == benchFaction)
                {
                    shared = true;
                    break;
                }
            }
            if (!shared)
            {
                totalDifference += SynergyValue(benchFaction);
            }
        }
        return totalDifference;
    }
    // Needs To Track From Field Or Bench, Bench Only Hurts Econ.
    float SynergyLossIfRemoved(string removedFaction, bool bench = true)
    {
        float loss = 0;
        bool econ = factionManager.factionData.EconFaction(removedFaction);
        // Bench Units Only Count For Econ.
        if (bench && !econ){return 0f;}
        if (removedFaction == "Harmony")
        {
            for (int i = 0; i < mainFactions.Count; i++)
            {
                loss += SynergyLossIfRemoved(mainFactions[i]);
            }
            return loss;
        }
        int currentCount = factionManager.factionData.GetCountOfFaction(removedFaction);
        int[] thresholds = GetThresholds(removedFaction);
        foreach (var t in thresholds)
        {
            if (currentCount >= t && (currentCount - 1) < t)
            {
                if (econ)
                {
                    loss += genome.GetByName("W_ECON_SYN_HIT");
                }
                else if (factionManager.factionData.MainFaction(removedFaction))
                {
                    loss += genome.GetByName("W_MAIN_SYN_HIT");
                }
                else
                {
                    loss += genome.GetByName("W_SYN_HIT");
                }
            }
        }
        return loss;
    }
    // ======================================================
    // HELPERS — Wire these to your database
    // ======================================================
    public int GetUnitTier(string name)
    {
        return int.Parse(unitRarity.ReturnValue(name));
    }
    int GetUnitCost(string name)
    {
        return shopManager.ReturnActorCost(name);
    }
    float GetPower(AutoActorRollUpData unit)
    {
        return (unit.GetLevel() * 10f) + (unit.GetHealth() / 10) + (unit.GetAttack() / 5) + (unit.GetDefense() / 3) + (unit.GetResist() / 5);
    }
    int[] GetThresholds(string factionName)
    {
        string val = factionThresholds.ReturnValue(factionName); // e.g. "2|3" or "3|6"
        if (string.IsNullOrEmpty(val)) return new int[] { 2 }; // fallback
        string[] parts = val.Split('|');
        return System.Array.ConvertAll(parts, int.Parse);
    }
    bool EmblemSynergy(string emblemName)
    {
        string faction = emblemName.Replace(" Emblem", "");
        return SynergyValue(faction) > 0;
    }
    public float SynergyValue(string faction)
    {
        if (string.IsNullOrEmpty(faction)){return 0f;}
        if (faction == "Harmony")
        {
            float totalSynergy = 0f;
            for (int i = 0; i < mainFactions.Count; i++)
            {
                totalSynergy += SynergyValue(mainFactions[i]);
            }
            return totalSynergy;
        }
        int currentCount = factionManager.factionData.GetCountOfFaction(faction);
        int newCount = currentCount + 1;
        int[] thresholds = GetThresholds(faction);
        bool hitsThreshold = false;
        bool maintainsActive = false;
        bool oneAway = false;
        for (int i = 0; i < thresholds.Length; i++)
        {
            int t = thresholds[i];
            if (currentCount < t && newCount >= t) hitsThreshold = true;
            if (currentCount >= t) maintainsActive = true;
            if (currentCount == t - 1) oneAway = true;
        }
        if (hitsThreshold)
        {
            // Split This By Type Of Hit.
            if (factionManager.factionData.EconFaction(faction))
            {
                return genome.GetByName("W_ECON_SYN_HIT");
            }
            else if (factionManager.factionData.MainFaction(faction))
            {
                return genome.GetByName("W_MAIN_SYN_HIT");
            }
            else
            {
                return genome.GetByName("W_SYN_HIT");
            }
        }
        if (maintainsActive)
        {
            return genome.GetByName("W_SYN_MAINTAIN");
        }
        if (oneAway)
        {
            return genome.GetByName("W_SYN_ONEAWAY");
        }
        return 0f;
    }
    float SynergyValue(AutoActorRollUpData unit)
    {
        // If The Unit Already Exists On The Field Then It Does Not Increase Synergy.
        if (prepManager.UnitExists(unit, false)){return 0f;}
        List<string> factions = unit.GetFactions();
        if (factions == null || factions.Count == 0){return 0f;}
        float totalSynergy = 0f;
        if (factions.Contains("Harmony"))
        {
            for (int i = 0; i < mainFactions.Count; i++)
            {
                totalSynergy += SynergyValue(mainFactions[i]);
            }
            return totalSynergy;
        }
        for (int i = 0; i < factions.Count; i++)
        {
            // If Econ Faction And Unit Exists Then Return 0f, It Will Not Gain Extra Synergy.
            if (factionManager.factionData.EconFaction(factions[i]) && prepManager.UnitExists(unit)){return 0f;}
            totalSynergy += SynergyValue(factions[i]);
        }
        return totalSynergy;
    }
    float StackScalingValue(AutoActorRollUpData unit)
    {
        AutoChessTrait trait = unit.GetTrait();
        if (trait == null || trait.timing != "DuringBattle"){return 0f;}
        if (!trait.specifics.Contains("Scaling")){return 0f;}
        float value = 0f;
        string[] parts = trait.specifics.Split("Scaling");
        if (parts.Length < 2){return 0f;}
        string[] factions = parts[1].Split("AND");
        foreach (string faction in factions)
        {
            if (faction == "Main")
            {
                for (int i = 0; i < mainFactions.Count; i++)
                {
                    value += GetStacks(mainFactions[i]);
                }
            }
            else
            {
                value += GetStacks(faction);
            }
        }
        return value / 10f;
    }
    float StackGainValue(AutoActorRollUpData unit, bool isBuying = true)
    {
        float value = 0f;
        AutoChessTrait trait = unit.GetTrait();
        if (trait == null || trait.timing == "None"){return value;}
        float stackAmount = GetTraitStackAmount(trait);
        string effect = trait.effect;
        List<string> factions = new List<string>();
        // Directly Help Self.
        switch(trait.effect)
        {
            // Consider Self.
            case "Self":
            case "SelfActive":
                factions.AddRange(unit.GetFactions());
                break;
            // Can Fit Into Any Team.
            case "HighestActive":
            case "RandomActive":
            case "SelfAndBackActive":
            case "FrontActive":
            case "SelfAndFrontActive":
            case "SelfAndFrontLineActive":
            case "CopyFront":
            case "CopyBack":
                string target = factionManager.HighestStackActiveFaction();
                if (target == "")
                {
                    target = factionManager.HighestStackFaction();
                }
                factions.Add(target);
                break;
        }
        foreach(string faction in factions)
        {
            float factionValue = EvaluateFaction(faction);
            float lifetime = GetStackLifetime(trait, isBuying);
            value += stackAmount * lifetime * factionValue;
        }
        return value;
    }
    float GetTraitStackAmount(AutoChessTrait trait)
    {
        if (float.TryParse(trait.specifics, out float amount))
        {
            return amount;
        }
        return 1f;
    }
    float GetStackLifetime(AutoChessTrait trait, bool isBuying)
    {
        int roundsLeft = Mathf.Min(15, prepManager.dataManager.GetHealth() / (Mathf.Max(1, prepManager.dataManager.GetLevel() + prepManager.dataManager.GetRound())));
        switch(trait.timing)
        {
            case "OnPurchase":
                return isBuying ? 1f : 0f;
            case "OnForwardSkill":
                return roundsLeft / 2;
            case "FirstSkill":
            case "StartBattle":
                return roundsLeft;
            case "OnForwardAttack":
            case "OnSkill":
                return roundsLeft * 2f;
            case "OnAttack":
                return roundsLeft * 3f;
            case "OnKill":
                return roundsLeft * 0.5f;
        }
        return 0f;
    }
    string GetFactionType(string faction)
    {
        if (faction == "Harmony"){return "Main";}
        if (factionManager.factionData.MainFaction(faction)){return "Main";}
        if (factionManager.factionData.EconFaction(faction)){return "Econ";}
        return "Side";
    }
    float CycleValue(AutoActorRollUpData unit, bool isBuying = true)
    {
        AutoChessTrait trait = unit.GetTrait();
        if (trait == null) return 0f;
        float value = 1f;
        // Make Some Traits More Valuable.
        switch (trait.effect)
        {
            default:
            break;
            case "Equipment":
            value = 3f;
            break;
            case "HighestActiveUnit":
            value = 3f;
            break;
        }
        switch (trait.timing)
        {
            case "OnPurchase":
                return (isBuying ? 2f : 0f) * value;
            case "OnSold":
                return 1f * value;
            default:
                return 0f;
        }
    }
    // Those That Are More Valuable To Keep On The Field Due To Their Traits.
    float CycleScalingValue(AutoActorRollUpData unit, bool bench = false)
    {
        AutoChessTrait trait = unit.GetTrait();
        float value = 0f;
        if (trait == null) return value;
        string s = trait.specifics;
        if (s.Contains("UnitsBoughtMultiBy"))
            value += 1f;
        if (s.Contains("GoldSpentMultiBy"))
            value += 2f;
        if (s.Contains("BenchSizeMultiBy"))
            value += 2f;
        if (s.Contains("SelfActiveUnits"))
            value += 2f;
        if (trait.timing == "StartBattle")
        {
            value += 1f;
        }
        // Free Stacks
        if (bench && unit.GetName() == "Ceylon")
        {
            value += 1f;
        }
        // Potentially Free Gold.
        if (bench && unit.GetName() == "Swire")
        {
            value += 0.5f;
        }
        return value;
    }
    protected List<string> mainFactions = new List<string>(){"Yan", "Sargon", "Kjerag", "Aegir", "Victoria", "Laterano"};
    float EvaluateMainFactionScaling()
    {
        float score = 0f;
        for (int i = 0; i < mainFactions.Count; i++)
        {
            score += EvaluateFaction(mainFactions[i]);
        }
        return score;
    }
    // Tracks How Valuable A Faction Is.
    float EvaluateFaction(string faction)
    {
        if (faction == "Harmony")
        {
            return EvaluateMainFactionScaling();
        }
        float score = 0;
        int count = factionManager.factionData.GetCountOfFaction(faction);
        bool active = factionManager.factionData.FactionActive(faction);
        // All Factions Require At Least 2 Units To Be Active.
        float stacks = GetStacks(faction);
        // Divide By 10 For Now To Decrease The Value Of Stacks.
        stacks /= 10f;
        score += stacks * genome.GetByName("W_FACTION_STACKS");
        if (active)
        {
            score *= genome.GetByName("W_FACTION_ACTIVE");
        }
        else
        {
            score *= genome.GetByName("W_FACTION_INACTIVE");
        }
        switch(GetFactionType(faction))
        {
            case "Main":
                score += count * genome.GetByName("W_FACTION_MAIN");
                break;
            case "Econ":
                score += count * genome.GetByName("W_FACTION_ECON");
                break;
            case "Side":
                score += count * genome.GetByName("W_FACTION_SIDE");
                break;
        }
        if (faction == genome.GetPreferredFaction())
        {
            score += genome.GetByName("W_PREF_FACTION") * (count + 1);
        }
        return score;
    }
    float DuplicateValue(string name)
    {
        int count = prepManager.GetLevelOneActorsWithName(name);
        if (count >= 2) return genome.GetByName("W_DUP_MERGE");
        if (count == 1) return genome.GetByName("W_DUP_CLOSE");
        return 0f;
    }
    // Only Called For Units You Already Own.
    float DuplicatePotential(string name)
    {
        int count = prepManager.GetLevelOneActorsWithName(name);
        // You Have 2 Of The Same Unit.
        if (count == 2) return genome.GetByName("W_DUP_POTENTIAL");
        // You Have 1 Of The Same Unit.
        return genome.GetByName("W_DUP_NONE");
    }
}