using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AutoChessTraitManager", menuName = "ScriptableObjects/AutoChess/AutoChessTraitManager", order = 1)]
public class AutoChessTraitManager : ScriptableObject
{
    public int ReturnTraitSpecificsInt(AutoActorRollUpData actor, AutoChessPrepManager manager)
    {
        AutoChessTrait trait = actor.trait;
        int level = actor.GetLevel();
        int baseAmount = 1;
        string[] blocks = trait.specifics.Split("MultiBy");
        if (blocks.Length > 1)
        {
            baseAmount = int.Parse(blocks[1]);
        }
        else
        {
            return level * int.Parse(blocks[0]);
        }
        switch (blocks[0])
        {
            default:
            return level * baseAmount;
            case "ShopLevel":
            return level * baseAmount * manager.dataManager.GetLevel();
        }
    }
}
