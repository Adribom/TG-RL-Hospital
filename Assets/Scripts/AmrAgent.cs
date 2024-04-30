using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine.Rendering;

public class AmrAgent : Agent
{
    private EnvController envController;
    private float collisionRadius = 10f;
    public GameObject sphereIndicator;
    private bool IHaveAnInstrument;
    public LayerMask layerForAgentSpawnDetection;
    private Rigidbody amrAgent;
    private float agentRunSpeed = 3f;

    // Called when the agent is first initialized
    public override void Initialize()
    {
        envController = GetComponentInParent<EnvController>();
        List<(GameObject,GameObject)> pickupDeliveryPairs = envController.GetPickupDeliveryPairs();
        amrAgent = GetComponent<Rigidbody>();
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
        if (other.gameObject.tag == "PickupPoint" && !sphereIndicator.activeSelf)
        {
            // If the pickupPoint has a sphere object as a child, check if the color of the sphere is the same as the delivery point
            Transform sphere = other.transform.Find("Sphere");
            if (sphere != null)
            {
                // Set the color of SphereIndicator to the color of the PickupPoint sphere
                sphereIndicator.GetComponent<Renderer>().material.color = sphere.GetComponent<Renderer>().material.color;
                sphereIndicator.SetActive(true);
                IHaveAnInstrument = true;
            }
        }
        if (other.gameObject.tag == "DeliveryPoint")
        {
            // If the sphere on the agent is activated and has the same color as the delivery point, the delivery is successful
            if (sphereIndicator.activeSelf)
            {
                Transform otherSphere = other.transform.Find("Sphere");
                if (otherSphere != null) { 
                    if (sphereIndicator.GetComponent<Renderer>().material.color == otherSphere.GetComponent<Renderer>().material.color)
                    {
                        ResetAgentComponents();
                        envController.CompleteDelivery();
                    }
                }
            }
        }
    }

    private void ResetAgentComponents()
    {
        sphereIndicator.GetComponent<Renderer>().material.color = Color.white;
        sphereIndicator.SetActive(false);
        IHaveAnInstrument = false;
    }

    //Rigidbody amrAgent;
    //// Start is called before the first frame update
    //void Start()
    //{
    //    MyInstrument.SetActive(false);
    //    IHaveAnInstrument = false;
    //}


    public override void CollectObservations(VectorSensor sensor)
    {
        // Target and Agent positions

        // Agent velocity

        // Other Agenst's positions?

        // Raycast detections
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
        amrAgent.AddForce(dirToGo * agentRunSpeed,
            ForceMode.VelocityChange);
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

    ////TODO: Check if continuous would be better
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
}
