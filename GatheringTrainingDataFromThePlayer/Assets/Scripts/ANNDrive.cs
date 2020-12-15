using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class ANNDrive : MonoBehaviour
{
    private ANN ann;

    public float visibleDistance = 200;

    public int epochs = 1000;

    public float speed = 50.0f;

    public float rotationSpeed = 100.0f;

    private bool trainingDone = false;

    private float trainingProgress = 0;

    private double sse = 0;

    private double lastSSE = 1;

    public float translation;

    public float rotation;

    public bool loadFromFile = false;
    
    // Start is called before the first frame update
    void Start()
    {
        ann = new ANN(5, 2, 1, 10, 0.5);
        if (loadFromFile)
        {
            LoadWeightsFromFile();
            trainingDone = true;
        }
        else
        {
            StartCoroutine(LoadTrainingSet());
        }
        
    }
    
    // Update is called once per frame
    void Update()
    {
        ActivateDriver();
    }
    
    private void OnGUI()
    {
        GUI.Label(new Rect(25,25,250,30), "SSE: " + lastSSE);
        GUI.Label(new Rect(25,40,250,30), "Alpha: " + ann.alpha);
        GUI.Label(new Rect(25,55,250,30), "Trained: " + trainingProgress);
    }

    IEnumerator LoadTrainingSet()
    {
        string path = Application.dataPath + "/trainingData.txt";
        string line;
        if (File.Exists(path))
        {
            int lineCount = File.ReadAllLines(path).Length;
            StreamReader tdf = File.OpenText(path);
            List<double> calcOutputs = new List<double>();
            List<double> inputs = new List<double>();
            List<double> outputs = new List<double>();

            for (int i = 0; i < epochs; i++)
            {
                //set file pointer to beginning of file
                sse = 0;
                tdf.BaseStream.Position = 0;
                string currentWeights = ann.PrintWeights();
                while ((line = tdf.ReadLine()) != null)
                {
                    string[] data = line.Split(',');

                    float thisError = 0;
                    if (System.Convert.ToDouble(data[5]) != 0 && System.Convert.ToDouble(data[6]) != 0)
                    {
                        inputs.Clear();
                        outputs.Clear();
                        inputs.Add(System.Convert.ToDouble(data[0]));
                        inputs.Add(System.Convert.ToDouble(data[1]));
                        inputs.Add(System.Convert.ToDouble(data[2]));
                        inputs.Add(System.Convert.ToDouble(data[3]));
                        inputs.Add(System.Convert.ToDouble(data[4]));

                        double o1 = Map(0, 1, -1, 1, System.Convert.ToSingle(data[5]));
                        outputs.Add(o1);
                        double o2 = Map(0, 1, -1, 1, System.Convert.ToSingle(data[6]));
                        outputs.Add(o2);

                        calcOutputs = ann.Train(inputs, outputs);
                        thisError = ((Mathf.Pow((float) (outputs[0] - calcOutputs[0]), 2) +
                                      Mathf.Pow((float) (outputs[1] - calcOutputs[1]), 2))) / 2.0f;
                    }

                    sse += thisError;
                }

                trainingProgress = (float) i / (float) epochs;
                sse /= lineCount;
                if (lastSSE < sse)
                {
                    ann.LoadWeights(currentWeights);
                    ann.alpha = Mathf.Clamp((float) ann.alpha - 0.001f, 0.01f, 0.9f);
                }
                else
                {
                    ann.alpha = Mathf.Clamp((float) ann.alpha + 0.001f, 0.01f, 0.9f);
                    lastSSE = sse;
                }

                yield return null;
            }
        }

        trainingDone = true;
        SaveWeightsToFile();
    }

    private float Map(float newfrom, float newto, float origfrom, float origto, float value)
    {
        if (value <= origfrom)
        {
            return newfrom;
        }
        else if (value >= origto)
        {
            return newto;
        }

        return (newto - newfrom) * ((value - origfrom) / (origto - origfrom)) + newfrom;
    }
    
    private float Round(float x)
    {
        return (float) System.Math.Round(x, System.MidpointRounding.AwayFromZero) / 2.0f;
    }

    private void ActivateDriver()
    {
        if (!trainingDone)
        {
            return;
        }
        
        List<double> calcOutputs = new List<double>();
        List<double> inputs = new List<double>();
        List<double> outputs = new List<double>();

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

        inputs.Add(fDist);
        inputs.Add(rDist);
        inputs.Add(lDist);
        inputs.Add(r45Dist);
        inputs.Add(l45Dist);
        
        outputs.Add(0);
        outputs.Add(0);

        calcOutputs = ann.CalcOutput(inputs, outputs);
        float translationInput = Map(-1, 1, 0, 1, (float) calcOutputs[0]);
        float rotationInput = Map(-1, 1, 0, 1, (float) calcOutputs[1]);

        translation = translationInput * speed * Time.deltaTime;
        rotation = rotationInput * rotationSpeed * Time.deltaTime;
        
        transform.Translate(0,0, translation);
        transform.Rotate(0, rotation, 0);

    }

    private void SaveWeightsToFile()
    {
        string path = Application.dataPath + "/weights.txt";
        StreamWriter wf = File.CreateText(path);
        wf.WriteLineAsync(ann.PrintWeights());
        wf.Close();
    }

    private void LoadWeightsFromFile()
    {
        string path = Application.dataPath + "/weights.txt";
        StreamReader wf = File.OpenText(path);

        if (File.Exists(path))
        {
            string line = wf.ReadLine();
            ann.LoadWeights(line);
        }
    }

    
}
