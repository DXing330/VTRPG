using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum RelicType
{
    Constant,
    SingleUse,
    Charged,
    Counter
}
public enum RelicTiming
{
    Reward,
    PickUp,
    Battle
}
[CreateAssetMenu(fileName = "Relic", menuName = "ScriptableObjects/StS/Relic", order = 1)]
public class Relic : ScriptableObject
{
    public GeneralUtility utility;
    private void InitializeDefaults()
    {
        relicName = "Trash";
        relicType = RelicType.Constant;
        relicBaseCounters = 0;
        relicCounterTiming = "";
        relicTiming = RelicTiming.PickUp;
        condition = "";
        conditionSpecifics = "";
        target = "";
        effect = "";
        effectSpecifics = "";
        description = "";
        flavor = "";
    }
    public void LoadRelic(string relicData, string newName = "")
    {
        relicName = newName;
        string[] dataBlocks = relicData.Split("|");
        if (dataBlocks.Length < 11)
        {
            Debug.LogError($"Invalid relic data for {newName}. Expected 11 blocks, got {dataBlocks.Length}.");
            InitializeDefaults();
            return;
        }
        relicType = (RelicType)System.Enum.Parse(typeof(RelicType), dataBlocks[0]);
        relicBaseCounters = int.Parse(dataBlocks[1]);
        relicCounterTiming = dataBlocks[2];
        relicTiming = (RelicTiming)System.Enum.Parse(typeof(RelicTiming), dataBlocks[3]);
        condition = dataBlocks[4];
        conditionSpecifics = dataBlocks[5];
        target = dataBlocks[6];
        effect = dataBlocks[7];
        effectSpecifics = dataBlocks[8];
        description = dataBlocks[9];
        flavor = dataBlocks[10];
    }
    public string relicName;
    public RelicType relicType;
    public bool TrackCounters()
    {
        return (relicType == RelicType.Charged || relicType == RelicType.Counter);
    }
    public bool ChargedRelic()
    {
        return (relicType == RelicType.Charged);
    }
    public int relicBaseCounters;
    public int GetBaseCounters(){return relicBaseCounters;}
    public string relicCounterTiming;
    public string GetCounterTiming()
    {
        return relicCounterTiming;
    }
    public RelicTiming relicTiming;
    public bool BattleRelic()
    {
        return (relicTiming == RelicTiming.Battle);
    }
    public bool PickUpRelic()
    {
        return (relicTiming == RelicTiming.PickUp);
    }
    public string condition;
    public string GetCondition()
    {
        return condition;
    }
    public string conditionSpecifics;
    public string GetConditionSpecifics()
    {
        return conditionSpecifics;
    }
    public string target;
    public string GetTarget()
    {
        return target;
    }
    public string effect;
    public string GetEffect()
    {
        return effect;
    }
    public string effectSpecifics;
    public string GetEffectSpecifics()
    {
        return effectSpecifics;
    }
    public string description;
    public string flavor;
}