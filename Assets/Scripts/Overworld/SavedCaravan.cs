using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Stores horses, wagons and cargo.
// Guards are stored in permanent party.
// Quests are stored in guild card.
[CreateAssetMenu(fileName = "SavedCaravan", menuName = "ScriptableObjects/DataContainers/SavedData/SavedCaravan", order = 1)]
public class SavedCaravan : SavedData
{
    public PartyData permanentParty;
    public override void NewGame()
    {
        dataPath = Application.persistentDataPath+"/"+filename;
        allData = newGameData;
        File.WriteAllText(dataPath, allData);
        Load();
        Save();
    }
    public string delimiterTwo;
    // If you lose everything you can still ship a little by yourself to rebuild.
    public int basePullWeight;
    public int baseCarryWeight;
    // By yourself you're about twice as fast as a normal traveller.
    // Of course horses can be much faster than you and pull much more weight.
    public int baseSpeed = 2;
    // Buy more horses/wagons at cities/villages.
    public List<string> mules;
    public void AddMule(string newStats){mules.Add(newStats);}
    public void ConsumeMuleEnergy(int energyCost)
    {
        for (int i = mules.Count - 1; i >= 0; i--)
        {
            dummyMule.LoadAllStats(mules[i]);
            dummyMule.ConsumeEnergy(energyCost);
            if (!dummyMule.Alive()) { mules.RemoveAt(i); }
            else { mules[i] = dummyMule.ReturnAllStats(); }
        }
    }
    public CaravanMule dummyMule;
    public int GetMaxSpeed()
    {
        if (mules.Count == 0){return baseSpeed;}
        int speed = 999;
        for (int i = 0; i < mules.Count; i++)
        {
            dummyMule.LoadAllStats(mules[i]);
            // Mules without energy means the whole caravan slows.
            if (dummyMule.GetEnergy() <= 0)
            {
                speed = 1;
                break;
            }
            // Only as fast as the slowest link.
            else
            {
                speed = Mathf.Min(speed, dummyMule.GetMaxSpeed());
            }
        }
        return speed;
    }
    public int GetCurrentSpeed()
    {
        if (!CargoLessThanMax()){ return 0; }
        return Mathf.Min(GetMaxSpeed(), ReturnPullCargoRatio());
    }
    public int GetMuleCount(){return mules.Count;}
    public string foodString;
    public int ReturnFood(){return ReturnItemWeight(foodString);}
    public bool FoodAvailable(){ return ReturnItemQuantity(foodString) > 0; }
    public void ConsumeFood(int amount = 1) { UnloadCargo(foodString, amount); }
    public string muleFoodString;
    public int ReturnMuleFood(){return ReturnItemWeight(muleFoodString);}
    public void ConsumeMuleFood(int amount){UnloadCargo(muleFoodString, amount);}
    public int muleFoodRequirment;
    public int GetMuleFoodRequirement(){return muleFoodRequirment;}
    public int DailyMuleFood(){return GetMuleCount()*GetMuleFoodRequirement();}
    public bool EnoughMuleFood(){return EnoughCargo(muleFoodString, DailyMuleFood());}
    public List<string> wagons;
    public void AddWagon(string newStats){wagons.Add(newStats);}
    public OverworldWagon dummyWagon;
    public int GetWagonCount(){return wagons.Count;}
    public int GetMaxPullWeight()
    {
        int max = basePullWeight;
        // Add each horse's individual pull weight.
        for (int i = 0; i < mules.Count; i++)
        {
            dummyMule.LoadAllStats(mules[i]);
            if (dummyMule.GetEnergy() <= 0){continue;}
            else
            {
                max += dummyMule.GetPullStrength();
            }
        }
        return max;
    }
    public int GetMaxCarryWeight()
    {
        int max = baseCarryWeight;
        // Add each wagon's individual carry weight.
        for (int i = 0; i < wagons.Count; i++)
        {
            dummyWagon.LoadAllStats(wagons[i]);
            {
                max += dummyWagon.GetCarryWeight();
            }
        }
        return max;
    }
    // Carry a variety of things.
    public List<string> cargoItems;
    public List<string> GetAllCargo()
    {
        List<string> allCargo = new List<string>();
        for (int i = 0; i < cargoItems.Count; i++)
        {
            if (ReturnItemQuantity(cargoItems[i]) > 0){allCargo.Add(cargoItems[i]);}
        }
        return allCargo;
    }
    public List<string> cargoQuantities;
    public List<string> GetAllCargoQuantities()
    {
        List<string> allQuantities = new List<string>();
        for (int i = 0; i < cargoQuantities.Count; i++)
        {
            if (int.Parse(cargoQuantities[i]) > 0) {allQuantities.Add(cargoQuantities[i]); }
        }
        return allQuantities;
    }
    public StatDatabase cargoWeightData;
    public List<string> GetAllCargoWeights()
    {
        List<string> allWeights = new List<string>();
        for (int i = 0; i < cargoQuantities.Count; i++)
        {
            int quantity = int.Parse(cargoQuantities[i]);
            if (quantity < 1){continue;}
            int weight = quantity * ReturnIndividualItemWeight(cargoItems[i]);
            allWeights.Add(weight.ToString());
        }
        return allWeights;

    }
    public int GetCargoWeight()
    {
        int totalWeight = 0;
        for (int i = 0; i < cargoItems.Count; i++)
        {
            if (cargoItems[i].Length < 1){continue;}
            int quantity = int.Parse(cargoQuantities[i]);
            if (quantity < 1){continue;}
            totalWeight += quantity * ReturnIndividualItemWeight(cargoItems[i]);
        }
        // TODO Also add all the wagon weights.
        for (int i = 0; i < wagons.Count; i++)
        {
            dummyWagon.LoadAllStats(wagons[i]);
            {
                totalWeight += dummyWagon.GetWeight();
            }
        }
        return totalWeight;
    }
    public int ReturnIndividualItemWeight(string itemName)
    {
        string value = cargoWeightData.ReturnValue(itemName);
        if (value == ""){return 1;}
        return int.Parse(value);
    }
    public int ReturnItemWeight(string itemName)
    {
        int indexOf = cargoItems.IndexOf(itemName);
        if (indexOf == -1){return 0;}
        int quantity = int.Parse(cargoQuantities[indexOf]);
        if (quantity < 1){return 0;}
        return quantity * ReturnIndividualItemWeight(itemName);
    }
    public int ReturnItemQuantity(string itemName)
    {
        int indexOf = cargoItems.IndexOf(itemName);
        if (indexOf == -1){return 0;}
        int quantity = int.Parse(cargoQuantities[indexOf]);
        if (quantity < 1){return 0;}
        return quantity;
    }
    public bool EnoughCargo(string cargoName, int amount)
    {
        int indexOf = cargoItems.IndexOf(cargoName);
        if (indexOf == -1){return false;}
        return int.Parse(cargoQuantities[indexOf]) >= amount;
    }
    public void AddCargo(string itemName, int quantity)
    {
        int indexOf = cargoItems.IndexOf(itemName);
        if (indexOf == -1)
        {
            cargoItems.Add(itemName);
            cargoQuantities.Add(quantity.ToString());
        }
        else
        {
            cargoQuantities[indexOf] = (int.Parse(cargoQuantities[indexOf])+quantity).ToString();
        }
    }
    public void UnloadCargo(string cargoName, int quantity)
    {
        int indexOf = cargoItems.IndexOf(cargoName);
        if (indexOf == -1){return;}
        cargoQuantities[indexOf] = (int.Parse(cargoQuantities[indexOf])-quantity).ToString();
    }
    public void DumpCargo(int indexOf)
    {
        if (indexOf < 0 || indexOf > cargoQuantities.Count){ return; }
        int quantity = int.Parse(cargoQuantities[indexOf]);
        if (quantity <= 0){ return; }
        cargoQuantities[indexOf] = (quantity-1).ToString();
    }
    public List<string> people;
    public OverworldActor dummyActor;
    public override void Save()
    {
        dataPath = Application.persistentDataPath + "/" + filename;
        allData = "";
        allData += String.Join(delimiterTwo, mules);
        allData += delimiter;
        allData += String.Join(delimiterTwo, wagons);
        allData += delimiter;
        allData += String.Join(delimiterTwo, cargoItems);
        allData += delimiter;
        allData += String.Join(delimiterTwo, cargoQuantities);
        allData += delimiter;
        allData += String.Join(delimiterTwo, people);
        allData += delimiter;
        File.WriteAllText(dataPath, allData);
    }
    public override void Load()
    {
        dataPath = Application.persistentDataPath + "/" + filename;
        if (File.Exists(dataPath)) { allData = File.ReadAllText(dataPath); }
        else
        {
            NewGame();
            return;
        }
        dataList = allData.Split(delimiter).ToList();
        mules = dataList[0].Split(delimiterTwo).ToList();
        wagons = dataList[1].Split(delimiterTwo).ToList();
        cargoItems = dataList[2].Split(delimiterTwo).ToList();
        cargoQuantities = dataList[3].Split(delimiterTwo).ToList();
        people = dataList[4].Split(delimiterTwo).ToList();
        utility.RemoveEmptyListItems(cargoItems);
        utility.RemoveEmptyListItems(cargoQuantities);
        utility.RemoveEmptyListItems(mules);
        utility.RemoveEmptyListItems(wagons);
        utility.RemoveEmptyListItems(people);
    }
    public int ReturnPullCargoRatio()
    {
        int maxPull = GetMaxPullWeight();
        int cargoWeight = GetCargoWeight();
        if (cargoWeight > maxPull){return 0;}
        else if (cargoWeight <= 0){return 999;}
        return (maxPull/cargoWeight);
    }
    public bool CargoLessThanMax()
    {
        int maxCarry = GetMaxCarryWeight();
        int cargoWeight = GetCargoWeight();
        if (cargoWeight <= maxCarry){return true;}
        return false;
    }
    public override void Rest()
    {
        // Consume mule food.
        for (int i = mules.Count - 1; i >= 0; i--)
        {
            if (ReturnItemQuantity(muleFoodString) < 1)
            {
                dummyMule.LoadAllStats(mules[i]);
                dummyMule.HungerDamage();
                if (!dummyMule.Alive())
                {
                    mules.RemoveAt(i);
                }
                else
                {
                    mules[i] = dummyMule.ReturnAllStats();
                }
            }
            else
            {
                ConsumeMuleFood(1);
                dummyMule.LoadAllStats(mules[i]);
                dummyMule.RestoreEnergy();
                dummyMule.RestoreHealth();
                mules[i] = dummyMule.ReturnAllStats();
            }
        }
        // Consume rations. // This is handled by the party data manager.
    }
}
