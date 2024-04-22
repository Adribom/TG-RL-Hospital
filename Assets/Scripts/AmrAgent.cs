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
    public LayerMask layerForAgentSpawnDetection;

    public override void Initialize()
    {
        envController = GetComponentInParent<EnvController>();
    }

    public override void OnEpisodeBegin()
    {
        ResetSphere();
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

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position, collisionRadius);
    }

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
                        ResetSphere();
                        envController.CompleteDelivery();
                    }
                }
            }
        }
    }

    private void ResetSphere()
    {
        sphereIndicator.GetComponent<Renderer>().material.color = Color.white;
        sphereIndicator.SetActive(false);
    }

    //[SerializeField] Transform Target;
    //Rigidbody amrAgent;
    //// Start is called before the first frame update
    //void Start()
    //{
    //    MyInstrument.SetActive(false);
    //    IHaveAnInstrument = false;
    //}

    //public override void Initialize()
    //{
    //    amrAgent = GetComponent<Rigidbody>();
    //}

    //// Called to set-up the environment for a new episode
    //public override void OnEpisodeBegin()
    //{

    //}

    //public override void CollectObservations(VectorSensor sensor)
    //{
    //    // Target and Agent positions

    //    // Agent velocity

    //    // Other Agenst's positions?

    //    // Raycast detections
    //}

    //public override void OnActionReceived(ActionBuffers actions)
    //{

    //}

    ////TODO: Check if continuous would be better
    //public override void Heuristic(in ActionBuffers actionsOut)
    //{
    //    var discreteActionsOut = actionsOut.DiscreteActions;
    //    if (Input.GetKey(KeyCode.D))
    //    {
    //        discreteActionsOut[0] = 3;
    //    }
    //    else if (Input.GetKey(KeyCode.W))
    //    {
    //        discreteActionsOut[0] = 1;
    //    }
    //    else if (Input.GetKey(KeyCode.A))
    //    {
    //        discreteActionsOut[0] = 4;
    //    }
    //    else if (Input.GetKey(KeyCode.S))
    //    {
    //        discreteActionsOut[0] = 2;
    //    }
    //}
}
