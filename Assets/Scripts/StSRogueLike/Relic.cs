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
        relicCounterTiming = 0;
        relicTiming = RelicTiming.PickUp;
        condition = "";
        specifics = "";
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
        relicCounterTiming = int.Parse(dataBlocks[2]);
        relicTiming = (RelicTiming)System.Enum.Parse(typeof(RelicTiming), dataBlocks[3]);
        condition = dataBlocks[4];
        specifics = dataBlocks[5];
        target = dataBlocks[6];
        effect = dataBlocks[7];
        effectSpecifics = dataBlocks[8];
        description = dataBlocks[9];
        flavor = dataBlocks[10];
    }
    public string relicName;
    public RelicType relicType;
    public int relicBaseCounters;
    public int relicCounterTiming;
    public RelicTiming relicTiming;
    public string condition;
    public string specifics;
    public string target;
    public string effect;
    public string effectSpecifics;
    public string description;
    public string flavor;
}
