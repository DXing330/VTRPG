using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SpellStore : MonoBehaviour
{
    /*
    void Start()
    {
        partyData.SetFullParty();
        CheckIfCastersInParty();
        UpdateCurrency();
        spellStoreOptionsObject.SetActive(false);
    }
    public GeneralUtility utility;
    public PartyDataManager partyData;
    public string baseSpellStats;
    public StatDatabase spellNamePrices;
    public StatDatabase spellNameManaCosts;
    public StatDatabase spellNameDetails;
    public SpellBook spellBook;
    public CharacterList spellCasterList;
    public TacticActor dummyActor;
    public MagicSpell dummySpell;
    public MagicSpell secondDummySpell;
    public MagicSpell previewSpell;
    public List<string> spellComponents;
    public List<int> spellComponentCosts;
    public TMP_Text spellSlots;
    public void UpdateSpellSlots()
    {
        spellSlots.text = dummyActor.GetSpells().Count + "/" + spellBook.ReturnActorSpellSlots(dummyActor);
    }
    public bool OpenSpellSlots()
    {
        bool openSpellSlots = dummyActor.GetSpells().Count < spellBook.ReturnActorSpellSlots(dummyActor);
        if (!openSpellSlots)
        {
            ResetState();
            NoSpellSlotError();
        }
        return openSpellSlots;
    }
    public TMP_Text tomes;
    public string tomeString = "Tome";
    public TMP_Text mana;
    public string manaString = "Mana";
    public int practiceToMasteryIterations = 6;
    public void UpdateCurrency()
    {
        tomes.text = partyData.inventory.ReturnQuantityOfItem(tomeString).ToString();
        mana.text = partyData.inventory.ReturnQuantityOfItem(manaString).ToString();
    }
    public GameObject casterSelectObject;
    public ActorSpriteHPList partyCasters;
    public GameObject noCasterErrorObject;
    public List<string> casterNames;
    public List<string> casterStats;
    // Track the indexes so you know which party member to upgrade when adding spells.
    public List<int> casterPartyIndexes;
    public void CheckIfCastersInParty()
    {
        bool casters = false;
        casterStats = new List<string>();
        List<string> casterSprites = new List<string>();
        casterNames = new List<string>();
        List<string> characterStats = partyData.fullParty.GetCharacterStats();
        List<string> characterNames = partyData.fullParty.GetCharacterNames();
        for (int i = 0; i < characterStats.Count; i++)
        {
            dummyActor.SetInitialStatsFromString(characterStats[i]);
            if (spellBook.ReturnActorSpellSlots(dummyActor) > 0)
            {
                casters = true;
                casterStats.Add(characterStats[i]);
                casterNames.Add(characterNames[i]);
                casterPartyIndexes.Add(i);
            }
        }
        if (casters)
        {
            spellCasterList.SetLists(casterSprites, casterStats, casterNames);
            casterSelectObject.SetActive(true);
            partyCasters.RefreshData();
            noCasterErrorObject.SetActive(false);
        }
        else
        {
            casterSelectObject.SetActive(false);
            noCasterErrorObject.SetActive(true);
        }
    }
    public int selectedCaster;
    public ActorSpriteAndName casterSpriteAndName;
    public GameObject spellStoreOptionsObject;
    public PopUpMessage errorPopUp;
    protected void NoSpellSlotError()
    {
        errorPopUp.SetMessage("No Spell Slots Available");
        ResetState();
    }
    protected void NoCurrencyError()
    {
        errorPopUp.SetMessage("Not Enough Resources");
        ResetState();
    }
    public void SelectCaster()
    {
        selectedCaster = partyCasters.GetSelected();
        dummyActor.SetInitialStatsFromString(partyCasters.GetSelectedData());
        dummyActor.SetPersonalName(partyCasters.GetSelectedName());
        spellStoreOptionsObject.SetActive(true);
        casterSpriteAndName.ShowActorInfo(dummyActor);
        UpdateSpellSlots();
    }
    public List<GameObject> storePanels;
    public int state = -1;
    public void ResetState()
    {
        utility.DisableGameObjects(storePanels);
    }
    public void SetState(int newInfo)
    {
        ResetState();
        if (state == newInfo || newInfo == -1)
        {
            state = -1;
            return;
        }
        state = newInfo;
        storePanels[state].SetActive(true);
    }
    public SelectList spellsList;
    protected List<string> currentActorSpells;
    public SpellDetailViewer spellDetails;
    public SpellTesterMap testMap;
    public void StartViewingSpells()
    {
        // Get the list of spells from the actor.
        // Get the spell names of each spell.
        currentActorSpells = new List<string>(dummyActor.GetSpells());
        spellsList.SetSelectables(dummyActor.GetSpellNames());
        spellDetails.ResetDetails();
    }
    public void ViewSelectedSpell()
    {
        int index = spellsList.GetSelected();
        dummySpell.LoadSkillFromString(currentActorSpells[index]);
        spellDetails.LoadSpell(dummySpell);
    }
    public void PreviewSelectedSpell()
    {
        testMap.ResetAll();
        testMap.SetActorSpriteName(dummyActor.GetSpriteName());
        testMap.LoadSpell(dummySpell.GetSkillInfo());
    }
    public void PreviewOriginalSpell()
    {
        testMap.ResetAll();
        testMap.SetActorSpriteName(dummyActor.GetSpriteName());
        int index = upgradeSpellList.GetSelected();
        dummySpell.LoadSkillFromString(currentActorSpells[index]);
        testMap.LoadSpell(dummySpell.GetSkillInfo());
    }
    public List<int> CalculateUpgradeCost()
    {
        List<int> costs = new List<int>();
        int baseCost = (int)Mathf.Sqrt(previewSpell.ReturnManaCost());
        costs.Add(baseCost);
        int manaCost = baseCost * practiceToMasteryIterations;
        costs.Add(manaCost);
        return costs;
    }
    public void PreviewSpellPotential()
    {
        testMap.ResetAll();
        spellComponentCosts = CalculateUpgradeCost();
        testMap.LearnSpellChance(spellComponentCosts[0].ToString(), spellComponentCosts[1].ToString());
        testMap.SetActorSpriteName(dummyActor.GetSpriteName());
        testMap.LoadSpell(previewSpell.GetSkillInfo());
    }
    public void PreviewNewSpell()
    {
        testMap.ResetAll();
        string spellName = previewSpell.GetSkillName();
        int manaCost = int.Parse(spellNameManaCosts.ReturnValue(spellName));
        testMap.LearnSpellChance(spellNamePrices.ReturnValue(spellName), (manaCost * practiceToMasteryIterations).ToString());
        testMap.SetActorSpriteName(dummyActor.GetSpriteName());
        testMap.LoadSpell(previewSpell.GetSkillInfo());
    }
    public SpellUpgradeManager upgradeManager;
    public SelectList upgradeSpellList;
    public SpellDetailViewer currentDetails;
    public SpellDetailViewer potentialDetails;
    public void StartUpgradingSpells()
    {
        if (!OpenSpellSlots()) { return; }
        currentActorSpells = new List<string>(dummyActor.GetSpells());
        upgradeSpellList.SetSelectables(dummyActor.GetSpellNames());
        currentDetails.ResetDetails();
        potentialDetails.ResetDetails();
    }
    public void SelectSpellToUpgrade()
    {
        int index = upgradeSpellList.GetSelected();
        dummySpell.LoadSkillFromString(currentActorSpells[index]);
        previewSpell.LoadSkillFromString(currentActorSpells[index]);
        currentDetails.LoadSpell(dummySpell);
        potentialDetails.LoadSpell(previewSpell);
    }
    public void ChangeSpellRangeShape(bool increase = true)
    {
        if (upgradeSpellList.GetSelected() < 0) { return; }
        upgradeManager.UpgradeSpell(dummySpell, previewSpell, "RangeShape", increase);
        potentialDetails.LoadSpell(previewSpell);
    }
    public void ChangeSpellRange(bool increase = true)
    {
        if (upgradeSpellList.GetSelected() < 0) { return; }
        upgradeManager.UpgradeSpell(dummySpell, previewSpell, "Range", increase);
        potentialDetails.LoadSpell(previewSpell);
    }
    public void ChangeSpellEffectShape(bool increase = true)
    {
        if (upgradeSpellList.GetSelected() < 0) { return; }
        upgradeManager.UpgradeSpell(dummySpell, previewSpell, "EffectShape", increase);
        potentialDetails.LoadSpell(previewSpell);
    }
    public void ChangeSpellSpan(bool increase = true)
    {
        if (upgradeSpellList.GetSelected() < 0) { return; }
        upgradeManager.UpgradeSpell(dummySpell, previewSpell, "Span", increase);
        potentialDetails.LoadSpell(previewSpell);
    }
    public void LearnUpgradedSpell()
    {
        // Get the cost of the spell.
        spellComponentCosts = CalculateUpgradeCost();
        // Try to pay the cost.
        if (!partyData.inventory.MultiQuantityExists(spellComponents, spellComponentCosts))
        {
            NoCurrencyError();
            return;
        }
        partyData.inventory.RemoveMultiItems(spellComponents, spellComponentCosts);
        // Get the spell info.
        previewSpell.RefreshSkillInfo();
        string newSpell = previewSpell.GetSkillInfo();
        // Get the actor from the party data.
        int partyIndex = casterPartyIndexes[selectedCaster];
        // Add the spell to them.
        partyData.AddSpellToPartyMember(newSpell, partyIndex);
        // Refresh the party.
        partyData.SetFullParty();
        CheckIfCastersInParty();
        UpdateSpellSlots();
        UpdateCurrency();
        ResetState();

    }
    public SelectList basicSpellList;
    public SpellDetailViewer basicSpellDetails;
    public void StartLearningSpells()
    {
        if (!OpenSpellSlots()) { return; }
        basicSpellList.SetSelectables(spellNamePrices.GetAllKeys());
        basicSpellDetails.ResetDetails();
    }
    public void SelectSpellToLearn()
    {
        int index = basicSpellList.GetSelected();
        string spellName = spellNameDetails.ReturnKeyAtIndex(index);
        string spellInfo = spellName + baseSpellStats + spellNameDetails.ReturnValue(spellName);
        Debug.Log(spellInfo);
        previewSpell.LoadSkillFromString(spellInfo);
        previewSpell.SetEnergyCost(spellNameManaCosts.ReturnValue(spellName));
        basicSpellDetails.LoadSpell(previewSpell);
    }
    public SelectList combineSpell1;
    public void SelectSpellToCombineOne()
    {
        if (combineSpell2.GetSelected() >= 0)
        {
            ShowCombinedSpell();
        }
    }
    public SelectList combineSpell2;
    public void SelectSpellToCombineTwo()
    {
        if (combineSpell1.GetSelected() >= 0)
        {
            ShowCombinedSpell();
        }
    }
    public SpellDetailViewer combinedSpellDetails;
    public void ShowCombinedSpell()
    {
        // Get the combined spells.
        dummySpell.LoadSkillFromString(currentActorSpells[combineSpell1.GetSelected()]);
        secondDummySpell.LoadSkillFromString(currentActorSpells[combineSpell2.GetSelected()]);
        spellBook.CombineSpells(dummySpell, secondDummySpell, previewSpell);
        combinedSpellDetails.LoadSpell(previewSpell);
    }
    public void StartCombiningSpells()
    {
        if (!OpenSpellSlots()) { return; }
        currentActorSpells = new List<string>(dummyActor.GetSpells());
        combineSpell1.SetSelectables(dummyActor.GetSpellNames());
        combineSpell2.SetSelectables(dummyActor.GetSpellNames());
        combinedSpellDetails.ResetDetails();
    }
    public NameRater nameRater;
    public SelectList renamingList;
    public SpellDetailViewer renamingDetails;
    public void StartRenamingSpells()
    {
        currentActorSpells = new List<string>(dummyActor.GetSpells());
        renamingList.SetSelectables(dummyActor.GetSpellNames());
        renamingDetails.ResetDetails();
    }
    public void SelectSpellToRename()
    {
        dummySpell.LoadSkillFromString(currentActorSpells[renamingList.GetSelected()]);
        renamingDetails.LoadSpell(dummySpell);
        nameRater.ResetNewName();
    }
    public void RenameSpell()
    {
        string newName = nameRater.ConfirmName();
        // Avoid blank names.
        if (newName == "") { return; }
        if (utility.CountCharactersInString(newName) == newName.Length) { return; }
        dummySpell.SetSkillName(newName);
        currentActorSpells[renamingList.GetSelected()] = dummySpell.GetSkillInfo();
        dummyActor.SetSpells(currentActorSpells);
        // Get the actor from the party data.
        int partyIndex = casterPartyIndexes[selectedCaster];
        // Set the dummyActor data to the right slot.
        partyData.UpdatePartyMember(dummyActor, partyIndex);
        // Refresh the party.
        partyData.SetFullParty();
        StartRenamingSpells();
    }
    */
}
