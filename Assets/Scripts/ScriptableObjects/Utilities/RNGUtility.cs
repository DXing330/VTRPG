using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Security.Cryptography;

[CreateAssetMenu(fileName = "SavedData", menuName = "ScriptableObjects/Utility/SavedSeed", order = 1)]
public class RNGUtility : SavedData
{
    public List<RNGUtility> subRNG;
    public RNGUtility masterRNG;
    // SAVED SEED
    public ulong seed;
    public List<ulong> seedHistory;
    public void ShowSeedHistory()
    {
        Debug.Log(String.Join(",", seedHistory));
    }
    public void RandomSeed()
    {
        byte[] bytes = new byte[8];
        RandomNumberGenerator.Fill(bytes);
        seed = BitConverter.ToUInt64(bytes, 0);
        seedHistory.Clear();
        Save();
        for (int i = 0; i < subRNG.Count; i++)
        {
            subRNG[i].SetSeed(seed);
        }
    }
    public void SetSeed(ulong newSeed)
    {
        seed = newSeed;
        seedHistory.Clear();
        Save();
    }
    public ulong GetSeed()
    {
        return seed;
    }
    // SAVING/LOADING
    public override void NewGame()
    {
        RandomSeed();
    }
    public override void Save()
    {
        dataPath = Application.persistentDataPath+"/"+filename;
        allData = seed.ToString();
        File.WriteAllText(dataPath, allData);
    }
    public override void Load()
    {
        dataPath = Application.persistentDataPath+"/"+filename;
        if (File.Exists(dataPath))
        {
            allData = File.ReadAllText(dataPath);
            SetSeed(ulong.Parse(allData));
        }
        else
        {
            RandomSeed();
            return;
        }
    }
    // RNG FUNCTIONS
    public ulong NextUInt64()
    {
        seedHistory.Add(seed);
        seed ^= seed << 13;
        seed ^= seed >> 7;
        seed ^= seed << 17;
        return seed;
    }

    public int Range(int min, int max)
    {
        if (max <= min) return min;
        ulong r = NextUInt64();
        return min + (int)(r % (ulong)(max - min));
    }
}
