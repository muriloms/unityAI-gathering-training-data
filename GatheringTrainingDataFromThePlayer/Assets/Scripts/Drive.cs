using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class Drive : MonoBehaviour
{
    public float speed = 50.0f;
    public float rotationSpeed = 100.0f;
    public float visibleDistance = 200.0f;
    
    private List<string> collectedTrainingData = new List<string>( );
    private StreamWriter tdf;

    private void Start()
    {
        string path = Application.dataPath + "/trainingData.txt";
        tdf = File.CreateText(path);
    }

    private void Update()
    {
        ActivateDriver();
    }

    private void OnApplicationQuit()
    {
        foreach (string td in collectedTrainingData)
        {
            tdf.WriteLine(td);
        }
        tdf.Close();
    }

    private void ActivateDriver()
    {
        float translationInput = Input.GetAxis("Vertical");
        float rotationInput = Input.GetAxis("Horizontal");
        float translation = translationInput * speed * Time.deltaTime;
        float rotation = rotationInput * speed * Time.deltaTime;
        
        transform.Translate(0,0, translation);
        transform.Rotate(0, rotation,0);

        Debug.DrawRay(transform.position, transform.forward * visibleDistance, Color.red);
        Debug.DrawRay(transform.position, transform.right * visibleDistance, Color.blue);
        
        // Raucasts
        RaycastHit hit;
        float fDist = 0;
        float rDist = 0;
        float lDist = 0;
        float r45Dist = 0;
        float l45Dist = 0;
        
        // Forward
        if (Physics.Raycast(transform.position, transform.forward, out hit, visibleDistance))
        {
            fDist = 1 - Round(hit.distance/visibleDistance);
        }
        
        // Right
        if (Physics.Raycast(transform.position, transform.right, out hit, visibleDistance))
        {
            rDist = 1 - Round(hit.distance/visibleDistance);
        }
        
        // Left
        if (Physics.Raycast(transform.position, -transform.right, out hit, visibleDistance))
        {
            lDist = 1 - Round(hit.distance / visibleDistance);
        }
        
        // Right 45
        if (Physics.Raycast(transform.position, Quaternion.AngleAxis(45,Vector3.up) * -transform.right, out hit, visibleDistance))
        {
            r45Dist = 1 - Round(hit.distance/visibleDistance);
        }
        
        // Left 45
        if (Physics.Raycast(transform.position, Quaternion.AngleAxis(-45,Vector3.up) * transform.right, out hit, visibleDistance))
        {
            l45Dist = 1 - Round(hit.distance/visibleDistance);
        }

        string td = fDist + "," + rDist + "," + lDist + "," + r45Dist + "," + l45Dist + "," + Round(translationInput) +
                    "," + Round(rotationInput);

        if (!collectedTrainingData.Contains(td))
        {
            collectedTrainingData.Add(td);
        }
        
    }

    private float Round(float x)
    {
        return (float) System.Math.Round(x, System.MidpointRounding.AwayFromZero) / 2.0f;
    }
    
    
}
