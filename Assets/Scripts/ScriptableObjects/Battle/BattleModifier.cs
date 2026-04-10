using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BattleMod", menuName = "ScriptableObjects/BattleLogic/BattleMod", order = 1)]
public class BattleModifier : SkillEffect
{
    public StatDatabase modifierData;
    public string condition;
    public string cSpecifics;
    public string effect;
    public string eSpecifics;
    public void ResetModifier()
    {
        condition = "";
        cSpecifics = "";
        effect = "";
        eSpecifics = "";
    }
    public void LoadModifierByName(string newInfo)
    {
        LoadModifier(modifierData.ReturnValue(newInfo));
    }
    public void LoadModifier(string newInfo)
    {
        string[] blocks = newInfo.Split("|");
        if (blocks.Length < 4)
        {
            ResetModifier();
            return;
        }
        condition = blocks[0];
        cSpecifics = blocks[1];
        effect = blocks[2];
        eSpecifics = blocks[3];
    }
    public bool CheckCondition(TacticActor actor)
    {
        switch (condition)
        {
            case "None":
                return true;
        }
        return false;
    }
    public void ApplyModifiers(TacticActor actor)
    {
        if (CheckCondition(actor))
        {
            AffectActor(actor, effect, eSpecifics);
        }
    }
}
