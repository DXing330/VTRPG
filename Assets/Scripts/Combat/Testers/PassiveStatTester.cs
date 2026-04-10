using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PassiveStatTester : MonoBehaviour
{
    public TacticActor dummyActor;
    public PassiveSkill passive;
    public StatDatabase allPassives;
    public TerrainPassivesList tilePassives;
    public TerrainPassivesList tEffectPassives;
    [ContextMenu("Debug TEffect Passives")]
    public void DebugTEffectPassives()
    {
        List<string> effects = tEffectPassives.keys;
        for (int i = 0; i < effects.Count; i++)
        {
            Debug.Log(effects[i]);
            Debug.Log("Attack:"+tEffectPassives.ReturnAttackingPassive(effects[i]));
            Debug.Log("Defend:"+tEffectPassives.ReturnDefendingPassive(effects[i]));
            Debug.Log("Move:"+tEffectPassives.ReturnMovingPassive(effects[i]));
            Debug.Log("Start:"+tEffectPassives.ReturnStartPassive(effects[i]));
            Debug.Log("End:"+tEffectPassives.ReturnEndPassive(effects[i]));
        }
    }
    public TerrainPassivesList weatherPassives;
    [ContextMenu("Debug Weather Passives")]
    public void DebugWeatherPassives()
    {
        List<string> effects = weatherPassives.keys;
        for (int i = 0; i < effects.Count; i++)
        {
            Debug.Log(effects[i]);
            Debug.Log("Attack:"+weatherPassives.ReturnAttackingPassive(effects[i]));
            Debug.Log("Defend:"+weatherPassives.ReturnDefendingPassive(effects[i]));
            Debug.Log("Move:"+weatherPassives.ReturnMovingPassive(effects[i]));
            Debug.Log("Start:"+weatherPassives.ReturnStartPassive(effects[i]));
            Debug.Log("End:"+weatherPassives.ReturnEndPassive(effects[i]));
        }
    }
}
