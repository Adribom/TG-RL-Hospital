using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.Networking.UnityWebRequest;
using UnityEngine.UIElements;
using System.Runtime.CompilerServices;

public class EnvController : MonoBehaviour
{
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
    private float hospitalSize = 2.0f; // Small Hospital
    private int m_ResetTimer;

    public List<PlayerInfo> AgentsList = new List<PlayerInfo>();

    private Dictionary<AmrAgent, PlayerInfo> m_PlayerDict = new Dictionary<AmrAgent, PlayerInfo>();

    [SerializeField]
    private static float pairSphereRadius = .5f;
    public static LayerMask layerForAgentSpawnDetection;
    private List<(GameObject, GameObject)> pickupDeliveryPairs = new List<(GameObject, GameObject)>();
    // private SimpleMultiAgentGroup m_AgentGroup;
    private int successfullDeliveries = 0;
    private int maxDeliveryPoints;

    //Start is called before the first frame update
    void Start()
    {
        // successfullDeliveries = 0;
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


    void FixedUpdate()
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

        if (Input.GetKeyDown(KeyCode.Y))
        {
            StartCoroutine(DestroyAllRooms(1f));
            ResetScene();
        }
        if (Input.GetKey(KeyCode.T))
        {
            LevelGeneration levelGeneration = GetComponentInChildren<LevelGeneration>();
            // Destroy all children of the level generation object
            foreach (Transform child in levelGeneration.transform)
            {
                Destroy(child.gameObject);
            }
        }

    }

    IEnumerator DestroyAllRooms(float time)
    {
        LevelGeneration levelGeneration = GetComponentInChildren<LevelGeneration>();
        // Destroy all children of the level generation object
        foreach (Transform child in levelGeneration.transform)
        {
            Destroy(child.gameObject);
        }
        yield return new WaitForSeconds(time); 
    }

    public void CompleteDelivery()
    {
        print("Surgical instrument delivered!");
        //m_AgentGroup.AddGroupReward(1f);

        if (successfullDeliveries == maxDeliveryPoints) 
        {
            successfullDeliveries = 0;
            //m_AgentGroup.EndGroupEpisode();
            ResetScene();
        }
        else
        {
            successfullDeliveries++;
        }
    }

    public List<(GameObject, GameObject)> GetPickupDeliveryPairs()
    {
        return pickupDeliveryPairs;
    }
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
        // Get the LevelGeneration
        LevelGeneration levelGeneration = GetComponentInChildren<LevelGeneration>();
        
        // Exit function if levelGeneration contains children
        if (levelGeneration.transform.childCount > 0)
        {
            return;
        }

        // Reset levelGeneration and remove all game objects that are children of the level generation object
        levelGeneration.Reset();

        //Set world size variables
        levelGeneration.setWorldSize(hospitalSize);
        // levelGeneration.setWorldSize(Academy.Instance.EnvironmentParameters.GetWithDefault("HospitalSize", hospitalSize));

        SheetAssigner sheetAssigner = GetComponentInChildren<SheetAssigner>();

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
        List<Vector2> positionsTakenByAgents = new List<Vector2>();

        AmrAgent singleAgent = FindObjectOfType<AmrAgent>();
        SpawnOnRandomRoom(
            singleAgent, 
            levelGeneration.takenPositions, 
            sheetAssigner.roomDimensions,
            sheetAssigner.gutterSize,
            sheetAssigner.transform.position,
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

        GeneratePickupAndDeliveryPoints(
            levelGeneration.rooms,
            levelGeneration.takenPositions,
            1
            //AgentsList.Count
            );

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


    private void SpawnOnRandomRoom(AmrAgent gameObject, List<Vector2> takenPositions, Vector2 roomDimensions, Vector2 gutterSize, Vector3 rootPosition, List<Vector2> positionsTakenByAgents)
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
        Vector3 agentSpawnPosition = new Vector3(spawnPos.x * (roomDimensions.x + gutterSize.x), (rootPosition.y + (gameObject.transform.localScale.y / 2)), spawnPos.y * (roomDimensions.y + gutterSize.y));

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
        // Max amount of delivery points
        maxDeliveryPoints = numberOfAgents + 1;

        // Get out of function if takenPositions is doesn't have enough rooms
        if (takenPositions == null)
        {
            return;
        }
        else if (takenPositions.Count < maxDeliveryPoints)
        {
            maxDeliveryPoints = takenPositions.Count;
        }

        // Get all RoomRoot objects that are children of the level generation object and have the tag "OR" and tag "SPD"
        // TODO: Verificar o pq que há lixo dentro dos points
        GameObject[] roomRootsOR = GameObject.FindGameObjectsWithTag("OR");
        GameObject roomRootCS = GameObject.FindGameObjectWithTag("CS");
        GameObject roomRootSPD = GameObject.FindGameObjectWithTag("SPD");

        // Randomly select the rooms that will have the delivery points
        List<GameObject> selectedDeliveryRooms = new List<GameObject>();
        if (roomRootsOR.Length > 0)
        {
            selectedDeliveryRooms = RandomlySelectRooms(maxDeliveryPoints, roomRootsOR);
        }
        // Last delivery point is in the SPD
        selectedDeliveryRooms.Add(roomRootSPD);
        //Debug.Log("Number of Delivery Rooms: " + selectedDeliveryRooms.Count);

        //Randomly select the rooms that will have the pickup points
        List<GameObject> selectedPickupRooms = new List<GameObject>();
        if (roomRootsOR.Length > 0)
        {
            selectedPickupRooms = RandomlySelectRooms(maxDeliveryPoints, roomRootsOR);
        }
        // Last pickup point is in the CS
        selectedPickupRooms.Add(roomRootCS);
        //Debug.Log("Number of Pickup Rooms: " + selectedPickupRooms.Count);

        // For each selected OR room, deactivate all delivery points and activate one
        List<GameObject> activatedDeliveryPoints = new List<GameObject>();
        activatedDeliveryPoints = ActivateOnePointInRangeWithTag(selectedDeliveryRooms, "DeliveryPoint");

        // Check if all delivery points are activated
        for (int i = 0; i < activatedDeliveryPoints.Count; i++)
        {
            activatedDeliveryPoints[i].SetActive(true);
        }

        // For each selected OR room, deactivate all pickup points and activate one
        List<GameObject> activatedPickupPoints = new List<GameObject>();
        activatedPickupPoints = ActivateOnePointInRangeWithTag(selectedPickupRooms, "PickupPoint");

        // Check if all pickup points are activated
        for (int i = 0; i < activatedPickupPoints.Count; i++)
        {
            activatedPickupPoints[i].SetActive(true);
        }

        //Create pair list of activated pickup and delivery points, eg. (pickup1, delivery1), (pickup2, delivery2), ...
        pickupDeliveryPairs = new List<(GameObject, GameObject)>();
        int backwardIndex;
        for (int i = 0; i < activatedPickupPoints.Count; i++)
        {
            backwardIndex = activatedDeliveryPoints.Count - 1 - i;
            pickupDeliveryPairs.Add((activatedPickupPoints[i], activatedDeliveryPoints[backwardIndex]));
        }

        //Debug.Log("Number of Pickup-Delivery Pairs: " + pickupDeliveryPairs.Count);

        // Generate a id for each pair based on the color of the prefab 
        ColourSamePairs(pickupDeliveryPairs, pairSphereRadius);

    }

    public GameObject getDeliveryPointPair(List<(GameObject, GameObject)> pickupDeliveryPairs, GameObject sphereIndicator)
    {
        Color sphereIndicatorColor = sphereIndicator.GetComponent<Renderer>().material.color;
        foreach ((GameObject pickup, GameObject delivery) in pickupDeliveryPairs)
        {
            Transform sphere = delivery.transform.Find("Sphere");
            if (sphere.GetComponent<Renderer>().material.color == sphereIndicatorColor)
            {
                return delivery;
            }
        }
        return null;
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

    private List<GameObject> ActivateOnePointInRangeWithTag(List<GameObject> selectedRooms, string tag)
    {
        List<GameObject> activatedPoints = new List<GameObject>();
        foreach (GameObject roomRoot in selectedRooms)
        {
            // Activate one point of interest with the tag
            foreach (Transform child in roomRoot.transform)
            {
                if (child.gameObject.tag == tag)
                {
                    child.gameObject.SetActive(true);
                    // print parent location
                    //Debug.Log("Activated child " + child.gameObject.activeInHierarchy + " point in room: " + roomRoot.transform.position);
                    activatedPoints.Add(child.gameObject);
                    goto outerLoop;
                }
            }
            outerLoop:;
        }
        return activatedPoints;
    }

    private void ColourSamePairs(List<(GameObject, GameObject)> pickupDeliveryPairs, float pairSphereRadius)
    {
        // Iterate over the pairs and assign a color to each pair
        for (int i = 0; i < pickupDeliveryPairs.Count; i++)
        {
            // Get the pair
            (GameObject pickup, GameObject delivery) = pickupDeliveryPairs[i];

            // Create a floating sphere that will represent the color of the pair
            GameObject sphereDelivery = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(sphereDelivery.GetComponent<SphereCollider>());
            sphereDelivery.transform.parent = delivery.transform;
            sphereDelivery.transform.localScale = new Vector3(pairSphereRadius, pairSphereRadius, pairSphereRadius);
            sphereDelivery.transform.position = new Vector3(
                delivery.transform.position.x,
                delivery.transform.position.y + 30,
                delivery.transform.position.z
                );
            Debug.Log("Delivery position: " + delivery.transform.position);

            GameObject spherePickup = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(spherePickup.GetComponent<SphereCollider>());
            spherePickup.transform.parent = pickup.transform;
            spherePickup.transform.localScale = new Vector3(pairSphereRadius, pairSphereRadius, pairSphereRadius);
            spherePickup.transform.position = new Vector3(
                pickup.transform.position.x,
                pickup.transform.position.y + 30,
                pickup.transform.position.z
                );
            Debug.Log("Pickup position: " + pickup.transform.position);

            // Assign the same color to the pickup and delivery spheres
            Color color = Random.ColorHSV();
            sphereDelivery.GetComponent<Renderer>().material.color = color;
            spherePickup.GetComponent<Renderer>().material.color = color;
        }
    }

}
