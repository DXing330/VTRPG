using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartGameManager : MonoBehaviour
{
    public PartyDataManager partyManager;
    public List<SavedData> gameData;
    public List<References> allReferences;
    public PartyDataManager roguelikeParty;
    public List<SavedData> roguelikeGameData;
    public StSState roguelikeState;
    public SceneMover roguelikeSceneMover;
    public PopUpMessage popUp;

    public void StartGame()
    {
        partyManager.Load();
        for (int i = 0; i < gameData.Count; i++)
        {
            gameData[i].Load();
        }
        for (int i = 0; i < allReferences.Count; i++)
        {
            allReferences[i].Load();
        }
    }

    public void StartRoguelike()
    {
        roguelikeParty.Load();
        for (int i = 0; i < roguelikeGameData.Count; i++)
        {
            roguelikeGameData[i].Load();
        }
        // Can't start if in new game state.
        // Have to start a new game through the new game button.
        if (roguelikeState.StartingNewGame())
        {
            popUp.SetMessage("No saved data to continue from.");
            return;
        }
        roguelikeSceneMover.StartGame();
    }

    public void NewRun()
    {
        roguelikeParty.OtherDataNewName();
        for (int i = 0; i < roguelikeGameData.Count; i++)
        {
            roguelikeGameData[i].NewGame();
        }
    }
}
