using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;

public class SwitchCamera : MonoBehaviour
{

    
    public GameObject Camera_1; 
    public GameObject Camera_2;
    public int Manager;


    void Update()
    {
        if (Input.GetKeyDown("space"))
        {
            //Debug.Log("Space key was pressed.");
            ManageCamera();
        }
    }

    public void ManageCamera()
    {
        if (Manager == 0)
        {
            Cam_2();
            Manager = 1;
        }
        else
        {
            Cam_1();
            Manager = 0;
        }
    }

    void Cam_1()
    {
        Camera_1.SetActive(true); 
        Camera_2.SetActive(false);
    }
    void Cam_2()
    {
        Camera_1.SetActive(false);
        Camera_2.SetActive(true);
    }

}
