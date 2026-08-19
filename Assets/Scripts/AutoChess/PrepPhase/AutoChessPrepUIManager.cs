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
    public SpriteContainer tileSprites;
    public SpriteContainer masterSprites;
    public bool PVP = false;
    public Sprite castleSprite;
    public Color spawnZoneColor;
    public Color attackZoneColor;
    public GameObject actorDisplayObject;
    public AutoActorDisplay actorDisplay;
    public AutoChessEquipmentDisplay equipDisplay;
    public GameObject sellActorObject;
    public GameObject rotateActorObject;
    public GameObject manageEquipObject;
    public void ResetObjects()
    {
        actorDisplay.ResetDisplay();
        actorDisplayObject.SetActive(false);
        sellActorObject.SetActive(false);
        rotateActorObject.SetActive(false);
        manageEquipObject.SetActive(false);
    }
    public void ActivateFieldActorObjects()
    {
        sellActorObject.SetActive(true);
        rotateActorObject.SetActive(true);
        manageEquipObject.SetActive(true);
    }
    public void ActivateBenchActorObjects()
    {
        sellActorObject.SetActive(true);
        manageEquipObject.SetActive(true);
    }
    public void UpdateActorDisplay(AutoActorRollUpData actor)
    {
        actorDisplayObject.SetActive(true);
        actorDisplay.DisplayActor(actor);
    }
    public void UpdateActorDisplayByName(string newName)
    {
        actorDisplay.DisplayActor(newName);
    }
    public TMP_Text roundText;
    public TMP_Text levelText;
    public TMP_Text goldText;
    public TMP_Text castleHealthText;
    public TMP_Text deployLimitText;
    public void UpdateAutoChessUI(AutoChessPrepManager prepManager)
    {
        ResetObjects();
        for (int i = 0; i < benchSlots.Count; i++)
        {
            benchSlots[i].ResetDisplay();
        }
        for (int i = 0; i < prepManager.benchSlots.Count; i++)
        {
            int benchIndex = prepManager.benchSlots[i].GetLocation();
            benchSlots[benchIndex].UpdateBenchSlot(prepManager.benchSlots[i]);
        }
        UpdateMap(prepManager);
        roundText.text = dataManager.GetRound().ToString();
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
        equipDisplay.UpdateDisplay();
    }
    public void UpdateMapWithDataManager(AutoChessDataManager newDataManager)
    {
        for (int i = 0; i < dataManager.mapTiles.Count; i++)
        {
            mapSlots[i].UpdateLayerSprite(tileSprites.SpriteDictionary(newDataManager.mapTiles[i]), 0);
        }
        for (int i = 0; i < mapSlots.Count; i++)
        {
            mapSlots[i].UpdateText();
            mapSlots[i].ResetDirectionArrows();
            mapSlots[i].ResetHealthBar();
            mapSlots[i].ResetHighlight();
            // Reset Actors.
            mapSlots[i].ResetLayerSprite(2);
            // Reset Any Terrain Changes From The Battle.
            mapSlots[i].ResetLayerSprite(3);
            mapSlots[i].ResetAutoChessEquipment();
        }
        List<string> fieldActorData = newDataManager.GetFieldActorData();
        for (int i = 0; i < fieldActorData.Count; i++)
        {
            if (fieldActorData[i].Length <= 1){continue;}
            AutoActorRollUpData newActor = new();
            newActor.LoadRollUpData(fieldActorData[i]);
            int location = newActor.GetLocation();
            string name = newActor.GetName();
            mapSlots[location].ActivateLayerSprite(2);
            masterSprites.ApplyToImage(mapSlots[location].GetLayerSprite(2), name);
            mapSlots[location].ActivateDirectionArrow(newActor.GetDirection());
            List<string> equipNames = newActor.GetEquipmentNames();
            for (int j = 0; j < equipNames.Count; j++)
            {
                if (equipNames[j].Length <= 0){continue;}
                mapSlots[location].EnableEquipSlot(j);
                masterSprites.ApplyToImage(mapSlots[location].GetEquipSlotImage(j), equipNames[j]);
            }
        }
    }
    public void UpdateMap(AutoChessPrepManager prepManager)
    {
        UpdateMapWithDataManager(prepManager.dataManager);
        if (!PVP)
        {
            int castleTile = prepManager.GetCastleTile();
            mapSlots[castleTile].UpdateLayerSprite(castleSprite, 1);
        }
        HighlightEnemySpawnZone(prepManager);
    }
    public void HighlightEnemySpawnZone(AutoChessPrepManager prepManager)
    {
        List<int> spawnTiles = prepManager.GetSpawnTiles();
        for (int i = 0; i < spawnTiles.Count; i++)
        {
            mapSlots[spawnTiles[i]].HighlightTile(spawnZoneColor);
            // Show Bonus Slots
            if (i < prepManager.bonusSlots.Count)
            {
                string name = prepManager.bonusSlots[i].GetName();
                mapSlots[spawnTiles[i]].ActivateLayerSprite(2);
                masterSprites.ApplyToImage(mapSlots[spawnTiles[i]].GetLayerSprite(2), name);
            }
        }

    }
    public void HighlightSelectedAttackRange(AutoChessPrepManager prepManager, AutoActorRollUpData actor)
    {
        // Determine Attack Range + Type.
        string[] blocks = prepManager.actorData.ReturnValue(actor.GetName()).Split("|");
        string range = blocks[11];
        string rangeType = blocks[14];
        int location = actor.GetLocation();
        int direction = actor.GetDirection();
        int selectedTile = prepManager.mapUtility.PointInDirection(location, direction, prepManager.mapSize);
        List<int> rangeTiles = new List<int>();
        rangeTiles = prepManager.mapUtility.GetAutoActorAttackTilesByShapeSpan(selectedTile, rangeType, int.Parse(range), prepManager.mapSize, location);
        for (int i = 0; i < rangeTiles.Count; i++)
        {
            mapSlots[rangeTiles[i]].HighlightTile(attackZoneColor);
        }
    }
}
