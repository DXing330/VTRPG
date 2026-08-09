using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Can be used to inject different strategies into the gene pool and force AI to tunnel on a single faction.
[CreateAssetMenu(fileName = "AutoChessPVPGenomeTemplate", menuName = "ScriptableObjects/AutoChessGeneTemplates/AutoChessPVPGenomeTemplate", order = 1)]
public class AutoChessPVPGenomeTemplate : ScriptableObject
{
    //public readonly HashSet<string> autoChessMainFactions = new(){"Aegir", "Kjerag", "Laterano", "Sargon", "Victoria", "Yan"};
    public string templateName;
    public string preferredFaction;
    public bool lockFaction;
    public AutoChessPVPGenome geneTemplate;
    [ContextMenu("Debug Gene Weights")]
    public void DebugGeneWeights()
    {
        string genesAndWeights = "";
        for (int i = 0; i < geneTemplate.genes.Length; i++)
        {
            genesAndWeights += geneTemplate.GetGeneNameAtIndex(i) + ":" + geneTemplate.genes[i] + "\n";
        }
        Debug.Log(genesAndWeights);
    }
    public AutoChessPVPGenome CreateGenome()
    {
        AutoChessPVPGenome genome = geneTemplate.Copy();
        genome.SetGenePool(templateName);
        genome.SetPreferredFaction(preferredFaction);
        return genome;
    }
}
