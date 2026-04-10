using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CaravanMule : MonoBehaviour
{
    public int hungerPain = 10;
    public string delimiter = "|";
    public string delimiter2 = ",";
    public string muleSprite;
    // BASE STATS: Generated when the horse is created.
    public int pullStrength;
    public int maxSpeed;
    public int maxEnergy;
    public int maxHealth;
    public List<string> statuses;
    public List<string> passives;
    public List<string> actives;
    public int GetPullStrength(){return pullStrength;}
    public int GetMaxSpeed(){return maxSpeed;}
    // CURRENT STATS: Updated during travel.
    // Energy is consumed 1/day when not resting.
    // Energy is restored by eating daily.
    // If out of energy, the horse's pull strength becomes 0 and it's max speed becomes 1.
    public int currentEnergy;
    public int GetEnergy(){return currentEnergy;}
    public void ConsumeEnergy(int amount)
    {
        currentEnergy -= amount;
        if (currentEnergy < 0)
        {
            HungerDamage();
            currentEnergy = 0;
        }
    }
    public void RestoreEnergy(){currentEnergy = maxEnergy;}
    // If health is 0 then the horse dies.
    // Health is restored by eating/resting.
    public int currentHealth;
    public bool Alive(){ return currentHealth > 0; }
    public void RestoreHealth(int amount = 1)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
    }
    public int GetHealth() { return currentHealth; }
    public void HungerDamage(){ currentHealth -= hungerPain; }
    public string ReturnAllStats()
    {
        string allStats = "";
        allStats += pullStrength + delimiter + maxSpeed + delimiter + maxEnergy + delimiter + maxHealth + delimiter + currentEnergy + delimiter + currentHealth + delimiter;
        for (int i = 0; i < statuses.Count; i++)
        {
            if (statuses[i].Length <= 1) { continue; }
            allStats += statuses[i];
            if (i < statuses.Count - 1)
            {
                allStats += delimiter2;
            }
            else { allStats += delimiter; }
        }
        for (int i = 0; i < passives.Count; i++)
        {
            if (passives[i].Length <= 1) { continue; }
            allStats += passives[i];
            if (i < passives.Count - 1)
            {
                allStats += delimiter2;
            }
            else { allStats += delimiter; }
        }
        for (int i = 0; i < actives.Count; i++)
        {
            if (actives[i].Length <= 1) { continue; }
            allStats += actives[i];
            if (i < actives.Count - 1)
            {
                allStats += delimiter2;
            }
            else { allStats += delimiter; }
        }
        return allStats;
    }
    public void ResetStats()
    {
        pullStrength = 0;
        maxSpeed = 0;
        maxEnergy = 0;
        maxHealth = 0;
        currentEnergy = 0;
        currentHealth = 0;
        statuses.Clear();
        passives.Clear();
        actives.Clear();
    }
    public void LoadAllStats(string newStats)
    {
        if (newStats.Length < 6)
        {
            ResetStats();
            return;
        }
        string[] stats = newStats.Split(delimiter);
        pullStrength = int.Parse(stats[0]);
        maxSpeed = int.Parse(stats[1]);
        maxEnergy = int.Parse(stats[2]);
        maxHealth = int.Parse(stats[3]);
        currentEnergy = int.Parse(stats[4]);
        currentHealth = int.Parse(stats[5]);
        if (stats.Length > 6)
        {
            statuses = stats[6].Split(delimiter2).ToList();
        }
        if (stats.Length > 7)
        {
            passives = stats[7].Split(delimiter2).ToList();
        }
        if (stats.Length > 8)
        {
            actives = stats[8].Split(delimiter2).ToList();
        }
    }
}
