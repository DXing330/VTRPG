using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class BattleAiDataValidator
{
    static readonly HashSet<string> KnownConditions = new HashSet<string>
    {
        "Damaged>",
        "Damaged<",
        "Sprite",
        "TempPassive<>",
        "SkillExists",
        "TempActiveCount>",
        "TempActiveCount<",
        "AdjacentActorCount<",
        "AdjacentActorCount>",
        "AdjacentAllyCount<",
        "AdjacentAllyCount>",
        "AdjacentEnemyCount>",
        "AdjacentEnemyCount<",
        "AdjacentEnemyCount",
        "AttackableEnemyCount>",
        "AttackableEnemyCount<",
        "Shootable",
        "MaxEnergy",
        "Health",
        "Weather",
        "Weather<>",
        "Time",
        "Time<>",
        "MoveType",
        "MoveType<>",
        "Energy<",
        "Round",
        "Counter",
        "Counter<",
        "Counter>",
        "AllyCount<",
        "AllyCount>",
        "AllyExists",
        "AllyExists<>",
        "EnemyCount<",
        "EnemyCount>",
        "Grappling",
        "Grappling<>",
        "Tile",
        "Tile<>",
        "TileExists",
        "TileExists<>",
        "TargetSandwiched",
        "TargetSandwichable",
        "TargetAligned",
        "TargetDistance<",
        "TargetFacingOff",
        "SkillEffect",
        "SpellEffect",
        "SandwichedByTarget",
        "TileSandwiched",
        "TileSandwichable",
        "TargetAdjacentAllyCount>",
        "TargetAdjacentAllyCount<",
        "Buff<>",
        "Buff",
        "Guarding",
        "Guarding<>",
        "GuardCoveringAlly",
        "GuardCoveringAlly<>",
        "CurrentTileCanGuard",
        "CurrentTileCanGuard<>",
        "None"
    };

    static readonly HashSet<string> KnownBossActions = new HashSet<string>
    {
        "Basic",
        "Change Form",
        "One Time Spell",
        "One Time Skill",
        "One Time Chain Skill",
        "Spell",
        "Skill",
        "Summon Skill",
        "Summon Spell",
        "MoveToTile",
        "MoveToSandwichTarget",
        "MoveToSandwichTile",
        "Split",
        "Chain Skill",
        "Random Skill"
    };

    [MenuItem("Window/Battle Tests/Validate Boss AI Data")]
    public static void ValidateBossAiData()
    {
        int issueCount = 0;
        string[] guids = AssetDatabase.FindAssets("t:ActorAI");
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            ActorAI actorAI = AssetDatabase.LoadAssetAtPath<ActorAI>(path);
            if (actorAI == null)
            {
                continue;
            }

            issueCount += ValidateActorAI(actorAI, path);
        }

        if (issueCount == 0)
        {
            Debug.Log("Boss AI data validation passed. Checked " + guids.Length + " ActorAI asset(s).");
        }
        else
        {
            Debug.LogWarning("Boss AI data validation found " + issueCount + " issue(s). See warnings above.");
        }
    }

    static int ValidateActorAI(ActorAI actorAI, string path)
    {
        int issueCount = 0;
        if (actorAI.actorSkillRotation == null)
        {
            LogIssue(path, "actorSkillRotation is missing.");
            return 1;
        }
        if (actorAI.spriteToBossRotation == null)
        {
            LogIssue(path, "spriteToBossRotation is missing.");
            return 1;
        }
        if (actorAI.bossSkillRotation == null)
        {
            LogIssue(path, "bossSkillRotation is missing.");
            return 1;
        }

        List<string> actorKeys = actorAI.actorSkillRotation.keys;
        int count = actorKeys == null ? 0 : actorKeys.Count;
        for (int i = 0; i < count; i++)
        {
            string spriteName = actorKeys[i];
            string rotationType = actorAI.actorSkillRotation.ReturnValue(spriteName);
            if (rotationType != "Boss")
            {
                continue;
            }

            issueCount += ValidateBossActor(actorAI, path, spriteName);
        }
        return issueCount;
    }

    static int ValidateBossActor(ActorAI actorAI, string path, string spriteName)
    {
        int issueCount = 0;
        if (!actorAI.spriteToBossRotation.KeyExists(spriteName))
        {
            LogIssue(path, "Boss actor `" + spriteName + "` has no spriteToBossRotation entry.");
            return 1;
        }

        string rotationKey = actorAI.spriteToBossRotation.ReturnValue(spriteName);
        if (string.IsNullOrEmpty(rotationKey))
        {
            LogIssue(path, "Boss actor `" + spriteName + "` maps to an empty boss rotation key.");
            return 1;
        }

        if (!actorAI.bossSkillRotation.KeyExists(rotationKey))
        {
            LogIssue(path, "Boss actor `" + spriteName + "` maps to missing boss rotation `" + rotationKey + "`.");
            return 1;
        }

        string rotation = actorAI.bossSkillRotation.ReturnValue(rotationKey);
        if (string.IsNullOrEmpty(rotation))
        {
            LogIssue(path, "Boss rotation `" + rotationKey + "` for `" + spriteName + "` is empty.");
            return 1;
        }

        string[] blocks = rotation.Split('#');
        for (int i = 0; i < blocks.Length; i++)
        {
            issueCount += ValidateRotationBlock(actorAI, path, spriteName, rotationKey, i, blocks[i]);
        }
        return issueCount;
    }

    static int ValidateRotationBlock(ActorAI actorAI, string path, string spriteName, string rotationKey, int blockIndex, string block)
    {
        int issueCount = 0;
        string label = "`" + spriteName + "` rotation `" + rotationKey + "` block " + blockIndex;
        string[] fields = block.Split('|');
        if (fields.Length < 4)
        {
            LogIssue(path, label + " has " + fields.Length + " field(s); expected 4: condition|specifics|action|target.");
            return 1;
        }

        string[] conditions = fields[0].Split(',');
        string[] specifics = fields[1].Split(',');
        if (conditions.Length != specifics.Length)
        {
            LogIssue(path, label + " has " + conditions.Length + " condition(s) but " + specifics.Length + " specific value(s).");
            issueCount++;
        }

        for (int i = 0; i < conditions.Length; i++)
        {
            string condition = conditions[i].Trim();
            if (!KnownConditions.Contains(condition))
            {
                LogIssue(path, label + " uses unknown condition `" + condition + "`.");
                issueCount++;
            }
        }

        string action = fields[2].Trim();
        string target = fields[3].Trim();
        if (!KnownBossActions.Contains(action))
        {
            LogIssue(path, label + " uses unknown boss action `" + action + "`.");
            issueCount++;
        }

        issueCount += ValidateActionReference(actorAI, path, label, action, target);
        return issueCount;
    }

    static int ValidateActionReference(ActorAI actorAI, string path, string label, string action, string target)
    {
        switch (action)
        {
            case "Skill":
            case "One Time Skill":
            case "Chain Skill":
            case "One Time Chain Skill":
            case "Random Skill":
                return ValidateDatabaseReferences(path, label, actorAI.activeData, target, "skill");
            case "Spell":
            case "One Time Spell":
                if (actorAI.conditionChecker == null)
                {
                    LogIssue(path, label + " references spell `" + target + "` but conditionChecker is missing.");
                    return 1;
                }
                return ValidateDatabaseReferences(path, label, actorAI.conditionChecker.basicSpellData, target, "spell");
            case "Change Form":
                if (actorAI.actorSkillRotation == null || actorAI.spriteToBossRotation == null)
                {
                    return 0;
                }
                if (!actorAI.actorSkillRotation.KeyExists(target))
                {
                    LogIssue(path, label + " changes form to `" + target + "`, but actorSkillRotation has no entry for that form.");
                    return 1;
                }
                if (!actorAI.spriteToBossRotation.KeyExists(target))
                {
                    LogIssue(path, label + " changes form to `" + target + "`, but spriteToBossRotation has no entry for that form.");
                    return 1;
                }
                return 0;
        }
        return 0;
    }

    static int ValidateDatabaseReferences(string path, string label, StatDatabase database, string references, string referenceType)
    {
        int issueCount = 0;
        if (database == null)
        {
            LogIssue(path, label + " references " + referenceType + " `" + references + "` but the database is missing.");
            return 1;
        }

        string[] referenceList = references.Split(',');
        for (int i = 0; i < referenceList.Length; i++)
        {
            string reference = referenceList[i].Trim();
            if (string.IsNullOrEmpty(reference) || reference == "None")
            {
                continue;
            }

            if (!database.KeyExists(reference))
            {
                LogIssue(path, label + " references missing " + referenceType + " `" + reference + "`.");
                issueCount++;
            }
        }
        return issueCount;
    }

    static void LogIssue(string path, string message)
    {
        Debug.LogWarning("Boss AI data issue in " + path + ": " + message);
    }
}
