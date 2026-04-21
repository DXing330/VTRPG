using System;
using System.Collections.Generic;

[Serializable]
public class BattleSimulationActorResult
{
    public string actorName;
    public string spriteName;
    public int team;
    public string species;
    public string elements;
    public string attributes;
    public string baseStats;
    public int health;
    public int attack;
    public int range;
    public int defense;
    public int moveSpeed;
    public string moveType;
    public int energy;
    public int initiative;
    public int dodge;
    public int magicPower;
    public int magicResist;
    public string passives;
    public string actives;
    public string spells;
    public int damageDealt;
    public int damageTaken;
}

[Serializable]
public class BattleSimulationCombatLogEntry
{
    public int round;
    public int turn;
    public int eventIndex;
    public string text;
    public string actorName;
    public string actionType;
    public string skillName;
}

[Serializable]
public class BattleSimulationRunResult
{
    public string scenarioName;
    public int runIndex;
    public int seed;
    public string terrain;
    public string weather;
    public string time;
    public string startingFormation;
    public int winningTeam = -1;
    public int rounds;
    public int turnIndex;
    public int logEntries;
    public bool failed;
    public string failureReason;
    public List<string> warnings = new List<string>();
    public List<string> errors = new List<string>();
    public List<BattleSimulationActorResult> actors = new List<BattleSimulationActorResult>();
    public List<string> combatLogs = new List<string>();
    public List<BattleSimulationCombatLogEntry> combatLogEntries = new List<BattleSimulationCombatLogEntry>();
}

[Serializable]
public class BattleSimulationSuiteResult
{
    public string suiteName;
    public string startedAt;
    public string finishedAt;
    public bool failed;
    public List<BattleSimulationRunResult> runs = new List<BattleSimulationRunResult>();
    public List<string> failures = new List<string>();

    public void AddRun(BattleSimulationRunResult run)
    {
        runs.Add(run);
        if (run.failed)
        {
            failed = true;
            failures.Add(run.scenarioName + " run " + run.runIndex + ": " + run.failureReason);
        }
    }
}
