using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StSEventScene : MonoBehaviour
{
    public string mainMapScene = "StSMap";
    public GeneralUtility utility;
    public PartyDataManager partyData;
    public int randomActorIndex = -1;
    public StSStateManager stsManager;
    public StSEventSaveDataManager eventData;
    public StSEventData currentEvent;
    public TMP_Text eventName;
    public TMP_Text eventDescription;
    public Image eventImage;
    public SpriteContainer eventSprites;
    public List<GameObject> choiceObjects;
    public List<TMP_Text> choiceTexts;
    public void ResetChoices()
    {
        utility.DisableGameObjects(choiceObjects);
    }
    // Load The Event.
    void Start()
    {
        GenerateEvent();
    }
    [ContextMenu("Test Generate Event")]
    protected void GenerateEvent()
    {
        currentEvent = eventData.GetRandomEvent();
        ApplyEvent();
    }
    protected void ApplyEvent()
    {
        // If Event Is Null Then Get Shop/Treasure/Battle And Move Scenes.
        if (currentEvent == null)
        {
            ResolveNullEvent();
            return;
        }
        // Else Display Like Normal.
        DisplayEvent();
    }
    protected void ResolveNullEvent()
    {
        int roll = eventData.eventRNGSeed.SeedRange(0, 6);
        if (roll == 0)
        {
            stsManager.MoveToTile("Treasure");
        }
        else if (roll == 1 || roll == 2)
        {
            stsManager.MoveToTile("Shop");
        }
        else
        {
            stsManager.MoveToTile("Enemy");
        }
    }
    protected void DisplayEvent()
    {
        ResetChoices();
        eventName.text = currentEvent.eventName;
        eventDescription.text = currentEvent.eventDescription;
        eventImage.sprite = eventSprites.SpriteDictionary(currentEvent.eventSpriteName);
        // For Each Choice, Display Choice Flavor + Effect.
        for (int i = 0; i < currentEvent.choices.Count; i++)
        {
            choiceObjects[i].SetActive(true);
            choiceTexts[i].text = currentEvent.choices[i].choiceText + "\n" + currentEvent.choices[i].choiceEffectDescription;
        }
    }
    public void SelectChoice(int index)
    {
        ResetChoices();
        randomActorIndex = -1;
        ApplyEffectChoice(index);
    }
    protected void ApplyEffectChoice(int choiceIndex)
    {
        StSEventChoice choice = currentEvent.ReturnChoiceAtIndex(choiceIndex);
        if (choice == null)
        {
            stsManager.ReturnToMap();
            return;
        }
        // TODO Handle Some Special Looping Events Later.
        bool battle = false;
        string battleMapType = "";
        string battleSpecifics = "";
        for (int i = 0; i < choice.choiceEffects.Count; i++)
        {
            StSEventEffect eventEffect = choice.choiceEffects[i];
            if (eventEffect.target == "Battle")
            {
                battle = true;
                battleMapType = eventEffect.effect;
                battleSpecifics = eventEffect.effectSpecifics;
            }
            else
            {
                ApplyStandardEffect(eventEffect);
            }
        }
        if (battle)
        {
            // TODO Set Up The Battle Then Move To It.
        }
        else
        {
            stsManager.ReturnToMap();
        }
    }
    protected void ApplyStandardEffect(StSEventEffect eventEffect)
    {
        switch (eventEffect.target)
        {
            case "None":
                break;
            case "Inventory":
                ApplyInventoryEffect(eventEffect);
                break;
            case "RandomActor":
                ApplyRandomActorEffect(eventEffect);
                break;
            case "AllActors":
                partyData.ApplyEffectToParty(eventEffect.effect, eventEffect.effectSpecifics, "1");
                break;
        }
    }
    protected void ApplyRandomActorEffect(StSEventEffect eventEffect)
    {
        // Only select a random actor once.
        if (randomActorIndex < 0)
        {
            randomActorIndex = eventData.eventRNGSeed.SeedRange(0, partyData.ReturnTotalPartyCount());
        }
        partyData.ApplyEffectToPartyMember(eventEffect.effect, eventEffect.effectSpecifics, "1", randomActorIndex);
    }
    // TODO: Add Consumables/Gold/Relics/Etc.
    protected void ApplyInventoryEffect(StSEventEffect eventEffect)
    {
        switch (eventEffect.effect)
        {
            case "Gold":
            stsManager.GainGold(utility.SafeParseInt(eventEffect.effectSpecifics));
            break;
            case "Relic":
            stsManager.GainRelic(eventEffect.effectSpecifics);
            break;
            case "Item":
            break;
        }
    }
}
