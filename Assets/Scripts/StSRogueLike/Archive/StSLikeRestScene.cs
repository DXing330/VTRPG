using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class StSLikeRestScene : MonoBehaviour
{
    public SceneMover sceneMover;
    public string StSMapSceneName;
    public int statGainPercentage = 10;
    public void FinishRest()
    {
        // Update stats based on chosen downtime activities.
        for (int i = 0; i < restingChoices.Count; i++)
        {
            dummyActor.SetInitialStatsFromString(partyData.ReturnPartyMemberStatsAtIndex(i));
            switch (restingChoices[i])
            {
                case "Rest":
                    dummyActor.UpdateHealth(dummyActor.GetBaseHealth() / 2, false);
                    dummyActor.ClearStatuses();
                    break;
                case "Health":
                    dummyActor.UpdateBaseHealth(Mathf.Max(1, dummyActor.GetBaseHealth() / statGainPercentage), false);
                    break;
                case "Attack":
                    dummyActor.UpdateBaseAttack(1);
                    break;
                case "Defense":
                    dummyActor.UpdateBaseDefense(1);
                    break;
                case "Energy":
                    dummyActor.UpdateBaseEnergy(1);
                    break;
            }
            partyData.UpdatePartyMember(dummyActor, i);
        }
        partyData.SetFullParty();
    }
    void Start()
    {
        restingChoices = new List<string>();
        for (int i = 0; i < partyData.ReturnTotalPartyCount(); i++)
        {
            restingChoices.Add("");
        }
    }
    public PartyDataManager partyData;
    public TacticActor dummyActor;
    public ActorSpriteHPList actorSelectList;
    public SelectStatTextList statList;
    // Keep track of what each party member does during the downtime.
    public List<string> restingChoices;
    public List<TMP_Text> restEffects;
    public void ResetRestEffects()
    {
        for (int i = 0; i < restEffects.Count; i++)
        {
            restEffects[i].text = "";
        }
    }
    public void UpdateRestingChoices(string newInfo)
    {
        int index = actorSelectList.GetSelected();
        if (index == -1) { return; }
        restingChoices[index] = newInfo;
        ResetRestEffects();
        UpdateRestingEffects();
    }
    public void UpdateRestingEffects()
    {
        int index = actorSelectList.GetSelected();
        if (index == -1) { return; }
        switch (restingChoices[index])
        {
            case "Rest":
                restEffects[0].text = "+ " + (dummyActor.GetBaseHealth() / 2).ToString();
                break;
            case "Health":
                restEffects[1].text = "+ " + (Mathf.Max(1, dummyActor.GetBaseHealth()/ statGainPercentage)).ToString();
                break;
            case "Attack":
                restEffects[2].text = "+ 1";
                break;
            case "Defense":
                restEffects[3].text = "+ 1";
                break;
            case "Energy":
                restEffects[4].text = "+ 1";
                break;
            
        }
    }
    public void ViewStats()
    {
        int index = actorSelectList.GetSelected();
        if (index == -1) { return; }
        ResetRestEffects();
        dummyActor.SetInitialStatsFromString(partyData.ReturnPartyMemberStatsAtIndex(index));
        List<string> stats = new List<string>();
        List<string> data = new List<string>();
        stats.Add("Current Health");
        stats.Add("Health");
        stats.Add("Attack");
        stats.Add("Defense");
        stats.Add("Energy");
        data.Add(dummyActor.GetHealth().ToString());
        data.Add(dummyActor.GetBaseHealth().ToString());
        data.Add(dummyActor.GetBaseAttack().ToString());
        data.Add(dummyActor.GetBaseDefense().ToString());
        data.Add(dummyActor.GetBaseEnergy().ToString());
        statList.SetStatsAndData(stats, data);
        UpdateRestingEffects();
    }
}
