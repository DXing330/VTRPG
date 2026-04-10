using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleStats : BattleUIBaseClass
{
    public TacticActor actor;
    public override void SetActor(TacticActor newActor)
    {
        actor = newActor;
        UpdateBasicStats();
        UpdateSpendableStats();
    }
    public override void ResetUI()
    {
        for (int i = 0; i < basicStats.Count; i++)
        {
            basicStats[i].SetText("");
        }
        for (int i = 0; i < spendableStats.Count; i++)
        {
            spendableStats[i].SetText("");
        }
    }
    public override void UpdateUI()
    {
        UpdateBasicStats();
        UpdateSpendableStats();
    }
    public List<StatImageText> basicStats;
    public List<StatImageText> spendableStats;

    public void UpdateStats()
    {
        UpdateBasicStats();
        UpdateSpendableStats();
    }

    public void UpdateBasicStats()
    {
        if (actor == null)
        {
            ResetUI();
            return;
        }
        List<string> stats = actor.ReturnStats();
        for (int i = 0; i < Mathf.Min(stats.Count, basicStats.Count); i++)
        {
            basicStats[i].SetText(stats[i]);
        }
    }

    public void UpdateSpendableStats()
    {
        if (actor == null)
        {
            ResetUI();
            return;
        }
        List<string> stats = actor.ReturnSpendableStats();
        for (int i = 0; i < Mathf.Min(stats.Count, spendableStats.Count); i++)
        {
            spendableStats[i].SetText(stats[i]);
        }
    }

    public void UpdateNonTurnActor(TacticActor newActor)
    {
        List<string> stats = newActor.ReturnStats();
        for (int i = 0; i < Mathf.Min(stats.Count, basicStats.Count); i++)
        {
            basicStats[i].SetText(stats[i]);
        }
        stats = newActor.ReturnSpendableStats();
        for (int i = 0; i < Mathf.Min(stats.Count, spendableStats.Count); i++)
        {
            spendableStats[i].SetText("?");
        }
    }
}
