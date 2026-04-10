using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ActorAuraViewer : BattleUIBaseClass
{
    public GeneralUtility utility;
    public BattleMap map;
    public TacticActor auraActor;
    public override void SetActor(TacticActor newActor)
    {
        auraActor = newActor;
        auraIndex = 0;
        actorAuras = map.ReturnActorAuras(auraActor);
        UpdateUI();
    }
    public PassiveDetailViewer detailViewer;
    public TMP_Text auraDescription;
    public override void ResetUI()
    {
        auraDescription.text = "";   
    }
    public List<AuraEffect> actorAuras;
    public int auraIndex = 0;
    public void ChangeIndex(bool right = true)
    {
        if (actorAuras.Count <= 1){return;}
        auraIndex = utility.ChangeIndex(auraIndex, right, actorAuras.Count - 1, 0);
        UpdateUI();
        HighlightAura();
    }
    public override void UpdateUI()
    {
        if (actorAuras.Count <= 0)
        {
            ResetUI();
            auraDescription.text = "No auras currently active.";
            return;
        }
        // Display the description.
        auraDescription.text = detailViewer.ReturnAuraDetails(actorAuras[auraIndex]);
    }
    public void HighlightAura()
    {
        if (actorAuras.Count <= 0)
        {
            map.ResetHighlights();
            return;
        }
        map.HighlightAura(actorAuras[auraIndex]);
    }
}
