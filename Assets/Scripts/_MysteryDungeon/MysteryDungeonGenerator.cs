using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Generates A Dungeon Based On Various Templates.
// Forest/Mountain/Water/Underground Dungeons To Start.
// Plains+Forest+Water/Plains+Mountain+Water/Plains+Water/Plains Is Passable Terrain
// DeepWater/Pit/DeepWater/Pit Is Unpassable* Terrain (Passable By Flying+Swimming/Flying/Flying+Swimming/Flying Respectively)
// Generate Rooms + Items + Treasure + Stairs
[CreateAssetMenu(fileName = "MysteryDungeonGenerator", menuName = "ScriptableObjects/MysteryDungeon/Generator", order = 1)]
public class MysteryDungeonGenerator : DungeonGenerator
{

}