using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BattleTestSuite", menuName = "ScriptableObjects/Debug/Battle Test Suite", order = 1)]
public class BattleTestSuite : ScriptableObject
{
    public string suiteName = "Battle Test Suite";
    public string reportRoot = "Assets/output/battle-tests";
    public bool stopOnFailure = false;
    public List<BattleTestScenario> scenarios = new List<BattleTestScenario>();

    public string SuiteName()
    {
        if (!string.IsNullOrEmpty(suiteName))
        {
            return suiteName;
        }
        return name;
    }

    public List<BattleTestScenario> EnabledScenarios()
    {
        return EnabledScenariosWithTag("");
    }

    public List<BattleTestScenario> EnabledScenariosWithTag(string tag)
    {
        List<BattleTestScenario> enabledScenarios = new List<BattleTestScenario>();
        for (int i = 0; i < scenarios.Count; i++)
        {
            if (scenarios[i] == null || !scenarios[i].enabled)
            {
                continue;
            }
            if (!scenarios[i].HasTag(tag))
            {
                continue;
            }
            enabledScenarios.Add(scenarios[i]);
        }
        return enabledScenarios;
    }
}
