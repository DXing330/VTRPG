using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BattleSimulationRunner
{
    static List<string> capturedErrors = new List<string>();

    public static BattleSimulationRunResult RunScenarioInLoadedScene(BattleTestScenario scenario, int runIndex)
    {
        BattleSimulationRunResult result = CreateResult(scenario, runIndex);
        capturedErrors = new List<string>();
        Application.logMessageReceived += CaptureUnityErrors;

        try
        {
            BattleSimulator simulator = FindLoadedSimulator();
            ValidateScenario(simulator, scenario);
            ApplyScenario(simulator, scenario, runIndex, result.seed);
            simulator.StartBattle();
            if (simulator.battleManager == null)
            {
                throw new InvalidOperationException("BattleSimulator has no BattleManager reference.");
            }
            simulator.battleManager.ForceStart();
            CollectResult(simulator, scenario, result);
        }
        catch (Exception exception)
        {
            MarkFailed(result, exception.Message);
        }
        finally
        {
            Application.logMessageReceived -= CaptureUnityErrors;
        }

        if (capturedErrors.Count > 0)
        {
            result.errors.AddRange(capturedErrors);
            MarkFailed(result, "Unity logged errors or exceptions during the run.");
        }

        return result;
    }

    static BattleSimulationRunResult CreateResult(BattleTestScenario scenario, int runIndex)
    {
        BattleSimulationRunResult result = new BattleSimulationRunResult();
        result.scenarioName = scenario == null ? "Missing Scenario" : scenario.ScenarioName();
        result.runIndex = runIndex;
        result.seed = scenario == null ? 0 : scenario.SeedForRun(runIndex);
        return result;
    }

    static BattleSimulator FindLoadedSimulator()
    {
        BattleSimulator[] simulators = Resources.FindObjectsOfTypeAll<BattleSimulator>();
        for (int i = 0; i < simulators.Length; i++)
        {
            Scene scene = simulators[i].gameObject.scene;
            if (scene.IsValid() && scene.isLoaded)
            {
                return simulators[i];
            }
        }
        throw new InvalidOperationException("No loaded BattleSimulator was found.");
    }

    static void ValidateScenario(BattleSimulator simulator, BattleTestScenario scenario)
    {
        if (scenario == null)
        {
            throw new InvalidOperationException("Scenario is missing.");
        }
        if (simulator == null)
        {
            throw new InvalidOperationException("BattleSimulator is missing.");
        }
        if (simulator.simulatorState == null)
        {
            throw new InvalidOperationException("BattleSimulator has no simulatorState reference.");
        }
        if (simulator.partyOneList == null || simulator.partyTwoList == null)
        {
            throw new InvalidOperationException("BattleSimulator has missing party list references.");
        }
        if (simulator.actorStats == null)
        {
            throw new InvalidOperationException("BattleSimulator has no actorStats reference.");
        }
        simulator.actorStats.Initialize();

        ValidateParty("Party One", scenario.partyOne, simulator.actorStats);
        ValidateParty("Party Two", scenario.partyTwo, simulator.actorStats);
        ValidateChoices("terrain", scenario.allowedTerrains, simulator.simulatorState.allTerrainTypes);
        ValidateChoices("weather", scenario.allowedWeather, simulator.simulatorState.allWeathers);
        ValidateChoices("time", scenario.allowedTimes, simulator.simulatorState.allTimes);
        ValidateChoices("starting formation", scenario.startingFormations, simulator.simulatorState.allStartingFormations);
        ValidateChoices("party one battle modifier", scenario.partyOneBattleModifiers, simulator.simulatorState.allBattleModifiers);
        ValidateChoices("party two battle modifier", scenario.partyTwoBattleModifiers, simulator.simulatorState.allBattleModifiers);
    }

    static void ValidateParty(string partyName, List<BattleTestActorSpec> actors, StatDatabase actorStats)
    {
        if (actors == null || actors.Count == 0)
        {
            throw new InvalidOperationException(partyName + " has no actors.");
        }
        for (int i = 0; i < actors.Count; i++)
        {
            BattleTestActorSpec actor = actors[i];
            if (actor == null || string.IsNullOrEmpty(actor.spriteName))
            {
                throw new InvalidOperationException(partyName + " actor " + i + " has no spriteName.");
            }
            if (!actorStats.KeyExists(actor.spriteName) && string.IsNullOrEmpty(actor.statsOverride))
            {
                throw new InvalidOperationException(partyName + " actor '" + actor.spriteName + "' is missing from actorStats and has no statsOverride.");
            }
        }
    }

    static void ValidateChoices(string label, List<string> selected, List<string> allowed)
    {
        if (selected == null || selected.Count == 0)
        {
            return;
        }
        for (int i = 0; i < selected.Count; i++)
        {
            if (string.IsNullOrEmpty(selected[i]))
            {
                continue;
            }
            if (allowed == null || !allowed.Contains(selected[i]))
            {
                throw new InvalidOperationException("Invalid " + label + " '" + selected[i] + "'.");
            }
        }
    }

    static void ApplyScenario(BattleSimulator simulator, BattleTestScenario scenario, int runIndex, int seed)
    {
        UnityEngine.Random.InitState(seed);
        PopulateParty(simulator.partyOneList, scenario.partyOne, simulator.actorStats, runIndex, 0);
        PopulateParty(simulator.partyTwoList, scenario.partyTwo, simulator.actorStats, runIndex, 1);

        BattleSimulatorState state = simulator.simulatorState;
        state.selectedTerrainTypes = NewList(scenario.allowedTerrains);
        state.selectedWeathers = NewList(scenario.allowedWeather);
        state.selectedTimes = NewList(scenario.allowedTimes);
        state.selectedStartingFormations = NewList(scenario.startingFormations);
        state.selectedP1BattleMods = NewList(scenario.partyOneBattleModifiers);
        state.selectedP2BattleMods = NewList(scenario.partyTwoBattleModifiers);
        // Use the existing multibattle stats path for exactly one run so BattleStatsTracker keeps damage data.
        state.multiBattle = 1;
        state.prevMultiBattle = 0;
        state.multiBattleCurrent = 1;
        state.multiBattleCount = 1;
        state.autoBattle = scenario.autoBattle ? 1 : 0;
        state.controlAI = scenario.controlAI ? 1 : 0;
        state.winningTeam = -1;
        state.ClearCustomBattleName();

        if (simulator.battleStatsTrackerSaving != null)
        {
            simulator.battleStatsTrackerSaving.NewGame();
        }
        if (simulator.battleManager != null && simulator.battleManager.battleEndManager != null)
        {
            simulator.battleManager.battleEndManager.test = true;
        }
    }

    static void PopulateParty(CharacterList targetList, List<BattleTestActorSpec> actors, StatDatabase actorStats, int runIndex, int team)
    {
        targetList.ResetLists();
        for (int i = 0; i < actors.Count; i++)
        {
            BattleTestActorSpec actor = actors[i];
            string stats = string.IsNullOrEmpty(actor.statsOverride) ? actorStats.ReturnValue(actor.spriteName) : actor.statsOverride;
            string id = actor.ActorId((runIndex * 1000) + (team * 100) + i);
            targetList.AddMemberToParty(actor.DisplayName(i), stats, actor.spriteName, id, actor.equipment);
        }
    }

    static List<string> NewList(List<string> source)
    {
        if (source == null)
        {
            return new List<string>();
        }
        return new List<string>(source);
    }

    static void CollectResult(BattleSimulator simulator, BattleTestScenario scenario, BattleSimulationRunResult result)
    {
        BattleManager battleManager = simulator.battleManager;
        BattleStatsTracker tracker = battleManager.battleStatsTracker;

        result.rounds = battleManager.GetRoundNumber();
        result.turnIndex = battleManager.GetTurnIndex();
        result.winningTeam = tracker == null ? -1 : tracker.winningTeam;
        if (result.winningTeam < 0)
        {
            result.winningTeam = battleManager.FindWinningTeam();
        }
        result.terrain = simulator.simulatorState.selectedTerrain;
        result.weather = simulator.simulatorState.selectedWeather;
        result.time = simulator.simulatorState.selectedTime;
        result.startingFormation = simulator.simulatorState.selectedStartingFormation;

        if (battleManager.combatLog != null)
        {
            result.logEntries = battleManager.combatLog.allLogs.Count;
            result.combatLogs = new List<string>(battleManager.combatLog.allLogs);
            result.combatLogEntries = BuildCombatLogEntries(battleManager.combatLog);
        }

        if (tracker != null)
        {
            AddActorResults(result, tracker, simulator, scenario);
        }

        if (result.winningTeam < 0)
        {
            MarkFailed(result, "Runner could not determine a winning team.");
        }
        if (scenario.maxRounds > 0 && result.rounds > scenario.maxRounds)
        {
            MarkFailed(result, "Battle exceeded max rounds: " + result.rounds + " > " + scenario.maxRounds + ".");
        }
        if (scenario.maxTurns > 0 && result.logEntries > scenario.maxTurns)
        {
            MarkFailed(result, "Battle exceeded max turn/log limit: " + result.logEntries + " > " + scenario.maxTurns + ".");
        }
    }

    static List<BattleSimulationCombatLogEntry> BuildCombatLogEntries(CombatLog combatLog)
    {
        List<BattleSimulationCombatLogEntry> entries = new List<BattleSimulationCombatLogEntry>();
        for (int i = 0; i < combatLog.allLogs.Count; i++)
        {
            string[] events = combatLog.allLogs[i].Split('|');
            int round = i < combatLog.combatRoundTracker.Count ? combatLog.combatRoundTracker[i] : 0;
            int turn = i < combatLog.combatTurnTracker.Count ? combatLog.combatTurnTracker[i] : 0;
            for (int eventIndex = 0; eventIndex < events.Length; eventIndex++)
            {
                if (string.IsNullOrEmpty(events[eventIndex]))
                {
                    continue;
                }
                BattleSimulationCombatLogEntry entry = new BattleSimulationCombatLogEntry();
                entry.round = round;
                entry.turn = turn;
                entry.eventIndex = eventIndex;
                entry.text = events[eventIndex];
                ParseCombatAction(entry);
                entries.Add(entry);
            }
        }
        return entries;
    }

    static void ParseCombatAction(BattleSimulationCombatLogEntry entry)
    {
        ParseAction(entry, " uses ", "Skill");
        if (!string.IsNullOrEmpty(entry.actionType))
        {
            return;
        }
        ParseAction(entry, " casts  ", "Spell");
        if (!string.IsNullOrEmpty(entry.actionType))
        {
            return;
        }
        ParseAction(entry, " casts ", "Spell");
        if (!string.IsNullOrEmpty(entry.actionType))
        {
            return;
        }
        ParseAction(entry, " attacks ", "Attack");
    }

    static void ParseAction(BattleSimulationCombatLogEntry entry, string marker, string actionType)
    {
        int markerIndex = entry.text.IndexOf(marker, StringComparison.Ordinal);
        if (markerIndex < 0)
        {
            return;
        }

        entry.actorName = entry.text.Substring(0, markerIndex).Trim();
        entry.actionType = actionType;
        string skill = entry.text.Substring(markerIndex + marker.Length).Trim();
        if (skill.EndsWith("."))
        {
            skill = skill.Substring(0, skill.Length - 1);
        }
        entry.skillName = skill;
    }

    static void AddActorResults(BattleSimulationRunResult result, BattleStatsTracker tracker, BattleSimulator simulator, BattleTestScenario scenario)
    {
        List<string> names = tracker.GetActorNames();
        List<string> sprites = tracker.GetActorSprites();
        List<int> teams = tracker.GetActorTeams();
        List<int> damageDealt = tracker.GetDamageDealt();
        List<int> damageTaken = tracker.GetDamageTaken();

        for (int i = 0; i < names.Count; i++)
        {
            BattleSimulationActorResult actor = new BattleSimulationActorResult();
            actor.actorName = names[i];
            actor.spriteName = i < sprites.Count ? sprites[i] : "";
            actor.team = i < teams.Count ? teams[i] : -1;
            actor.damageDealt = i < damageDealt.Count ? damageDealt[i] : 0;
            actor.damageTaken = i < damageTaken.Count ? damageTaken[i] : 0;
            AddBaseStats(actor, simulator, scenario);
            result.actors.Add(actor);
        }
    }

    static void AddBaseStats(BattleSimulationActorResult actor, BattleSimulator simulator, BattleTestScenario scenario)
    {
        BattleTestActorSpec spec = FindActorSpec(actor, scenario);
        string stats = "";
        if (spec != null)
        {
            stats = string.IsNullOrEmpty(spec.statsOverride) ? simulator.actorStats.ReturnValue(spec.spriteName) : spec.statsOverride;
        }
        else if (!string.IsNullOrEmpty(actor.spriteName))
        {
            stats = simulator.actorStats.ReturnValue(actor.spriteName);
        }

        ActorStatSummary summary = ActorStatSummary.FromString(stats);
        actor.species = summary.species;
        actor.elements = summary.elements;
        actor.attributes = summary.attributes;
        actor.health = summary.health;
        actor.attack = summary.attack;
        actor.range = summary.range;
        actor.defense = summary.defense;
        actor.moveSpeed = summary.moveSpeed;
        actor.moveType = summary.moveType;
        actor.energy = summary.energy;
        actor.initiative = summary.initiative;
        actor.dodge = summary.dodge;
        actor.magicPower = summary.magicPower;
        actor.magicResist = summary.magicResist;
        actor.passives = summary.passives;
        actor.actives = summary.actives;
        actor.spells = summary.spells;
        actor.baseStats = summary.CompactString();
    }

    static BattleTestActorSpec FindActorSpec(BattleSimulationActorResult actor, BattleTestScenario scenario)
    {
        List<BattleTestActorSpec> actors = actor.team == 0 ? scenario.partyOne : scenario.partyTwo;
        for (int i = 0; i < actors.Count; i++)
        {
            if (actors[i] == null)
            {
                continue;
            }
            if (actor.actorName == actors[i].DisplayName(i))
            {
                return actors[i];
            }
        }
        return null;
    }

    class ActorStatSummary
    {
        public string species = "";
        public string elements = "";
        public string attributes = "";
        public int health;
        public int attack;
        public int range;
        public int defense;
        public int moveSpeed;
        public string moveType = "";
        public int energy;
        public int initiative;
        public int dodge;
        public int magicPower;
        public int magicResist;
        public string passives = "";
        public string actives = "";
        public string spells = "";

        public static ActorStatSummary FromString(string stats)
        {
            ActorStatSummary summary = new ActorStatSummary();
            if (string.IsNullOrEmpty(stats))
            {
                return summary;
            }

            string[] values = stats.Split('!');
            summary.species = Value(values, 1);
            summary.elements = Value(values, 2);
            summary.attributes = Value(values, 3);
            summary.health = IntValue(values, 4);
            summary.attack = IntValue(values, 5);
            summary.range = IntValue(values, 6);
            summary.defense = IntValue(values, 7);
            summary.moveSpeed = IntValue(values, 8);
            summary.moveType = Value(values, 9);
            summary.initiative = IntValue(values, 11);
            summary.energy = IntValue(values, 12);
            summary.dodge = IntValue(values, 17);
            summary.passives = Value(values, 18);
            summary.actives = Value(values, 21);
            summary.magicPower = IntValue(values, 24);
            summary.magicResist = IntValue(values, 25);
            summary.spells = Value(values, 28);
            return summary;
        }

        public string CompactString()
        {
            return "HP " + health
                + ", ATK " + attack
                + ", DEF " + defense
                + ", RNG " + range
                + ", SPD " + moveSpeed
                + ", EN " + energy
                + ", INIT " + initiative
                + ", DODGE " + dodge
                + ", MP " + magicPower
                + ", MR " + magicResist;
        }

        static string Value(string[] values, int index)
        {
            if (index < 0 || index >= values.Length)
            {
                return "";
            }
            return values[index];
        }

        static int IntValue(string[] values, int index)
        {
            int parsed;
            if (!int.TryParse(Value(values, index), out parsed))
            {
                return 0;
            }
            return parsed;
        }
    }

    static void MarkFailed(BattleSimulationRunResult result, string reason)
    {
        result.failed = true;
        if (string.IsNullOrEmpty(result.failureReason))
        {
            result.failureReason = reason;
        }
        else if (!result.failureReason.Contains(reason))
        {
            result.failureReason += " " + reason;
        }
    }

    static void CaptureUnityErrors(string condition, string stackTrace, LogType type)
    {
        if (type != LogType.Assert && type != LogType.Error && type != LogType.Exception)
        {
            return;
        }
        capturedErrors.Add(type + ": " + condition + "\n" + stackTrace);
    }
}
