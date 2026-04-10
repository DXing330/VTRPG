using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireworkManager : MonoBehaviour
{
    public Firework fireworkPrefab;
    public Firework downFirework;
    public CircleFormation circles;
    public Transform mainPanel;
    public int maxSpawn;
    public int maxCircleSpawns;
    public float timeToSpawn;
    public float spawnDelay;
    public float minDelay = 0.5f;
    public float maxDelay = 1.5f;

    public void Start()
    {
        spawnDelay = Random.Range(minDelay,maxDelay);
        timeToSpawn = 0;
    }

    public void Update()
    {
        timeToSpawn -= Time.deltaTime;
        if (timeToSpawn < 0)
        {
            Spawn();
            spawnDelay = Random.Range(minDelay,maxDelay);
            timeToSpawn = spawnDelay;
        }
    }

    protected void Spawn()
    {
        int spawnAmount = Random.Range(1, maxSpawn + 1);
        for (int i = 0; i < spawnAmount; i++)
        {
            Firework newFirework = Instantiate(fireworkPrefab, mainPanel);
            Firework newDFirework = Instantiate(downFirework, mainPanel);
        }
        int cirleSpawns = Random.Range(1, maxCircleSpawns + 1);
        for (int i = 0; i < cirleSpawns; i++)
        {
            CircleFormation newCircle = Instantiate(circles, mainPanel);
            newCircle.SetMainPanel(mainPanel);
        }
    }
}
