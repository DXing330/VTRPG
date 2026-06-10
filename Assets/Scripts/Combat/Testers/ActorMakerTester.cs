using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActorMakerTester : MonoBehaviour
{
    public BattleManager battleManager;
    [Header("Actor Settings")]
    public StatDatabase actorDB;
    public TacticActor dummyActor;
    public string actorSpriteName;
    [Header("Equipment Settings")]
    public List<string> actorEquipNames;
    public List<Equipment> actorEquip;
    protected void ResetTestEquipment()
    {
        for (int i = 0; i < actorEquip.Count; i++)
        {
            actorEquip[i].ResetStatsExceptSlot();
        }
    }
    [ContextMenu("Test Make Actor With Equipment")]
    public void TestMakeActorWithEquipment()
    {
        string equipmentString = "";
        for (int i = 0; i < actorEquip.Count; i++)
        {
            actorEquip[i].RefreshStats();
            equipmentString += actorEquip[i].GetStats();
            if (i < actorEquip.Count - 1)
            {
                equipmentString += "@";
            }
        }
        battleManager.actorMaker.SpawnActorWithEquipment(dummyActor, 0, actorSpriteName, 0, equipmentString);
    }
    public string testSet;
    public int testSetCount;
    [ContextMenu("Test Make Actor With Equipment Set")]
    public void TestMakeActorWithEquipmentSet()
    {
        for (int i = 0; i < actorEquip.Count; i++)
        {
            actorEquip[i].SetEquipSet("");
            if (i < testSetCount)
            {
                actorEquip[i].SetEquipSet(testSet);
            }
        }
        string equipmentString = "";
        for (int i = 0; i < actorEquip.Count; i++)
        {
            actorEquip[i].RefreshStats();
            equipmentString += actorEquip[i].GetStats();
            if (i < actorEquip.Count - 1)
            {
                equipmentString += "@";
            }
        }
        battleManager.actorMaker.SpawnActorWithEquipment(dummyActor, 0, actorSpriteName, 0, equipmentString);
    }
    public List<string> actorExtraPassives;
    public List<string> actorExtraPassiveLevels;
    [ContextMenu("Test Make Actor With Extra Passives")]
    public void TestMakeActorWithExtraPassives()
    {
        string equipmentString = "";
        for (int i = 0; i < actorEquip.Count; i++)
        {
            actorEquip[i].RefreshStats();
            equipmentString += actorEquip[i].GetStats();
            if (i < actorEquip.Count - 1)
            {
                equipmentString += "@";
            }
        }
        battleManager.actorMaker.SpawnActorWithEquipment(dummyActor, 0, actorSpriteName, 0, equipmentString);
        for (int i = 0; i < actorExtraPassives.Count; i++)
        {
            dummyActor.AddPassiveSkill(actorExtraPassives[i], actorExtraPassiveLevels[i]);
            battleManager.actorMaker.passiveOrganizer.OrganizeActorPassives(dummyActor);
        }
    }
    public StatDatabase runeLetters;
    public StatDatabase runeWords;
    public string GenerateRandomLetters(int length = 6)
    {
        string letters = "";
        for (int i = 0; i < length; i++)
        {
            letters += runeLetters.ReturnRandomValue();
        }
        if (runeWords.ReturnValue(letters).Length > 0)
        {
            return GenerateRandomLetters(length);
        }
        return letters;
    }
    public string GenerateRandomWord()
    {
        return runeWords.ReturnRandomValue();
    }
    // RUNE Testing
    [ContextMenu("Test Make Actor With Rune Grid")]
    public void TestApplyRuneGridToActor()
    {
        ResetTestEquipment();
        TestRowWithoutWord();
        TestRowWithWord();
        TestColumnWithoutWord();
        TestColumnWithWord();
        // Test Diagonal Without Word.
        // Test Diagonal With Word.
    }
    public void TestRowWithoutWord()
    {
        actorEquip[0].ResetStatsExceptSlot();
        List<string> expectedRunePassives = new List<string>();
        string randomLetters = GenerateRandomLetters();
        // Add The Appropriate Runes To The First Slot.
        for (int i = 0; i < randomLetters.Length; i++)
        {
            string runeName = runeLetters.ReturnKeyFromValue(randomLetters[i].ToString());
            expectedRunePassives.Add(runeName);
            actorEquip[0].DebugAddRune(runeName);
        }
        expectedRunePassives = expectedRunePassives.Distinct().ToList();
        TestMakeActorWithEquipmentSet();
        // Confirm That All Expected Rune Passives Exist.
        List<string> runePassives = dummyActor.GetRunePassives();
        for (int i = 0; i < expectedRunePassives.Count; i++)
        {
            if (!runePassives.Contains(expectedRunePassives[i]))
            {
                Debug.Log("TestRowWithoutWord Failed: " + expectedRunePassives[i] + " Not Found In Rune Passives");
                Debug.Log("Chosen Letters: " + randomLetters);
                return;
            }
        }
        Debug.Log("TestRowWithoutWord Passed");
    }
    public void TestRowWithWord()
    {
        actorEquip[0].ResetStatsExceptSlot();
         List<string> unexpectedRunePassives = new List<string>();
         string word = GenerateRandomWord();
         // Add The Appropriate Runes To The First Slot.
        for (int i = 0; i < word.Length; i++)
        {
            string runeName = runeLetters.ReturnKeyFromValue(word[i].ToString());
            unexpectedRunePassives.Add(runeName);
            actorEquip[0].DebugAddRune(runeName);
        }
        unexpectedRunePassives = unexpectedRunePassives.Distinct().ToList();
        TestMakeActorWithEquipmentSet();
        // Confirm That Only The Rune Word Passives Exist.
        List<string> runePassives = dummyActor.GetRunePassives();
        if (!runePassives.Contains(word))
        {
            Debug.Log("TestRowWithWord Failed: " + word + " Not Found In Rune Passives");
            Debug.Log("Chosen Word: " + word);
            return;
        }
        for (int i = 0; i < unexpectedRunePassives.Count; i++)
        {
            if (runePassives.Contains(unexpectedRunePassives[i]))
            {
                Debug.Log("TestRowWithWord Failed: " + unexpectedRunePassives[i] + " Found In Rune Passives");
                Debug.Log("Chosen Word: " + word);
                return;
            }
        }
        Debug.Log("TestRowWithWord Passed");
    }
    public void TestColumnWithoutWord()
    {
        ResetTestEquipment();
        List<string> expectedRunePassives = new List<string>();
        string randomLetters = GenerateRandomLetters();
        // Put one rune in column 0 of each equipment row.
        for (int row = 0; row < randomLetters.Length && row < actorEquip.Count; row++)
        {
            string runeName = runeLetters.ReturnKeyFromValue(randomLetters[row].ToString());
            expectedRunePassives.Add(runeName);
            actorEquip[row].DebugAddRune(runeName);
        }
        expectedRunePassives = expectedRunePassives.Distinct().ToList();
        TestMakeActorWithEquipmentSet();
        List<string> runePassives = dummyActor.GetRunePassives();
        for (int i = 0; i < expectedRunePassives.Count; i++)
        {
            if (!runePassives.Contains(expectedRunePassives[i]))
            {
                Debug.Log("TestColumnWithoutWord Failed: " + expectedRunePassives[i] + " Not Found In Rune Passives");
                Debug.Log("Chosen Letters: " + randomLetters);
                return;
            }
        }
        if (runePassives.Contains(randomLetters))
        {
            Debug.Log("TestColumnWithoutWord Failed: Column word was found unexpectedly: " + randomLetters);
            return;
        }
        Debug.Log("TestColumnWithoutWord Passed");
    }
    public void TestColumnWithWord()
    {
        ResetTestEquipment();
        List<string> expectedRunePassives = new List<string>();
        string word = GenerateRandomWord();
        // Put one rune in column 0 of each equipment row.
        for (int row = 0; row < word.Length && row < actorEquip.Count; row++)
        {
            string runeName = runeLetters.ReturnKeyFromValue(word[row].ToString());
            expectedRunePassives.Add(runeName);
            actorEquip[row].DebugAddRune(runeName);
        }
        expectedRunePassives = expectedRunePassives.Distinct().ToList();
        TestMakeActorWithEquipmentSet();
        List<string> runePassives = dummyActor.GetRunePassives();
        // Column word passive should exist.
        if (!runePassives.Contains(word))
        {
            Debug.Log("TestColumnWithWord Failed: " + word + " Not Found In Rune Passives");
            Debug.Log("Chosen Word: " + word);
            return;
        }
        // Individual rune passives should also exist, because rows did not form words.
        for (int i = 0; i < expectedRunePassives.Count; i++)
        {
            if (!runePassives.Contains(expectedRunePassives[i]))
            {
                Debug.Log("TestColumnWithWord Failed: " + expectedRunePassives[i] + " Not Found In Rune Passives");
                Debug.Log("Chosen Word: " + word);
                return;
            }
        }
        Debug.Log("TestColumnWithWord Passed");
    }
}
