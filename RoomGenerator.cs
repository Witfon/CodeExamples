using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RoomTools;

namespace RoomTools
{
    public enum RoomType { Normal, Start, Boss, Brew, Loot };
}


public class RoomGenerator : MonoBehaviour
{
    public bool generateOnStart = true;
    public float maxRooms;
    public bool drawDebug;

    public RoomLibrary roomLibrary;

    List<GameObject> roomObjects;

    public Vector2 roomSize;

    public class RoomTile
    {
        public bool[] openings = new bool[4];
        //public string roomType;
        public RoomType roomType;

        public GameObject roomObject;

        public void SetOpening(int direction, bool isOpen)
        {
            if (direction >= 0 && direction <= 3)
                openings[direction] = isOpen;

        }

        public int GetSideCount(bool isOpen)
        {
            int sideAmount = 0;
            for (int i = 0; i < openings.Length; i++)
            {
                if (openings[i] == isOpen)
                {
                    sideAmount++;
                }
            }

            return sideAmount;
        }

        public List<int> GetSides(bool isOpen)
        {
            List<int> matchingSides = new List<int>();

            for (int i = 0; i < openings.Length; i++)
            {
                if (openings[i] == isOpen)
                {
                    matchingSides.Add(i);
                }
            }
            return matchingSides;
        }

        public int GetRandomSide(bool isOpen)
        {
            List<int> randomSide = new List<int>();

            for (int i = 0; i < openings.Length; i++)
            {
                if (openings[i] == isOpen)
                {
                    randomSide.Add(i);
                }
            }

            if (randomSide.Count > 0)
            {
                return randomSide[Random.Range(0, randomSide.Count)];
            }

            Debug.LogError("Can not get random opening, the room contains no openings!");
            return -1;
        }
    }

    List<RoomType> specialRoomTypes;

    public Dictionary<Vector2, RoomTile> tileDict;

    public int lootRooms;
    List<Vector2> cantSpawnLootRooms;

    public static RoomGenerator s;

    public bool useRoomWeight;

    void InitiateLists()
    {
        specialRoomTypes = new List<RoomType>() { RoomType.Boss, RoomType.Brew, RoomType.Start, RoomType.Loot };

        cantSpawnLootRooms = new List<Vector2>();

        if (roomObjects != null)
        {
            for (int i = 0; i < roomObjects.Count; i++)
            {
                Destroy(roomObjects[i]);
            }
        }
        
        roomObjects = new List<GameObject>();
    }

    /*private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            
            //Generate();
        }
    }*/

    private void Awake()
    {
        s = this;
    }

    private void Start()
    {
        if (generateOnStart)
        {
            Generate();
        }
    }

    public void Generate()
    {
        tileDict = new Dictionary<Vector2, RoomTile>();
        InitiateLists();

        Vector2 currentGenPos = new Vector2();

        tileDict.Add(currentGenPos, new RoomTile() { roomType = RoomType.Start });

        for (int i = 0; i < maxRooms; i++)
        {
            Vector2 tileDir = SideToDir(Random.Range(0, 4));
            currentGenPos += tileDir;

            if (!tileDict.ContainsKey(currentGenPos))
            {
                tileDict.Add(currentGenPos, new RoomTile());
            }
            else
            {
                i--;
            }
        }

        //setting room directions
        SetRoomOpenings();

        try
        {
            SetBossRoom();
        }
        catch (System.Exception)
        {
            Debug.LogWarning("Boss Room Error!");
        }

        try
        {
            SetBrewRoom();
        }
        catch (System.Exception)
        {
            Debug.LogWarning("Brew Room Error!");
        }

        

        for (int i = 0; i < lootRooms; i++)
        {
            //Debug.Log("Generating loot room " + i);
            try
            {
                SetLootRoom();
            }
            catch (System.Exception)
            {
                Debug.LogWarning("Loot Room Error!");
            }
            
        }

        try
        {
            SpawnRooms();
        }
        catch (System.Exception)
        {
            Debug.LogWarning("Error while spawning rooms!!");
        }

        try
        {
            PrepareRooms();
        }
        catch (System.Exception)
        {
            Debug.LogWarning("Error while preparing rooms!!");
        }
        

        //Minimap.s.UpdateMap();
    }

    public void PrepareRooms()
    {
        /*for (int i = 0; i < roomObjects.Count; i++)
        {
            RoomStats roomScript = roomObjects[i].GetComponent<RoomStats>();
            if (roomScript.dynamicSides)
            {

            }
        }

        for (int i = 0; i < roomObjects.Count; i++)
        {
            
            roomScript.SetPassageBlockers();
        }*/

        foreach (Vector2 key in tileDict.Keys)
        {
            RoomStats roomScript = tileDict[key].roomObject.GetComponent<RoomStats>();
            if (/*roomScript.dynamicSides*/ true)
            {
                bool[] blockedDynamicSides = new bool[4];

                if (!CheckIfRoomExists(key + new Vector2(0, 1)))
                    blockedDynamicSides[0] = true;

                if (!CheckIfRoomExists(key + new Vector2(1, 0)))
                    blockedDynamicSides[1] = true;

                if (!CheckIfRoomExists(key + new Vector2(0, -1)))
                    blockedDynamicSides[2] = true;

                if (!CheckIfRoomExists(key + new Vector2(-1, 0)))
                    blockedDynamicSides[3] = true;

                roomScript.SetPassageBlockers(blockedDynamicSides);

                roomScript.SetDoors(blockedDynamicSides);
            }
            else
            {
                //roomScript.SetDoors(tileDict[key].openings);
            }
        }
    }

    public bool CheckIfRoomExists(Vector2 pos)
    {
        if (tileDict.ContainsKey(pos))
        {
            return true;
        }
        return false;
    }

    public void SpawnRooms()
    {
        foreach (Vector2 key in tileDict.Keys)
        {
            //DONT FORGET TO ADD WEIGHT AND RARITY!!!
            InstantiateRoom(tileDict[key].roomType, key);
        }
    }

    void InstantiateRoom(RoomType _roomType, Vector2 _roomPos)
    {
        GameObject[] libraryRooms;
        
        if (useRoomWeight)
        {
            libraryRooms = WeightRooms(roomLibrary.GetLibrary(_roomType));
        }
        else
        {
            List<GameObject> libraryRoomsList = new List<GameObject>();

            for (int i = 0; i < roomLibrary.GetLibrary(_roomType).rooms.Length; i++)
            {
                libraryRoomsList.Add(roomLibrary.GetLibrary(_roomType).rooms[i].room);
            }
            libraryRooms = libraryRoomsList.ToArray();
        }

        
        List<GameObject> roomsToSpawn = new List<GameObject>();

        //choosing which rooms to pick from. They must match bu sides
        for (int i = 0; i < libraryRooms.Length; i++)
        {
            RoomStats roomScript = libraryRooms[i].GetComponent<RoomStats>();
            if (roomScript.dynamicSides || MatchSides(roomScript.sides, tileDict[_roomPos].openings))
            {
                roomsToSpawn.Add(libraryRooms[i]);
            }
        }

        //removes dynamic rooms if there is a matching non-dynamic room
        bool removeDynamicRooms = false;
        for (int i = 0; i < roomsToSpawn.Count; i++)
        {
            if (!roomsToSpawn[i].GetComponent<RoomStats>().dynamicSides)
            {
                removeDynamicRooms = true;
                break;
            }
        }

        if (removeDynamicRooms)
        {
            for (int i = 0; i < roomsToSpawn.Count; i++)
            {
                if (roomsToSpawn[i].GetComponent<RoomStats>().dynamicSides)
                {
                    roomsToSpawn.RemoveAt(i);
                }
            }
        }

        GameObject spawnedRoom = Instantiate(roomsToSpawn[Random.Range(0, roomsToSpawn.Count)], _roomPos * roomSize, Quaternion.identity, transform);
        roomObjects.Add(spawnedRoom);
        tileDict[_roomPos].roomObject = spawnedRoom;
        spawnedRoom.GetComponent<RoomStats>().posInGenerator = _roomPos;
    }

    GameObject[] WeightRooms(RoomLibrary.Library _roomLib)
    {
        //float minWeight = 100;
        /*for (int i = 0; i < _roomLib.rooms.Length; i++)
        {
            minWeight = Mathf.Min(minWeight, _roomLib.rooms[i].weight);
        }*/

        float randomVal = Random.Range(0, 100);
        List<GameObject> roomCandidates = new List<GameObject>();

        for (int i = 0; i < _roomLib.rooms.Length; i++)
        {
            if (randomVal < _roomLib.rooms[i].weight)
            {
                roomCandidates.Add(_roomLib.rooms[i].room);
            }
        }

        return roomCandidates.ToArray();
    }

    bool MatchSides(bool[] _sidesA, bool[] _sidesB)
    {
        for (int i = 0; i < 4; i++)
        {
            if (_sidesA[i] != _sidesB[i])
            {
                return false;
            }
        }
        return true;
    }

    void SetRoomOpenings()
    {
        foreach (Vector2 key in tileDict.Keys)
        {
            if (tileDict.ContainsKey(key + Vector2.up))
            {
                tileDict[key].SetOpening(0, true);
            }
            else
            {
                tileDict[key].SetOpening(0, false);
            }

            if (tileDict.ContainsKey(key + Vector2.right))
            {
                tileDict[key].SetOpening(1, true);
            }
            else
            {
                tileDict[key].SetOpening(1, false);
            }

            if (tileDict.ContainsKey(key + Vector2.down))
            {
                tileDict[key].SetOpening(2, true);
            }
            else
            {
                tileDict[key].SetOpening(2, false);
            }

            if (tileDict.ContainsKey(key + Vector2.left))
            {
                tileDict[key].SetOpening(3, true);
            }
            else
            {
                tileDict[key].SetOpening(3, false);
            }
        }
    }

    void SetBossRoom()
    {
        float maxRoomDist = 0;
        Vector2 distantRoomPos = new Vector2();

        List<Vector2> deadEnds = GetRoomsWithOpenings(1);

        for (int i = 0; i < deadEnds.Count; i++)
        {
            if (tileDict[deadEnds[i]].roomType == RoomType.Start)
            {
                deadEnds.RemoveAt(i);
                break;
            }
        }

        if (deadEnds.Count > 0)
        {
            for (int i = 0; i < deadEnds.Count; i++)
            {
                float roomDist = Vector2.Distance(Vector2.zero, deadEnds[i]);
                if (roomDist > maxRoomDist)
                {
                    maxRoomDist = roomDist;
                    distantRoomPos = deadEnds[i];
                }
            }
        }
        else
        {
            //places the boss room as far away as possible if there are no dead ends
            foreach (Vector2 key in tileDict.Keys)
            {
                float roomDist = Vector2.Distance(Vector2.zero, key);
                if (roomDist > maxRoomDist)
                {
                    maxRoomDist = roomDist;
                    distantRoomPos = key;
                }
            }
        }

        tileDict[distantRoomPos].roomType = RoomType.Boss;
        SetRoomOpenings();
    }

    void SetBrewRoom()
    {
        tileDict[GetRandomRoomKey(specialRoomTypes)].roomType = RoomType.Brew;
    }

    void SetLootRoom()
    {
        //loot rooms are generated as attachments
        //NEED TO MAKE SO THE LOOT ROOMS DONT SPAWN NEXT TO EACH OTHER!!!
        List<RoomType> lootRoomBlackList = new List<RoomType>();
        lootRoomBlackList = specialRoomTypes;
        lootRoomBlackList.Remove(RoomType.Start);
        lootRoomBlackList.Remove(RoomType.Brew);

        for (int i = 0; i < tileDict.Count; i++)
        {
            Vector2 randomRoomKey = GetRandomRoomKey(lootRoomBlackList, cantSpawnLootRooms);

            int[] walledSides = tileDict[randomRoomKey].GetSides(false).ToArray();

            if (walledSides.Length < 4)
            {
                for (int j = 0; j < walledSides.Length; j++)
                {
                    //int randomSide = tileDict[randomRoomKey].GetRandomSide(false);

                    //Vector2 newLootRoomPos = SideToDir(randomSide) + randomRoomKey;

                    Vector2 newLootRoomPos = SideToDir(walledSides[j]) + randomRoomKey;

                    //check if the new position is adjacent to another loot room
                    if (!HasNeighbor(newLootRoomPos, RoomType.Loot))
                    {
                        tileDict.Add(newLootRoomPos, new RoomTile() {roomType = RoomType.Loot});
                        cantSpawnLootRooms.Add(newLootRoomPos);

                        SetRoomOpenings();
                        return;
                    }
                }

            }
            //if the random room had no solid sides or if it has another loot room adjacent to the spot
            cantSpawnLootRooms.Add(randomRoomKey);
        }

        Debug.LogError("Can't spawn loot room!");
    }

    bool HasNeighbor(Vector2 roomPos, RoomType roomType)
    {

        try
        {
            if (tileDict[roomPos + Vector2.up].roomType == roomType)
            {
                return true;
            }
        }
        catch
        {
        }

        try
        {
            if (tileDict[roomPos + Vector2.right].roomType == roomType)
            {
                return true;
            }
        }
        catch
        {
        }

        try
        {
            if (tileDict[roomPos + Vector2.down].roomType == roomType)
            {
                return true;
            }
        }
        catch
        {
        }

        try
        {
            if (tileDict[roomPos + Vector2.left].roomType == roomType)
            {
                return true;
            }
        }
        catch
        {
        }

        return false;
    }

    Vector2 GetRandomRoomKey(List<RoomType> typeBlacklist)
    {
        return GetRandomRoomKey(typeBlacklist, new List<Vector2>());
    }

    Vector2 GetRandomRoomKey(List<RoomType> typeBlacklist, List<Vector2> posBlacklist)
    {
        List<Vector2> allKeys = GetAllRoomKeys();

        for (int i = 0; i < tileDict.Count; i++)
        {
            Vector2 randomRoomKey = allKeys[Random.Range(0, allKeys.Count)];

            for (int j = 0; j < typeBlacklist.Count; j++)
            {
                if (tileDict[randomRoomKey].roomType == typeBlacklist[j])
                {
                    allKeys.Remove(randomRoomKey);
                    break;
                }
            }

            for (int j = 0; j < posBlacklist.Count; j++)
            {
                if (randomRoomKey == posBlacklist[j])
                {
                    //Debug.Log("Found conflicting room pos, removing...");
                    try
                    {
                        allKeys.Remove(randomRoomKey);
                    }
                    catch
                    {
                    }
                    break;
                }
            }

            if (allKeys.Contains(randomRoomKey))
            {
                return (randomRoomKey);
            }
        }

        Debug.LogError("Could not find a matching room!");
        return Vector2.zero;
    }

    List<Vector2> GetAllRoomKeys()
    {
        List<Vector2> allKeys = new List<Vector2>();
        foreach (Vector2 key in tileDict.Keys)
        {
            allKeys.Add(key);
        }

        return allKeys;
    }

    List<Vector2> GetRoomsWithOpenings(int openingAmount)
    {
        List<Vector2> matchingRooms = new List<Vector2>();

        foreach (Vector2 key in tileDict.Keys)
        {
            if (tileDict[key].GetSideCount(true) == openingAmount)
            {
                matchingRooms.Add(key);
            }
        }

        return matchingRooms;
    }

    public Vector2 SideToDir(int _dir)
    {
        switch (_dir)
        {
            case 0:
                return new Vector2(0,1);

            case 1:
                return new Vector2(1, 0);

            case 2:
                return new Vector2(0, -1);

            case 3:
                return new Vector2(-1, 0);

            default:
                break;
        }

        Debug.LogError("Can't get random direction! The input must be between 0-3!");
        return Vector3.zero;
    }

    private void OnDrawGizmos()
    {
        if (tileDict != null && drawDebug)
        {

            foreach (Vector2 key in tileDict.Keys) // loop through keys
            {
                if (tileDict[key].roomType == RoomType.Boss)
                {
                    Gizmos.color = Color.red;
                }
                else if (tileDict[key].roomType == RoomType.Brew)
                {
                    Gizmos.color = Color.cyan;
                }
                else if (tileDict[key].roomType == RoomType.Loot)
                {
                    Gizmos.color = Color.green;
                }
                else
                {
                    Gizmos.color = Color.white;
                }

                Gizmos.DrawCube(key, Vector3.one * 0.9f);
            }

            Gizmos.color = Color.yellow;

            foreach (Vector2 key in tileDict.Keys)
            {
                float drawDoorDisplace = 0.4f;
                float drawDoorSize = 0.2f;

                if (tileDict[key].openings[0] == true)
                {
                    Gizmos.DrawCube(key + (Vector2.up * drawDoorDisplace), Vector3.one * drawDoorSize);
                }

                if (tileDict[key].openings[1] == true)
                {
                    Gizmos.DrawCube(key + (Vector2.right * drawDoorDisplace), Vector3.one * drawDoorSize);
                }

                if (tileDict[key].openings[2] == true)
                {
                    Gizmos.DrawCube(key + (Vector2.down * drawDoorDisplace), Vector3.one * drawDoorSize);
                }

                if (tileDict[key].openings[3] == true)
                {
                    Gizmos.DrawCube(key + (Vector2.left * drawDoorDisplace), Vector3.one * drawDoorSize);
                }
            }
        }
    }

}
