using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoChessAIPrepControllerEncoded : AutoChessAIPrepController
{
    public override void AIPrepPhase(AutoChessDataManager dataManager, bool timed = false)
    {
        prepManager.SetDataManager(dataManager);
        prepManager.DisableUI();
        shopManager.shopData.GeneratePVPCurrentListing();
        shopManager.PVPRefreshData();
        BuildStackCache();
        ClearScoreCaches();
        // Encode State Before Each Choice.
        EconomyAction(dataManager);
        // Encode State Before Each Buy.
        AcquireLoop(dataManager);
        // Encode State Before Each Choice.
        AutoPlaceFieldUnits(dataManager);
        // Log Each Equipment Score + Choice.
        equipmentController.AutoPlaceEquipment(this);
        aegirPlacement.AegirPlaceFieldUnits();
        // Encode State Before End.
        prepManager.StartBattle();
    }
}
