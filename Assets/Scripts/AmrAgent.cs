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
    private List<(GameObject, GameObject)> pickupDeliveryPairs;
    private float collisionRadius = 10f;
    [HideInInspector]
    public bool iHaveInstrument = false;
    public GameObject sphereIndicator;
    public LayerMask layerForAgentSpawnDetection;
    private Rigidbody amrAgent;
    private float agentRunSpeed = 3f;

    public void Start()
    {
        ResetAgentComponents();
        envController = GetComponentInParent<EnvController>();
        pickupDeliveryPairs = envController.GetPickupDeliveryPairs();
    }

    // Called when the agent is first initialized
    public override void Initialize()
    {
        ResetAgentComponents();
        envController = GetComponentInParent<EnvController>();
        pickupDeliveryPairs = envController.GetPickupDeliveryPairs();
        amrAgent = GetComponent<Rigidbody>();
    }

    // Called to set-up the environment for a new episode
    public override void OnEpisodeBegin()
    {
        ResetAgentComponents();
        pickupDeliveryPairs = envController.GetPickupDeliveryPairs();
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

        Debug.Log(pickupDeliveryPairs.Count);
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
            sensor.AddObservation(AStarObservation(path));
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
                sensor.AddObservation(AStarObservation(path));
            }
        }

        // Agent current position
        sensor.AddObservation(transform.position);

        // Agent velocity
        sensor.AddObservation(amrAgent.velocity);

        // Agent rotation
        sensor.AddObservation(transform.rotation);

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

    private Vector2 AStarObservation(List<Room> path)
    {
        // Create a vector observation that contains: (x_next_A*_room, y_next_A*_room, distance_point_of_interest)
        if (path != null)
        {
            // print the path to the room with point of interest
            Debug.Log("Path to room with point of interest:");
            foreach (Room room in path)
            {
                Debug.Log(room.gridPos);
            }

            Vector2 nextRoomGridPos = new Vector2();

            //Next room position
            if (path.Count > 1)
            {
                nextRoomGridPos = path[1].gridPos;
            }
            else
            {
                nextRoomGridPos = path[0].gridPos;
                Debug.Log("Arrived at final destination!!");
            }
            //distance to delivery point
            int distancePointOfInterest = path.Count - 1;

            Vector3 AStarObservation = new Vector3(nextRoomGridPos.x, nextRoomGridPos.y, distancePointOfInterest);
            Debug.Log("Final Observation: " + AStarObservation);
            return AStarObservation;
        }
        else
        {
            Vector3 AStarObservation = new Vector3(0, 0, 0);
            Debug.Log("Null Final Observation: " + AStarObservation);
            return AStarObservation;
        }
    }

    public void Update()
    {
        // When pressing the m key
        if (Input.GetKeyDown(KeyCode.M))
        {
            pathfinding = GetComponent<PathFinding>();
            sheetAssigner = GameObject.Find("LevelGenerator").GetComponent<SheetAssigner>();

            Debug.Log(pickupDeliveryPairs.Count);
            if (iHaveInstrument) 
            {
                Vector2 currentRoomGridPos = sheetAssigner.PositionToGridPos(this.transform);
                Debug.Log("currentRoomGridPos: " + currentRoomGridPos);
                GameObject deliveryPoint = envController.getDeliveryPointPair(pickupDeliveryPairs, sphereIndicator);
                if (deliveryPoint == null)
                {
                    Debug.Log("No delivery point pair found for the current instrument");
                    return;
                }

                Vector2 deliveryPointGridPos = sheetAssigner.PositionToGridPos(deliveryPoint.transform);
                Debug.Log("deliveryPointGridPos: " + deliveryPointGridPos);
                List<Room> path = pathfinding.FindPath(currentRoomGridPos, deliveryPointGridPos);

                AStarObservation(path);
            }
            else
            {
                for (int i = 0; i < pickupDeliveryPairs.Count; i++)
                {
                    Vector2 currentRoomGridPos = sheetAssigner.PositionToGridPos(this.transform);
                    Debug.Log("currentRoomGridPos: " + currentRoomGridPos);
                    GameObject pickupPoint = pickupDeliveryPairs[i].Item1;
                    Vector2 pickupPointGridPos = sheetAssigner.PositionToGridPos(pickupPoint.transform);
                    Debug.Log("pickupPointGridPos: " + pickupPointGridPos);
                    List<Room> path = pathfinding.FindPath(currentRoomGridPos, pickupPointGridPos);

                    AStarObservation(path);
                }
            }
        }
    }
}
