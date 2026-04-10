using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectStatTextList : StatTextList
{
    public Equipment equipment;
    public ColorDictionary colors;
    public bool highlights = true;
    public void PublicResetPage()
    {
        ResetPage();
    }
    public void ResetTextText()
    {
        for (int i = 0; i < statTexts.Count; i++)
        {
            statTexts[i].SetText("");
        }
    }
    protected override void ResetPage()
    {
        base.ResetPage();
        ResetHighlights();
    }
    public int GetPage()
    {
        return page;
    }
    public void SetPage(int newPage)
    {
        page = newPage;
        UpdateCurrentPage();
    }
    // These are specifically for the passive viewer.
    public bool useComparisons = false;
    public List<string> comparisons;
    protected override void UpdateCurrentPage()
    {
        ResetPage();
        List<int> newPageIndices = new List<int>(utility.GetCurrentPageIndices(page, objects, data));
        for (int i = 0; i < newPageIndices.Count; i++)
        {
            objects[i].SetActive(true);
            statTexts[i].SetStatText(stats[newPageIndices[i]]);
            statTexts[i].SetText(data[newPageIndices[i]]);
            if (useComparisons)
            {
                statTexts[i].SetColor(colors.GetColor(comparisons[i+(page*objects.Count)]));
            }
        }
    }
    public void ResetHighlights()
    {
        for (int i = 0; i < statTexts.Count; i++)
        {
            statTexts[i].SetColor(colors.GetColor("Default"));
        }
    }
    public void HighlightIndex(int index, string color = "Highlight")
    {
        statTexts[index%objects.Count].SetColor(colors.GetColor(color));
    }
    public void HighlightSelected(string color = "Highlight")
    {
        ResetHighlights();
        if (!highlights){ return; }
        if (GetSelected() < 0) { return; }
        statTexts[GetSelected()%statTexts.Count].SetColor(colors.GetColor(color));
    }
    public int selectedIndex = -1;
    public void SetSelected(int index){ selectedIndex = index; }
    public void ResetSelected()
    {
        selectedIndex = -1;
        ResetHighlights();
    }
    public void Select(int index)
    {
        selectedIndex = index + (page*objects.Count);
        HighlightSelected();
    }
    public int GetSelected(){return selectedIndex;}
    public string GetSelectedStat()
    {
        return stats[selectedIndex];
    }
    public string GetSelectedData(){return data[selectedIndex];}
    public string GetCurrentPageStat(int index)
    {
        if (objects[index].activeSelf)
        {
            return statTexts[index].GetStatText();
        }
        return "";
    }
    // This is more specialized but placed here for now.
    public void UpdateActorStatTexts(TacticActor actor, bool currentHealth = false)
    {
        DisableChangePage();
        page = 0;
        stats.Clear();
        data.Clear();
        stats.Add("Health");
        stats.Add("Attack");
        stats.Add("Defense");
        stats.Add("Energy");
        stats.Add("Move Speed");
        stats.Add("Attack Range");
        stats.Add("Initiative");
        stats.Add("Weight");
        if (!currentHealth)
        {
            data.Add(actor.GetBaseHealth().ToString());
        }
        else
        {
            data.Add(actor.GetHealth() + "/" + actor.GetBaseHealth());
        }
        data.Add(actor.GetBaseAttack().ToString());
        data.Add(actor.GetBaseDefense().ToString());
        data.Add(actor.GetBaseEnergy().ToString());
        data.Add(actor.GetMoveSpeed().ToString());
        data.Add(actor.GetAttackRange().ToString());
        data.Add(actor.GetInitiative().ToString());
        data.Add(actor.GetBaseWeight().ToString());
        for (int i = 0; i < Mathf.Min(stats.Count, statTexts.Count); i++)
        {
            objects[i].SetActive(true);
            statTexts[i].SetStatText(stats[i]);
            statTexts[i].SetText(data[i]);
        }
    }
    public void UpdateActorSpriteStats(TacticActor actor)
    {
        DisableChangePage();
        page = 0;
        stats.Clear();
        data.Clear();
        stats.Add("Species");
        stats.Add("Element");
        stats.Add("Attributes");
        stats.Add("Movement Type");
        data.Add(actor.GetSpecies());
        data.Add(actor.GetElementString());
        data.Add(actor.GetAttributeString());
        data.Add(actor.GetMoveType());
        for (int i = 0; i < Mathf.Min(stats.Count, statTexts.Count); i++)
        {
            objects[i].SetActive(true);
            statTexts[i].SetStatText(stats[i]);
            statTexts[i].SetText(data[i]);
        }
    }
    public void UpdateActorPassiveTexts(TacticActor actor, string currentlyEquipped = "")
    {
        page = 0;
        stats.Clear();
        data.Clear();
        ResetPage();
        if (currentlyEquipped.Length > 0)
        {
            string[] allEquipped = currentlyEquipped.Split("@");
            for (int i = 0; i < allEquipped.Length; i++)
            {
                equipment.SetAllStats(allEquipped[i]);
                equipment.EquipToActor(actor);
            }
        }
        stats = new List<string>(actor.GetPassiveSkills());
        data = new List<string>(actor.GetPassiveLevels());
        comparisons = new List<string>();
        for (int i = 0; i < stats.Count; i++)
        {
            comparisons.Add("Default");
        }
        for (int i = 0; i < stats.Count; i++)
            {
                if (i >= objects.Count) { break; }
                objects[i].SetActive(true);
                statTexts[i].SetStatText(stats[i]);
                statTexts[i].SetText(data[i]);
            }
        if (stats.Count > objects.Count){EnableChangePage();}
    }
    public void UpdateActorEquipmentTexts(string selectedActorEquipment)
    {
        page = 0;
        data.Clear();
        // 3 types of equipment, weapon, armor, charm
        for (int i = 0; i < 6; i++)
        {
            statTexts[i].SetText("None");
        }
        string[] dataBlocks = selectedActorEquipment.Split("@");
        for (int i = 0; i < dataBlocks.Length; i++)
        {
            equipment.SetAllStats(dataBlocks[i]);
            switch (equipment.GetSlot())
            {
                case "Weapon":
                statTexts[0].SetText(equipment.GetName());
                break;
                case "Armor":
                statTexts[1].SetText(equipment.GetName());
                break;
                case "Charm":
                statTexts[2].SetText(equipment.GetName());
                break;
                case "Helmet":
                statTexts[3].SetText(equipment.GetName());
                break;
                case "Boots":
                statTexts[4].SetText(equipment.GetName());
                break;
                case "Gloves":
                statTexts[5].SetText(equipment.GetName());
                break;
            }
        }
    }

    // raw data is full equip stats, just extract the names.
    public void UpdateEquipNames()
    {
        stats.Clear();
        for (int i = 0; i < data.Count; i++)
        {
            string[] splitData = data[i].Split("|");
            stats.Add(splitData[0]);
        }
        page = 0;
        UpdateStatPortion();
    }
    public void UpdatePotentialPassives(TacticActor actor, string currentEquipment, string newEquipment)
    {
        // Keep track of the base actor passive stats.
        List<string> basePassives = new List<string>(actor.GetPassiveSkills());
        List<string> baseLevels = new List<string>(actor.GetPassiveLevels());
        // Equipped the current equipment.
        string[] allEquipped = currentEquipment.Split("@");
        for (int i = 0; i < allEquipped.Length; i++)
        {
            equipment.SetAllStats(allEquipped[i]);
            equipment.EquipToActor(actor);
        }
        // Keep track of current stats.
        List<string> currentPassives = new List<string>(actor.GetPassiveSkills());
        List<string> currentLevels = new List<string>(actor.GetPassiveLevels());
        // Reset to base stats.
        actor.SetPassiveSkills(basePassives);
        actor.SetPassiveLevels(baseLevels);
        equipment.SetAllStats(newEquipment);
        equipment.EquipToActor(actor);
        string slot = equipment.GetSlot();
        // Replace the equipment in the specified slot with the new equipment.
        for (int i = 0; i < allEquipped.Length; i++)
        {
            equipment.SetAllStats(allEquipped[i]);
            if (slot == equipment.GetSlot())
            {
                continue;
            }
            equipment.EquipToActor(actor);
        }
        List<string> potentialPassives = new List<string>(actor.GetPassiveSkills());
        List<string> potentialLevels = new List<string>(actor.GetPassiveLevels());
        // Generate a list will all skills.
        List<string> allPassives = new List<string>(potentialPassives);
        allPassives.AddRange(currentPassives.Except(potentialPassives));
        List<string> allPassiveLevels = new List<string>();
        string passiveName = "";
        int indexOf = -1;
        for (int i = 0; i < allPassives.Count; i++)
        {
            passiveName = allPassives[i];
            indexOf = potentialPassives.IndexOf(passiveName);
            if (indexOf < 0)
            {
                allPassiveLevels.Add("0");
                continue;
            }
            allPassiveLevels.Add(potentialLevels[indexOf]);
        }
        // Sort the list by passive levels.
        allPassives = utility.QuickSortByIntStringList(allPassives, allPassiveLevels, 0, allPassives.Count - 1);
        //allPassiveLevels = utility.QuickSortIntStringList(allPassiveLevels, 0, allPassiveLevels.Count - 1);
        // Compare the levels to the previous levels.
        comparisons = new List<string>();
        int potentialLevel = 0;
        int currentLevel = 0;
        for (int i = 0; i < allPassives.Count; i++)
        {
            passiveName = allPassives[i];
            indexOf = currentPassives.IndexOf(passiveName);
            if ((indexOf < 0))
            {
                comparisons.Add("Increase");
                continue;
            }
            potentialLevel = int.Parse(allPassiveLevels[i]);
            currentLevel = int.Parse(currentLevels[indexOf]);
            if (potentialLevel == currentLevel)
            {
                comparisons.Add("Default");
            }
            else if (potentialLevel > currentLevel)
            {
                comparisons.Add("Increase");
            }
            else
            {
                comparisons.Add("Decrease");
            }
        }
        // Display the levels.
        stats = new List<string>(allPassives);
        data = new List<string>(allPassiveLevels);
        ResetPage();
        for (int i = 0; i < allPassives.Count; i++)
        {
            if (i >= objects.Count){ break; }
            objects[i].SetActive(true);
            statTexts[i].SetStatText(allPassives[i]);
            statTexts[i].SetText(allPassiveLevels[i]);
            statTexts[i].SetColor(colors.GetColor(comparisons[i]));
        }
    }
}
