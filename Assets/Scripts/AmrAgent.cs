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

    //public void Start()
    //{
    //    ResetAgentComponents();
    //    envController = GetComponentInParent<EnvController>();
    //    pickupDeliveryPairs = envController.GetPickupDeliveryPairs();
    //}

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
            AddReward(-.1f);
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
        pickupDeliveryPairs = envController.pickupDeliveryPairs;

        if (iHaveInstrument)
        {
            Vector2 currentRoomGridPos = sheetAssigner.PositionToGridPos(this.transform);
            //Debug.Log("currentRoomGridPos: " + currentRoomGridPos);
            GameObject deliveryPoint = envController.getDeliveryPointPair(pickupDeliveryPairs, sphereIndicator);
            if (deliveryPoint == null)
            {
                Debug.Log("No delivery point pair found for the current instrument");
                return;
            }

            Vector2 deliveryPointGridPos = sheetAssigner.PositionToGridPos(deliveryPoint.transform);
            //Debug.Log("deliveryPointGridPos: " + deliveryPointGridPos);
            List<Room> path = pathfinding.FindPath(currentRoomGridPos, deliveryPointGridPos);

            // Use a buffer sensor to consider variable length observations
            bufferSensorComponent.AppendObservation(AStarObservation(path));
        }
        else
        {
            for (int i = 0; i < pickupDeliveryPairs.Count; i++)
            {
                Vector2 currentRoomGridPos = sheetAssigner.PositionToGridPos(this.transform);
                //Debug.Log("currentRoomGridPos: " + currentRoomGridPos);
                GameObject pickupPoint = pickupDeliveryPairs[i].Item1;
                Vector2 pickupPointGridPos = sheetAssigner.PositionToGridPos(pickupPoint.transform);
                //Debug.Log("pickupPointGridPos: " + pickupPointGridPos);
                List<Room> path = pathfinding.FindPath(currentRoomGridPos, pickupPointGridPos);
                bufferSensorComponent.AppendObservation(AStarObservation(path));
            }
        }

        // Agent normalized current position
        float minValue = -(levelGeneration.GetGridSizeX() * (sheetAssigner.roomDimensions.x + sheetAssigner.gutterSize.x));
        float maxValue = levelGeneration.GetGridSizeX() * (sheetAssigner.roomDimensions.x + sheetAssigner.gutterSize.x);
        float yValueRange = 20;
        float yOffSet = levelGeneration.transform.position.y;
        float x = (transform.position.x - minValue) / (maxValue - minValue);
        float y = ((transform.position.y - yOffSet) - (-yValueRange)) / (yValueRange - (-yValueRange));
        float z = (transform.position.z - minValue) / (maxValue - minValue);
        Vector3 normalizedPosition = new Vector3(x, y, z);
        sensor.AddObservation(normalizedPosition.x);
        sensor.AddObservation(normalizedPosition.z);

        // Agent velocity
        Vector2 agentVelocity = new Vector2(amrAgent.velocity.x, amrAgent.velocity.z);
        sensor.AddObservation(agentVelocity);

        // Agent normalized rotation
        Vector3 normalizedRotation = transform.rotation.eulerAngles / 180.0f - Vector3.one;  // [-1,1]
        sensor.AddObservation(normalizedRotation.y);

        // Other Agenst's positions? #TODO: test if the model gets better with this observation
    }

    /// <summary>
    /// Moves the agent according to the selected action.
    /// </summary>
    public void MoveAgent(ActionSegment<int> act)
    {
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
        // Force agent to reduce time to complete the task
        AddReward(-1f / MaxStep);
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

    private float[] AStarObservation(List<Room> path)
    {
        // Create a vector observation that contains: (x_next_A*_room, y_next_A*_room, distance_point_of_interest)
        if (path != null)
        {
            Vector2 directionToMove = new Vector2();

            //Next room position
            if (path.Count > 1)
            {
                directionToMove = path[1].gridPos - path[0].gridPos;
            }
            else
            {
                directionToMove = Vector2.zero;
            }
            //distance to delivery point
            float distancePointOfInterest = path.Count - 1;



            //Normalize data between 0 and 1 before adding to the observation
            float normalizedDistance = distancePointOfInterest / (levelGeneration.GetGridSizeX() + levelGeneration.GetGridSizeY());

            float[] AStarObservationFloats = { directionToMove.x, directionToMove.y, normalizedDistance };
            return AStarObservationFloats;
        }
        else
        {
            float[] AStarObservationFloats = { 0, 0, 0 };
            return AStarObservationFloats;
        }
    }

    public void Update()
    {
        //// When pressing the m key
        //if (Input.GetKeyDown(KeyCode.M))
        //{
        //    pathfinding = GetComponent<PathFinding>();
        //    sheetAssigner = GameObject.Find("LevelGenerator").GetComponent<SheetAssigner>();

        //    Debug.Log(pickupDeliveryPairs.Count);
        //    if (iHaveInstrument) 
        //    {
        //        Vector2 currentRoomGridPos = sheetAssigner.PositionToGridPos(this.transform);
        //        Debug.Log("currentRoomGridPos: " + currentRoomGridPos);
        //        GameObject deliveryPoint = envController.getDeliveryPointPair(pickupDeliveryPairs, sphereIndicator);
        //        if (deliveryPoint == null)
        //        {
        //            Debug.Log("No delivery point pair found for the current instrument");
        //            return;
        //        }

        //        Vector2 deliveryPointGridPos = sheetAssigner.PositionToGridPos(deliveryPoint.transform);
        //        Debug.Log("deliveryPointGridPos: " + deliveryPointGridPos);
        //        List<Room> path = pathfinding.FindPath(currentRoomGridPos, deliveryPointGridPos);

        //        AStarObservation(path);
        //    }
        //    else
        //    {
        //        for (int i = 0; i < pickupDeliveryPairs.Count; i++)
        //        {
        //            Vector2 currentRoomGridPos = sheetAssigner.PositionToGridPos(this.transform);
        //            Debug.Log("currentRoomGridPos: " + currentRoomGridPos);
        //            GameObject pickupPoint = pickupDeliveryPairs[i].Item1;
        //            Vector2 pickupPointGridPos = sheetAssigner.PositionToGridPos(pickupPoint.transform);
        //            Debug.Log("pickupPointGridPos: " + pickupPointGridPos);
        //            List<Room> path = pathfinding.FindPath(currentRoomGridPos, pickupPointGridPos);

        //            AStarObservation(path);
        //        }
        //    }
        //}
    }
}
