using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BattleTestActorBaseline
{
    public string actorName;
    public int team = -1;
    public float minAverageDamageDealt = -1f;
    public float maxAverageDamageDealt = -1f;
    public float minAverageDamageTaken = -1f;
    public float maxAverageDamageTaken = -1f;
}

[CreateAssetMenu(fileName = "BattleTestScenario", menuName = "ScriptableObjects/Debug/Battle Test Scenario", order = 1)]
public class BattleTestScenario : ScriptableObject
{
    public string scenarioName = "Battle Test Scenario";
    public bool enabled = true;
    public int runCount = 10;
    public int maxRounds = 100;
    public int maxTurns = 1000;
    public int baseSeed = 1000;
    public bool randomizeSeed = false;
    public bool autoBattle = true;
    public bool controlAI = false;

    public List<BattleTestActorSpec> partyOne = new List<BattleTestActorSpec>();
    public List<BattleTestActorSpec> partyTwo = new List<BattleTestActorSpec>();
    public List<string> allowedTerrains = new List<string>();
    public List<string> allowedWeather = new List<string>();
    public List<string> allowedTimes = new List<string>();
    public List<string> startingFormations = new List<string>();
    public List<string> partyOneBattleModifiers = new List<string>();
    public List<string> partyTwoBattleModifiers = new List<string>();

    public bool baselineEnabled = false;
    public float minTeamZeroWinRate = -1f;
    public float maxTeamZeroWinRate = -1f;
    public float minTeamOneWinRate = -1f;
    public float maxTeamOneWinRate = -1f;
    public float minAverageRounds = -1f;
    public float maxAverageRounds = -1f;
    public float maxFailureRate = 0f;
    public float maxUnknownWinRate = 0f;
    public List<BattleTestActorBaseline> actorBaselines = new List<BattleTestActorBaseline>();

    public string ScenarioName()
    {
        if (!string.IsNullOrEmpty(scenarioName))
        {
            return scenarioName;
        }
        return name;
    }

    public int RunCount()
    {
        return Mathf.Max(1, runCount);
    }

    public int SeedForRun(int runIndex)
    {
        if (randomizeSeed)
        {
            return Random.Range(int.MinValue, int.MaxValue);
        }
        return baseSeed + runIndex;
    }
}
