using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpellUpgradeManager : MonoBehaviour
{
    public GeneralUtility utility;
    public SpellBook spellBook;
    /*public enum upgradeTypes
    {
        RangeShape,
        Range,
        Shape,
        Span
    }*/
    public void UpgradeSpell(MagicSpell originalSpell, MagicSpell potentialSpell, string upgradeType, bool increase = true)
    {
        switch (upgradeType)
        {
            case "RangeShape":
                potentialSpell.SetRangeShape(ChangeRangeShape(potentialSpell, increase));
                break;
            case "Range":
                potentialSpell.SetRange(ChangeRange(originalSpell, potentialSpell, increase).ToString());
                break;
            case "EffectShape":
                potentialSpell.SetShape(ChangeEffectShape(potentialSpell, increase).ToString());
                break;
            case "Span":
                potentialSpell.SetSpan(ChangeSpan(originalSpell, potentialSpell, increase).ToString());
                break;
        }
    }

    protected string ChangeRangeShape(MagicSpell spell, bool increase = true)
    {
        return utility.GetNextItemInList(spellBook.GetRangeShapes(), spell.GetRangeShape(), increase);
    }
    protected string ChangeEffectShape(MagicSpell spell, bool increase = true)
    {
        return utility.GetNextItemInList(spellBook.GetEffectShapes(), spell.GetShape(), increase);
    }
    protected int ChangeRange(MagicSpell original, MagicSpell potential, bool increase = true)
    {
        // Can only go +-1 at a time.
        int range = potential.GetRange();
        if (increase)
        {
            range++;
        }
        else
        {
            range--;
        }
        int originalRange = original.GetRange();
        if (range > originalRange + 1)
        {
            range = originalRange - 1;
        }
        else if (range < originalRange - 1)
        {
            range = originalRange + 1;
        }
        if (range < 0){ range = 0; }
        return range;
    }
    protected int ChangeSpan(MagicSpell original, MagicSpell potential, bool increase = true)
    {
        // Can only go +-1 at a time.
        int span = potential.GetSpan();
        if (increase)
        {
            span++;
        }
        else
        {
            span--;
        }
        int originalSpan = original.GetSpan();
        if (span > originalSpan + 1)
        {
            span = originalSpan - 1;
        }
        else if (span < originalSpan - 1)
        {
            span = originalSpan + 1;
        }
        if (span < 0){ span = 0; }
        return span;
    }
}
