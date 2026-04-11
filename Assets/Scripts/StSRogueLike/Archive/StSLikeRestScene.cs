using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class StSLikeRestScene : MonoBehaviour
{
    public SceneMover sceneMover;
    public string StSMapSceneName = "StSMap";
    public PartyDataManager partyData;
    public TacticActor dummyActor;
    public ActorSpriteHPList actorSelectList;
    // TODO Step 1.
    public void SelectActor()
    {
        if (GetSelectedActorIndex() < 0){return;}
        restChoiceSelectObject.SetActive(true);
        restChoiceSelect.SetSelectedString(restChoices[GetSelectedActorIndex()]);
        actorChoiceObject.SetActive(true);
        UpdateActorChoiceText();
    }
    public List<string> possibleChoices;
    public List<bool> choiceAvailable;
    public GameObject restChoiceSelectObject;
    public SelectList restChoiceSelect;
    protected void UpdateAvailableChoices()
    {
        List<string> availableChoices = new List<string>();
        for (int i = 0; i < possibleChoices.Count; i++)
        {
            if (choiceAvailable[i]){availableChoices.Add(possibleChoices[i]);}
        }
        restChoiceSelect.SetSelectables(availableChoices);
    }
    public void SelectRestChoice()
    {
        if (restChoiceSelect.GetSelected() < 0){return;}
        restChoices[GetSelectedActorIndex()] = restChoiceSelect.GetSelectedString();
        switch (restChoices[GetSelectedActorIndex()])
        {
            // Resting goes here.
            default:
            UpdateActorChoiceText();
            break;
            // Have the training popup.
            case "Train":
            break;
            // Have another actor select popup.
            case "Mend":
            break;
        }
    }
    // ACTOR CHOICES.
    public List<string> restChoices;
    public List<string> restChoiceSpecifics;
    protected void InitializeChoices()
    {
        int actorCount = actorSelectList.allActorNames.Count;
        restChoices.Clear();
        restChoiceSpecifics.Clear();
        for (int i = 0; i < actorCount; i++)
        {
            restChoices.Add("");
            restChoiceSpecifics.Add("");
        }
    }
    public GameObject actorChoiceObject;
    public TMP_Text actorChoiceText;
    public TMP_Text actorChoiceSpecificsText;
    public void UpdateActorChoiceText()
    {
        actorChoiceText.text = restChoices[GetSelectedActorIndex()];
        switch (restChoices[GetSelectedActorIndex()])
        {
            default:
            actorChoiceSpecificsText.text = "";
            break;
        }
    }
    public StatDatabase activeData;
    public ActiveSkill previewActive;
    // On the home page, show a high level overview of the training chosen by each actor.
    public List<TMP_Text> restEffects;
    // Select a skill and popup the current skill stats and training options.
    public BasicSkillTrainingUI skillTraining;

    void Start()
    {
        // Load in the actors.
        partyData.Load();
        actorSelectList.RefreshData();
        // TODO determine options based on relics.
        // Initialize the choices available/selected.
        UpdateAvailableChoices();
        InitializeChoices();
    }

    protected int GetSelectedActorIndex()
    {
        return actorSelectList.GetSelected();
    }

    protected string GetChoiceDelimiter()
    {
        if (previewActive == null){return "_";}
        return previewActive.activeSkillDelimiter;
    }

    protected string BuildTrainSpecifics(string skillName, string upgradeType)
    {
        return skillName + GetChoiceDelimiter() + upgradeType;
    }

    protected bool TryParseTrainSpecifics(string specifics, out string skillName, out string upgradeType)
    {
        string[] blocks = specifics.Split(GetChoiceDelimiter());
        if (blocks.Length < 2)
        {
            skillName = "";
            upgradeType = "";
            return false;
        }
        skillName = blocks[0];
        upgradeType = blocks[1];
        return true;
    }

    protected string ReturnActorNameFromID(int actorID)
    {
        for (int i = 0; i < partyData.ReturnTotalPartyCount(); i++)
        {
            if (partyData.ReturnIDAtIndex(i) == actorID)
            {
                return partyData.GetAllPartyNames()[i];
            }
        }
        return actorID.ToString();
    }

    protected int ReturnActorIndexFromID(int actorID)
    {
        for (int i = 0; i < partyData.ReturnTotalPartyCount(); i++)
        {
            if (partyData.ReturnIDAtIndex(i) == actorID)
            {
                return i;
            }
        }
        return -1;
    }

    protected void RestoreActor(TacticActor actor)
    {
        List<string> curses = new List<string>(actor.GetCurses());
        actor.SetCurrentHealth(actor.GetBaseHealth());
        actor.SetMana(actor.GetMaxMana());
        actor.ClearStatuses();
        for (int i = 0; i < curses.Count; i++)
        {
            actor.AddStatus(curses[i], -1);
        }
    }

    protected void ApplyTraining(TacticActor actor, string specifics)
    {
        string skillName;
        string upgradeType;
        if (!TryParseTrainSpecifics(specifics, out skillName, out upgradeType))
        {
            return;
        }
        actor.AddActiveMod(skillName, upgradeType, GetChoiceDelimiter());
    }

    public void FinishRest()
    {
        // TODO 
        partyData.Save();
        sceneMover.LoadScene(StSMapSceneName);
    }
}
