using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpellBookTester : MonoBehaviour
{
    public SpellBook spellBook;
    public TacticActor dummyActor;
    public int testAmount = 6;
    public int testEffectCount = 6;

    [ContextMenu("Random Spell")]
    public void TestRandomSpell()
    {
        for (int i = 0; i < testAmount; i++)
        {
            string spell = spellBook.ReturnRandomSpell(testEffectCount);
            Debug.Log(spell);
        }
    }

    [ContextMenu("Check Spell Slots")]
    public void TestSpellSlots()
    {
        Debug.Log(spellBook.ReturnActorSpellSlots(dummyActor));
    }
}
