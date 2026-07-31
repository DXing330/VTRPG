using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AutoChessPVPGenome
{
    public float[] genes = new float[17];
    // --- Name lookup (static readonly, not const) ---
    public static readonly List<string> GeneNames = new List<string>()
    {
        "W_BUY_TIER",
        "W_BUY_COST_EFF",
        "W_BUY_SYNERGY",
        "W_BUY_POWER",
        "W_BUY_DUPLICATE",
        "W_BUY_ROLE_FIT",
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
        "W_ECON_STREAK_LOSS"
    };
    // --- Defaults aligned to the indices above ---
    public static readonly float[] Defaults = new float[]
    {
        2.0f,   // 0  W_BUY_TIER
        1.5f,   // 1  W_BUY_COST_EFF
        3.0f,   // 2  W_BUY_SYNERGY
        1.0f,   // 3  W_BUY_POWER
        6.0f,   // 4  W_BUY_DUPLICATE
        2.0f,   // 5  W_BUY_ROLE_FIT
        -1.0f,  // 6  W_BUY_ECON_STRETCH
        1.0f,   // 7  W_KEEP_TIER
        2.0f,   // 8  W_KEEP_SYNERGY
        3.0f,   // 9  W_KEEP_DUPLICATE
        3.0f,   // 10 W_KEEP_STAR_LEVEL
        3.0f,   // 11 W_ECON_INTEREST
        2.0f,   // 12 W_ECON_LEVEL_TIMING
        0.3f,   // 13 W_ECON_HP_URGENCY
        1.0f,   // 14 W_ECON_REROLL
        2.0f,   // 15 W_ECON_STREAK_WIN
        1.5f    // 16 W_ECON_STREAK_LOSS
    };
    public float this[int index] => genes[index];
    public float GetByName(string name)
    {
        int idx = GeneNames.IndexOf(name);
        return idx >= 0 ? genes[idx] : 0f;
    }
    public void SetByName(string name, float value)
    {
        int idx = GeneNames.IndexOf(name);
        if (idx >= 0) genes[idx] = value;
    }
    public static AutoChessPVPGenome CreateDefault()
    {
        var g = new AutoChessPVPGenome();
        Defaults.CopyTo(g.genes, 0);
        return g;
    }
}
