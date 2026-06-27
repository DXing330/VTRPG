using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Shows All Factions, Sorting By Stack Count.
// Show Stack Count Of Each Faction And Whether It's Active Or Not.
public class AutoChessFactionDisplay : MonoBehaviour
{
    public GeneralUtility utility;
    public AutoChessFactionDataManager factionData;
    public SpriteContainer factionIcons;
    public List<GameObject> factionDisplayObjects;
    public List<RectTransform> rectTiles;
    public RectTransformAdjustor adjustor;
    public List<AutoChessFactionSlot> factionSlots;
    public List<string> activeFactionNames;
    public List<string> activeFactionStacks;
    public List<string> inactiveFactionNames;
    public List<string> inactiveFactionStacks;
    public void ResetDisplay()
    {
        for (int i = 0; i < factionDisplayObjects.Count; i++)
        {
            factionDisplayObjects[i].SetActive(false);
        }
    }
    // Show All Factions With Units And Their Stacks.
    // Sort By Active -> Stack Count.
    public void UpdateFactionDisplay(List<string> allActiveFactions, List<string> allFactionsWithUnits)
    {
        ResetDisplay();
        activeFactionNames = new List<string>(allActiveFactions);
        inactiveFactionNames = new List<string>(allFactionsWithUnits);
        activeFactionStacks.Clear();
        for (int i = 0; i < activeFactionNames.Count; i++)
        {
            activeFactionStacks.Add(factionData.GetStacksOfFaction(activeFactionNames[i]));
            inactiveFactionNames.Remove(activeFactionNames[i]);
        }
        utility.QuickSortByIntStringList(activeFactionNames, activeFactionStacks, 0, activeFactionNames.Count - 1);
        // Update Active Factions.
        for (int i = 0; i < activeFactionNames.Count; i++)
        {
            factionDisplayObjects[i].SetActive(true);
            factionSlots[i].UpdateSlot(factionIcons.SpriteDictionary(activeFactionNames[i]), activeFactionStacks[i], true);
        }
        // Update Inactive Factions.
        inactiveFactionStacks.Clear();
        for (int i = 0; i < inactiveFactionNames.Count; i++)
        {
            inactiveFactionStacks.Add(factionData.GetStacksOfFaction(inactiveFactionNames[i]));
        }
        utility.QuickSortByIntStringList(inactiveFactionNames, inactiveFactionStacks, 0, inactiveFactionNames.Count - 1);
        // Update Other Factions.
        for (int i = activeFactionNames.Count; i < activeFactionNames.Count + inactiveFactionNames.Count; i++)
        {
            factionDisplayObjects[i].SetActive(true);
            factionSlots[i].UpdateSlot(factionIcons.SpriteDictionary(inactiveFactionNames[i - activeFactionNames.Count]), factionData.GetStacksOfFaction(inactiveFactionNames[i - activeFactionNames.Count]), false);
        }
        // Update The Spacing.
        List<RectTransform> rectsToAdjust = new List<RectTransform>();
        for (int i = 0; i < activeFactionNames.Count + inactiveFactionNames.Count; i++)
        {
            rectsToAdjust.Add(rectTiles[i]);
        }
        adjustor.SetRectTiles(rectsToAdjust);
        adjustor.Initialize();
    }
    public void ClickFactionToolTipButton(int index)
    {
        if (index < 0){return;}
        // Get The Faction Clicked.
        string factionToolTipText = "";
        string factionName = "";
        string factionStacks = "";
        if (index < activeFactionNames.Count)
        {
            factionName = activeFactionNames[index];
            factionStacks = activeFactionStacks[index];
            factionToolTipText = factionName + " (Active)";
        }
        else
        {
            factionName = inactiveFactionNames[index - activeFactionNames.Count];
            factionStacks = inactiveFactionStacks[index - activeFactionNames.Count];
            factionToolTipText = factionName + " (Inactive)";
        }
        factionToolTipText += "\n" + ReturnFactionDescription(factionName, factionStacks);
        // Display The Name, Effect and Flavor.
        factionSlots[index].ShowTooltip(factionToolTipText);
    }
    public string ReturnFactionDescription(string factionName, string factionStacks)
    {
        int stacks = int.Parse(factionStacks);
        switch (factionName)
        {
            default:
            return "???";
            // MAIN
            case "Harmony":
            return "Harmony Faction allies count as Aegir, Kjerag, Laterano, Sargon, Victoria and Yan Faction allies.";
            case "Assist":
            return "All allies gain +30% HP and +30% ATK. Doubled for upgraded allies.";
            case "Aegir":
            return "At the start of battle, Aegir units consume the unit in front, absorbing their HP/ATK. All Aegir units have {+" + (stacks * 2 + 30) + "%} health. If there are 5+ Aegir Faction allies, then the first 3 Aegir Faction allies defeated will immediately be revived.";
            case "Kjerag":
            return "Kjerag allies deal 130% damage, increased to {" + (130 + stacks) + "%} when attacking enemies that have cold or frozen status. If there are 6+ Kjerag Faction allies, then every {" + (Mathf.Max(1, 6 / Mathf.Max(1, (stacks / 100)))).ToString() + "} rounds, apply [Cold] status to all enemies.";
            // Increase buff duration of Laterano skills?
            case "Laterano":
            return "After Laterano allies use a skill, the next {" + (1 + (stacks / 60)) + "} attacks deal double damage. Additionally, if there are 6+ Laterano Faction allies, then whenever a Laterano unit uses a skill all Laterano Faction allies gain +10% attack (up to 300%)";
            case "Victoria":
            return "Victoria allies deal {" + (130 + stacks) + "%} damage when holding 1+ equipment. If there are 6+ Victoria Faction allies, then all Victoria Faction allies gain +80% attack.";
            case "Yan":
            return "Yan allies gain {+" + (30 + stacks) + "%} attack. If there are 6+ Yan Faction allies, then summon an additional powerful Yan ally.";
            case "Sargon":
            return "After a Sargon ally uses a skill, all Sargon Faction allies gain {+" + (stacks) + "%} attack speed. If there are 6+ Sargon Faction allies, then additionally gain {+" + (stacks) + "%} attack.";
            // SUB Damage
            case "Agile":
            return "Agile units and adjacent allies gain {+" + (stacks + 10) + "%} attack speed.";
            case "Precision":
            return "Ranged units gain {+" + (stacks + 20) + "%} attack. If there are 3+ Precision Faction allies, then ranged units gain 30% defense penetration.";
            case "Swift":
            return "After using a skill, swift units have a {+" + ((100 * stacks) / (50 + stacks)) + "%} chance to restore 2 energy. At 40+ stacks, all units have the same chance to restore 1 energy.";
            case "Raid":
            return "Raid allies gain {+" + (stacks + 30) + "%} attack, while having a buff and will redeploy to an empty tile closest to an enemy if no enemy is in range during their skill activation.";
            // SUB Support
            case "Resilient":
            return "Melee units have {~" + ((100 * stacks) / (50 + stacks)) + "%} chance to be instantly revived upon being defeated.";
            case "Durable":
            return "All allies gain {+" + (35 + stacks) + "%} health. If there are 3+ Durable Faction allies, then all durable allies gain [Guard] and will attempt to block attacks for nearby allies.";
            case "Aid":
            return "All allies gain {+" + (35 + stacks) + "%} defense. At the start of each battle, all active factions gain 2 stacks. If there are 3+ Aid Faction allies, then instead gain 4 stacks.";
            // ECON
            case "Marvel":
            return "When rerolling, there is {~" + ((100 * stacks) / (100 + stacks)) + "%} chance to gain 1 gold. Every 50 stacks, gain 10 gold.";
            case "Foresight":
            return "Every 10 stacks, gain 2 gold. At 100+ stacks, shop units cost -1 gold.";
            case "Investor":
            return "Double all [When Obtained] trait effects. At 70+ stacks, triple the effects.";
        }
    }
}
