using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeliveryPointManager : MonoBehaviour
{
    public Color deliverySphereColor;

    private void Start()
    {
        // Get child component with the name "Sphere"
        Transform sphere = transform.Find("Sphere");
        if (sphere != null)
        {
            // Set the color of the sphere object
            deliverySphereColor = sphere.GetComponent<Renderer>().material.color;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "AmrAgent")
        {
            // If the agent has a sphere object as a child, check if the color of the sphere is the same as the delivery point
            Transform sphere = other.transform.Find("SphereIndicator");
            if (sphere != null)
            {
                if (sphere.GetComponent<Renderer>().material.color == deliverySphereColor)
                {
                    // Delivery successful, the delivery point is disabled
                    gameObject.SetActive(false);
                }
            }
        }
    }
}
