using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoChessAIPrepController : MonoBehaviour
{
    // Load In Genome To Make Choices.
    public AutoChessPVPGenome genome;
    public AutoChessPrepManager prepManager;
    public AutoChessFactionManager factionManager;
    public AutoChessShopManager shopManager;
    public StatDatabase unitRoles;
    public StatDatabase unitRarity;
    public StatDatabase itemRoles;
    public StatDatabase factionThresholds;
    public void AIPrepPhase(AutoChessDataManager dataManager)
    {
        prepManager.SetDataManager(dataManager);
        GetPitTiles();
        shopManager.shopData.GeneratePVPCurrentListing();
        shopManager.PVPRefreshData();
        EconomyAction(dataManager);
        AcquireLoop(dataManager);
        AutoPlaceFieldUnits(dataManager);
        // Apply Trait Effects.
        prepManager.StartBattle();
        prepManager.SaveToDataManager();
    }
    // --- PLACEMENT ---
    const float SWAP_THRESHOLD = 2.5f;
    const float IDEAL_TANK_RATIO = 0.3f;
    const float IDEAL_DPS_RATIO = 0.5f;
    const float IDEAL_SUPPORT_RATIO = 0.2f;
    // ============================================================
    // 1. ECONOMY
    // ============================================================
    void EconomyAction(AutoChessDataManager dataManager)
    {
        int gold = dataManager.GetGold();
        int level = dataManager.GetLevel();
        int hp = dataManager.GetHealth();
        int winStreak = dataManager.GetWinStreak();
        int lossStreak = dataManager.GetLossStreak();
        float levelScore = genome.GetByName("W_ECON_LEVEL_TIMING") * LevelUrgency(dataManager);
        float saveScore = genome.GetByName("W_ECON_INTEREST") * (Mathf.Min(gold / 10, 5));
        float hpScore = genome.GetByName("W_ECON_HP_URGENCY") * (100 - hp);
        float rerollScore = genome.GetByName("W_ECON_REROLL") * ShopQualityScore();
        float winStreakScore = genome.GetByName("W_ECON_STREAK_WIN") * (Mathf.Min(winStreak, 5));
        float lossStreakScore = genome.GetByName("W_ECON_STREAK_LOSS") * (Mathf.Min(lossStreak, 5));
        // Priority: Level up if urgent and affordable, else reroll if shop is weak and gold is high, else save
        if (!dataManager.MaxLevel() && gold >= 4 && levelScore > saveScore && levelScore > rerollScore)
        {
            prepManager.BuyExp();
        }
        else if (gold >= 1 && rerollScore > saveScore && rerollScore > levelScore)
        {
            prepManager.RerollShop();
        }
        // Else Save Money -> Do Nothing.
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
    // ============================================================
    // 2. ACQUIRE (Shop + Bench merged)
    // ============================================================
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
                if (dataManager.GetGold() < 20)
                {
                    score += genome.GetByName("W_BUY_ECON_STRETCH") * ((float)cost / Mathf.Max(1, dataManager.GetGold()));
                }
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
                if (bestScore <= worstKeep + 0.5f) break; // margin threshold
                prepManager.selectedActorLocation = 0;
                prepManager.selectedActorIndex = worstIndex;
                prepManager.SellSelectedActor();
            }
            if (dataManager.GetGold() >= buyCost)
            {
                prepManager.PVPBuySelectedActor(buyCost);
                actionTaken = true;
            }
        }
    }
    float BuyScore(AutoActorRollUpData unit)
    {
        if (unit == null){return -999f;}
        string name = unit.GetName();
        int tier = GetUnitTier(name);
        int cost = GetUnitCost(name);
        int role = GetUnitRole(name);
        float score = 0;
        score += genome.GetByName("W_BUY_POWER") * GetPower(unit);
        score += genome.GetByName("W_BUY_TIER") * tier;
        score += genome.GetByName("W_BUY_COST_EFF") * ((float)tier / Mathf.Max(1, cost));
        score += genome.GetByName("W_BUY_SYNERGY") * SynergyValue(unit.GetFactions());
        score += genome.GetByName("W_BUY_DUPLICATE") * DuplicateValue(name);
        score += genome.GetByName("W_BUY_ROLE_FIT") * RoleNeedScore(role);
        return score;
    }
    float KeepScore(AutoActorRollUpData unit)
    {
        if (unit == null){return -999f;}
        string name = unit.GetName();
        int tier = GetUnitTier(name);
        int role = GetUnitRole(name);
        int star = unit.GetLevel();
        float score = 0;
        score += genome.GetByName("W_KEEP_TIER") * tier;
        score += genome.GetByName("W_KEEP_SYNERGY") * SynergyValue(unit.GetFactions());
        score += genome.GetByName("W_KEEP_DUPLICATE") * DuplicatePotential(name);
        score += genome.GetByName("W_KEEP_STAR_LEVEL") * star * 5;
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
        float bestScore = -999f;
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
    // ============================================================
    // 3. BOARD PLACEMENT
    // ============================================================
    int GetWorstFieldIndex()
    {
        if (prepManager.fieldSlots.Count <= 0){return -1;}
        float worstScore = 999f;
        int index = -1;
        for (int i = 0; i < prepManager.fieldSlots.Count; i++)
        {
            float fieldScore = KeepScore(prepManager.fieldSlots[i]);
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
        // Don't Swap Infinitely With A Bad Genome.
        if (totalSwaps > allBench.Count + allField.Count){return;}
        int benchIndex = -1;
        int fieldIndex = -1;
        float bestSwapValue = 0f;
        for (int i = 0; i < allBench.Count; i++)
        {
            for (int j = 0; j < allField.Count; j++)
            {
                float swapValue = KeepScore(allBench[i]) - KeepScore(allField[j]) + FindActiveFactionDifferences(allBench[i], allField[j]);
                if (swapValue > bestSwapValue)
                {
                    benchIndex = i;
                    fieldIndex = j;
                    bestSwapValue = swapValue;
                }
            }
        }
        if (bestSwapValue >= SWAP_THRESHOLD)
        {
            int benchLocation = allBench[benchIndex].GetLocation();
            allBench[benchIndex].SetLocation(FindBestTileForRole(allBench[benchIndex]));
            allBench[benchIndex].SetDirection(1);
            allField[fieldIndex].SetLocation(benchLocation);
            prepManager.MoveFromBenchToField(allBench[benchIndex]);
            prepManager.MoveFromFieldToBench(allField[fieldIndex]);
            // Good Swap Means Try Again.
            CheckAllSwaps(totalSwaps + 1);
        }
    }
    void AutoPlaceFieldUnits(AutoChessDataManager dataManager)
    {
        int bestBenchIndex = GetBestBenchIndex();
        // Place The Best Units If There Is Space.
        if (prepManager.fieldSlots.Count < prepManager.GetMaxFieldSlots())
        {
            if (bestBenchIndex < 0)
            {
                // Go Buy Something, Anything Is Better Than Nothing.
                AcquireLoop(dataManager);
                return;
            }
            AutoActorRollUpData bestBenchActor = prepManager.benchSlots[bestBenchIndex];
            bestBenchActor.SetLocation(FindBestTileForRole(bestBenchActor));
            bestBenchActor.SetDirection(1);
            prepManager.MoveFromBenchToField(bestBenchActor);
            // Loop Until Full.
            AutoPlaceFieldUnits(dataManager);
            return;
        }
        // Determine The Weakest Field Unit And The Best Bench And See If They Should Be Swapped.
        else
        {
            CheckAllSwaps(0);
        }
        // Sort The Field Into Whatever Turn Order You Want.
        prepManager.fieldSlots.Sort((a, b) =>
        {
            int colA = prepManager.mapUtility.GetColumn(a.GetLocation(), 9);
            int colB = prepManager.mapUtility.GetColumn(b.GetLocation(), 9);
            return colB.CompareTo(colA);
        });
        prepManager.SaveToDataManager();
    }
    protected List<int> pitTiles = new List<int>();
    void GetPitTiles()
    {
        pitTiles.Clear();
        for (int i = 0; i < prepManager.dataManager.mapTiles.Count; i++)
        {
            if (prepManager.dataManager.mapTiles[i] == "Pit")
            {
                pitTiles.Add(i);
            }
        }
    }
    int FindOpenSpot(int startColumn = 2, int direction = -1)
    {
        List<int> spots = prepManager.mapUtility.GetTilesInColumn(startColumn, 9);
        // Remove Any Pits/Occupied Spaces.
        List<int> occupiedSpots = prepManager.GetTakenSpots();
        for (int i = spots.Count - 1; i >= 0; i--)
        {
            if (pitTiles.Contains(spots[i]) || occupiedSpots.Contains(spots[i]))
            {
                spots.RemoveAt(i);
            }
        }
        // Get The First Open Middle-Most Tile.
        int row = 4;
        for (int i = 0; i < 9; i++)
        {
            int tile = prepManager.mapUtility.ReturnTileNumberFromRowCol(row, startColumn, 9);
            if (spots.Contains(tile)){return tile;}
            if (i % 2 == 0)
            {
                row += ((i + 1));
            }
            else
            {
                row -= ((i + 1));
            }
        }
        // Move To Next Column.
        return FindOpenSpot(startColumn + direction, direction);
    }
    int FindBestTileForRole(AutoActorRollUpData actor)
    {
        // TODO Back (Column 0-1) DPS/Support, Front (Column 1-2) Tanks
        string role = unitRoles.ReturnValue(actor.GetName());
        switch (role)
        {
            default:
            return FindOpenSpot(0, 1);
            case "Tank":
            return FindOpenSpot(2, -1);
            case "Support":
            return FindOpenSpot(0, 1);
        }
    }
    // Check If You're Losing/Gaining Any Thresholds During A Swap
    float FindActiveFactionDifferences(AutoActorRollUpData benchUnit, AutoActorRollUpData fieldUnit)
    {
        float totalDifference = 0f;
        List<string> benchFactions = new List<string>(benchUnit.GetFactions());
        List<string> fieldFactions = new List<string>(fieldUnit.GetFactions());
        List<string> overlappingFactions = new List<string>();
        for (int i = 0; i < benchFactions.Count; i++)
        {
            if (fieldFactions.Contains(benchFactions[i]))
            {
                overlappingFactions.Add(benchFactions[i]);
            }
        }
        for (int i = 0; i < overlappingFactions.Count; i++)
        {
            benchFactions.Remove(overlappingFactions[i]);
            fieldFactions.Remove(overlappingFactions[i]);
        }
        // Check Any Losses And Gains.
        totalDifference -= SynergyLossIfRemoved(fieldFactions);
        totalDifference += SynergyValue(benchFactions);
        return totalDifference;
    }
    float SynergyLossIfRemoved(List<string> removedFactions)
    {
        float loss = 0;
        for (int i = 0; i < removedFactions.Count; i++)
        {
            int currentCount = factionManager.factionData.GetCountOfFaction(removedFactions[i]);
            int[] thresholds = GetThresholds(removedFactions[i]);
            foreach (var t in thresholds)
            {
                if (currentCount >= t && (currentCount - 1) < t)
                {
                    loss += 3.0f; // Would break this threshold
                }
            }
        }
        return loss;
    }
    // ============================================================
    // HELPERS — Wire these to your database
    // ============================================================
    int GetUnitTier(string name)
    {
        return int.Parse(unitRarity.ReturnValue(name));
    }
    int GetUnitCost(string name)
    {
        return shopManager.ReturnActorCost(name);
    }
    int GetUnitRole(string name)
    {
        string role = unitRoles.ReturnValue(name);
        switch (role)
        {
            default:
            return 1; // Default DPS
            case "Tank":
            return 0;
            case "Support":
            return 2;
        }
    }
    float GetPower(AutoActorRollUpData unit)
    {
        return (unit.GetLevel() * 10f) + (unit.GetHealth() / 10) + (unit.GetAttack() / 5) + (unit.GetDefense() / 3);
    }
    int[] GetThresholds(string factionName)
    {
        string val = factionThresholds.ReturnValue(factionName); // e.g. "2|3" or "3|6"
        if (string.IsNullOrEmpty(val)) return new int[] { 2 }; // fallback
        string[] parts = val.Split('|');
        return System.Array.ConvertAll(parts, int.Parse);
    }
    float SynergyValue(List<string> factions)
    {
        if (factions == null || factions.Count == 0){return 0f;}
        float totalSynergy = 0f;
        for (int j = 0; j < factions.Count; j++)
        {
            string targetFaction = factions[j];
            if (string.IsNullOrEmpty(targetFaction)){continue;}
            int currentCount = factionManager.factionData.GetCountOfFaction(targetFaction);
            int newCount = currentCount + 1;
            int[] thresholds = GetThresholds(targetFaction);
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
                totalSynergy += 3.0f;
                continue;
            }
            if (maintainsActive)
            {
                totalSynergy += 1.5f;
                continue;
            }
            if (oneAway)
            {
                totalSynergy += 0.5f;
                continue;
            }
        }
        return totalSynergy;
    }
    float DuplicateValue(string name)
    {
        int count = prepManager.GetLevelOneActorsWithName(name);
        if (count >= 2) return 8f;  // Buying this triggers a merge to level 2
        if (count == 1) return 2f;  // One step closer to merge
        return 0f;
    }
    float DuplicatePotential(string name)
    {
        int count = prepManager.GetLevelOneActorsWithName(name);
        if (count == 1) return 5f;   // Could become level 2
        if (count >= 2) return 0f;   // Already enough copies on bench/field
        return -3f;                   // Already have level 2 or no copies
    }
    float RoleNeedScore(int role)
    {
        int totalUnits = prepManager.fieldSlots.Count;
        if (totalUnits == 0) return 1f;
        int roleCount = 0;
        foreach (var u in prepManager.fieldSlots) if (GetUnitRole(u.GetName()) == role) roleCount++;
        float idealRatio = role switch
        {
            0 => IDEAL_TANK_RATIO,
            1 => IDEAL_DPS_RATIO,
            _ => IDEAL_SUPPORT_RATIO
        };
        int idealCount = Mathf.RoundToInt(totalUnits * idealRatio);
        return Mathf.Max(0, idealCount - roleCount);
    }
}