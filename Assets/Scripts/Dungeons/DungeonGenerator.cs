using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DungeonGen", menuName = "ScriptableObjects/Dungeons/DungeonGen", order = 1)]
public class DungeonGenerator : ScriptableObject
{
    public GeneralUtility utility;
    public MapUtility mapUtility;
    // TEMPORARY GENERATION CONSTANTS
    // These will eventually come from the dungeon-generation data structure supplied to this generator.
    public MysteryDungeonState dungeonState;
    protected const int FLOOR_TILE = 0;
    protected const int WALL_TILE = 1;
    protected const int DEFAULT_BASE_SIZE = 32;
    protected const int DEFAULT_SIZE_VARIANCE = 6;
    protected const int DEFAULT_MIN_ROOM_SIZE = 6;
    protected const int DEFAULT_MAX_ROOMS = 6;
    // Chance to add additional connections after the main room graph.
    protected const float EXTRA_CONNECTION_CHANCE = 0.05f;
    // Chance for a rectangular room to have its corners removed.
    protected const float IRREGULAR_ROOM_CHANCE = 0.35f;
    // Important locations should not be immediately adjacent.
    protected const int MIN_IMPORTANT_LOCATION_DISTANCE = 3;
    // ============================================================
    // BASIC SETTINGS
    // ============================================================
    public int treasureCount = 1;
    public void SetTreasureCount(int newAmount)
    {
        treasureCount = Mathf.Max(0, newAmount);
    }
    public int size;
    public int GetSize()
    {
        return size;
    }
    protected int baseSize = DEFAULT_BASE_SIZE;
    protected int sizeVariance = DEFAULT_SIZE_VARIANCE;
    public int GetMinSize()
    {
        return baseSize - sizeVariance;
    }
    public int minRoomSize = DEFAULT_MIN_ROOM_SIZE;
    public int maxRooms = DEFAULT_MAX_ROOMS;

    // These are now independent from treasureCount.
    // Set to 0 when a floor should have none.
    public int minItems = 0;
    public int maxItems = 4;
    public int minTraps = 0;
    public int maxTraps = 2;

    // ============================================================
    // GENERATED DATA
    // ============================================================
    public List<int> allTiles = new List<int>();
    public List<int> tileRooms = new List<int>();
    public int startLocation;
    public int stairsLocation;
    public List<int> itemLocations;
    public List<int> trapLocations;
    // start|direction|width|height
    public List<string> roomDetails = new List<string>();
    // ============================================================
    // INTERNAL ROOM DATA
    // ============================================================
    protected enum RoomType
    {
        Normal,
        Start,
        Stairs,
        Special,
        Locked
    }
    protected class GeneratedRoom
    {
        public int index;
        public RoomType type = RoomType.Normal;
        public int start;
        public int direction;
        public int width;
        public int height;
        public List<int> interiorTiles = new List<int>();
        public List<int> allTiles = new List<int>();
    }
    // ============================================================
    // MAIN GENERATION
    // ============================================================
    public void GenerateDungeon(int newSize = -1, int newMaxRooms = -1)
    {
        // --------------------------------------------------------
        // Size
        // --------------------------------------------------------
        if (newSize < baseSize - sizeVariance)
        {
            size = baseSize + Random.Range(-sizeVariance, sizeVariance + 1);
        }
        else
        {
            size = newSize;
        }
        size = Mathf.Max(size, minRoomSize + 2);
        if (newMaxRooms >= 0)
        {
            maxRooms = newMaxRooms;
        }
        else
        {
            maxRooms = Mathf.Max(2, size / minRoomSize);
        }
        Reset();
        // --------------------------------------------------------
        // Generate base map
        // --------------------------------------------------------
        GetTiles();
        // --------------------------------------------------------
        // Generate rooms
        // --------------------------------------------------------
        MakeRooms();
        // Extremely unlikely fallback, but guarantees that we have
        // something usable.
        if (roomDetails.Count == 0)
        {
            MakeFallbackRoom();
        }
        // --------------------------------------------------------
        // Build room connectivity
        // --------------------------------------------------------
        ConnectRoomGraph();
        // --------------------------------------------------------
        // Choose important locations
        // --------------------------------------------------------
        int startRoom = Random.Range(0, roomDetails.Count);
        int stairsRoom = Random.Range(0, roomDetails.Count);
        startLocation = GetRandomPointInRoom(startRoom);
        stairsLocation = GetRandomPointInRoom(stairsRoom);
        // Mark important rooms.
        // This is currently only internal information; eventually this can become part of your serialized floor structure.
        // Start/stairs themselves are just tile locations for now.
        // --------------------------------------------------------
        // Make absolutely certain the main route exists.
        // --------------------------------------------------------
        ConnectPoints(startLocation, stairsLocation);
        // --------------------------------------------------------
        // Generate item locations.
        // --------------------------------------------------------
        int actualMinItems = Mathf.Max(0, minItems);
        int actualMaxItems = Mathf.Max(actualMinItems, maxItems);
        int itemCount = Random.Range(actualMinItems, actualMaxItems + 1);
        List<int> takenLocations = new List<int>();
        itemLocations = new List<int>();
        takenLocations.Add(startLocation);
        takenLocations.Add(stairsLocation);
        for (int i = 0; i < itemCount; i++)
        {
            int location = GetRandomPointInAnyRoom(stairsLocation, takenLocations);
            if (location < 0)
            {
                break;
            }
            takenLocations.Add(location);
            itemLocations.Add(location);
        }
        // --------------------------------------------------------
        // Generate traps.
        // --------------------------------------------------------
        int actualMinTraps = Mathf.Max(0, minTraps);
        int actualMaxTraps = Mathf.Max(actualMinTraps, maxTraps);
        int trapCount = Random.Range(actualMinTraps, actualMaxTraps + 1);
        trapLocations = new List<int>();
        for (int i = 0; i < trapCount; i++)
        {
            int location = GetRandomPointInAnyRoom(stairsLocation,takenLocations);
            if (location < 0)
            {
                break;
            }
            takenLocations.Add(location);
            trapLocations.Add(location);
        }
        // --------------------------------------------------------
        // Final connectivity validation.
        // --------------------------------------------------------
        if (!IsReachable(startLocation, stairsLocation))
        {
            // This should almost never happen because the main
            // connection was explicitly carved.
            //
            // Repair rather than silently returning an invalid floor.
            ConnectPoints(startLocation, stairsLocation);
        }
    }
    // ============================================================
    // RESET / BASE MAP
    // ============================================================
    protected void Reset()
    {
        allTiles.Clear();
        tileRooms.Clear();
        startLocation = -1;
        stairsLocation = -1;
        roomDetails.Clear();
    }
    protected void GetTiles()
    {
        for (int i = 0; i < size * size; i++)
        {
            allTiles.Add(WALL_TILE);
            tileRooms.Add(-1);
        }
    }
    // ============================================================
    // ROOM GENERATION
    // ============================================================
    protected void MakeRooms(int tries = 100)
    {
        roomDetails.Clear();
        for (int i = 0; i < tries; i++)
        {
            if (roomDetails.Count >= maxRooms)
            {
                break;
            }

            TryToMakeRoom();
        }
        if (roomDetails.Count <= 1)
        {
            MakeFallbackRoom();
        }
    }
    protected void MakeFallbackRoom()
    {
        for (int i = 0; i < allTiles.Count; i++)
        {
            allTiles[i] = FLOOR_TILE;
            tileRooms[i] = 1;
        }
    }
    protected void TryToMakeRoom()
    {
        int startPoint = Random.Range(0, allTiles.Count);
        int direction = Random.Range(0, 4);
        int maxRoomDimension = minRoomSize + Mathf.Max(1, (size / minRoomSize) / 2);
        int width = Random.Range(minRoomSize, maxRoomDimension + 1);
        int height = Random.Range(minRoomSize, maxRoomDimension + 1);
        List<int> roomTiles = new List<int>();
        if (!CheckRoomTiles(roomTiles, startPoint, direction, width, height)){return;}
        startPoint = AdjustStart(startPoint, direction);
        string roomDets = startPoint + "|" + direction + "|" + width + "|" + height;
        int roomNumber = roomDetails.Count;
        roomDetails.Add(roomDets);
        bool irregular = Random.value < IRREGULAR_ROOM_CHANCE;
        for (int j = 0; j < width; j++)
        {
            for (int k = 0; k < height; k++)
            {
                int index = k * width + j;
                if (index < 0 || index >= roomTiles.Count)
                {
                    continue;
                }
                int tile = roomTiles[index];
                bool border = j == 0 || j == width - 1 || k == 0 || k == height - 1;
                bool corner = (j == 0 || j == width - 1) && (k == 0 || k == height - 1);
                // Keep side walls but allow the interior to be open.
                if (border)
                {
                    allTiles[tile] = WALL_TILE;
                }
                else
                {
                    allTiles[tile] = FLOOR_TILE;
                    tileRooms[tile] = roomNumber;
                }
                // Cut corners for some rooms.
                if (irregular && corner)
                {
                    allTiles[tile] = WALL_TILE;
                    tileRooms[tile] = -1;
                }
            }
        }
    }
    // ============================================================
    // ROOM GRAPH
    // ============================================================
    protected void ConnectRoomGraph()
    {
        if (roomDetails.Count <= 1)
        {
            return;
        }
        // First create a guaranteed connected graph.
        // Each room gets connected to an already-connected room.
        // This gives us a spanning tree, meaning every room is
        // reachable without needing random luck.
        List<int> connectedRooms = new List<int>();
        int firstRoom = Random.Range(0, roomDetails.Count);
        connectedRooms.Add(firstRoom);
        while (connectedRooms.Count < roomDetails.Count)
        {
            int newRoom = -1;
            int connectionRoom = -1;
            float bestDistance = float.MaxValue;
            // Find a reasonably nearby unconnected room.
            for (int i = 0; i < roomDetails.Count; i++)
            {
                if (connectedRooms.Contains(i))
                {
                    continue;
                }
                int pointA = GetRoomCenter(i);
                for (int j = 0; j < connectedRooms.Count; j++)
                {
                    int pointB = GetRoomCenter(connectedRooms[j]);
                    float distance = GetGridDistance(pointA, pointB);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        newRoom = i;
                        connectionRoom = connectedRooms[j];
                    }
                }
            }
            if (newRoom < 0 || connectionRoom < 0)
            {
                break;
            }
            ConnectRooms(connectionRoom, newRoom);
            connectedRooms.Add(newRoom);
        }
        // Add occasional extra edges.
        // The spanning tree above guarantees connectivity.
        // These extra connections create loops and alternate routes.
        for (int i = 0; i < roomDetails.Count; i++)
        {
            for (int j = i + 1; j < roomDetails.Count; j++)
            {
                if (Random.value > EXTRA_CONNECTION_CHANCE)
                {
                    continue;
                }
                ConnectRooms(i, j);
            }
        }
    }
    protected int GetFarthestRoom(int startRoom)
    {
        int farthestRoom = startRoom;
        float farthestDistance = -1;
        int startPoint = GetRoomCenter(startRoom);
        for (int i = 0; i < roomDetails.Count; i++)
        {
            if (i == startRoom)
            {
                continue;
            }
            int point = GetRoomCenter(i);
            float distance = GetGridDistance(startPoint, point);
            if (distance > farthestDistance)
            {
                farthestDistance = distance;
                farthestRoom = i;
            }
        }
        return farthestRoom;
    }
    protected int GetRoomCenter(int roomNum)
    {
        string[] roomInfo = roomDetails[roomNum].Split("|");
        int startPoint = int.Parse(roomInfo[0]);
        int direction = int.Parse(roomInfo[1]);
        int width = int.Parse(roomInfo[2]);
        int height = int.Parse(roomInfo[3]);
        // Convert the room's starting corner into a row/column.
        int startRow = mapUtility.GetRow(startPoint, size);
        int startCol = mapUtility.GetColumn(startPoint, size);
        int rowOffset = height / 2;
        int colOffset = width / 2;
        switch (direction)
        {
            case 0:
                startCol += colOffset;
                startRow += rowOffset;
                break;
            case 1:
                startCol -= colOffset;
                startRow += rowOffset;
                break;
            case 2:
                startCol += colOffset;
                startRow -= rowOffset;
                break;
            case 3:
                startCol -= colOffset;
                startRow -= rowOffset;
                break;
        }
        startCol = Mathf.Clamp(startCol, 0, size - 1);
        startRow = Mathf.Clamp(startRow, 0, size - 1);
        return startRow * size + startCol;
    }
    // ============================================================
    // LOCATION SELECTION
    // ============================================================
    protected int GetRandomPointInAnyRoom(int except = -1, List<int> otherExceptions = null)
    {
        if (roomDetails.Count <= 0)
        {
            return -1;
        }
        // Try several random rooms before giving up.
        for (int attempt = 0; attempt < 50; attempt++)
        {
            int roomNum = Random.Range(0, roomDetails.Count);

            int location = GetRandomPointInRoom(roomNum, except, otherExceptions);
            if (location >= 0)
            {
                return location;
            }
        }
        // Fallback: search all rooms.
        for (int roomNum = 0; roomNum < roomDetails.Count; roomNum++)
        {
            int location = GetRandomPointInRoom(roomNum, except, otherExceptions);
            if (location >= 0)
            {
                return location;
            }
        }
        return -1;
    }
    protected int GetRandomPointInRoom(int roomNum, int except = -1, List<int> otherExceptions = null)
    {
        if (roomNum < 0 || roomNum >= roomDetails.Count)
        {
            return -1;
        }
        List<int> roomTiles = GetRoomTiles(roomNum);
        if (roomTiles.Count <= 0)
        {
            return -1;
        }
        // Random attempts first.
        for (int attempt = 0; attempt < 25; attempt++)
        {
            int start = roomTiles[Random.Range(0, roomTiles.Count)];
            if (!IsValidLocation(start, except, otherExceptions))
            {
                continue;
            }
            allTiles[start] = FLOOR_TILE;
            return start;
        }
        // Deterministic fallback.
        for (int i = 0; i < roomTiles.Count; i++)
        {
            int start = roomTiles[i];
            if (!IsValidLocation(start, except, otherExceptions))
            {
                continue;
            }
            allTiles[start] = FLOOR_TILE;
            return start;
        }
        return -1;
    }
    protected bool IsValidLocation(int location, int except, List<int> otherExceptions)
    {
        if (location < 0 || location >= allTiles.Count)
        {
            return false;
        }
        if (location == except)
        {
            return false;
        }
        if (otherExceptions != null)
        {
            for (int i = 0; i < otherExceptions.Count; i++)
            {
                int other = otherExceptions[i];
                if (location == other)
                {
                    return false;
                }
                if (GetGridDistance(location, other) < MIN_IMPORTANT_LOCATION_DISTANCE)
                {
                    return false;
                }
            }
        }
        return true;
    }
    // ============================================================
    // CONNECTIVITY
    // ============================================================
    protected bool IsReachable(int start, int end)
    {
        if (start < 0 || end < 0 || start >= allTiles.Count || end >= allTiles.Count)
        {
            return false;
        }
        if (start == end)
        {
            return true;
        }
        HashSet<int> visited =
            new HashSet<int>();
        Queue<int> queue =
            new Queue<int>();
        queue.Enqueue(start);
        visited.Add(start);
        while (queue.Count > 0)
        {
            int current = queue.Dequeue();
            for (int direction = 0;
                 direction < 6;
                 direction++)
            {
                int next = PointInDirection(
                    current,
                    direction
                );
                if (next < 0 ||
                    next >= allTiles.Count)
                {
                    continue;
                }
                if (visited.Contains(next))
                {
                    continue;
                }
                // For now only WALL_TILE is impassable.
                // Later this becomes the dungeon's
                // "is passable?" data structure.
                if (allTiles[next] == WALL_TILE)
                {
                    continue;
                }
                if (next == end)
                {
                    return true;
                }
                visited.Add(next);
                queue.Enqueue(next);
            }
        }
        return false;
    }
    protected float GetGridDistance(int pointA, int pointB)
    {
        int rowA = mapUtility.GetRow(pointA, size);
        int colA = mapUtility.GetColumn(pointA, size);
        int rowB = mapUtility.GetRow(pointB, size);
        int colB = mapUtility.GetColumn(pointB, size);
        return Mathf.Abs(rowA - rowB) + Mathf.Abs(colA - colB);
    }
    // PATH CARVING
    protected void ConnectPointyTopPoints(int start, int end)
    {
        if (start == end){return;}
        if (mapUtility.StraightLineBetweenPoints(start, end, size))
        {
            List<int> path = mapUtility.GetTileInLineBetweenPoints(start, end, size);
            for (int i = 0; i < path.Count; i++)
            {
                if (path[i] >= 0)
                {
                    allTiles[path[i]] = FLOOR_TILE;
                }
            }
            return;
        }
        int intersection = mapUtility.GetStraightPathIntersection(start, end, size);
        if (intersection < 0){return;}
        // First straight segment.
        List<int> firstPath = mapUtility.GetTileInLineBetweenPoints(start, intersection, size);
        for (int i = 0; i < firstPath.Count; i++)
        {
            if (firstPath[i] >= 0)
            {
                allTiles[firstPath[i]] = FLOOR_TILE;
            }
        }
        // Second straight segment.
        List<int> secondPath = mapUtility.GetTileInLineBetweenPoints(intersection, end, size);
        for (int i = 0; i < secondPath.Count; i++)
        {
            if (secondPath[i] >= 0)
            {
                allTiles[secondPath[i]] = FLOOR_TILE;
            }
        }
        // Make sure the intersection itself is included.
        allTiles[intersection] = FLOOR_TILE;
    }
    protected void ConnectPoints(int startPoint, int endPoint)
    {
        if (startPoint < 0 || endPoint < 0 || startPoint >= allTiles.Count || endPoint >= allTiles.Count)
        {
            return;
        }
        if (!mapUtility.flatTop)
        {
            ConnectPointyTopPoints(startPoint, endPoint);
            return;
        }
        int maxIterations = size * size * 2;
        for (int i = 0; i < maxIterations; i++)
        {
            if (startPoint == endPoint)
            {
                break;
            }
            int startRow = startPoint / size;
            int startCol = GetColumn(startPoint);
            int endRow = endPoint / size;
            int endCol = GetColumn(endPoint);
            List<int> possibleNextPoints = new List<int>();
            if (startRow < endRow)
            {
                possibleNextPoints.Add(PointInDirection(startPoint, 3));
                if (startCol > endCol)
                {
                    possibleNextPoints.Add(PointInDirection(startPoint, 4));
                }
                if (startCol < endCol)
                {
                    possibleNextPoints.Add(PointInDirection(startPoint, 2));
                }
            }
            else if (startRow > endRow)
            {
                possibleNextPoints.Add(PointInDirection(startPoint, 0));
                if (startCol > endCol)
                {
                    possibleNextPoints.Add(PointInDirection(startPoint, 5));
                }
                if (startCol < endCol)
                {
                    possibleNextPoints.Add(PointInDirection(startPoint, 1));
                }
            }
            if (startCol < endCol)
            {
                if (startRow > 0 ||(startCol % 2 == 1 && startCol < size - 1))
                {
                    possibleNextPoints.Add(PointInDirection(startPoint, 1));
                }
                if (startRow < size - 1 || (startCol % 2 == 0 && startCol < size - 1))
                {
                    possibleNextPoints.Add(PointInDirection(startPoint, 2));
                }
            }
            else if (startCol > endCol)
            {
                if (startRow > 0 || startCol % 2 == 1)
                {
                    possibleNextPoints.Add(PointInDirection(startPoint, 5));
                }
                if (startRow < size - 1 || (startCol % 2 == 0 && startCol > 0))
                {
                    possibleNextPoints.Add(PointInDirection(startPoint, 4));
                }
            }
            RemoveInvalidPoints(possibleNextPoints);
            if (possibleNextPoints.Count <= 0)
            {
                break;
            }
            // Prefer points that actually get closer to the target.
            // Keep randomness so corridors aren't always straight.
            possibleNextPoints.Sort(
                delegate (int a, int b)
                {
                    float distanceA = GetGridDistance(a, endPoint);
                    float distanceB = GetGridDistance(b, endPoint);
                    return distanceA.CompareTo(distanceB);
                }
            );
            int choiceCount = Mathf.Min(possibleNextPoints.Count, 2);
            startPoint = possibleNextPoints[Random.Range(0, choiceCount)];
            allTiles[startPoint] = FLOOR_TILE;
        }
        // Final direct attempt if the randomized path somehow
        // failed to reach the destination.
        if (!IsReachable(startPoint, endPoint))
        {
            ConnectDirectly(startPoint, endPoint);
        }
    }
    protected void ConnectDirectly(int start, int end)
    {
        int current = start;
        int maxIterations = size * size * 2;
        for (int i = 0; i < maxIterations; i++)
        {
            if (current == end)
            {
                return;
            }
            int currentRow = mapUtility.GetRow(current, size);
            int currentCol = mapUtility.GetColumn(current, size);
            int endRow = mapUtility.GetRow(end, size);
            int endCol = mapUtility.GetColumn(end, size);
            int bestDirection = -1;
            float bestDistance = float.MaxValue;
            for (int direction = 0; direction < 6; direction++)
            {
                int next = PointInDirection(current, direction);
                if (next < 0 || next >= allTiles.Count)
                {
                    continue;
                }
                float distance = GetGridDistance(next, end);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestDirection = direction;
                }
            }
            if (bestDirection < 0)
            {
                return;
            }
            current = PointInDirection(current, bestDirection);
            allTiles[current] = FLOOR_TILE;
        }
    }
    protected void RemoveInvalidPoints(List<int> points)
    {
        for (int i = points.Count - 1; i >= 0; i--)
        {
            if (points[i] < 0 || points[i] >= allTiles.Count)
            {
                points.RemoveAt(i);
            }
        }
    }
    // ============================================================
    // ROOM CONNECTION
    // ============================================================
    protected void ConnectRooms(int roomOne, int roomTwo)
    {
        if (roomOne < 0 || roomTwo < 0 || roomOne >= roomDetails.Count || roomTwo >= roomDetails.Count)
        {
            return;
        }
        int startPoint = GetRoomCenter(roomOne);
        int endPoint = GetRoomCenter(roomTwo);
        ConnectPoints(startPoint, endPoint);
    }
    // ============================================================
    // ROOM GEOMETRY
    // ============================================================
    protected int GetColumn(int location)
    {
        return location % size;
    }
    protected int PointInDirection(int location, int direction)
    {
        return mapUtility.PointInDirection(location, direction, size);
    }
    protected int AdjustStart(int start, int direction)
    {
        switch (direction)
        {
            case 0:
                return PointInDirection(start, 2);
            case 1:
                return PointInDirection(start, 4);
            case 2:
                return PointInDirection(start, 1);
            case 3:
                return PointInDirection(start, 5);
        }
        return start;
    }
    protected bool CheckRoomTiles(List<int> roomTiles, int startPoint, int direction, int width, int height)
    {
        int nextTile = startPoint;
        int startRow = mapUtility.GetRow(startPoint, size);
        int startCol = mapUtility.GetColumn(startPoint, size);
        switch (direction)
        {
            case 0:
                if (startCol + width >= size ||
                    startRow + height >= size)
                {
                    return false;
                }
                for (int i = 0; i < height; i++)
                {
                    for (int j = 0; j < width; j++)
                    {
                        if (nextTile < 0 ||
                            nextTile >= size * size ||
                            allTiles[nextTile] != WALL_TILE)
                        {
                            return false;
                        }

                        roomTiles.Add(nextTile);
                        nextTile++;
                    }

                    nextTile -= width;
                    nextTile += size;
                }
                break;
            case 1:
                if (startCol - width < 0 ||
                    startRow + height >= size)
                {
                    return false;
                }
                for (int i = 0; i < height; i++)
                {
                    for (int j = 0; j < width; j++)
                    {
                        if (nextTile < 0 ||
                            nextTile >= size * size ||
                            allTiles[nextTile] != WALL_TILE)
                        {
                            return false;
                        }

                        roomTiles.Add(nextTile);
                        nextTile--;
                    }
                    nextTile += width;
                    nextTile += size;
                }
                break;
            case 2:
                if (startCol + width >= size ||
                    startRow - height < 0)
                {
                    return false;
                }
                for (int i = 0; i < height; i++)
                {
                    for (int j = 0; j < width; j++)
                    {
                        if (nextTile < 0 ||
                            nextTile >= size * size ||
                            allTiles[nextTile] != WALL_TILE)
                        {
                            return false;
                        }

                        roomTiles.Add(nextTile);
                        nextTile++;
                    }
                    nextTile -= width;
                    nextTile -= size;
                }
                break;
            case 3:
                if (startCol - width < 0 ||
                    startRow - height < 0)
                {
                    return false;
                }
                for (int i = 0; i < height; i++)
                {
                    for (int j = 0; j < width; j++)
                    {
                        if (nextTile < 0 ||
                            nextTile >= size * size ||
                            allTiles[nextTile] != WALL_TILE)
                        {
                            return false;
                        }

                        roomTiles.Add(nextTile);
                        nextTile--;
                    }

                    nextTile += width;
                    nextTile -= size;
                }
                break;
            default:
                return false;
        }
        return true;
    }

    // ============================================================
    // ROOM TILE RETRIEVAL
    // ============================================================

    protected List<int> GetRoomTiles(int roomNumber)
    {
        List<int> roomTiles = new List<int>();
        for (int i = 0; i < tileRooms.Count; i++)
        {
            if (tileRooms[i] == roomNumber)
            {
                roomTiles.Add(i);
            }
        }
        return roomTiles;
    }
}