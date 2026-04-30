using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "SpriteContainerDuplicateChecker", menuName = "ScriptableObjects/Editor/SpriteContainerDuplicateChecker", order = 1)]
public class SpriteContainerDuplicateChecker : ScriptableObject
{
    public SpriteContainer mainContainer;

    [ContextMenu("Log Duplicate Sprite Names")]
    public void LogDuplicateSpriteNames()
    {
        if (mainContainer == null)
        {
            Debug.LogError("No main SpriteContainer assigned.", this);
            return;
        }

        List<string> reportLines = new List<string>();
        Dictionary<string, int> mainNames = SpriteNameCounts(mainContainer);
        AddInternalDuplicateReport(mainContainer.name, mainNames, reportLines);

        List<SpriteContainer> linkedContainers = mainContainer.linkedSpriteContainers;
        if (linkedContainers == null || linkedContainers.Count == 0)
        {
            Debug.Log(mainContainer.name+" has no linked sprite containers.", mainContainer);
            return;
        }

        for (int i = 0; i < linkedContainers.Count; i++)
        {
            SpriteContainer linkedContainer = linkedContainers[i];
            if (linkedContainer == null)
            {
                reportLines.Add("Linked container at index "+i+" is null.");
                continue;
            }
            if (linkedContainer == mainContainer)
            {
                reportLines.Add("Linked container at index "+i+" references the main container.");
            }

            Dictionary<string, int> linkedNames = SpriteNameCounts(linkedContainer);
            AddInternalDuplicateReport(linkedContainer.name, linkedNames, reportLines);
            AddOverlapReport(mainContainer.name, mainNames, linkedContainer.name, linkedNames, reportLines);
        }

        for (int i = 0; i < linkedContainers.Count; i++)
        {
            SpriteContainer leftContainer = linkedContainers[i];
            if (leftContainer == null){continue;}
            Dictionary<string, int> leftNames = SpriteNameCounts(leftContainer);
            for (int j = i + 1; j < linkedContainers.Count; j++)
            {
                SpriteContainer rightContainer = linkedContainers[j];
                if (rightContainer == null){continue;}
                AddOverlapReport(leftContainer.name, leftNames, rightContainer.name, SpriteNameCounts(rightContainer), reportLines);
            }
        }

        if (reportLines.Count == 0)
        {
            Debug.Log("No duplicate sprite names found for "+mainContainer.name+" and its linked sprite containers.", mainContainer);
            return;
        }

        Debug.LogWarning("Sprite duplicate check for "+mainContainer.name+":\n"+string.Join("\n", reportLines), mainContainer);
    }

    static Dictionary<string, int> SpriteNameCounts(SpriteContainer spriteContainer)
    {
        Dictionary<string, int> counts = new Dictionary<string, int>();
        if (spriteContainer == null || spriteContainer.sprites == null){return counts;}
        for (int i = 0; i < spriteContainer.sprites.Count; i++)
        {
            Sprite sprite = spriteContainer.sprites[i];
            if (sprite == null){continue;}
            if (!counts.ContainsKey(sprite.name))
            {
                counts.Add(sprite.name, 0);
            }
            counts[sprite.name]++;
        }
        return counts;
    }

    static void AddInternalDuplicateReport(string containerName, Dictionary<string, int> names, List<string> reportLines)
    {
        List<string> duplicates = names.Where(x => x.Value > 1).Select(x => x.Key).OrderBy(x => x).ToList();
        if (duplicates.Count == 0){return;}
        reportLines.Add(containerName+" has duplicate sprite references: "+string.Join(", ", duplicates));
    }

    static void AddOverlapReport(string leftName, Dictionary<string, int> leftNames, string rightName, Dictionary<string, int> rightNames, List<string> reportLines)
    {
        List<string> duplicates = leftNames.Keys.Where(x => rightNames.ContainsKey(x)).OrderBy(x => x).ToList();
        if (duplicates.Count == 0){return;}
        reportLines.Add(leftName+" overlaps "+rightName+": "+string.Join(", ", duplicates));
    }
}
