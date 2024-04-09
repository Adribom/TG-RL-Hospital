using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.Networking.UnityWebRequest;
using UnityEngine.UIElements;

public class EnvController : MonoBehaviour
{
    private LevelGeneration levelGeneration;
    private SheetAssigner sheetAssigner;

    public class PlayerInfo
    {
        //public AmrAgent Agent;
        [HideInInspector]
        public Vector3 StartingPos;
        [HideInInspector]
        public Quaternion StartingRot;
        [HideInInspector]
        public Rigidbody Rb;
        [HideInInspector]
        public Collider Col;
    }

    /// <summary>
    /// Max Academy steps before this platform resets
    /// </summary>
    /// <returns></returns>
    [Header("Max Environment Steps")] public int MaxEnvironmentSteps = 25000;
    private int m_ResetTimer;

    public List<PlayerInfo> AgentsList = new List<PlayerInfo>();
    private List<Vector2> positionsTakenByAgents = new List<Vector2>();

    private Dictionary<AmrAgent, PlayerInfo> m_PlayerDict = new Dictionary<AmrAgent, PlayerInfo>();
    public bool UseRandomAgentRotation = true;
    public bool UseRandomAgentPosition = true;

    public LayerMask layerForAgentSpawnDetection;
    public GameObject Key;
    private SimpleMultiAgentGroup m_AgentGroup;
    private int deliveryCount = 0;
    private int totalDeliveriesToReset = 10;

    //Start is called before the first frame update
    void Start()
    {
        //Hide The Key
        //Key.SetActive(false);

        //// Initialize TeamManager
        //m_AgentGroup = new SimpleMultiAgentGroup();
        //foreach (var item in AgentsList)
        //{
        //    item.StartingPos = item.Agent.transform.position;
        //    item.StartingRot = item.Agent.transform.rotation;
        //    item.Rb = item.Agent.GetComponent<Rigidbody>();
        //    item.Col = item.Agent.GetComponent<Collider>();
        //    // Add to team manager
        //    m_AgentGroup.RegisterAgent(item.Agent);
        //}
        ResetScene();
    }

    //Update is called once per frame
    void Update()
    {
        //m_ResetTimer += 1;
        //if (m_ResetTimer >= MaxEnvironmentSteps && MaxEnvironmentSteps > 0)
        //{
        //    m_AgentGroup.GroupEpisodeInterrupted();
        //    ResetScene();
        //}


        if (Input.GetKeyDown(KeyCode.Q))
        {
            ResetScene();
        }

    }

    
    //public void ManageSucessfullDelivery()
    //{
    //    print("Surgical instrument delivered!");
    //    m_AgentGroup.AddGroupReward(1f);

    //    if (deliveryCount == totalDeliveriesToReset)
    //        m_AgentGroup.EndGroupEpisode();

    //    ResetScene();
    //}

    //public void AddDeliveryCount()
    //{
    //    deliveryCount++;
    //}

    ///// <summary>
    ///// Use the ground's bounds to pick a random spawn position.
    ///// </summary>
    //public Vector3 GetRandomSpawnPos()
    //{
    //    return Vector3.zero;
    //}
    //Quaternion GetRandomRot()
    //{
    //    return Quaternion.Euler(0, Random.Range(0.0f, 360.0f), 0);
    //}

    void ResetScene()
    {
        levelGeneration = GetComponentInChildren<LevelGeneration>();
        sheetAssigner =  GetComponentInChildren<SheetAssigner>();

        // Remove all game objects that are children of the level generation object
        foreach (Transform child in levelGeneration.transform)
        {
            Destroy(child.gameObject);
        }

        if (levelGeneration.numberOfRooms >= (levelGeneration.worldSize.x * 2) * (levelGeneration.worldSize.y * 2))
        { // make sure we dont try to make more rooms than can fit in our grid
            levelGeneration.numberOfRooms = Mathf.RoundToInt((levelGeneration.worldSize.x * 2) * (levelGeneration.worldSize.y * 2));
        }
        levelGeneration.gridSizeX = Mathf.RoundToInt(levelGeneration.worldSize.x); //note: these are half-extents
        levelGeneration.gridSizeY = Mathf.RoundToInt(levelGeneration.worldSize.y);
        levelGeneration.CreateRooms(); //lays out the actual map
        levelGeneration.SetRoomDoors(); //assigns the doors where rooms would connect
        levelGeneration.DrawMap(); //instantiates objects to make up a map
        sheetAssigner.Assign(levelGeneration.rooms, levelGeneration.takenPositions); //passes room info to another script which handles generatating the level geometry

        // Spawn agents on random rooms
        // Reset positionsTakenByAgents
        positionsTakenByAgents = new List<Vector2>();

        AmrAgent singleAgent = FindObjectOfType<AmrAgent>();
        SpawnOnRandomRoom(
            singleAgent, 
            levelGeneration.takenPositions, 
            sheetAssigner.roomDimensions,
            sheetAssigner.gutterSize,
            positionsTakenByAgents
            );

        //foreach (var agent in AgentsList)
        //{
        //SpawnOnRandomRoom(
        //    agent.Agent.gameObject,
        //    levelGeneration.takenPositions,
        //    sheetAssigner.roomDimensions,
        //    sheetAssigner.gutterSize,
        //    positionsTakenByAgents
        //    );
        //}

        //GeneratePickupAndDeliveryPoints(
        //    levelGeneration.rooms,
        //    levelGeneration.takenPositions,
        //    AgentsList.Count
        //    );

        //    //Reset counter
        //    m_ResetTimer = 0;

        //    //Random hospital layout

        //    //Reset Agents
        //    foreach (var item in AgentsList)
        //    {
        //        var pos = UseRandomAgentPosition ? GetRandomSpawnPos() : item.StartingPos;
        //        var rot = UseRandomAgentRotation ? GetRandomRot() : item.StartingRot;

        //        item.Agent.transform.SetPositionAndRotation(pos, rot);
        //        item.Rb.velocity = Vector3.zero;
        //        item.Rb.angularVelocity = Vector3.zero;
        //        item.Agent.MyInstrument.SetActive(false);
        //        item.Agent.IHaveAnInstrument = false;
        //        item.Agent.gameObject.SetActive(true);
        //        m_AgentGroup.RegisterAgent(item.Agent);
        //    }

        //    //Reset Key
        //    Key.SetActive(false);
    }


    private void SpawnOnRandomRoom(AmrAgent gameObject, List<Vector2> takenPositions, Vector2 roomDimensions, Vector2 gutterSize, List<Vector2> positionsTakenByAgents)
    {
        // Get out of function if takenPositions is null
        if (takenPositions == null)
        {
            return;
        }

        Vector2 spawnPos = Vector2.zero;

        // Get a random room that is not taken by an agent
        do
        {
            int x = 0, z = 0;
            int index = Mathf.RoundToInt(Random.value * (takenPositions.Count - 1)); // pick a random room
            x = (int)takenPositions[index].x;//capture its x, z position
            z = (int)takenPositions[index].y;
            spawnPos = new Vector2(x, z);
        } while (positionsTakenByAgents.Contains(spawnPos));

        // Add the spawn position to the list of taken positions
        positionsTakenByAgents.Add(spawnPos);

        // Fix coordinates to be in the center of the room
        Vector3 agentSpawnPosition = new Vector3(spawnPos.x * (roomDimensions.x + gutterSize.x), (sheetAssigner.transform.position.y + (gameObject.transform.localScale.y / 2)), spawnPos.y * (roomDimensions.y + gutterSize.y));

        // Spawn the agent on the room based on the room's position with random rotation
        gameObject.gameObject.transform.position = agentSpawnPosition;
        gameObject.gameObject.transform.rotation = Quaternion.Euler(0, Random.Range(0.0f, 360.0f), 0);

        // Hard coded, TODO: Get the tile size from the RoomInstance
        int tileSize = 16;
        Vector2 roomSizeInTiles = new Vector2(17 - 2, 9 - 2);
        int xMovementLimit = (int)roomSizeInTiles.x * tileSize;
        int zMovementLimit = (int)roomSizeInTiles.y * tileSize;

        // Coordinates for the corner of the room
        int roomCornerX = (int)agentSpawnPosition.x - ((int)(System.Math.Floor(roomSizeInTiles.x / 2)) * tileSize);
        int roomCornerZ = (int)agentSpawnPosition.z - ((int)(System.Math.Floor(roomSizeInTiles.y / 2)) * tileSize);

        if (gameObject.CheckObjectCollision() == true)
        {
            Debug.Log("Collision on center of the room");

            float checkSphereRadius = 10f;
            int numberOfCollidersFound = 0;
            int securityCounter = 0;
            int i = (int)agentSpawnPosition.x + tileSize;
            int j = (int)agentSpawnPosition.z;
            
            // Array to store the colliders found inside the overlap sphere
            Collider[] collidersInsideOverlapSphere = new Collider[1];

            // Loop until either no colliders are found or the security counter reaches the limit
            do
            {
                Vector3 newAgentSpawnPosition = new Vector3(i, agentSpawnPosition.y, j);
                gameObject.gameObject.transform.position = newAgentSpawnPosition;

                // Perform overlap detection using a sphere with the defined radius
                numberOfCollidersFound = Physics.OverlapSphereNonAlloc(newAgentSpawnPosition, checkSphereRadius, collidersInsideOverlapSphere, layerForAgentSpawnDetection);
                Debug.Log("Number of colliders found: " + numberOfCollidersFound + " | on position: " + gameObject.transform.position);

                // Move to the next position in the grid
                i += tileSize;
                if (i >= (roomCornerX + xMovementLimit))
                {
                    i = roomCornerX;
                    j += tileSize;
                }

                securityCounter++;

            } while (numberOfCollidersFound != 0 && securityCounter < 45); // 45 Always is besides the door

        }
    }

    private void GeneratePickupAndDeliveryPoints(Room[,] rooms, List<Vector2> takenPositions, int numberOfAgents)
    {
        // Get out of function if takenPositions is null
        if (takenPositions == null)
        {
            return;
        }

        // Get all rooms that have type 2 (OR)
        List<Room> roomsWithORType = new List<Room>();
        foreach (var room in rooms)
        {
            if (room != null && room.type == 2)
            {
                roomsWithORType.Add(room);
            }
        }

        // Max amount of delivery points
        int maxDeliveryPoints = numberOfAgents + 2;


        // Get all RoomRoot objects that are children of the level generation object and have the tag "OR" and tag "SPD"
        GameObject[] roomRootsOR = GameObject.FindGameObjectsWithTag("OR");
        GameObject roomRootCS = GameObject.FindGameObjectWithTag("CS"); 
        GameObject roomRootSPD = GameObject.FindGameObjectWithTag("SPD");

        // Randomly select the rooms that will have the delivery points
        List<GameObject> selectedDeliveryRooms = RandomlySelectRooms(maxDeliveryPoints, roomRootsOR);
        // Last delivery point is in the SPD
        selectedDeliveryRooms.Add(roomRootSPD);

        //Randomly select the rooms that will have the pickup points
        List<GameObject> selectedPickupRooms = RandomlySelectRooms(maxDeliveryPoints, roomRootsOR);
        // Last pickup point is in the CS
        selectedPickupRooms.Add(roomRootCS);

        // For each selected OR room, deactivate all delivery points and activate one
        ActivateOnePointInRangeWithTag(selectedDeliveryRooms, "DeliveryPoint");

        // For each selected OR room, deactivate all pickup points and activate one
        ActivateOnePointInRangeWithTag(selectedPickupRooms, "PickupPoint");

        //Create pair list of activated pickup and delivery points, eg. (pickup1, delivery1), (pickup2, delivery2), ...
        List<(GameObject, GameObject)> pickupDeliveryPairs = new List<(GameObject, GameObject)>();
        int index = 0;
        foreach (var room in selectedDeliveryRooms)
        {
            Transform[] deliveryPoints = room.GetComponentsInChildren<Transform>();
            foreach (var deliveryPoint in deliveryPoints)
            {
                if (deliveryPoint.gameObject.tag == "DeliveryPoint")
                {
                    Transform[] pickupPoints = room.GetComponentsInChildren<Transform>();
                    foreach (var pickupPoint in pickupPoints)
                    {
                        if (pickupPoint.gameObject.tag == "PickupPoint")
                        {
                            pickupDeliveryPairs.Add((pickupPoint.gameObject, deliveryPoint.gameObject));
                        }
                    }
                }
            }
        }

    }

    private List<GameObject> RandomlySelectRooms(int numberOfRooms, GameObject[] roomRoots)
    {
        List<GameObject> selectedRooms = new List<GameObject>();
        for (int i = 0; i < numberOfRooms - 1; i++)
        {
            int randomIndex = Random.Range(0, roomRoots.Length);
            selectedRooms.Add(roomRoots[randomIndex]);
        }
        return selectedRooms;
    }

    private void ActivateOnePointInRangeWithTag(List<GameObject> SelectedRooms, string tag)
    {
        foreach (var roomRoot in SelectedRooms)
        {
            // Get all points of interest in the room
            Transform[] pointsOfInterest = roomRoot.GetComponentsInChildren<Transform>();

            // Deactivate all points of interest
            foreach (var Point in pointsOfInterest)
            {
                if (Point.gameObject.tag == tag)
                {
                    Point.gameObject.SetActive(false);
                }
            }

            // Activate one point
            int randomIndex = Random.Range(0, pointsOfInterest.Length);
            pointsOfInterest[randomIndex].gameObject.SetActive(true);
        }
    }
}
