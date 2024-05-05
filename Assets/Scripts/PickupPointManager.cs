using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickupPointManager : MonoBehaviour
{
    public Color pickupSphereColor;

    private void Start()
    {
        // Get child component with the name "Sphere"
        Transform sphere = transform.Find("Sphere");
        if (sphere != null)
        {
            // Set the color of the sphere object
            pickupSphereColor = sphere.GetComponent<Renderer>().material.color;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "AmrAgent")
        {
            Debug.Log("The other is an agent");

            // If the agent has a white child sphere, this means the agent has not picked up any color yet
            Transform sphere = other.transform.Find("SphereIndicator");
            if (sphere != null)
            {
                Debug.Log("The other agent has a sphere");

                if (sphere.GetComponent<Renderer>().material.color == Color.white)
                {
                    Debug.Log("The other's sphere is white, so terminate me");
                    // Pickup successful, the pickup point is disabled
                    gameObject.SetActive(false);
                }
            }
        }
    }
}
