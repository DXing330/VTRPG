using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StSEventScene : MonoBehaviour
{
    public string mainMapScene = "StSMap";
    public GeneralUtility utility;
    public SceneMover sceneMover;
    public StSEvent stsEvent;
    public void DebugNewEvent()
    {
        stsEvent.ForceGenerate();
        DisplayEvent();
    }
    public PartyDataManager partyData;
    public BattleState battleState;
    public CharacterList enemyList;
    public TacticActor dummyActor;
    public Equipment dummyEquip;
    public TMP_Text eventName;
    public TMP_Text eventDescription;
    public Image eventImage;
    public SpriteContainer eventSprites;
    public List<GameObject> choiceObjects;
    public void ResetChoices()
    {
        utility.DisableGameObjects(choiceObjects);
    }
    public List<TMP_Text> eventChoices;
    public List<TMP_Text> choiceEffects;
    public List<TMP_Text> choiceDescriptions;
    public EventDescriptionViewer descriptionViewer;
    public GameObject actorSelect;
    public ActorSpriteHPList actorSelectList;

    void Start()
    {
        stsEvent.GenerateEvent(partyData);
        // Check if it's another scene.
        if (stsEvent.SceneChangeEvent() != "")
        {
            sceneMover.LoadScene(stsEvent.SceneChangeEvent());
            return;
        }
        // Check if it's a random battle.
        DisplayEvent();
    }

    public void DisplayEvent()
    {
        eventName.text = stsEvent.GetEventName();
        eventImage.sprite = eventSprites.SpriteDictionary(stsEvent.GetEventName());
        eventDescription.text = stsEvent.GetEventDescription();
        ResetChoices();
        List<string> choices = stsEvent.GetChoices();
        for (int i = 0; i < choices.Count; i++)
        {
            choiceObjects[i].SetActive(true);
            string[] blocks = choices[i].Split("|");
            eventChoices[i].text = blocks[0];
            choiceEffects[i].text = descriptionViewer.ReturnEventDescription(blocks);
            choiceDescriptions[i].text = blocks[4];
        }
    }

    public void SelectChoice(int index)
    {
        stsEvent.SelectChoice(index);
        // Check if the choice requires selecting another choice.
        string choice = eventChoices[index].text;
        if (choice == "Choose Party Member")
        {
            // Choose a party member.
            actorSelect.SetActive(true);
            return;
        }
        // Check if the choice results in moving to a battle or some other special effect.
        else if (choice == "Battle")
        {
            // Get any rewards that you were promised at the start of battle.
            stsEvent.ApplyEventEffects(partyData);
            partyData.Save();
            // Set the terrain.
            battleState.ForceTerrainType(stsEvent.eventEffect[0]);
            // Set weather.
            // Set time.
            // Load the enemies.
            enemyList.ResetLists();
            List<string> allEnemies = new List<string>();
            for (int i = 0; i < stsEvent.eventSpecifics.Count; i++)
            {
                string[] enemyGroup = stsEvent.eventSpecifics[i].Split("*");
                // If no multiplier then it's a single enemy.
                if (enemyGroup.Length == 1)
                {
                    allEnemies.Add(enemyGroup[0]);
                    continue;
                }
                for (int j = 0; j < int.Parse(enemyGroup[1]); j++)
                {
                    allEnemies.Add(enemyGroup[0]);
                }
            }
            enemyList.SetLists(allEnemies);
            // Can we get the ascension stuff here?
            sceneMover.MoveToBattle();
            return;
        }
        // Else apply the regular effect.
        stsEvent.ApplyEventEffects(partyData);
        partyData.Save();
        sceneMover.LoadScene(mainMapScene);
    }

    public void SelectActor()
    {
        int index = actorSelectList.GetSelected();
        if (index < 0){ return; }
        // Eventually might do some 1v1 or special event battles?
        // Load the actor based on the partydata.
        stsEvent.ApplyEventEffects(partyData, partyData.ReturnActorAtIndex(index), index);
        partyData.Save();
        sceneMover.LoadScene(mainMapScene);
    }
}
