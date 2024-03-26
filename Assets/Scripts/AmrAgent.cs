using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class AmrAgent : Agent
{
    public GameObject MyInstrument; //my instrument gameobject. will be enabled when instrument picked up.
    public bool IHaveAnInstrument; //have i picked up a instrument
    [SerializeField] Transform Target;
    Rigidbody amrAgent;
    // Start is called before the first frame update
    void Start()
    {
        MyInstrument.SetActive(false);
        IHaveAnInstrument = false;
    }

    public override void Initialize()
    {
        amrAgent = GetComponent<Rigidbody>();
    }

    // Called to set-up the environment for a new episode
    public override void OnEpisodeBegin()
    {
        
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Target and Agent positions

        // Agent velocity

        // Other Agenst's positions?

        // Raycast detections
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        
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
}
