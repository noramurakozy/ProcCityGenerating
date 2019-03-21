using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralTest : MonoBehaviour
{
    [SerializeField]
    private LineRenderer segment;

    [SerializeField]
    private int numOfIterations;

    [SerializeField]
    private List<Vector3> seedPoints;

    //private List<LineRenderer> segments;

    private List<List<LineRenderer>> listOfSegmentLists;

    // Start is called before the first frame update
    void Start()
    {
        listOfSegmentLists = new List<List<LineRenderer>>();
        for (int i = 0; i < seedPoints.Count; i++)
        {
            LineRenderer seg = Instantiate(segment);
            seg.SetPosition(0, seedPoints[i]);
            seg.SetPosition(1, new Vector3(seedPoints[i].x + 1, 0, seedPoints[i].z));

            listOfSegmentLists.Add(new List<LineRenderer>() { seg });
        }


        StartCoroutine(Draw());


        //generateKoch(numOfIterations);
    }

    private IEnumerator Draw()
    {
        for (int i = 0; i < numOfIterations; i++)
        {
            UnityEngine.Random.InitState(System.DateTime.Now.Millisecond);
            foreach (List<LineRenderer> segments in listOfSegmentLists)
            {
                int randomRotation = UnityEngine.Random.Range(0, 4);
                Vector3 rot = getRotationVector(randomRotation);
                LineRenderer nextSeg = Instantiate(segment);
                nextSeg.SetPosition(0, segments[segments.Count - 1].GetPosition(1));
                nextSeg.SetPosition(1, nextSeg.GetPosition(0) + rot);
                segments.Add(nextSeg);

            }
            //yield return new WaitForSeconds(0.2f);
            yield return null;
        }
    }

    private void generateKoch(int numberOfIterations)
    {
        //F—F—F
        //F → F + F—F + F
        //60 fok

        string init = "F-F-F";

        for(int i = 0; i < numberOfIterations; i++)
        {
            init = init.Replace("F", "F+F-F+F");
        }

        Debug.Log(init);
        kochCurveDraw(init, 60);
    }

    private void kochCurveDraw(string input, int degree)
    {
        List<LineRenderer> segs = new List<LineRenderer>();
        Vector3 vector = new Vector3(5, 0, 0);
        Vector3 currentPos = new Vector3(0, 0, 0);

        LineRenderer r = null;
        foreach(char c in input)
        {
            if (c.Equals('F'))
            {
                r = Instantiate(segment);
                segs.Add(r);
                r.SetPosition(0, currentPos);
                r.SetPosition(1, r.GetPosition(0) + vector);
                currentPos = r.GetPosition(1);
            }
            else if (c.Equals('-'))
            {
                vector = Quaternion.Euler(0, 180-degree, 0) * vector;
            }
            else if (c.Equals('+'))
            {
                vector = Quaternion.Euler(0, -degree, 0) * vector;
            }

            Debug.Log(vector);
        }
    }

    private Vector3 getRotationVector(int randomRotation)
    {
        int randomLength = UnityEngine.Random.Range(-3, 4);
        switch (randomRotation) {
            case 0:
                return new Vector3(0, 0, randomLength);
            case 1:
                return new Vector3(randomLength, 0, 0);
            case 2:
                return new Vector3(0, 0, randomLength);
            case 3:
                return new Vector3(randomLength, 0, 0);
            case 4:
                return new Vector3(1, 0, 1);
            case 5:
                return new Vector3(-1, 0, 1);
            case 6:
                return new Vector3(-1, 0, -1);
            case 7:
                return new Vector3(-1, 0, 1);
            default:
                return new Vector3(0, 0, 0);
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
