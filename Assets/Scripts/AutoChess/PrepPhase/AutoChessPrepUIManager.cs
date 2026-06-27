using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AutoChessPrepUIManager : MonoBehaviour
{
    public AutoChessDataManager dataManager;
    public List<AutoChessBenchSlot> benchSlots;
    public List<MapTile> mapSlots;
    public Sprite castleSprite;
    public GameObject actorDisplayObject;
    public AutoActorDisplay actorDisplay;
    public GameObject sellActorObject;
    public GameObject rotateActorObject;
    public void ResetObjects()
    {
        actorDisplay.ResetDisplay();
        actorDisplayObject.SetActive(false);
        sellActorObject.SetActive(false);
        rotateActorObject.SetActive(false);
    }
    public void ActivateSellObject()
    {
        sellActorObject.SetActive(true);
    }
    public void ActivateRotateObject()
    {
        rotateActorObject.SetActive(true);
    }
    public void UpdateActorDisplay(AutoActorRollUpData actor)
    {
        UpdateActorDisplayByName(actor.GetName());
    }
    public void UpdateActorDisplayByName(string newName)
    {
        actorDisplayObject.SetActive(true);
        actorDisplay.DisplayActor(newName);
    }
    public TMP_Text levelText;
    public TMP_Text goldText;
    public TMP_Text castleHealthText;
    public TMP_Text deployLimitText;
    public void UpdateUI(AutoChessPrepManager prepManager)
    {
        ResetObjects();
        for (int i = 0; i < benchSlots.Count; i++)
        {
            benchSlots[i].ResetDisplay();
        }
        for (int i = 0; i < prepManager.benchSlots.Count; i++)
        {
            int benchIndex = prepManager.benchSlots[i].GetLocation();
            string benchSlotText = prepManager.benchSlots[i].GetName();
            benchSlotText += "\n";
            benchSlots[benchIndex].UpdateBenchSlot(prepManager.benchSlots[i].GetBaseStatString());
        }
        UpdateMap(prepManager);
        // TODO Update Faction Slots
        if (dataManager.MaxLevel())
        {
            levelText.text = "MAX";
        }
        else
        {
            levelText.text = dataManager.GetLevel().ToString() + "\n" + dataManager.GetExp() + "/" + dataManager.ExpToLevelUp();
        }
        goldText.text = dataManager.GetGold().ToString();
        castleHealthText.text = dataManager.GetHealth().ToString();
        deployLimitText.text = prepManager.fieldSlots.Count + "/" + (prepManager.GetMaxFieldSlots()).ToString();
    }
    public void UpdateMap(AutoChessPrepManager prepManager)
    {
        // Reset.
        for (int i = 0; i < mapSlots.Count; i++)
        {
            mapSlots[i].UpdateText();
            mapSlots[i].ResetDirectionArrows();
            mapSlots[i].ResetHighlight();
        }
        // Display The Actors (As Text For Now).
        for (int i = 0; i < prepManager.fieldSlots.Count; i++)
        {
            mapSlots[prepManager.fieldSlots[i].GetLocation()].UpdateText(prepManager.fieldSlots[i].GetBaseStatString());
            mapSlots[prepManager.fieldSlots[i].GetLocation()].ActivateDirectionArrow(prepManager.fieldSlots[i].GetDirection());
        }
        int castleTile = prepManager.GetCastleTile();
        mapSlots[castleTile].UpdateLayerSprite(castleSprite, 1);
        HighlightEnemySpawnZone(prepManager);
        List<int> spawnTiles = prepManager.GetSpawnTiles();
    }
    public void HighlightEnemySpawnZone(AutoChessPrepManager prepManager)
    {
        List<int> spawnTiles = prepManager.GetSpawnTiles();
        for (int i = 0; i < spawnTiles.Count; i++)
        {
            mapSlots[spawnTiles[i]].HighlightTile(Color.red);
        }
    }
    public void HighlightSelectedAttackRange(AutoChessPrepManager prepManager, AutoActorRollUpData actor)
    {
        // Determine Attack Range + Type.
        string[] blocks = prepManager.actorData.ReturnValue(actor.GetName()).Split("|");
        string range = blocks[10];
        string rangeType = blocks[13];
        int location = actor.GetLocation();
        int direction = actor.GetDirection();
        int selectedTile = prepManager.mapUtility.PointInDirection(location, direction, prepManager.mapSize);
        List<int> rangeTiles = new List<int>();
        rangeTiles = prepManager.mapUtility.GetAutoActorAttackTilesByShapeSpan(selectedTile, rangeType, int.Parse(range), prepManager.mapSize, location);
        for (int i = 0; i < rangeTiles.Count; i++)
        {
            mapSlots[rangeTiles[i]].HighlightTile(Color.blue);
        }
    }
}
