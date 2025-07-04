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
        public AmrAgent Agent;
    }

    /// <summary>
    /// Max Academy steps before this platform resets
    /// </summary>
    /// <returns></returns>
    [Header("Max Environment Steps")] public int MaxEnvironmentSteps;
    public float hospitalSize = 1.0f; // Single Room Hospital
    private int m_ResetTimer;

    [Header("Agent Prefab")] public GameObject AgentPrefab;
    private AmrAgent amrAgentComponent;
    public List<PlayerInfo> AgentsList = new List<PlayerInfo>();
    [HideInInspector]
    public int numberOfAgents = 1;
    private int prevNumberOfAgents = 1;

    [SerializeField]
    private static float pairSphereRadius = .5f;
    public static LayerMask layerForAgentSpawnDetection;
    private List<(GameObject, GameObject)> pickupDeliveryPairs = new List<(GameObject, GameObject)>();
    private SimpleMultiAgentGroup m_AgentGroup;
    private int successfullDeliveries = 0;
    [HideInInspector]
    public int maxDeliveryPoints;
    private bool childrenDestroyed = true;

    private void setAgentCount(float worldSize)
    {
        switch (worldSize)
        {
            case 1.0f: // Single Room
                numberOfAgents = 1;
                maxDeliveryPoints = 1;
                MaxEnvironmentSteps = 15000;
                break;
            case 2.0f: // Pair Room
                numberOfAgents = 1;
                maxDeliveryPoints = 2;
                MaxEnvironmentSteps = 20000;
                break;
            case 3.0f: // Pair Room
                numberOfAgents = 1;
                maxDeliveryPoints = 2;
                MaxEnvironmentSteps = 20000;
                break;
            case 4.0f: // Three Rooms
                numberOfAgents = 1;
                maxDeliveryPoints = 2;
                MaxEnvironmentSteps = 20000;
                break;
            case 5.0f: // Small Hospital
                numberOfAgents = 2;
                maxDeliveryPoints = 2;
                MaxEnvironmentSteps = 25000;
                break;
            case 6.0f: // Medium Hospital
                numberOfAgents = 3;
                maxDeliveryPoints = 3;
                MaxEnvironmentSteps = 25000;
                break;
            case 7.0f: // Large Hospital
                numberOfAgents = 4;
                maxDeliveryPoints = 4;
                MaxEnvironmentSteps = 30000;
                break;
            default:
                Debug.Log("Invalid world size");
                return;
        }
    }

    //Start is called before the first frame update
    void Start()
    {
        // Set the number of agents based on the hospital size
        setAgentCount(hospitalSize);
        prevNumberOfAgents = numberOfAgents;

        // Initialize TeamManager
        m_AgentGroup = new SimpleMultiAgentGroup();

        // Add agents to the group
        InstatiateAgents(numberOfAgents);

        ResetScene();
    }


    void FixedUpdate()
    {
        m_ResetTimer += 1;
        if (m_ResetTimer >= MaxEnvironmentSteps && MaxEnvironmentSteps > 0)
        {
            m_AgentGroup.GroupEpisodeInterrupted();
            StartCoroutine(DestroyAllRooms(.3f));
            ResetScene();
        }
        if (childrenDestroyed == false)
        {
            ResetScene();
        }
        if (Input.GetKeyDown(KeyCode.Q))
        {
            StartCoroutine(DestroyAllRooms(.3f));
            ResetScene();
        }
        //if (Input.GetKey(KeyCode.T))
        //{
        //    LevelGeneration levelGeneration = GetComponentInChildren<LevelGeneration>();
        //    // Destroy all children of the level generation object
        //    foreach (Transform child in levelGeneration.transform)
        //    {
        //        Destroy(child.gameObject);
        //    }
        //}

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

    private void InstatiateAgents(int numberOfAgentsToAdd)
    {
        // Instantiate agents based on numberOfAgents and using PlayerInfo
        for (int i = 0; i < numberOfAgentsToAdd; i++)
        {
            PlayerInfo newPlayer = new PlayerInfo();
            GameObject AgentGameObject = Instantiate(AgentPrefab, this.gameObject.transform);
            amrAgentComponent = AgentGameObject.GetComponent<AmrAgent>();
            newPlayer.Agent = amrAgentComponent;
            m_AgentGroup.RegisterAgent(newPlayer.Agent);
            newPlayer.Agent.gameObject.name = "Agent" + (i + numberOfAgents);
            AgentsList.Add(newPlayer);
        }
    }

    public void AddRewardForGroup(float reward)
    {
        m_AgentGroup.AddGroupReward(reward);
    }

    public void CompleteDelivery(GameObject deliveryPoint)
    {
        successfullDeliveries++;

        Debug.Log("Surgical instrument delivered!");
        m_AgentGroup.AddGroupReward(1f / maxDeliveryPoints);

        if (pickupDeliveryPairs.Count > 0)
        {
            RemoveItemPickupDeliveryPair(deliveryPoint);
        }

        if (successfullDeliveries == maxDeliveryPoints) 
        {
            successfullDeliveries = 0;
            m_AgentGroup.EndGroupEpisode();
            StartCoroutine(DestroyAllRooms(.3f));
            ResetScene();
        }
    }

    public List<(GameObject, GameObject)> GetPickupDeliveryPairs()
    {
        return pickupDeliveryPairs;
    }

    private void RemoveItemPickupDeliveryPair(GameObject deliveryPoint)
    {
        foreach ((GameObject pickup, GameObject delivery) in pickupDeliveryPairs)
        {
            if (delivery == deliveryPoint)
            {
                pickupDeliveryPairs.Remove((pickup, delivery));
                break;
            }
        }
    }

    void ResetScene()
    {
        // Get the LevelGeneration
        LevelGeneration levelGeneration = GetComponentInChildren<LevelGeneration>();
        
        // Exit function if levelGeneration contains children
        if (levelGeneration.transform.childCount > 0)
        {
            childrenDestroyed = false;
            return;
        }
        else
        {
            childrenDestroyed = true;
        }

        // Reset levelGeneration and remove all game objects that are children of the level generation object
        levelGeneration.Reset();

        //Set world size variables with values from the yaml config file
        //levelGeneration.setWorldSize(hospitalSize);
        levelGeneration.setWorldSize(Academy.Instance.EnvironmentParameters.GetWithDefault("HospitalSize", hospitalSize));
        setAgentCount(Academy.Instance.EnvironmentParameters.GetWithDefault("HospitalSize", hospitalSize));
        if (numberOfAgents != prevNumberOfAgents)
        {
            // Add new agents
            int agentsToAdd = numberOfAgents - prevNumberOfAgents;
            InstatiateAgents(numberOfAgents);
            prevNumberOfAgents = numberOfAgents;
        }

        SheetAssigner sheetAssigner = GetComponentInChildren<SheetAssigner>();

        if (levelGeneration.numberOfRooms >= (levelGeneration.worldSize.x * 2) * (levelGeneration.worldSize.y * 2))
        { // make sure we dont try to make more rooms than can fit in our grid
            levelGeneration.numberOfRooms = Mathf.RoundToInt((levelGeneration.worldSize.x * 2) * (levelGeneration.worldSize.y * 2));
        }
        levelGeneration.gridSizeX = Mathf.RoundToInt(levelGeneration.worldSize.x); //note: these are half-extents
        levelGeneration.gridSizeY = Mathf.RoundToInt(levelGeneration.worldSize.y);
        levelGeneration.CreateRooms(); //lays out the actual map
        levelGeneration.SetRoomDoors(); //assigns the doors where rooms would connect
        //levelGeneration.DrawMap(); //instantiates objects to make up a map
        sheetAssigner.Assign(levelGeneration.rooms, levelGeneration.takenPositions); //passes room info to another script which handles generatating the level geometry

        // Spawn agents on random rooms
        List<Vector2> positionsTakenByAgents = new List<Vector2>();

        //AmrAgent singleAgent = FindObjectOfType<AmrAgent>();
        //SpawnOnRandomRoom(
        //    singleAgent, 
        //    levelGeneration.takenPositions, 
        //    sheetAssigner.roomDimensions,
        //    sheetAssigner.gutterSize,
        //    sheetAssigner.transform.position,
        //    positionsTakenByAgents
        //    );

        foreach (var agent in AgentsList)
        {
            SpawnOnRandomRoom(
                agent.Agent,
                levelGeneration.takenPositions,
                sheetAssigner.roomDimensions,
                sheetAssigner.gutterSize,
                sheetAssigner.transform.position,
                positionsTakenByAgents
                );
        }
        GeneratePickupAndDeliveryPoints(
            levelGeneration.takenPositions,
            levelGeneration.transform
            );

        //Reset counter
        m_ResetTimer = 0;
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
            float checkSphereRadius = 10f;
            int numberOfCollidersFound = 0;
            int securityCounter = 0;
            int i = (int)agentSpawnPosition.x + tileSize * 5;
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

    private void GeneratePickupAndDeliveryPoints(List<Vector2> takenPositions, Transform transform)
    {
        // Get out of function if takenPositions doesn't have enough rooms
        if (takenPositions == null)
        {
            return;
        }
        // Change maxDeliveryPoints in case of fewer taken rooms capacity of delivery points
        else if (takenPositions.Count * 2 < maxDeliveryPoints)
        {
            maxDeliveryPoints = takenPositions.Count;
        }

        if (takenPositions.Count == 1)
        {
            List<GameObject> roomRootOR = GetChildrenWithTag(transform, "OR");
            List<GameObject> activatedDeliveryPoint = ActivateOnePointInRangeWithTag(roomRootOR, "DeliveryPoint");
            List<GameObject> activatedPickupPoint = ActivateOnePointInRangeWithTag(roomRootOR, "PickupPoint");
            pickupDeliveryPairs = new List<(GameObject, GameObject)>
            {
                (activatedPickupPoint[0], activatedDeliveryPoint[0])
            };

            ColourSamePairs(pickupDeliveryPairs, pairSphereRadius);

            return;
        }

        // Get all RoomRoot objects that are children of the level generation object and have the tag "OR", "CS" and "SPD"
        List<GameObject> roomRootsOR = GetChildrenWithTag(transform, "OR");
        GameObject roomRootCS = GetChildrenWithTag(transform, "CS")[0];
        GameObject roomRootSPD = GetChildrenWithTag(transform, "SPD")[0];

        // Randomly select the rooms that will have the delivery points
        List<GameObject> selectedDeliveryRooms = new List<GameObject>();
        if (roomRootsOR.Count > 0)
        {
            selectedDeliveryRooms = RandomlySelectRooms(maxDeliveryPoints, roomRootsOR);
        }
        selectedDeliveryRooms.Add(roomRootSPD);
        if ((hospitalSize == 2.0f || hospitalSize == 3.0f) || (selectedDeliveryRooms.Count == 1 && hospitalSize >= 5.0f))
        {
            selectedDeliveryRooms.Add(roomRootSPD);
        }

        //Randomly select the rooms that will have the pickup points
        List<GameObject> selectedPickupRooms = new List<GameObject>();
        if (roomRootsOR.Count > 0)
        {
            selectedPickupRooms = RandomlySelectRooms(maxDeliveryPoints, roomRootsOR);
        }
        selectedPickupRooms.Add(roomRootCS);
        if ((hospitalSize == 2.0f || hospitalSize == 3.0f) || (selectedPickupRooms.Count == 1 && hospitalSize >= 5.0f))
        {
            selectedPickupRooms.Add(roomRootCS);
        }


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

        // Generate a id for each pair based on the color of the prefab 
        ColourSamePairs(pickupDeliveryPairs, pairSphereRadius);

    }

    public List<GameObject> GetChildrenWithTag(Transform parent, string tag)
    {
        List<GameObject> taggedGameObjects = new List<GameObject>();

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.tag == tag)
            {
                taggedGameObjects.Add(child.gameObject);
            }
        }
        return taggedGameObjects;
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


    private List<GameObject> RandomlySelectRooms(int numberOfRooms, List<GameObject> roomRoots)
    {
        int numberOfRoomsToSelect = numberOfRooms - 1; // Subtract 1 because the SPD or CS room is always selected at the end
        int safetyCounter = 0;
        List<GameObject> selectedRooms = new List<GameObject>();
        for (int i = 0; i < numberOfRoomsToSelect; i++)
        {
            if (safetyCounter > 100)
            {
                Debug.Log("Safety counter reached 100 when generating points of interest");
                break;
            }

            int randomIndex = Random.Range(0, roomRoots.Count);
            //Check if the room is already in the list
            if (selectedRooms.Contains(roomRoots[randomIndex]) && (roomRoots.Count >= numberOfRoomsToSelect))
            {
                i--;
                safetyCounter++;
                continue;
            }
            selectedRooms.Add(roomRoots[randomIndex]);
            safetyCounter++;
            
        }
        return selectedRooms;
    }

    private List<GameObject> ActivateOnePointInRangeWithTag(List<GameObject> selectedRooms, string tag)
    {
        List<GameObject> activatedPoints = new List<GameObject>();
        foreach (GameObject roomRoot in selectedRooms)
        {
            List<GameObject>  pointsWithTag = new List<GameObject>();
            // Activate one point of interest with the tag
            foreach (Transform child in roomRoot.transform)
            {
                if (child.gameObject.tag == tag && child.gameObject.activeSelf == false)
                {
                    // print parent location
                    pointsWithTag.Add(child.gameObject);
                }
            }
            // Activate on random point on list
            if (pointsWithTag.Count > 0)
            {
                int randomIndex = Random.Range(0, pointsWithTag.Count);
                pointsWithTag[randomIndex].SetActive(true);
                activatedPoints.Add(pointsWithTag[randomIndex]);
            }
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

            GameObject spherePickup = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(spherePickup.GetComponent<SphereCollider>());
            spherePickup.transform.parent = pickup.transform;
            spherePickup.transform.localScale = new Vector3(pairSphereRadius, pairSphereRadius, pairSphereRadius);
            spherePickup.transform.position = new Vector3(
                pickup.transform.position.x,
                pickup.transform.position.y + 30,
                pickup.transform.position.z
                );

            // Assign the same color to the pickup and delivery spheres
            Color color = Random.ColorHSV();
            sphereDelivery.GetComponent<Renderer>().material.color = color;
            spherePickup.GetComponent<Renderer>().material.color = color;
        }
    }

}
