using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AutoChessPVPGenome
{
    public float[] genes = new float[44];
    // --- Name lookup (static readonly, not const) ---
    public static readonly List<string> GeneNames = new List<string>()
    {
        "W_BUY_TIER",
        "W_FACTION_ACTIVE",
        "W_BUY_SYNERGY",
        "W_PREF_FACTION",
        "W_BUY_DUPLICATE",
        "W_FACTION_INACTIVE",
        "W_BUY_ECON_STRETCH",
        "W_KEEP_TIER",
        "W_KEEP_SYNERGY",
        "W_KEEP_DUPLICATE",
        "W_KEEP_STAR_LEVEL",
        "W_ECON_INTEREST",
        "W_ECON_LEVEL_TIMING",
        "W_ECON_HP_URGENCY",
        "W_ECON_REROLL",
        "W_ECON_STREAK_WIN",
        "W_ECON_STREAK_LOSS",
        "W_PLACE_SWAP_THRESH",
        "W_SELL_MARGIN",
        "W_ROLE_TANK_RATIO", // Think About Synergy More Than Roles, If You Get A Full Faction You'll Naturally Have The Roles.
        "W_MAIN_SYN_HIT",
        "W_ECON_SYN_HIT",
        "W_SYN_HIT",
        "W_SYN_MAINTAIN",
        "W_SYN_ONEAWAY",
        "W_DUP_MERGE",
        "W_DUP_CLOSE",
        "W_DUP_POTENTIAL",
        "W_DUP_NONE",
        "W_FACTION_MAIN",
        "W_FACTION_ECON",
        "W_FACTION_SIDE",
        "W_FACTION_COMMITMENT",
        "W_FACTION_STACKS",
        "W_UNIT_STACK_GENERATION",
        "W_UNIT_STACK_SCALING",
        "W_UNIT_CYCLE",
        "W_UNIT_CYCLE_SCALING",
        "W_ITEM_SAVE",
        "W_ITEM_FOCUS_HIGH_TIER_UNIT",
        "W_ITEM_UNIT_MATCH",
        "W_ITEM_COMPONENT_SAVE",
        "W_ITEM_DUPLICATE_PENALTY",
        "W_ITEM_VALUE"
    };
    // --- Defaults aligned to the indices above ---
    public static readonly float[] Defaults = new float[]
    {
        2.0f,   // 0  W_BUY_TIER
        2.0f,   // 1  W_FACTION_ACTIVE
        2.0f,   // 2  W_BUY_SYNERGY
        1.0f,   // 3  W_PREF_FACTION
        2.0f,   // 4  W_BUY_DUPLICATE
        0.5f,   // 5  W_FACTION_INACTIVE
        -1.0f,  // 6  W_BUY_ECON_STRETCH
        1.0f,   // 7  W_KEEP_TIER
        2.0f,   // 8  W_KEEP_SYNERGY
        2.0f,   // 9  W_KEEP_DUPLICATE
        3.0f,   // 10 W_KEEP_STAR_LEVEL
        3.0f,   // 11 W_ECON_INTEREST
        2.0f,   // 12 W_ECON_LEVEL_TIMING
        0.3f,   // 13 W_ECON_HP_URGENCY
        1.0f,   // 14 W_ECON_REROLL
        2.0f,   // 15 W_ECON_STREAK_WIN
        1.5f,   // 16 W_ECON_STREAK_LOSS
        2.5f,   // 17 W_PLACE_SWAP_THRESH
        0.5f,   // 18 W_SELL_MARGIN
        0.3f,   // 19 W_ROLE_TANK_RATIO
        3.0f,   // 20 W_MAIN_SYN_HIT
        1.0f,   // 21 W_ECON_SYN_HIT
        2.0f,   // 22 W_SYN_HIT
        1.5f,   // 23 W_SYN_MAINTAIN
        0.5f,   // 24 W_SYN_ONEAWAY
        3.0f,   // 25 W_DUP_MERGE
        2.0f,   // 26 W_DUP_CLOSE
        2.0f,   // 27 W_DUP_POTENTIAL
        -2.0f,  // 28 W_DUP_NONE
        2.0f,   // 29 W_FACTION_MAIN
        1.0f,   // 30 W_FACTION_ECON
        1.0f,   // 31 W_FACTION_SIDE
        2.0f,   // 32 W_FACTION_COMMITMENT
        1.0f,   // 33 W_FACTION_STACKS
        1.0f,   // 34 W_UNIT_STACK_GENERATION
        0.5f,   // 35 W_UNIT_STACK_SCALING
        0.5f,   // 36 W_UNIT_CYCLE
        0.5f,   // 37 W_UNIT_CYCLE_SCALING
        1.0f,   // 38 W_ITEM_SAVE
        1.0f,   // 39 W_ITEM_URGENCY
        1.0f,   // 40 W_ITEM_UNIT_MATCH
        1.0f,   // 41 W_ITEM_COMPONENT_SAVE
        1.0f,   // 42 W_ITEM_DUPLICATE_PENALTY
        1.0f    // 43 W_ITEM_VALUE
    };
    public string genePool = "";
    public void SetGenePool(string newPool)
    {
        genePool = newPool;
    }
    public string GetGenePool()
    {
        return genePool;
    }
    public string preferredFaction = "";
    public void SetPreferredFaction(string newFaction)
    {
        preferredFaction = newFaction;
    }
    public string GetPreferredFaction(){return preferredFaction;}
    public float this[int index] => genes[index];
    public float GetByName(string name)
    {
        int idx = GeneNames.IndexOf(name);
        if (idx < 0)
        {
            Debug.LogError($"Genome gene '{name}' does not exist.");
            return 0f;
        }
        return idx >= 0 ? genes[idx] : 0f;
    }
    public string GetGeneNameAtIndex(int index)
    {
        return GeneNames[index];
    }
    public void SetByName(string name, float value)
    {
        int idx = GeneNames.IndexOf(name);
        if (idx >= 0) genes[idx] = value;
    }
    public void ResetToDefault()
    {
        Defaults.CopyTo(genes, 0);
    }
    public static AutoChessPVPGenome CreateDefault()
    {
        var g = new AutoChessPVPGenome();
        Defaults.CopyTo(g.genes, 0);
        return g;
    }
    public static AutoChessPVPGenome RandomGenome()
    {
        var g = new AutoChessPVPGenome();
        for (int i = 0; i < g.genes.Length; i++)
        {
            // Simple random around default ±2, clamped loosely
            g.genes[i] = Mathf.Clamp(Defaults[i] + Random.Range(-2f, 2f), -5f, 10f);
        }
        return g;
    }
    public AutoChessPVPGenome Copy()
    {
        var g = new AutoChessPVPGenome();
        g.genePool = genePool;
        g.preferredFaction = preferredFaction;
        genes.CopyTo(g.genes, 0);
        return g;
    }
    public static AutoChessPVPGenome Crossover(AutoChessPVPGenome a, AutoChessPVPGenome b)
    {
        var child = new AutoChessPVPGenome();
        for (int i = 0; i < child.genes.Length; i++)
        {
            float minRange = Mathf.Min(a.genes[i], b.genes[i]);
            float maxRange = Mathf.Max(a.genes[i], b.genes[i]);
            child.genes[i] = UnityEngine.Random.Range(minRange, maxRange);
        }
        return child;
    }
    public void Mutate(float rate = 0.15f, float strength = 0.3f)
    {
        for (int i = 0; i < genes.Length; i++)
        {
            if (Random.value < rate)
            {
                float scale = Mathf.Sqrt(Mathf.Abs(genes[i]) + 1f);
                genes[i] += RandomGaussian() * strength * scale;
            }
        }
    }
    static float RandomGaussian()
    {
        float u1 = 1f - Random.value;
        float u2 = 1f - Random.value;
        return Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Cos(2f * Mathf.PI * u2);
    }
}
