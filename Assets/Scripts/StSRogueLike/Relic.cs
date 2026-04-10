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
    Battle,
    Resting,
    Reward,
    ChooseSkillBook,
    GainGold,
    Move,
    Event,
    Shop
}

[CreateAssetMenu(fileName = "Relic", menuName = "ScriptableObjects/StS/Relic", order = 1)]
public class Relic : ScriptableObject
{
    public void LoadRelic(string relicData)
    {

    }
    public string relicName;
    public string relicSprite;
    public RelicType relicType;
    public RelicTiming relicTiming;

}
