using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class HierarchicalMask
{
    public bool[] actionTypeMask; // 7
    public bool[] buyMask;        // 6
    public bool[] sellBenchMask;  // 12
    public bool[] placeBenchMask; // 12
    public bool[] fieldMask;      // 11
}
// In charge of converting game state to a fixed length float[] of [0-1] values
[CreateAssetMenu(fileName = "AutoChessStateEncoder", menuName = "ScriptableObjects/AutoChess/AutoChessStateEncoder", order = 1)]
public class AutoChessStateEncoder : ScriptableObject
{
    public const int ACTION_DONE = 0;
    public const int ACTION_LEVEL_UP = 1;
    public const int ACTION_REROLL = 2;
    public const int ACTION_BUY = 3;
    public const int ACTION_SELL_BENCH = 4;
    public const int ACTION_PLACE_BENCH = 5;
    public const int ACTION_RETURN_FIELD = 6;
    public const int ACTION_TYPE_COUNT = 7;
    public void DecodeAction(int flatAction, out int actionType, out int actionIndex)
    {
        actionType = ACTION_DONE;
        actionIndex = -1;
        if (flatAction == 0)
        {
            actionType = ACTION_DONE;
        }
        else if (flatAction == 1)
        {
            actionType = ACTION_LEVEL_UP;
        }
        else if (flatAction == 2)
        {
            actionType = ACTION_REROLL;
        }
        else if (flatAction >= 3 && flatAction <= 8)
        {
            actionType = ACTION_BUY;
            actionIndex = flatAction - 3;
        }
        else if (flatAction >= 9 && flatAction <= 20)
        {
            actionType = ACTION_SELL_BENCH;
            actionIndex = flatAction - 9;
        }
        else if (flatAction >= 21 && flatAction <= 32)
        {
            actionType = ACTION_PLACE_BENCH;
            actionIndex = flatAction - 21;
        }
        else if (flatAction >= 33 && flatAction <= 43)
        {
            actionType = ACTION_RETURN_FIELD;
            actionIndex = flatAction - 33;
        }
        else
        {
            Debug.LogError($"Invalid flat action: {flatAction}");
        }
    }
    [Header("Config")]
    [SerializeField] private int maxBench = 12;
    [SerializeField] private int maxField = 11; // 9 base + 2 from HR File
    [SerializeField] private int maxShop = 6;
    [SerializeField] private int maxFactionsPerUnit = 2;
    // Fixed faction order - MUST match Python
    private readonly List<string> factionOrder = new List<string>
    {
        // MAIN.
        "Aegir",
        "Kjerag",
        "Laterano",
        "Sargon",
        "Siracusa",
        "Victoria",
        "Yan",
        "Harmony",
        // ECON.
        "Foresight",
        "Investor",
        "Marvel",
        // SUB.
        "Agile",
        "Aid",
        "Assist",
        "Durable",
        "Precision",
        "Raid",
        "Resilient",
        "Swift"
    };
    // Fixed Trait Timings
    private readonly List<string> traitTimings = new List<string>
    {
        "DuringBattle",
        "None",
        "OnAttack",
        "OnDeath",
        "OnPurchase",
        "OnSkill",
        "OnSold",
        "StartBattle"
    };
    // Fixed Trait Effects
    private readonly List<string> traitEffects = new List<string>
    {
        "AegirUnit",
        "CopyBack",
        "CopyFront",
        "Equipment",
        "FrontActive",
        "Gold",
        "HighestActive",
        "HighestActiveUnit",
        "NextRoundGold",
        "None",
        "Reroll",
        "Self",
        "SelfActive",
        "SelfAndBackActive",
        "SelfAndFrontActive",
        "SelfAndFrontLineActive",
        "Unit"
    };
    // Fixed Trait Specifics (Might Be Too Much, Leave It Out For Now)
    public int StateSize => CalculateStateSize();
    // Feature counts per slot type
    private const int SCALAR_COUNT = 13;
    private const int BENCH_FEATURES = 52;
    private const int FIELD_FEATURES = 54;
    private const int SHOP_FEATURES = 52;
    private const int FACTION_FEATURES = 4;
    private int CalculateStateSize()
    {
        return SCALAR_COUNT + (factionOrder.Count * FACTION_FEATURES) + (maxBench * BENCH_FEATURES) + (maxField * FIELD_FEATURES) + (maxShop * SHOP_FEATURES);
    }
    private float Norm(float val, float max)
    {
        if (max <= 0f) return 0f;
        return Mathf.Clamp01(val / max);
    }
    public bool[] BuildValidMask(AutoChessPrepManager prep, AutoChessDataManager data, 
    AutoChessShopManager shop, AutoChessFactionManager factions)
    {
        bool[] valid = new bool[44];
        int gold = data.GetGold();
        bool maxLevel = data.MaxLevel();
        int benchCount = prep.benchSlots.Count;
        int fieldCount = prep.fieldSlots.Count;
        int maxField = prep.GetMaxFieldSlots();
        bool benchFull = benchCount >= prep.GetMaxBenchSlots();
        bool fieldFull = fieldCount >= maxField;
        // 0: Done
        valid[0] = true;
        // 1: Level Up
        valid[1] = !maxLevel && gold >= 4;
        // 2: Reroll
        valid[2] = data.GetFreeRerolls() > 0 || gold >= 1;
        // 3-8: Buy Shop Slot 0-5
        for (int i = 0; i < 6; i++)
        {
            bool slotExists = shop.shopActors.Count > i && shop.shopActors[i] != null;
            int cost = slotExists ? shop.ReturnActorCost(shop.shopActors[i].GetName()) : int.MaxValue;
            if (factions.FactionActive("Foresight") && factions.GetStacksOfFaction("Foresight") >= 80)
            {
                cost--;
            }
            valid[3 + i] = slotExists && gold >= cost;
        }
        // 9-20: Sell Bench Unit 0-11
        for (int i = 0; i < 12; i++)
            valid[9 + i] = benchCount > i;
        // 21-32: Place Bench Unit 0-11
        for (int i = 0; i < 12; i++)
            valid[21 + i] = benchCount > i && !fieldFull;
        // 33-43: Return Field Unit 0-10
        for (int i = 0; i < 11; i++)
        {
            bool canReturnNormally = !benchFull;
            bool canReturnForSwap = benchFull && fieldFull;
            valid[33 + i] = fieldCount > i && (canReturnNormally || canReturnForSwap);
        }
        return valid;
    }
    public HierarchicalMask BuildHierarchicalMask(bool[] flatMask)
    {
        var result = new HierarchicalMask
        {
            actionTypeMask = new bool[ACTION_TYPE_COUNT],
            buyMask = new bool[6],
            sellBenchMask = new bool[12],
            placeBenchMask = new bool[12],
            fieldMask = new bool[11]
        };
        // ----------------------------------------
        // ROOT ACTION MASK
        // ----------------------------------------
        // Done
        result.actionTypeMask[ACTION_DONE] = flatMask[0];
        // LevelUp
        result.actionTypeMask[ACTION_LEVEL_UP] = flatMask[1];
        // Reroll
        result.actionTypeMask[ACTION_REROLL] = flatMask[2];
        // ----------------------------------------
        // BUY
        // ----------------------------------------
        bool anyBuy = false;
        for (int i = 0; i < 6; i++)
        {
            bool valid = flatMask[3 + i];
            result.buyMask[i] = valid;
            if (valid)
                anyBuy = true;
        }
        result.actionTypeMask[ACTION_BUY] = anyBuy;
        // ----------------------------------------
        // BENCH ACTIONS
        // A slot is relevant if either selling or
        // placing that bench slot is possible.
        // ----------------------------------------
        bool anySell = false;
        bool anyPlace = false;
        for (int i = 0; i < 12; i++)
        {
            result.sellBenchMask[i] =
                flatMask[9 + i];
            result.placeBenchMask[i] =
                flatMask[21 + i];
            if (result.sellBenchMask[i])
                anySell = true;
            if (result.placeBenchMask[i])
                anyPlace = true;
        }
        result.actionTypeMask[ACTION_SELL_BENCH] =
            anySell;
        result.actionTypeMask[ACTION_PLACE_BENCH] =
            anyPlace;
        // ----------------------------------------
        // FIELD ACTIONS
        // ----------------------------------------
        bool anyReturnField = false;
        for (int i = 0; i < 11; i++)
        {
            bool valid = flatMask[33 + i];
            result.fieldMask[i] = valid;
            if (valid)
                anyReturnField = true;
        }
        result.actionTypeMask[ACTION_RETURN_FIELD] =
            anyReturnField;
        return result;
    }
    // ============================================
    // MAIN ENTRY
    // ============================================
    public float[] Encode(AutoChessPrepManager prep, AutoChessDataManager data, AutoChessShopManager shop, AutoChessFactionManager factions)
    {
        var state = new List<float>(StateSize);
        EncodeScalars(state, prep, data, shop, factions);
        EncodeFactions(state, factions);
        EncodeBench(state, prep, factions);
        EncodeField(state, prep, factions);
        EncodeShop(state, shop, data, factions, prep);
        if (state.Count != StateSize)
        {
            Debug.LogError($"NeuralStateEncoder size mismatch: {state.Count} vs {StateSize}");
        }
        return state.ToArray();
    }
    private void EncodeScalars(List<float> s, AutoChessPrepManager prep, AutoChessDataManager data, AutoChessShopManager shop, AutoChessFactionManager factions)
    {
        // Economy
        s.Add(Norm(data.GetGold(), 100f));
        s.Add(Norm(data.GetLevel(), 10f));
        s.Add(Norm(data.GetHealth(), 100f));
        s.Add(Norm(data.GetRound(), 50f));
        s.Add(Norm(data.GetWinStreak(), 5f));
        s.Add(Norm(data.GetLossStreak(), 5f));
        s.Add(Norm(Mathf.Min(data.GetGold() / 10, 5), 5f));
        s.Add(Norm(Mathf.Min(data.GetFreeRerolls(), 5), 5f));
        // Bench/field capacity
        s.Add(Norm(prep.GetMaxFieldSlots(), 11f)); // Max Field
        s.Add(Norm(prep.fieldSlots.Count, 11f)); // Current Field
        s.Add(Norm(prep.benchSlots.Count, 12f)); // Current Bench
        s.Add(Norm(prep.GetMaxBenchSlots() - prep.benchSlots.Count, 12f));  // bench space remaining
        // Econ faction indicators
        bool investorActive = factions.factionData.FactionActive("Investor");
        s.Add(investorActive ? 1f : 0f);
    }
    // ============================================
    // FACTIONS (4 per faction)
    // ============================================
    private void EncodeFactions(List<float> s, AutoChessFactionManager factions)
    {
        foreach (string faction in factionOrder)
        {
            int count = factions.factionData.GetCountOfFaction(faction);
            bool active = factions.factionData.FactionActive(faction);
            float stacks = 0f;
            string stackStr = factions.factionData.GetStacksOfFaction(faction);
            if (!string.IsNullOrEmpty(stackStr))
            {
                float.TryParse(stackStr, out stacks);
            }
            bool oneAway = IsOneAwayFromThreshold(factions, faction);
            s.Add(Norm(count, 10f));
            s.Add(active ? 1f : 0f);
            s.Add(Norm(stacks, 100f));
            s.Add(oneAway ? 1f : 0f);
        }
    }
    // ============================================
    // BENCH UNITS (12 floats each, 12 slots)
    // Focus: econ value, merge potential, trait value
    // ============================================
    private void EncodeBench(List<float> s, AutoChessPrepManager prep, AutoChessFactionManager factions)
    {
        var bench = prep.benchSlots.Select(u => (unit: u, score: GetUnitTier(u.GetName()))).OrderByDescending(x => x.score).ToList();
        for (int i = 0; i < prep.GetMaxBenchSlots(); i++)
        {
            if (i < bench.Count)
                EncodeBenchUnit(s, bench[i].unit, prep, factions);
            else
                EncodeEmptyBenchUnit(s);
        }
    }
    private void EncodeBenchUnit(List<float> s, AutoActorRollUpData unit, AutoChessPrepManager prep, AutoChessFactionManager factions)
    {
        string name = unit.GetName();
        var unitFactions = unit.GetFactions();
        s.Add(1f); // present
        s.Add(Norm(GetUnitTier(name), 5f)); // Tier
        s.Add(Norm(unit.GetLevel(), 3f)); // Level
        // Merge potential: how close to 3 copies?
        int copies = prep.GetLevelOneActorsWithName(name);
        s.Add(Norm(copies, 2f));  // 0-2 copies
        // Bench trait active?
        bool benchTraitActive = (prep.traitManager != null &&  prep.traitManager.BenchTraitActive(prep, unit));
        s.Add(benchTraitActive ? 1f : 0f);
        // Has equipment (can be moved to field unit)
        // Moving To Field Unit Requires Selling This Bench Unit Though.
        s.Add(unit.GetEquipmentNames().Count > 0 ? 1f : 0f);
        EncodeUnitFactions(s, unit.GetFactions(), factions); // 21
        EncodeUnitTrait(s, unit); // 25 floats
    }
    private void EncodeEmptyBenchUnit(List<float> s)
    {
        for (int i = 0; i < BENCH_FEATURES; i++) s.Add(0f);
    }
    // ============================================
    // FIELD UNITS (16 floats each, 11 slots)
    // Focus: combat power, positioning, active synergies
    // ============================================
    private void EncodeField(List<float> s, AutoChessPrepManager prep, AutoChessFactionManager factions)
    {
        var field = prep.fieldSlots.Select(u => (unit: u, power: GetUnitTier(u.GetName()))).OrderByDescending(x => x.power).ToList();
        for (int i = 0; i < maxField; i++)
        {
            if (i < field.Count)
            {
                EncodeFieldUnit(s, field[i].unit, prep, factions);
            }
            else
            {
                EncodeEmptyFieldUnit(s);
            }
        }
    }
    private void EncodeFieldUnit(List<float> s, AutoActorRollUpData unit, AutoChessPrepManager prep, AutoChessFactionManager factions)
    {
        string name = unit.GetName();
        var unitFactions = unit.GetFactions();
        s.Add(1f); // present
        s.Add(Norm(GetUnitTier(name), 5f));
        s.Add(Norm(unit.GetLevel(), 3f));
        // Combat stats
        s.Add(Norm(unit.GetHealth(), 200f));
        s.Add(Norm(unit.GetAttack(), 100f));
        s.Add(Norm(unit.GetDefense(), 100f));
        s.Add(Norm(unit.GetResist(), 100f));
        // Equipment
        s.Add(unit.GetEquipmentNames().Count > 0 ? 1f : 0f);
        // Faction + Trait
        EncodeUnitFactions(s, unit.GetFactions(), factions); // 21
        EncodeUnitTrait(s, unit); // 25
    }
    private void EncodeEmptyFieldUnit(List<float> s)
    {
        for (int i = 0; i < FIELD_FEATURES; i++) s.Add(0f);
    }
    // ============================================
    // SHOP (10 floats each, 6 slots) - unchanged
    // ============================================
    private void EncodeShop(List<float> s, AutoChessShopManager shop, AutoChessDataManager data, AutoChessFactionManager factions, AutoChessPrepManager prep)
    {
        for (int i = 0; i < maxShop; i++)
        {
            if (i < shop.shopActors.Count && shop.shopActors[i] != null)
            {
                EncodeShopUnit(s, shop.shopActors[i], data.GetGold(), factions, prep, shop);
            }
            else
            {
                EncodeEmptyShopUnit(s);
            }
        }
    }
    private void EncodeShopUnit(List<float> s, AutoActorRollUpData unit, int gold, AutoChessFactionManager factions, AutoChessPrepManager prep, AutoChessShopManager shop)
    {
        string name = unit.GetName();
        int cost = shop.ReturnActorCost(name);
        var unitFactions = unit.GetFactions();
        s.Add(1f);
        s.Add(Norm(GetUnitTier(name), 5f));
        s.Add(Norm(cost, 5f));
        s.Add(gold >= cost ? 1f : 0f);
        bool completesMerge = prep.GetLevelOneActorsWithName(name) >= 2;
        s.Add(completesMerge ? 1f : 0f);
        // Duplicate Check.
        bool unitExists = prep.UnitExists(unit);
        s.Add(unitExists ? 1f : 0f);
        // Total 6 Shop Checks.
        EncodeUnitFactions(s, unit.GetFactions(), factions); // 21
        EncodeUnitTrait(s, unit); // 25
    }
    private void EncodeEmptyShopUnit(List<float> s)
    {
        for (int i = 0; i < SHOP_FEATURES; i++) s.Add(0f);
    }
    // ============================================
    // UTILITIES
    // ============================================
    private void EncodeUnitFactions(List<float> s, List<string> unitFactions, AutoChessFactionManager factions)
    {
        float[] oneHot = new float[factionOrder.Count];
        float[] activeFlags = new float[maxFactionsPerUnit];
        for (int i = 0; i < maxFactionsPerUnit; i++)
        {
            if (i < unitFactions.Count)
            {
                string f = unitFactions[i];
                int idx = factionOrder.IndexOf(f);
                if (idx >= 0) oneHot[idx] = 1f;
                activeFlags[i] = factions.factionData.FactionActive(f) ? 1f : 0f;
            }
            else
            {
                activeFlags[i] = 0f;
            }
        }
        s.AddRange(oneHot); // 19
        s.AddRange(activeFlags); // 2
    }
    private void EncodeUnitTrait(List<float> s, AutoActorRollUpData unit)
    {
        float[] timingOneHot = new float[traitTimings.Count];
        float[] effectOneHot = new float[traitEffects.Count];
        var trait = unit.GetTrait();
        if (trait != null)
        {
            int tIdx = traitTimings.IndexOf(trait.timing);
            if (tIdx >= 0) timingOneHot[tIdx] = 1f;
            int eIdx = traitEffects.IndexOf(trait.effect);
            if (eIdx >= 0) effectOneHot[eIdx] = 1f;
        }
        s.AddRange(timingOneHot); // 8
        s.AddRange(effectOneHot); // 17
    }
    private bool IsOneAwayFromThreshold(AutoChessFactionManager factions, string faction)
    {
        int count = factions.factionData.GetCountOfFaction(faction);
        int[] thresholds = GetThresholds(faction);
        foreach (int t in thresholds)
        {
            if (count == t - 1) return true;
        }
        return false;
    }
    public StatDatabase unitTiers;
    // Stubs - wire to your existing systems
    private int GetUnitTier(string name)
    {
        return int.Parse(unitTiers.ReturnValue(name));
    }
    public StatDatabase factionBreakpoints;
    private int[] GetThresholds(string factionName)
    {
        return factionBreakpoints.ReturnValue(factionName).Split("|").Select(int.Parse).ToArray();;
    }
    ////
    // DEBUG
    ////
    public void DebugLogState(float[] state, bool[] mask, AutoChessPrepManager prep, AutoChessDataManager data, AutoChessShopManager shop)
    {
        Debug.Log("========== ENCODER DEBUG ==========");
        Debug.Log($"State: {state.Length} (want {StateSize}) | Mask: {mask.Length} (want 44) | Non-zero: {state.Count(f => f != 0f)}");

        int idx = 0;

        // --- SCALARS ---
        Debug.Log($"--- SCALARS [0-12] ---");
        Debug.Log($"  gold={state[idx]:F3}({data.GetGold()}) lvl={state[idx+1]:F3}({data.GetLevel()}) hp={state[idx+2]:F3}({data.GetHealth()})");
        Debug.Log($"  round={state[idx+3]:F3}({data.GetRound()}) win={state[idx+4]:F3} loss={state[idx+5]:F3}");
        Debug.Log($"  interest={state[idx+6]:F3} rerolls={state[idx+7]:F3}({data.GetFreeRerolls()})");
        Debug.Log($"  maxField={state[idx+8]:F3}({prep.GetMaxFieldSlots()}) curField={state[idx+9]:F3}({prep.fieldSlots.Count})");
        Debug.Log($"  curBench={state[idx+10]:F3}({prep.benchSlots.Count}) benchSpace={state[idx+11]:F3} investor={state[idx+12]:F3}");
        idx += 13;

        // --- FACTIONS ---
        Debug.Log($"--- FACTIONS [13-88] (19×4) ---");
        for (int i = 0; i < factionOrder.Count; i++)
        {
            int f = idx + i * 4;
            int count = Mathf.RoundToInt(state[f] * 10f);
            bool active = state[f + 1] > 0.5f;
            if (count > 0 || active)
                Debug.Log($"  {factionOrder[i]}: count={count} active={active} stacks={state[f+2]*100f:F0} oneAway={state[f+3]>0.5f}");
        }
        idx += 76;

        // --- BENCH ---
        Debug.Log($"--- BENCH [89-712] ({prep.GetMaxBenchSlots()}×{BENCH_FEATURES}) ---");
        for (int i = 0; i < prep.GetMaxBenchSlots(); i++)
        {
            int b = idx + i * BENCH_FEATURES;
            if (state[b] > 0.5f)
                Debug.Log($"  [{i}] present tier={state[b+1]*5f:F0} lvl={state[b+2]*3f:F0} copies={state[b+3]*2f:F0} traitActive={state[b+4]>0.5f} equip={state[b+5]>0.5f}");
        }
        idx += prep.GetMaxBenchSlots() * BENCH_FEATURES;

        // --- FIELD ---
        Debug.Log($"--- FIELD [713-1306] ({maxField}×{FIELD_FEATURES}) ---");
        for (int i = 0; i < maxField; i++)
        {
            int f = idx + i * FIELD_FEATURES;
            if (state[f] > 0.5f)
                Debug.Log($"  [{i}] present tier={state[f+1]*5f:F0} lvl={state[f+2]*3f:F0} hp={state[f+3]*200f:F0} atk={state[f+4]*100f:F0} def={state[f+5]*100f:F0} res={state[f+6]*100f:F0} equip={state[f+7]>0.5f}");
        }
        idx += maxField * FIELD_FEATURES;

        // --- SHOP ---
        Debug.Log($"--- SHOP [1307-1618] ({maxShop}×{SHOP_FEATURES}) ---");
        for (int i = 0; i < maxShop; i++)
        {
            int s = idx + i * SHOP_FEATURES;
            if (state[s] > 0.5f)
            {
                string name = (i < shop.shopActors.Count && shop.shopActors[i] != null) ? shop.shopActors[i].GetName() : "?";
                Debug.Log($"  [{i}] {name}: present tier={state[s+1]*5f:F0} cost={state[s+2]*5f:F0} afford={state[s+3]>0.5f} merge={state[s+4]>0.5f} exists={state[s+5]>0.5f}");
            }
        }

        // --- MASK ---
        Debug.Log("--- MASK ---");
        string[] names = { "Done", "LvlUp", "Reroll", "Buy0", "Buy1", "Buy2", "Buy3", "Buy4", "Buy5",
            "SellB0", "SellB1", "SellB2", "SellB3", "SellB4", "SellB5", "SellB6", "SellB7", "SellB8", "SellB9", "SellB10", "SellB11",
            "PlaceB0", "PlaceB1", "PlaceB2", "PlaceB3", "PlaceB4", "PlaceB5", "PlaceB6", "PlaceB7", "PlaceB8", "PlaceB9", "PlaceB10", "PlaceB11",
            "RetF0", "RetF1", "RetF2", "RetF3", "RetF4", "RetF5", "RetF6", "RetF7", "RetF8", "RetF9", "RetF10" };
        for (int i = 0; i < 44; i++) if (mask[i]) Debug.Log($"  [{i}] {names[i]} = TRUE");

        // --- ASSERTIONS ---
        bool ok = state.Length == StateSize;
        if (prep.benchSlots.Count > 0 && state[13 + 76] < 0.5f) { Debug.LogError("Bench has actors but slot 0 empty!"); ok = false; }
        if (prep.fieldSlots.Count > 0 && state[13 + 76 + prep.GetMaxBenchSlots() * BENCH_FEATURES] < 0.5f) { Debug.LogError("Field has actors but slot 0 empty!"); ok = false; }
        if (shop.shopActors.Count > 0 && state[13 + 76 + prep.GetMaxBenchSlots() * BENCH_FEATURES + maxField * FIELD_FEATURES] < 0.5f) { Debug.LogError("Shop has actors but slot 0 empty!"); ok = false; }
        Debug.Log(ok ? "PASS" : "FAIL");
        Debug.Log("========== END ==========");
    }
}
