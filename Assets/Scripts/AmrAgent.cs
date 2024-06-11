using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEngine.Device;
using System.IO;
using Unity.Mathematics;
//using UnityEditor.Experimental.GraphView;

public class AmrAgent : Agent
{
    private EnvController envController;
    private PathFinding pathfinding;
    private SheetAssigner sheetAssigner;
    private LevelGeneration levelGeneration;
    private BufferSensorComponent bufferSensorComponent;
    private List<(GameObject, GameObject)> pickupDeliveryPairs;
    private float collisionRadius = 10f;
    [HideInInspector]
    public bool iHaveInstrument = false;
    public GameObject sphereIndicator;
    public LayerMask layerForAgentSpawnDetection;
    private Rigidbody amrAgent;
    private float agentRunSpeed = 8f;

    // Called when the agent is first initialized
    public override void Initialize()
    {
        ResetAgentComponents();
        amrAgent = GetComponent<Rigidbody>();
        envController = GetComponentInParent<EnvController>();
        bufferSensorComponent = GetComponent<BufferSensorComponent>();
    }

    // Called to set-up the environment for a new episode
    public override void OnEpisodeBegin()
    {
        ResetAgentComponents();
    }
    public bool CheckObjectCollision()
    {
        if (Physics.CheckSphere(transform.position, collisionRadius, layerForAgentSpawnDetection))
        {
            return true;
        }
        else
        {
            //Debug.Log("No collision on" + transform.position + " with radius " + collisionRadius);
            return false;
        }
    }

    public Collider[] GetColliders()
    {
        return Physics.OverlapSphere(transform.position, collisionRadius, layerForAgentSpawnDetection);
    }

    //private void OnDrawGizmos()
    //{
    //    Gizmos.color = Color.red;
    //    Gizmos.DrawSphere(transform.position, collisionRadius);
    //}

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "PickupPoint" && !iHaveInstrument)
        {
            // If the pickupPoint has a sphere object as a child, check if the color of the sphere is the same as the delivery point
            Transform sphere = other.transform.Find("Sphere");
            if (sphere != null)
            {
                iHaveInstrument = true;

                // Set the color of SphereIndicator to the color of the PickupPoint sphere
                sphereIndicator.GetComponent<Renderer>().material.color = sphere.GetComponent<Renderer>().material.color;
                sphereIndicator.SetActive(true);

                // Add reward only for agent that has the instrument
                // if (envController.hospitalSize >= 4f)
                // {
                //     AddReward(.5f / envController.maxDeliveryPoints);
                // }
            }
        }
        if (other.gameObject.tag == "DeliveryPoint")
        {
            // If the sphere on the agent is activated and has the same color as the delivery point, the delivery is successful
            if (iHaveInstrument)
            {
                Transform otherSphere = other.transform.Find("Sphere");
                if (otherSphere != null)
                {
                    if (sphereIndicator.GetComponent<Renderer>().material.color == otherSphere.GetComponent<Renderer>().material.color)
                    {
                        ResetAgentComponents();
                        envController.CompleteDelivery(other.gameObject);
                    }
                }
            }
        }
        if (other.gameObject.tag == "Wall" || other.gameObject.tag == "Obstacle" || other.gameObject.tag == "AmrAgent")
        {
            AddReward(-.005f);
        }
    }

    private void ResetAgentComponents()
    {
        iHaveInstrument = false;
        sphereIndicator.GetComponent<Renderer>().material.color = Color.white;
        sphereIndicator.SetActive(false);
    }


    public override void CollectObservations(VectorSensor sensor)
    { 
        // A* Observation
        pathfinding = GetComponent<PathFinding>();
        sheetAssigner = GameObject.Find("LevelGenerator").GetComponent<SheetAssigner>();
        levelGeneration = GameObject.Find("LevelGenerator").GetComponent<LevelGeneration>();
        pickupDeliveryPairs = envController.GetPickupDeliveryPairs();

        if (iHaveInstrument)
        {
            Vector2 currentRoomGridPos = sheetAssigner.WorldPosToGridPos(this.transform);
            //Debug.Log("currentRoomGridPos: " + currentRoomGridPos);
            GameObject deliveryPoint = envController.getDeliveryPointPair(pickupDeliveryPairs, sphereIndicator);
            if (deliveryPoint == null)
            {
                Debug.Log("No delivery point pair found for the current instrument");
                return;
            }

            Vector2 deliveryPointGridPos = sheetAssigner.WorldPosToGridPos(deliveryPoint.transform);
            //Debug.Log("deliveryPointGridPos: " + deliveryPointGridPos);
            List<Room> path = pathfinding.FindPath(currentRoomGridPos, deliveryPointGridPos);

            // Use a buffer sensor to consider variable length observations
            bufferSensorComponent.AppendObservation(AStarObservation(path, deliveryPoint.transform, sheetAssigner.GridPosToWorldPos(currentRoomGridPos)));
        }
        else
        {
            for (int i = 0; i < pickupDeliveryPairs.Count; i++)
            {
                // TODO: Pass world position directly to the A* observation
                Vector2 currentRoomGridPos = sheetAssigner.WorldPosToGridPos(this.transform);
                //Debug.Log("currentRoomGridPos: " + currentRoomGridPos);
                GameObject pickupPoint = pickupDeliveryPairs[i].Item1;
                if (pickupPoint == null)
                {
                    List<Room> emptyPath = new List<Room>();
                    bufferSensorComponent.AppendObservation(AStarObservation(emptyPath, pickupPoint.transform, sheetAssigner.GridPosToWorldPos(currentRoomGridPos)));
                    return;
                }
                // If pickupPoint is not active, skip it
                if (!pickupPoint.activeSelf)
                {
                    continue;
                }

                Vector2 pickupPointGridPos = sheetAssigner.WorldPosToGridPos(pickupPoint.transform);
                List<Room> path = pathfinding.FindPath(currentRoomGridPos, pickupPointGridPos);
                bufferSensorComponent.AppendObservation(AStarObservation(path, pickupPoint.transform, sheetAssigner.GridPosToWorldPos(currentRoomGridPos)));
            }
        }
        // If agent has instrument
        sensor.AddObservation(iHaveInstrument);

        // Agent velocity
        //Vector2 agentVelocity = new Vector2(amrAgent.velocity.x / 90, amrAgent.velocity.z / 90);
        //sensor.AddObservation(agentVelocity);

        // Agent normalized rotation
        //Vector3 normalizedRotation = transform.rotation.eulerAngles / 180.0f - Vector3.one;  // [-1,1]
        //sensor.AddObservation(normalizedRotation.y);

        // Other Agenst's positions? #TODO: test if the model gets better with this observation
    }

    /// <summary>
    /// Moves the agent according to the selected action.
    /// </summary>
    public void MoveAgent(ActionSegment<int> act)
    {
        // Force agent to reduce time to complete the task
        envController.AddRewardForGroup((-1f / MaxStep) / envController.numberOfAgents);
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        var action = act[0];

        switch (action)
        {
            case 1:
                dirToGo = transform.forward * 1f;
                break;
            case 2:
                dirToGo = transform.forward * -1f;
                break;
            case 3:
                rotateDir = transform.up * 1f;
                break;
            case 4:
                rotateDir = transform.up * -1f;
                break;
        }
        transform.Rotate(rotateDir, Time.fixedDeltaTime * 200f);
        amrAgent.AddForce(dirToGo * agentRunSpeed, ForceMode.VelocityChange);
    }

    /// <summary>
    /// Called every step of the engine. Here the agent takes an action.
    /// </summary>
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        //if (envController.hospitalSize > 1)
        //{
        //    AddReward(-1f / MaxStep);
        //}
        // Move the agent using the action.
        MoveAgent(actionBuffers.DiscreteActions);
    }

    //TODO: Check if continuous would be better
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        if (Input.GetKey(KeyCode.D))
        {
            discreteActionsOut[0] = 3;
        }
        else if (Input.GetKey(KeyCode.W))
        {
            discreteActionsOut[0] = 1;
        }
        else if (Input.GetKey(KeyCode.A))
        {
            discreteActionsOut[0] = 4;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            discreteActionsOut[0] = 2;
        }
    }

    private float[] AStarObservation(List<Room> path, Transform pointOfInterestPos, Vector2 currentRoomWorldPos)
    {
        // Create a vector observation that contains: (distance_to_next_room's_door, distance_point_of_interest)
        if (path != null && path.Count > 0)
        {
            float normalizedDistanceNextDoor = 0;
            float alignmentWithDoor = 0;
            float alignmentWithPointOfInterest = 0;

            Vector3 directionAgent = this.transform.forward; // Direction to where the agent is looking for calculating alignments
            directionAgent.Normalize();

            //Next room position
            if (path.Count > 1 && levelGeneration.numberOfRooms != 4)
            {
                Vector2 directionToMove = path[1].gridPos - path[0].gridPos;

                // Coordinate to the next door on the A* path
                // Hard coded, TODO: Get the tile size from the RoomInstance
                float tileSize = 16;
                Vector2 roomSizeInTiles = new Vector2(17, 9);
                Vector2 distanceCenterToDoor = new Vector2(
                    (float)System.Math.Floor(roomSizeInTiles.x / 2) * tileSize,
                    (float)System.Math.Floor(roomSizeInTiles.y / 2) * tileSize
                    );
                Vector2 nextDoorcoordinate = currentRoomWorldPos + (directionToMove * distanceCenterToDoor);
                Vector3 nextDoorWorldPos = new Vector3(nextDoorcoordinate.x, this.transform.position.y, nextDoorcoordinate.y);

                // Distance of agent to next door 
                float distanceAmrDoorX = math.abs(nextDoorcoordinate.x - this.gameObject.transform.position.x);
                float distanceAmrDoorZ = math.abs(nextDoorcoordinate.y - this.gameObject.transform.position.z);
                float distanceNextDoor = math.sqrt(math.pow(distanceAmrDoorX, 2f) + math.pow(distanceAmrDoorZ, 2f));

                // Normalize distance to next door
                Vector2 maxDistance = new Vector2((roomSizeInTiles.x) * tileSize, (roomSizeInTiles.y) * tileSize);
                float normalizationRatio = math.abs((maxDistance.x * directionToMove.x) + (maxDistance.y * directionToMove.y));
                normalizedDistanceNextDoor = distanceNextDoor / normalizationRatio;


                // Get the alignment between the agent and the door
                Vector3 directionDoor = nextDoorWorldPos - this.transform.position; // Direction to the door

                // Normalize the vectors to a unit vector
                directionDoor.Normalize();

                // Calculate the dot product between the two vectors
                alignmentWithDoor = Vector3.Dot(directionAgent, directionDoor);
            }

            //distance to delivery point
            float distanceAmrPointX = math.abs(pointOfInterestPos.position.x - this.gameObject.transform.position.x);
            float distanceAmrPointZ = math.abs(pointOfInterestPos.position.z - this.gameObject.transform.position.z);
            float distancePointOfInterest = math.sqrt(math.pow(distanceAmrPointX, 2f) + math.pow(distanceAmrPointZ, 2f));

            //Normalize data between 0 and 1 before adding to the observation
            float gridSizeX = levelGeneration.GetGridSizeX() * (sheetAssigner.roomDimensions.x + sheetAssigner.gutterSize.x);
            float gridSizeY = levelGeneration.GetGridSizeY() * (sheetAssigner.roomDimensions.y + sheetAssigner.gutterSize.y);
            float normalizedDistance = distancePointOfInterest / ((gridSizeX + gridSizeY) * 0.7f); // 0.7f is a factor to make the distance smaller

            // Get Alignment between agent and point of interest
            Vector3 directionPoint = pointOfInterestPos.position - this.transform.position; // Direction to the point of interest

            // Normalize the vectors to a unit vector
            directionPoint.Normalize();

            // Calculate the dot product between the two vectors
            alignmentWithPointOfInterest = Vector3.Dot(directionAgent, directionPoint);

            float[] AStarObservationFloats = { normalizedDistanceNextDoor, alignmentWithDoor, normalizedDistance, alignmentWithPointOfInterest };
            //Debug.Log("A* Observation: " + AStarObservationFloats[0] + " " + AStarObservationFloats[1] + " " + AStarObservationFloats[2] + " " + AStarObservationFloats[3]);
            return AStarObservationFloats;
        }
        else
        {
            float[] AStarObservationFloats = { 0, 0, 0, 0 };
            //Debug.Log("A* Observation: " + AStarObservationFloats[0] + " " + AStarObservationFloats[1] + " " + AStarObservationFloats[2] + " " + AStarObservationFloats[3]);
            return AStarObservationFloats;
        }
    }
}
