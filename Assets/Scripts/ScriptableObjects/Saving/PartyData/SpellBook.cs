using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Also used to store skillbooks for the roguelike.
[CreateAssetMenu(fileName = "SpellBook", menuName = "ScriptableObjects/DataContainers/SavedData/SpellBook", order = 1)]
public class SpellBook : SavedData
{
    public List<string> books;
    public void GainBook(string bookName)
    {
        if (bookName.Length <= 1){return;}
        books.Add(bookName);
    }
    public void LoseBook(string bookName)
    {
        int indexOf = books.IndexOf(bookName);
        if (indexOf < 0){return;}
        books.RemoveAt(indexOf);
    }
    public void LoseBookAtIndex(int index)
    {
        if (index < 0 || index >= books.Count){return;}
        books.RemoveAt(index);
    }
    public void SetBooks(List<string> newBooks)
    {
        books.Clear();
        for (int i = 0; i < newBooks.Count; i++)
        {
            GainBook(newBooks[i]);
        }
    }
    public List<string> GetBooks(){return books;}
    public override void NewGame()
    {
        books.Clear();
        Save();
    }
    public override void Save()
    {
        dataPath = Application.persistentDataPath+"/"+filename;
        allData = "Books=" + String.Join(",", books);
        File.WriteAllText(dataPath, allData);
    }
    public override void Load()
    {
        dataPath = Application.persistentDataPath+"/"+filename;
        if (File.Exists(dataPath)){allData = File.ReadAllText(dataPath);}
        else
        {
            NewGame();
            return;
        }
        dataList = allData.Split(delimiter).ToList();
        for (int i = 0; i < dataList.Count; i++)
        {
            LoadStat(dataList[i]);
        }
    }
    public void LoadStat(string stat)
    {
        string[] statData = stat.Split("=");
        if (statData.Length < 2){return;}
        string key = statData[0];
        string value = statData[1];
        switch (key)
        {
            case "Books":
            SetBooks(value.Split(",").ToList());
            break;
        }
    }
    public StatDatabase spellComponents;
    public MagicSpell magicSpell;
    // Passive names that determine how many spells slots a character has.
    public List<string> spellSlotPassives;
    public int ReturnActorSpellSlots(TacticActor actor)
    {
        int slots = 0;
        for (int i = 0; i < actor.GetPassiveSkills().Count; i++)
        {
            string passiveName = actor.GetPassiveAtIndex(i);
            if (spellSlotPassives.Contains(passiveName))
            {
                slots += utility.SumDescending(actor.GetLevelFromPassive(passiveName));
            }
        }
        return slots;
    }
    public List<string> currentComponents;
    // Constants.
    // Action cost and energy default to 1 cost.
    public List<string> rangeShapes;
    public List<string> GetRangeShapes()
    {
        return rangeShapes;
    }
    public List<string> effectShapes;
    public List<string> GetEffectShapes()
    {
        return effectShapes;
    }
    public List<string> ranges;
    public List<string> GetRanges()
    {
        return ranges;
    }
    public List<string> spans;
    public List<string> GetSpans()
    {
        return spans;
    }
    public List<string> powers;

    public string ReturnRandomSpell(int effectCount = 3)
    {
        string activeDelimiter = magicSpell.activeSkillDelimiter;
        string effectDelimiter = magicSpell.effectDelimiter;
        string spell = "SpellName" + activeDelimiter + "1" + activeDelimiter + "1" + activeDelimiter;
        spell += ranges[UnityEngine.Random.Range(0, ranges.Count)] + activeDelimiter;
        spell += rangeShapes[UnityEngine.Random.Range(0, rangeShapes.Count)] + activeDelimiter;
        spell += effectShapes[UnityEngine.Random.Range(0, effectShapes.Count)] + activeDelimiter;
        spell += spans[UnityEngine.Random.Range(0, spans.Count)] + activeDelimiter;
        string effects = "";
        string specifics = "";
        string newPowers = "";
        for (int i = 0; i < effectCount; i++)
        {
            string eAndS = ReturnRandomEffectAndSpecifics();
            // split
            string[] blocks = eAndS.Split(activeDelimiter);
            effects += blocks[0];
            specifics += blocks[1];
            newPowers += powers[UnityEngine.Random.Range(0, powers.Count)];
            if (i < effectCount - 1)
            {
                effects += effectDelimiter;
                specifics += effectDelimiter;
                newPowers += effectDelimiter;
            }
        }
        spell += effects + activeDelimiter + specifics + activeDelimiter + newPowers + activeDelimiter;
        return spell;
    }

    public string ReturnRandomEffectAndSpecifics()
    {
        string eAndS = "";
        string effect = spellComponents.ReturnRandomKey();
        List<string> possibleSpecifics = spellComponents.ReturnStats(effect);
        string specifics = possibleSpecifics[UnityEngine.Random.Range(0, possibleSpecifics.Count)];
        eAndS = effect + magicSpell.activeSkillDelimiter + specifics;
        return eAndS;
    }

    public void CombineSpells(MagicSpell spell1, MagicSpell spell2, MagicSpell combinedSpell)
    {
        combinedSpell.LoadSkillFromString(spell1.GetSkillInfo());
        combinedSpell.AddEnergyCost(spell2.GetEnergyCost());
        combinedSpell.AddEffects(spell2.GetEffect());
        combinedSpell.AddSpecifics(spell2.GetSpecifics());
        combinedSpell.AddPowers(spell2.GetAllPowersString());
    }
}
