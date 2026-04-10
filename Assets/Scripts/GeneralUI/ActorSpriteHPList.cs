using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActorSpriteHPList : MonoBehaviour
{
    public bool startUp = true;
    public bool spritesNameDisplay = true;
    public GeneralUtility utility;
    public ColorDictionary colors;
    public int page = 0;
    protected void DisableChangePage() { utility.DisableGameObjects(changePageObjects); }
    protected void EnableChangePage() { utility.EnableGameObjects(changePageObjects); }
    public void ChangePage(bool right = true)
    {
        page = utility.ChangePage(page, right, objects, allActorNames);
        ResetSelected();
        UpdateList();
    }
    public void SetPage(int newInfo)
    {
        page = newInfo;
        ResetSelected();
        UpdateList();
    }
    public void ResetSelected()
    {
        selectedIndex = -1;
        ResetHighlights();
    }
    protected void ResetHighlights()
    {
        for (int i = 0; i < actors.Count; i++)
        {
            actors[i].ChangeTextColor(colors.GetColor("Default"));
        }
    }
    protected void HighlightSelected()
    {
        ResetHighlights();
        if (GetSelected() < 0) { return; }
        actors[GetSelected() % objects.Count].ChangeTextColor(colors.GetColor("Highlight"));
    }
    public List<GameObject> objects;
    public List<ActorSprite> actors;
    public int textSize;
    [ContextMenu("UpdateTextSize")]
    public void UpdateTextSize()
    {
        for (int i = 0; i < actors.Count; i++)
        {
            actors[i].SetTextSize(textSize);
        }
    }
    public List<GameObject> changePageObjects;
    public TacticActor dummyActor;
    public int selectedIndex = -1;
    public void SetSelectedIndex(int newInfo)
    {
        selectedIndex = newInfo;
        HighlightSelected();
    }
    public CharacterList savedData;
    public List<string> allActorNames;
    public List<string> allActorData;
    public List<string> allActorSpriteNames;
    public List<string> actorNames;
    public List<string> actorSpriteNames;
    public List<string> actorData;

    void Start()
    {
        if (startUp)
        {
            RefreshData();
        }
    }

    public void RefreshData()
    {
        page = 0;
        allActorNames = new List<string>(savedData.characterNames);
        allActorSpriteNames = new List<string>(savedData.characters);
        allActorData = new List<string>(savedData.stats);
        ResetSelected();
        UpdateList();
    }

    public void SetData(List<string> spriteNames, List<string> characterNames, List<string> stats)
    {
        page = 0;
        allActorNames = new List<string>(characterNames);
        allActorSpriteNames = new List<string>(spriteNames);
        allActorData = new List<string>(stats);
        ResetSelected();
        UpdateList();
    }

    public void UpdateList()
    {
        utility.DisableGameObjects(objects);
        DisableChangePage();
        actorNames = utility.GetCurrentPageStrings(page, objects, allActorNames);
        actorSpriteNames = utility.GetCurrentPageStrings(page, objects, allActorSpriteNames);
        actorData = utility.GetCurrentPageStrings(page, objects, allActorData);
        for (int i = 0; i < actorNames.Count; i++)
        {
            objects[i].SetActive(true);
            dummyActor.SetPersonalName(actorNames[i]);
            dummyActor.SetInitialStatsFromString(actorData[i]);
            actors[i].ShowActorInfo(dummyActor);
        }
        if (allActorNames.Count > objects.Count)
        {
            EnableChangePage();
        }
    }

    public void SelectActor(int index)
    {
        selectedIndex = index + (page * objects.Count);
        HighlightSelected();
    }

    public int GetSelected()
    {
        return selectedIndex;
    }

    public string GetSelectedData()
    {
        return allActorData[selectedIndex];
    }

    public string GetSelectedName()
    {
        return allActorNames[selectedIndex];
    }
}
