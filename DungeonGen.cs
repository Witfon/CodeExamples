using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonGen : MonoBehaviour
{
    public string stringSeed;
    public int seed;
    public bool generateAtStart;
    public enum RoomTypes {Normal, BranchEnd, Big2x2, Big2x4};

    [Header("Generation")]
    public int genCycles;
    public int mainBranchLength;
    public bool autoMainBranchLength;//if set to false, the user can manually control the main branch length
    public Vector2 branchGenCycles;
    List<Tile> roomList;

    [Header("Branch Gen")]
    public float openSpaceDist;//how far should the generator check for free space before generating a branch in that direction
    public float branchCentralizeCoeff;//room branches will be less likely to stretch out in one direction

    public class Tile
    {
        public Vector2 pos;
        //public List<Vector2> openings;
        //public bool bigRoom;
        public List<Tile> bigRoomParts = new List<Tile>();
        //public bool lastBranchRoom;
        public RoomTypes roomType;
    }

    //all rooms are in here
    public Dictionary<Vector2, Tile> genDict;

    void GenerateSeed()
    {
        if (stringSeed == "")
        {
            if (seed == 0)
            {
                seed = Random.Range(-int.MaxValue, int.MaxValue);
            }
        }
        else
        {
            seed = stringSeed.GetHashCode();
        }

        Random.InitState(seed);
    }

    public void Start()
    {
        GenerateSeed();
        if (generateAtStart)
            Generate();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Generate();
        }
    }

    //generate map
    public void Generate()
    {
        if (autoMainBranchLength)
            mainBranchLength = genCycles * 2;

        Debug.ClearDeveloperConsole();
        roomList = new();
        genDict = new Dictionary<Vector2, Tile>();
        Vector2 genPos = new();
        int oldGenDir = 0;
        Quaternion genRotation = new();

        if (openSpaceDist == 0)
            openSpaceDist = 1;

        for (int j = 0; j < genCycles; j++)
        {
            if (j > 0)//chooses a room to branch from
            {
                Vector2 spawnedBranchDir = new();
                List<Tile> openRoomList = new(roomList);
                for (int i = 0; i < roomList.Count; i++)
                {
                    Tile branchRoom = openRoomList[Random.Range(0, openRoomList.Count)];

                    List<Vector2> possiblePositions = new();

                    if (!genDict.ContainsKey(branchRoom.pos + Vector2.up))
                    {
                        possiblePositions.Add(branchRoom.pos + Vector2.up);
                        spawnedBranchDir = Vector2.up;
                    }
                    if (!genDict.ContainsKey(branchRoom.pos + Vector2.right))
                    {
                        possiblePositions.Add(branchRoom.pos + Vector2.right);
                        spawnedBranchDir = Vector2.right;
                    }
                    if (!genDict.ContainsKey(branchRoom.pos + Vector2.left))
                    {
                        possiblePositions.Add(branchRoom.pos + Vector2.left);
                        spawnedBranchDir = Vector2.left;
                    }

                    if (possiblePositions.Count > 1)
                    {
                        bool foundBranchRoom = true;
                        for (int k = 1; k < openSpaceDist; k++)
                        {
                            if (genDict.ContainsKey(branchRoom.pos + (spawnedBranchDir * k)))
                            {
                                foundBranchRoom = false;
                                break;
                            }
                        }

                        if (foundBranchRoom)
                        {
                            genPos = possiblePositions[Random.Range(0, possiblePositions.Count)];
                            break;
                        }
                    }
                    //else
                    openRoomList.Remove(branchRoom);
                }

                if (spawnedBranchDir == Vector2.left)
                    genRotation = Quaternion.Euler(0, 0, 90);
                if (spawnedBranchDir == Vector2.up)
                    genRotation = Quaternion.Euler(0, 0, 0);
                if (spawnedBranchDir == Vector2.right)
                    genRotation = Quaternion.Euler(0, 0, -90);
            }

            int branchCycles = (int)VectorRange(branchGenCycles);
            if (j == 0)
                branchCycles = mainBranchLength;
            
            for (int i = 0; i < branchCycles; i++)
            {
                //first we spawn the room
                int genDir = Random.Range(1, 4);
                Tile generatedTile = new();
                generatedTile.roomType = RoomTypes.Normal;

                if (genDict.TryAdd(genPos, generatedTile))
                {
                    generatedTile.pos = genPos;
                    roomList.Add(generatedTile);
                }
                else
                {
                    break;
                }
                
                //then we choose a new gen direction
                Vector2 newGenVector = new();
                switch (genDir)
                {
                    case 1: //right
                        newGenVector = Vector2.right;
                        if (oldGenDir == 3)
                            newGenVector = Vector2.up;
                        break;

                    case 2: //top
                        newGenVector = Vector2.up;
                        break;

                    case 3: //left
                        newGenVector = Vector2.left;
                        if (oldGenDir == 1)
                            newGenVector = Vector2.up;
                        break;

                    default:
                        break;
                }
                Vector3 newGenRot = genRotation * newGenVector;
                newGenRot = new Vector3(Mathf.RoundToInt(newGenRot.x), Mathf.RoundToInt(newGenRot.y));
                genPos += (Vector2)newGenRot;

                oldGenDir = genDir;

                if (i == branchCycles - 1)
                    generatedTile.roomType = RoomTypes.BranchEnd;

            }
        }

        GenerateBigRooms();
    }

    float VectorRange(Vector2 range)
    {
        return Random.Range(range.x, range.y);
    }

    void GenerateBigRooms()
    {

        Dictionary<Vector2, Tile> checkDict = new(genDict);

        Vector2[] checkPos = new Vector2[3];
        foreach (Vector2 pos in checkDict.Keys)
        {
            if (genDict[pos].roomType == RoomTypes.Normal && genDict[pos].bigRoomParts.Count == 0) //only checks non-big rooms and rooms that are not part of big rooms
            {
                bool foundBigRoom = false;

                for (int i = 0; i < 4; i++)
                {

                    switch (i)
                    {
                        case 0: //left, top-left, top
                            checkPos[0] = new Vector2(-1, 0);
                            checkPos[1] = new Vector2(-1, 1);
                            checkPos[2] = new Vector2(0, 1);
                            break;

                        case 1: //top, top-right, right
                            checkPos[0] = new Vector2(0, 1);
                            checkPos[1] = new Vector2(1, 1);
                            checkPos[2] = new Vector2(1, 0);
                            break;

                        case 2: //right, bot-right, bot
                            checkPos[0] = new Vector2(1, 0);
                            checkPos[1] = new Vector2(1, -1);
                            checkPos[2] = new Vector2(0, -1);
                            break;

                        case 3: //bot, bot-left, left
                            checkPos[0] = new Vector2(0, -1);
                            checkPos[1] = new Vector2(-1, -1);
                            checkPos[2] = new Vector2(-1, 0);
                            break;

                        default:
                            break;
                    }

                    //checking each side
                    for (int j = 0; j < checkPos.Length; j++)
                    {
                        foundBigRoom = true;
                        if (!genDict.TryGetValue(pos + checkPos[j], out Tile tile) || pos == Vector2.zero || pos + checkPos[j] == Vector2.zero)
                        {
                            foundBigRoom = false;
                            break;
                        }
                        else
                        {
                            if (tile.roomType != RoomTypes.Normal || tile.bigRoomParts.Count != 0)
                            {
                                foundBigRoom = false;
                                break;
                            }
                        }
                    }

                    if (foundBigRoom)
                    {
                        Tile spawnedBigRoom = new() { roomType = RoomTypes.Big2x2 };
                        genDict.Add((checkPos[1] / 2f) + pos, spawnedBigRoom);

                        //writing big room parts
                        for (int k = 0; k < checkPos.Length; k++)
                        {
                            //adds all the local composit rooms to the bigRoomParts reference list
                            genDict[pos + checkPos[k]].bigRoomParts.Add(genDict[pos]);
                            for (int l = 0; l < checkPos.Length; l++)
                            {
                                if (checkPos[k] != checkPos[l]) //to avoid adding the room to itself
                                    genDict[pos + checkPos[k]].bigRoomParts.Add(genDict[pos + checkPos[l]]);
                            }
                        }

                        for (int l = 0; l < checkPos.Length; l++)
                        {
                            genDict[pos].bigRoomParts.Add(genDict[pos + checkPos[l]]);
                        }
                        break;
                    }
                }
            }

        }
    }

    private void OnDrawGizmos()
    {
        if (genDict != null)
        {
            foreach (Vector2 pos in genDict.Keys)
            {
                switch (genDict[pos].roomType)
                {
                    case RoomTypes.Normal:
                        Gizmos.color = Color.white;
                        Gizmos.DrawCube(pos, new Vector2(0.9f, 0.9f));
                        break;
                    case RoomTypes.Big2x2:
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawCube(pos, new Vector3(1.9f, 1.9f, 0f));
                        break;
                    case RoomTypes.BranchEnd:
                        Gizmos.color = Color.red;
                        Gizmos.DrawCube(pos, new Vector2(0.9f, 0.9f));
                        break;

                    default:
                        break;
                }
            }
        }
        
    }
}
