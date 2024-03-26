using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

public class EnvController : MonoBehaviour
{
    [System.Serializable]
    public class PlayerInfo
    {
        public AmrAgent Agent;
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
    private Dictionary<AmrAgent, PlayerInfo> m_PlayerDict = new Dictionary<AmrAgent, PlayerInfo>();
    public bool UseRandomAgentRotation = true;
    public bool UseRandomAgentPosition = true;

    public GameObject Key;
    private SimpleMultiAgentGroup m_AgentGroup;
    private int deliveryCount = 0;
    private int totalDeliveriesToReset = 10;

    // Start is called before the first frame update
    void Start()
    {

        //Hide The Key
        Key.SetActive(false);

        // Initialize TeamManager
        m_AgentGroup = new SimpleMultiAgentGroup();
        foreach (var item in AgentsList)
        {
            item.StartingPos = item.Agent.transform.position;
            item.StartingRot = item.Agent.transform.rotation;
            item.Rb = item.Agent.GetComponent<Rigidbody>();
            item.Col = item.Agent.GetComponent<Collider>();
            // Add to team manager
            m_AgentGroup.RegisterAgent(item.Agent);
        }

        ResetScene();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        m_ResetTimer += 1;
        if (m_ResetTimer >= MaxEnvironmentSteps && MaxEnvironmentSteps > 0)
        {
            m_AgentGroup.GroupEpisodeInterrupted();
            ResetScene();
        }
    }

    public void ManageSucessfullDelivery()
    {
        print("Surgical instrument delivered!");
        m_AgentGroup.AddGroupReward(1f);
        
        if (deliveryCount == totalDeliveriesToReset)
            m_AgentGroup.EndGroupEpisode();

        ResetScene();
    }

    public void AddDeliveryCount()
    {
        deliveryCount++;
    }

    /// <summary>
    /// Use the ground's bounds to pick a random spawn position.
    /// </summary>
    public Vector3 GetRandomSpawnPos()
    {
        
    }
    Quaternion GetRandomRot()
    {
        return Quaternion.Euler(0, Random.Range(0.0f, 360.0f), 0);
    }

    void ResetScene()
    {

        //Reset counter
        m_ResetTimer = 0;

        //Random hospital layout

        //Reset Agents
        foreach (var item in AgentsList)
        {
            var pos = UseRandomAgentPosition ? GetRandomSpawnPos() : item.StartingPos;
            var rot = UseRandomAgentRotation ? GetRandomRot() : item.StartingRot;

            item.Agent.transform.SetPositionAndRotation(pos, rot);
            item.Rb.velocity = Vector3.zero;
            item.Rb.angularVelocity = Vector3.zero;
            item.Agent.MyInstrument.SetActive(false);
            item.Agent.IHaveAnInstrument = false;
            item.Agent.gameObject.SetActive(true);
            m_AgentGroup.RegisterAgent(item.Agent);
        }

        //Reset Key
        Key.SetActive(false);
    }
}
