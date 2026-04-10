using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonMap : MapManager
{
    public GameObject blackScreen;
    public GameObject failureScreen;
    public int maxDistanceFromCenter = 2;
    public DungeonEffectManager dungeonEffects;
    public DungeonMerchant merchant;
    public PartyDataManager partyData;
    public Dungeon dungeon;
    public DungeonMiniMap miniMap;
    public WeatherFilter weatherDisplay;
    public StatTextList dungeonLogDisplay;
    public GameObject stomachMeter;
    public void UpdateStomachMeter()
    {
        stomachMeter.transform.localScale = new Vector3((float)dungeon.GetStomach()/(float)dungeon.GetMaxStomach(),1,0);
    }
    public PopUpMessage dowsingMessage;
    protected int DetermineClosestLocation(List<int> locations)
    {
        if (locations.Count <= 0){return -1;}
        int distance = dungeon.GetDungeonSize();
        int index = 0;
        for (int i = 0; i < locations.Count; i++)
        {
            if (mapUtility.DistanceBetweenTiles(dungeon.GetPartyLocation(), locations[i], dungeon.GetDungeonSize()) < distance)
            {
                index = i;
                distance = mapUtility.DistanceBetweenTiles(dungeon.GetPartyLocation(), locations[i], dungeon.GetDungeonSize());
            }
        }
        return locations[index];
    }
    public int DetermineClosestEnemyLocation()
    {
        if (dungeon.GetEnemyLocations().Count <= 0){return -1;}
        else if (dungeon.GetEnemyLocations().Count == 1){return dungeon.GetEnemyLocations()[0];}
        return DetermineClosestLocation(dungeon.GetEnemyLocations());
    }
    public void UseDowsing(string target)
    {
        string message = "The rods point ";
        switch (target)
        {
            case "Stairs":
                message += mapUtility.IntDirectionToString(mapUtility.DirectionBetweenLocations(dungeon.GetPartyLocation(), dungeon.GetStairsDown(), dungeon.GetDungeonSize())) + ".";
                break;
            case "Treasure":
                message += mapUtility.IntDirectionToString(mapUtility.DirectionBetweenLocations(dungeon.GetPartyLocation(), dungeon.GetRandomTreasureLocation(), dungeon.GetDungeonSize())) + ".";
                break;
            case "Item":
                message += mapUtility.IntDirectionToString(mapUtility.DirectionBetweenLocations(dungeon.GetPartyLocation(), dungeon.GetRandomItemLocation(), dungeon.GetDungeonSize())) + ".";
                break;
            case "Trap":
                message += mapUtility.IntDirectionToString(mapUtility.DirectionBetweenLocations(dungeon.GetPartyLocation(), DetermineClosestLocation(dungeon.GetTrapLocations()), dungeon.GetDungeonSize())) + ".";
                break;
            case "Enemy":
                message += mapUtility.IntDirectionToString(mapUtility.DirectionBetweenLocations(dungeon.GetPartyLocation(), DetermineClosestEnemyLocation(), dungeon.GetDungeonSize())) + ".";
                break;
            case "Quest":
                message += mapUtility.IntDirectionToString(mapUtility.DirectionBetweenLocations(dungeon.GetPartyLocation(), dungeon.GetRandomQuestLocation(), dungeon.GetDungeonSize())) + ".";
                break;
            default:
                break;
        }
        dungeon.AddDungeonLog(target + "-" + message);
        dowsingMessage.SetMessage(message);
        UpdateMap();
    }
    public SceneMover sceneMover;
    public bool interactable = true;
    // layers: 0 = terrain, 1 = stairs/treasure/etc., 2 = actorsprites

    protected override void Start()
    {
        blackScreen.SetActive(true);
        // If you've fought the quest and returned then you get to complete part of the quest.
        // For capture missions you get a captured thing as a temp party member, better keep it safe.
        if (dungeon.GetQuestFought() == 1)
        {
            dungeon.AddDungeonLog("Defeated the requested target.");
            dungeon.SetQuestFought(0);
            switch (dungeon.mainStory.GetCurrentRequest())
            {
                case "Capture":
                partyData.AddTempPartyMember(dungeon.mainStory.GetRequestSpecificsName());
                dungeon.AddDungeonLog("Captured the requested target.");
                break;
                default:
                break;
            }
        }
        // If you've fought the boss and returned then you get to go to the reward scene.
        if (dungeon.GetBossFought() == 1)
        {
            sceneMover.ReturnFromDungeon();
            return;
        }
        RefreshMapSize();
        UpdateCenterTile(dungeon.GetPartyLocation());
        UpdateMap();
    }

    public void QuitDungeon()
    {
        sceneMover.ReturnFromDungeon(false);
    }

    public void EscapeDungeon()
    {
        if (dungeon.RobbedMerchant())
        {
            dungeon.AddDungeonLog("Something is wrong, you can't escape.");
            return;
        }
        dungeon.EscapeOrb();
        sceneMover.ReturnFromDungeon();
    }

    protected void RefreshMapSize()
    {
        mapSize = dungeon.GetDungeonSize();
        InitializeEmptyList();
        dungeon.UpdateEmptyTiles(emptyList);
    }

    public void TeleportToTile(int newTile)
    {
        if (dungeon.RobbedMerchant())
        {
            dungeon.AddDungeonLog("Something is wrong, you can't teleport.");
            return;
        }
        MoveToTile(newTile);
    }

    protected void MoveToTile(int newTile)
    {
        // Whenever moving, regenerators will regenerate.
        dungeonEffects.ApplyNaturalRegeneration();
        // Whenever moving, decrease the hunger meter.
        if (dungeon.Hungry(partyData.ReturnTotalPartyCount()))
        {
            // If the hunger meter is empty then the party suffers.
            if (partyData.DungeonHunger())
            {
                // If you starve then autofail.
                failureScreen.SetActive(true);
                return;
            }
            dungeon.AddDungeonLog("Feeling hungry...");
        }
        // When moving take damage from some statuses.
        if (dungeonEffects.ApplyDamagingStatus())
        {
            failureScreen.SetActive(true);
            return;
        }
        if (mapUtility.DistanceBetweenTiles(newTile, centerTile, mapSize) > maxDistanceFromCenter)
        {
            UpdateCenterTile(newTile);
        }
        if (dungeon.GoalOnTile(newTile) == "Rescue")
        {
            partyData.AddTempPartyMember(dungeon.GetEscortName());
            dungeon.AddDungeonLog("Located " + dungeon.GetEscortName() + ".");
            dungeon.RemoveGoalTile(newTile);
        }
        else if (dungeon.GoalOnTile(newTile) == "Deliver")
        {
            // Try to deliver.
            string[] requestSpecifics = dungeon.mainStory.GetRequestSpecifics().Split("*");
            if (partyData.dungeonBag.QuantityOfItemExists(requestSpecifics[0], int.Parse(requestSpecifics[1])))
            {
                partyData.dungeonBag.RemoveItemsOfType(requestSpecifics[0], int.Parse(requestSpecifics[1]));
                partyData.dungeonBag.GainItem("Check");
                dungeon.AddDungeonLog("Obtained a check as payment for your successful delivery.");
            }
            else
            {
                dungeon.AddDungeonLog("You currently don't have the requested delivery items.");
            }
        }
        else if (dungeon.GoalOnTile(newTile) == "Capture")
        {
            // Enter a battle with the thing you want to capture.
            // You have to capture it in battle.
            dungeon.PrepareQuestBattle(newTile);
            dungeon.MovePartyLocation(newTile);
            // Move to battle scene.
            EnterBattle();
            return;
        }
        else if (dungeon.GoalOnTile(newTile) == "Search")
        {
            if (!partyData.dungeonBag.BagFull())
            {
                // Claim an item.
                partyData.dungeonBag.GainItem(dungeon.GetSearchName());
                dungeon.RemoveGoalTile(newTile);
                dungeon.AddDungeonLog("Picked up " + dungeon.GetSearchName() + ".");
            }
            else
            {
                // Generate an inventory full error message.
                dungeon.AddDungeonLog("Bag is full, you cannot pick up the requested item.");
            }
        }
        if (dungeon.StairsDownLocation(newTile))
        {
            interactable = false;
            if (dungeon.FinalFloor())
            {
                // Set a flag to know that you are fighting the final boss of the dungeon so when you load back you don't fight the boss again.
                // If you lose to the boss it's simply a defeat in the dungeon and you get kicked out as expected.
                if (dungeon.PrepareBossBattle())
                {
                    EnterBattle();
                }
                else
                {
                    sceneMover.ReturnFromDungeon();
                }
                return;
            }
            dungeon.MoveFloors();
            RefreshMapSize();
            // Save whenever moving floors.
            dungeon.dungeonState.Save();
            partyData.Save();
            // This doesn't update the center when moving between dungeons for some reason.
            UpdateCenterTile(dungeon.GetPartyLocation());
            StartCoroutine(MoveFloors());
            return;
        }
        else if (dungeon.EnemyLocation(newTile))
        {
            dungeon.PrepareBattle(newTile);
            dungeon.MovePartyLocation(newTile);
            // Move to battle scene.
            EnterBattle();
            return;
        }
        else
        {
            dungeon.MovePartyLocation(newTile);
            // Check if any enemies moved onto the player.
            if (dungeon.EnemyLocation(newTile))
            {
                // Move to battle if they did.
                dungeon.EnemyBeginsBattle();
                UpdateMap();
                EnterBattle();
                return;
            }
        }
        dungeon.UpdatePartyModifierDurations();
        // Check if you stepped on a treasure, item or trap.
        if (dungeon.TrapLocation(newTile))
        {
            dungeonEffects.ActivateTrap(dungeon.TriggerTrap());
        }
        else if (dungeon.TreasureLocation(newTile))
        {
            // Check if mimic.
            if (dungeon.MimicFight())
            {
                dungeon.PrepareMimicBattle(partyData);
                dungeon.AddDungeonLog("The treasure chest is made of mimics.");
                EnterBattle();
                return;
            }
            // Check if inventory is full.
            if (!partyData.dungeonBag.BagFull())
            {
                // Claim an item.
                partyData.dungeonBag.GainItem(dungeon.ClaimTreasure());
            }
            else
            {
                // Generate an inventory full error message.
                dungeon.AddDungeonLog("Bag is full.");
            }
        }
        else if (dungeon.ItemLocation(newTile))
        {
            // Check if inventory is full.
            if (!partyData.dungeonBag.BagFull())
            {
                // Claim an item.
                partyData.dungeonBag.GainItem(dungeon.ClaimItem());
            }
            else
            {
                // Generate an inventory full error message.
                dungeon.AddDungeonLog("Bag is full.");
            }
        }
        if (dungeon.MerchantLocation(newTile) && !dungeon.RobbedMerchant())
        {
            merchant.ActivateMerchant();
        }
        UpdateMap();
    }

    protected void EnterBattle()
    {
        interactable = false;
        partyData.Save();
        sceneMover.MoveToBattle();
    }

    public void MoveInDirection(int direction)
    {
        if (!interactable){ return; }
        int newTile = mapUtility.PointInDirection(dungeon.GetPartyLocation(), direction, mapSize);
        if (newTile < 0 || newTile == dungeon.GetPartyLocation() || !dungeon.TilePassable(newTile)){return;}
        MoveToTile(newTile);
    }

    public List<int> AdjacentTilesToParty()
    {
        return mapUtility.AdjacentTiles(dungeon.GetPartyLocation(), mapSize);
    }

    public void UpdateActors()
    {
        // Get party/enemies from dungeon.
        mapDisplayers[1].ResetCurrentTiles(mapTiles);
        mapDisplayers[1].DisplayCurrentTiles(mapTiles, dungeon.partyLocations, currentTiles);
    }

    public void UpdateHighlights()
    {
        mapDisplayers[3].ResetHighlights(mapTiles);
        //mapDisplayers[3].HighlightTileSet(mapTiles, mapUtility.AdjacentTiles(dungeon.GetPartyLocation(), mapSize), currentTiles);
    }

    public List<int> GetCurrentTiles(int diameter)
    {
        return currentTileManager.GetCurrentTilesFromCenter(centerTile, mapSize, diameter);
    }

    public override void UpdateMap()
    {
        UpdateCurrentTiles();
        mapDisplayers[0].DisplayCurrentTiles(mapTiles, dungeon.currentFloorTiles, currentTiles);
        dungeon.UpdateViewedTiles(currentTiles);
        miniMap.UpdateMiniMapString(currentTiles);
        if (miniMap.active){miniMap.UpdateMiniMap();}
        UpdateActors();
        UpdateHighlights();
        UpdateStomachMeter();
        weatherDisplay.UpdateFilter(dungeon.GetWeather());
        dungeonLogDisplay.SetStatsAndData(dungeon.GetDungeonLogs());
        blackScreen.SetActive(false);
    }

    IEnumerator MoveFloors()
    {
        for (int i = 0; i < 3; i++)
        {
            if (i == 0)
            {
                loadingScreen.StartLoadingScreen();
            }
            if (i == 1)
            {
                UpdateMap();
            }
            if (i == 2)
            {
                loadingScreen.FinishLoadingScreen();
                interactable = true;
            }
            yield return new WaitForSeconds(loadingScreen.totalFadeTime);
        }
    }
}
