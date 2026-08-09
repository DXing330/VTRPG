using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoChessAIPrepController : MonoBehaviour
{
    // Load In Genome To Make Choices.
    public AutoChessPVPGenome genome;
    public void DefaultGenome()
    {
        genome = new AutoChessPVPGenome();
        genome.ResetToDefault();
    }
    public AutoChessPrepManager prepManager;
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
    Dictionary<AutoActorRollUpData, float> keepCache = new();
    Dictionary<AutoActorRollUpData, float> buyCache = new();
    void ClearScoreCaches()
    {
        keepCache.Clear();
        buyCache.Clear();
    }
    public AutoChessShopManager shopManager;
    public StatDatabase unitRoles;
    public List<string> GetUnitRoles(string unitName)
    {
        return unitRoles.ReturnStats(unitName);
    }
    public StatDatabase unitRarity;
    public StatDatabase factionThresholds;
    public void AIPrepPhase(AutoChessDataManager dataManager)
    {
        prepManager.SetDataManager(dataManager);
        GetPitTiles();
        shopManager.shopData.GeneratePVPCurrentListing();
        shopManager.PVPRefreshData();
        BuildStackCache();
        ClearScoreCaches();
        EconomyAction(dataManager);
        AcquireLoop(dataManager);
        AutoPlaceFieldUnits(dataManager);
        AutoPlaceEquipment(dataManager);
        prepManager.SaveToDataManager();
        prepManager.StartBattle();
    }
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
        // Priority: Level up if urgent and affordable, else reroll if shop is weak and gold is high, else save
        if (!dataManager.MaxLevel() && gold >= 4 && levelScore > saveScore && levelScore > rerollScore)
        {
            prepManager.BuyExp();
            EconomyAction(dataManager);
        }
        else if (gold >= 1 && rerollScore > saveScore && rerollScore > levelScore)
        {
            prepManager.PVPRerollShop();
            EconomyAction(dataManager);
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
                prepManager.FastSellSelectedActor(prepManager.benchSlots[worstIndex]);
                ClearScoreCaches();
            }
            if (dataManager.GetGold() >= buyCost)
            {
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
        //score += genome.GetByName("W_BUY_POWER") * GetPower(unit);
        score += genome.GetByName("W_BUY_TIER") * tier;
        score += genome.GetByName("W_BUY_SYNERGY") * SynergyValue(unit.GetFactions());
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
    float KeepScore(AutoActorRollUpData unit)
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
        score += genome.GetByName("W_KEEP_SYNERGY") * SynergyValue(unit.GetFactions());
        score += genome.GetByName("W_KEEP_DUPLICATE") * DuplicatePotential(name);
        score += genome.GetByName("W_KEEP_STAR_LEVEL") * star;
        score += genome.GetByName("W_UNIT_STACK_GENERATION") * StackGainValue(unit, false);
        score += genome.GetByName("W_UNIT_STACK_SCALING") * StackScalingValue(unit);
        score += genome.GetByName("W_UNIT_CYCLE") * CycleValue(unit, false);
        score += genome.GetByName("W_UNIT_CYCLE_SCALING") * CycleScalingValue(unit);
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
            // TODO decrease score for bench units with equipment, want to sell them faster to reclaim equipment.
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
        // Don't Swap Infinitely, Pass Through Thrice At Most.
        if (totalSwaps > 2){return;}
        int benchIndex = -1;
        int fieldIndex = -1;
        float bestSwapValue = float.MinValue;
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
        if (bestSwapValue >= genome.GetByName("W_PLACE_SWAP_THRESH") && benchIndex >= 0 && fieldIndex >= 0)
        {
            int benchLocation = allBench[benchIndex].GetLocation();
            allBench[benchIndex].SetDirection(1);
            allBench[benchIndex].SetLocation(FindBestTileForRole(allBench[benchIndex]));
            allField[fieldIndex].SetLocation(benchLocation);
            prepManager.MoveFromBenchToField(allBench[benchIndex]);
            prepManager.MoveFromFieldToBench(allField[fieldIndex]);
            ClearScoreCaches();
            // Good Swap Means Try Again.
            CheckAllSwaps(totalSwaps + 1);
        }
    }
    int AegirPlaceFieldUnits(AutoActorRollUpData actor)
    {
        // Form a Chain Of Aegir Units, Ending With A Carry And Starting With Highest Attack NonAegir If Possible.
        // Find The Base Of The Chain And The End.
        // Make Sure The Directions Line Up.
        return -1;
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
        if (genome.GetPreferredFaction() == "Aegir" && actor.FactionExists("Aegir"))
        {
            return AegirPlaceFieldUnits(actor);
        }
        // Role Is Now A Comma Separated List Of Roles.
        string role = unitRoles.ReturnValue(actor.GetName());
        if (role.Contains("Tank"))
        {
            return FindOpenSpot(2, - 1);
        }
        return FindOpenSpot(0, 1);
    }
    // Check If You're Losing/Gaining Any Thresholds During A Swap
    float FindActiveFactionDifferences(AutoActorRollUpData benchUnit, AutoActorRollUpData fieldUnit)
    {
        float totalDifference = 0f;
        List<string> benchFactions = benchUnit.RAWGetFactions();
        List<string> fieldFactions = fieldUnit.RAWGetFactions();
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
                totalDifference -= SynergyLossIfRemoved(fieldFaction);
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
    float SynergyLossIfRemoved(string removedFaction)
    {
        float loss = 0;
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
                loss += genome.GetByName("W_SYN_HIT");
            }
        }
        return loss;
    }
    // ============================================================
    // 4. EQUIPMENT PLACEMENT
    // ============================================================
    public StatDatabase itemTypeData;
    public List<string> ReturnItemTypes(string itemName)
    {
        return itemTypeData.ReturnStats(itemName);
    }
    List<string> GetAvailableComponents()
    {
        List<string> components = new();
        List<string> equipment = prepManager.dataManager.GetEquipment();
        for(int i = 0; i < equipment.Count; i++)
        {
            if(IsComponent(equipment[i]))
            {
                components.Add(equipment[i]);
            }
        }
        return components;
    }
    List<string> GetAvailableCombinedItems(AutoChessDataManager dataManager)
    {
        List<string> combined = new();
        List<string> equipment = dataManager.GetEquipment();
        for(int i = 0; i < equipment.Count; i++)
        {
            if(!IsComponent(equipment[i]))
            {
                combined.Add(equipment[i]);
            }
        }
        return combined;
    }
    bool EquipItemToUnit(string itemName, bool exists = true)
    {
        // Find The Best Matching Unit For The Equipment.
        int bestIndex = -1;
        float bestScore = float.MinValue;
        for (int i = 0; i < prepManager.fieldSlots.Count; i++)
        {
            AutoActorRollUpData unit = prepManager.fieldSlots[i];
            if (unit.GetOpenEquipmentSlots() <= 0){continue;}
            float score = itemValueDatabase.GetTagCompatibility(ReturnItemTypes(itemName), GetUnitRoles(unit.GetName()));
            // Not only a match but check the value of the unit:
            score += genome.GetByName("W_ITEM_FOCUS_HIGH_TIER_UNIT") * GetUnitTier(unit.GetName());
            if (score > bestScore)
            {
                bestScore = score;
                bestIndex = i;
            }
        }
        if (bestIndex >= 0)
        {
            prepManager.fieldSlots[bestIndex].EquipEquipment(itemName);
            if (exists)
            {
                prepManager.dataManager.UseEquipment(itemName);
            }
            return true;
        }
        return false;
    }
    void AutoPlaceEquipment(AutoChessDataManager dataManager)
    {
        // Heuristic Approach.
        // 1. Find regular equipment.
        List<string> combined = GetAvailableCombinedItems(dataManager);
        // 2. Equip vs Save
        for (int i = 0; i < combined.Count; i++)
        {
            float itemValue = ItemValue(combined[i]);
            if (itemValue > genome.GetByName("W_ITEM_SAVE"))
            {
                EquipItemToUnit(combined[i]);
            }
        }
        // 1. Find possible combinations
        List<string> components = GetAvailableComponents();
        List<ItemCombination> combinations = new();
        for(int i = 0; i < components.Count; i++)
        {
            for(int j = i + 1; j < components.Count; j++)
            {
                string result = CombineEquipment(components[i], components[j]);
                if(!string.IsNullOrEmpty(result))
                {
                    float combinedValue = ItemValue(result);
                    combinedValue -= genome.GetByName("W_ITEM_COMPONENT_SAVE") * GetComponentFutureValue(components[i]);
                    combinedValue -= genome.GetByName("W_ITEM_COMPONENT_SAVE") * GetComponentFutureValue(components[j]);
                    ItemCombination newCombination = new(result, components[i] + "|" + components[j], combinedValue);
                    combinations.Add(newCombination);
                }
            }
        }
        combinations.Sort((a, b) => a.value.CompareTo(b.value));
        for (int i = combinations.Count - 1; i >= 0; i--)
        {
            // Check If The Components Still Exist.
            if (!prepManager.dataManager.EquipmentComponentsExists(combinations[i].components)){continue;}
            float combinedValue = combinations[i].value;
            // Check If Equip.
            if (combinedValue > genome.GetByName("W_ITEM_SAVE"))
            {
                if (EquipItemToUnit(combinations[i].combinationName, false))
                {
                    // Remove The Components If Equipped.
                    prepManager.dataManager.RemoveComponents(combinations[i].components);
                }
            }
        }
    }
    // If lots of items of that type (dps/tank/supp) then value goes down.
    float ItemTypeDuplicatePenalty(List<string> itemTypes)
    {
        float itemTypeCount = 0f;
        for (int i = 0; i < prepManager.fieldSlots.Count; i++)
        {
            foreach (string itemName in prepManager.fieldSlots[i].GetEquipmentNames())
            {
                foreach (string itemType in itemTypes)
                {
                    if (ReturnItemTypes(itemName).Contains(itemType))
                    {
                        itemTypeCount++;
                        break;
                    }
                }
            }
        }
        return itemTypeCount;
    }
    public AutoChessItemValueDatabase itemValueDatabase;
    float ItemTrainingScore(string itemName)
    {
        return genome.GetByName("W_ITEM_VALUE") * itemValueDatabase.GetItemTrainingScore(itemName);
    }
    float ItemBestMatch(string itemName)
    {
        float bestMatch = 0f;
        for (int i = 0; i < prepManager.fieldSlots.Count; i++)
        {
            if (prepManager.fieldSlots[i].GetOpenEquipmentSlots() <= 0){continue;}
            float match = itemValueDatabase.GetTagCompatibility(ReturnItemTypes(itemName), GetUnitRoles(prepManager.fieldSlots[i].GetName()));
            if (match > bestMatch)
            {
                bestMatch = match;
            }
        }
        return bestMatch;
    }
    string ItemType(string itemStats)
    {
        if (itemStats.Contains("HP")){return "Tank";}
        return "DPS";
    }
    // Determines The Current Value Of An Item.
    float ItemValue(string itemName)
    {
        float value = 0f;
        // Value is based on history + need + best match.
        value += ItemTrainingScore(itemName);
        value -= genome.GetByName("W_ITEM_DUPLICATE_PENALTY") * ItemTypeDuplicatePenalty(ReturnItemTypes(itemName));
        value += genome.GetByName("W_ITEM_UNIT_MATCH") *
         ItemBestMatch(itemName);
        return value;
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
    float GetPower(AutoActorRollUpData unit)
    {
        return (unit.GetLevel() * 10f) + (unit.GetHealth() / 10) + (unit.GetAttack() / 5) + (unit.GetDefense() / 3) + (unit.GetResist() / 10);
    }
    int[] GetThresholds(string factionName)
    {
        string val = factionThresholds.ReturnValue(factionName); // e.g. "2|3" or "3|6"
        if (string.IsNullOrEmpty(val)) return new int[] { 2 }; // fallback
        string[] parts = val.Split('|');
        return System.Array.ConvertAll(parts, int.Parse);
    }
    float SynergyValue(string faction)
    {
        if (faction == "Harmony")
        {
            return SynergyValue(mainFactions);
        }
        if (string.IsNullOrEmpty(faction)){return 0f;}
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
            return genome.GetByName("W_SYN_HIT");
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
    float SynergyValue(List<string> factions)
    {
        if (factions == null || factions.Count == 0){return 0f;}
        if (factions.Contains("Harmony"))
        {
            return SynergyValue(mainFactions);
        }
        float totalSynergy = 0f;
        for (int i = 0; i < factions.Count; i++)
        {
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
        switch (trait.timing)
        {
            case "OnPurchase":
                return isBuying ? 1f : 0f;
            case "OnSold":
                return 1f;
            default:
                return 0f;
        }
    }
    float CycleScalingValue(AutoActorRollUpData unit)
    {
        AutoChessTrait trait = unit.GetTrait();
        if (trait == null) return 0f;
        string s = trait.specifics;
        if (s.Contains("UnitsBoughtMultiBy"))
            return 1f;
        if (s.Contains("GoldSpentMultiBy"))
            return 1f;
        if (s.Contains("BenchSizeMultiBy1"))
            return 1f;
        return 0f;
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
    // DB Copied From Equip Manager For Convenience
    public string CombineEquipment(string firstItem, string secondItem)
    {
        return itemValueDatabase.CombineEquipment(firstItem, secondItem);
    }
    float GetComponentFutureValue(string component)
    {
        return itemValueDatabase.GetComponentValue(component);
    }
    bool IsComponent(string itemName)
    {
        return itemOrder.Contains(itemName);
    }
    protected List<string> itemOrder = new()
    {
        "Aegirian Blade",
        "Protection Drone",
        "Goliath Helmet",
        "Quick Combat Rations",
        "Laser Sight",
        "Tough Launcher",
        "Minature Accelerator",
        "Raid Grenade",
        "Damazti Isomorph",
        "Kjeragi Nevermeltice",
        "Lateran Clip",
        "Sargonian Teaspresso",
        "Victorian Hammer",
        "Yanese Dagger"
    };
}