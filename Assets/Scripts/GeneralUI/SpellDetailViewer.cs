using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SpellDetailViewer : ActiveDescriptionViewer
{
    public MagicSpell dummySpell;
    public StatDatabase divineSpellData;
    public string ReturnSpellDescriptionFromName(string activeName, TacticActor actor = null)
    {
        dummySpell.LoadSkillFromString(divineSpellData.ReturnValue(activeName), actor);
        return ReturnSpellDescription(dummySpell, actor);
    }
    public string spellData;
    public void SelectDivineSpell()
    {
        if (activeSelect.GetSelected() < 0){return;}
        dummySpell.LoadSkillFromString(divineSpellData.ReturnValue(activeSelect.GetSelectedStat()));
        popUp.SetMessage(ReturnSpellDescription(dummySpell));
    }
    public string testActiveName;
    [ContextMenu("Debug Active Description From Name")]
    public void ShowActiveDescriptionFromName()
    {
        spellEffects.text = ReturnActiveDescriptionFromName(testActiveName);
    }
    public string testActiveData;
    [ContextMenu("Debug Active Description From Data")]
    public void ShowActiveDescription()
    {
        dummyActive.LoadSkillFromString(testActiveData, null);
        spellEffects.text = ReturnActiveDescription(dummyActive);
    }
    [ContextMenu("Debug Spell Description")]
    public void ShowSpellDescription()
    {
        dummySpell.LoadSkillFromString(spellData);
        spellEffects.text = ReturnSpellDescription(dummySpell);
    }
    [ContextMenu("Debug Standard Spell Descriptions")]
    public void ShowStandardSpellDescriptions()
    {
        string spellDescriptions = "";
        List<string> spellValues = divineSpellData.values;
        for (int i = 0; i < spellValues.Count; i++)
        {
            dummySpell.LoadSkillFromString(spellValues[i]);
            spellDescriptions += ReturnSpellDescription(dummySpell) + "\n";
        }
        spellEffects.text = spellDescriptions;
        Debug.Log(spellDescriptions);
    }
    public List<string> spellStatNames;
    public TMP_Text spellEffects;
    public List<StatTextText> spellStats;
    public void ResetDetails()
    {
        spellEffects.text = "";
        for (int i = 0; i < spellStats.Count; i++)
        {
            spellStats[i].ResetText();
        }
    }
    public void LoadSpell(MagicSpell newSpell)
    {
        // Update all the spells stats;
        spellEffects.text = ReturnSpellDescription(newSpell);
        spellStats[0].SetText(newSpell.ReturnManaCost().ToString());
        for (int i = 1; i < spellStats.Count; i++)
        {
            spellStats[i].SetText(newSpell.GetStat(spellStatNames[i]));
        }
    }
}
