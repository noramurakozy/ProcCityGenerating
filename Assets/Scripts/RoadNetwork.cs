
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RoadNetwork : MonoBehaviour
{
    private List<Road> primaryQueue;
    private List<Road> finalSegments;
    private double[,] heatMap;

    [SerializeField]
    private RoadView roadViewPrefab;

    [SerializeField]
    private GameObject heatMapField;

    [SerializeField]
    private int numOfSteps;

    private readonly double DEFAULT_BRANCH_PROBABILITY = 0.3;
    private readonly double HIGHWAY_BRANCH_PROBABILITY = 0.05;
    private readonly double HIGHWAY_BRANCH_POPULATION_THRESHOLD = 6;
    private readonly double NORMAL_BRANCH_POPULATION_THRESHOLD = 4;
    private readonly int NORMAL_BRANCH_TIME_DELAY_FROM_HIGHWAY = 5;
    private readonly int HIGHWAY_SEGMENT_LENGTH = 3;
    private readonly int BRANCH_SEGMENT_LENGTH = 2;
    private readonly int HIGHWAY_RANDOM_ANGLE = 15;
    private readonly int DEFAULT_ROAD_RANDOM_ANGLE = 3;
    private readonly float MAP_HEIGHT = 100;
    private readonly float MAP_WIDTH = 100;
    private Rect bounds;

    int heatMapScale = 1;

    // Start is called before the first frame update
    void Start()
    {
        bounds = new Rect(0, 0, MAP_WIDTH, MAP_HEIGHT);
        GeneratePopulationHeatMap((int)MAP_WIDTH / heatMapScale, (int)MAP_HEIGHT / heatMapScale);
        primaryQueue = new List<Road>() {
            new Road() { Start = new Vector3(MAP_HEIGHT/2, 0, MAP_WIDTH/2), End = new Vector3(MAP_HEIGHT/2+HIGHWAY_SEGMENT_LENGTH, 0, MAP_WIDTH/2), Number = 0, IsHighway = true },
            new Road() { Start = new Vector3(MAP_HEIGHT/2, 0, MAP_WIDTH/2), End = new Vector3(MAP_HEIGHT/2-HIGHWAY_SEGMENT_LENGTH, 0, MAP_WIDTH/2), Number = 0, IsHighway = true, DirectionAngle = 180 }
        };
        finalSegments = new List<Road>();

        while (primaryQueue.Count != 0 && numOfSteps != 0)
        {
            numOfSteps--;
            Road min = primaryQueue.Aggregate((r1, r2) => r1.Number < r2.Number ? r1 : r2);
            primaryQueue.Remove(min);

            Road modified = CheckLocalConstraints(min);
            if (modified == null)
            {
                continue;
            }

            finalSegments.Add(modified);

            foreach (Road road in GlobalGoals(modified))
            {
                primaryQueue.Add(new Road() { Number = min.Number + road.Number + 1, Start = road.Start, End = road.End, DirectionAngle = road.DirectionAngle, IsHighway = road.IsHighway });
            }

        }

        StartCoroutine(DrawSegments());
        //DrawSegments();
        Debug.Log("start end");
    }

    private System.Collections.IEnumerator DrawSegments()
    {
        foreach (var road in finalSegments)
        {
            var roadView = Instantiate(roadViewPrefab);
            roadView.Road = road;

            if (road.IsHighway)
            {
                roadView.GetComponent<LineRenderer>().endColor = new Color(0, 0, 1);
                roadView.GetComponent<LineRenderer>().startColor = new Color(0, 0, 1);
            }
            roadView.Draw();
            //yield return new WaitForSeconds(0.2f);
            //yield return null;
        }
        yield return null;

        Debug.Log("Done");
    }

    private Road CheckLocalConstraints(Road r)
    {
        foreach(Road segment in finalSegments)
        {
            if(lineSegmentsIntersect(r.Start, r.End, segment.Start, segment.End))
            {
                return null;
            }
        }
        return r;
    }

    private List<Road> GlobalGoals(Road previousRoad)
    {
        List<Road> newRoads = new List<Road>();

        Road straightRoad = Road.RoadWithDirection(previousRoad.End, previousRoad.DirectionAngle, previousRoad.Length, 0, previousRoad.IsHighway);
        var straightPopulation = GetHeatMapAt(straightRoad.End.x, straightRoad.End.z);

        if (previousRoad.IsHighway)
        {
            var angle = previousRoad.DirectionAngle + UnityEngine.Random.Range(-HIGHWAY_RANDOM_ANGLE, HIGHWAY_RANDOM_ANGLE);
            Road randomAngleRoad = Road.RoadWithDirection(previousRoad.End, angle, previousRoad.Length, 0, previousRoad.IsHighway);

            var randomRoadPopulation = GetHeatMapAt(randomAngleRoad.End.x, randomAngleRoad.End.z);

            double roadPopluation;

            //highway continues
            if (randomRoadPopulation > straightPopulation)
            {
                newRoads.Add(randomAngleRoad);
                roadPopluation = randomRoadPopulation;
                randomAngleRoad.Population = (float)roadPopluation;
            }
            else
            {
                newRoads.Add(straightRoad);
                roadPopluation = straightPopulation;
                straightRoad.Population = (float)roadPopluation;
            }

            //highway from highway
            if (roadPopluation > HIGHWAY_BRANCH_POPULATION_THRESHOLD)
            {
                if (UnityEngine.Random.value < HIGHWAY_BRANCH_PROBABILITY)
                {
                    float leftAngle = previousRoad.DirectionAngle - 90 + UnityEngine.Random.Range(-DEFAULT_ROAD_RANDOM_ANGLE, DEFAULT_ROAD_RANDOM_ANGLE);
                    var leftHighwayBranch = Road.RoadWithDirection(previousRoad.End, leftAngle, HIGHWAY_SEGMENT_LENGTH, 0, previousRoad.IsHighway);
                    newRoads.Add(leftHighwayBranch);
                }
                else if (UnityEngine.Random.value < HIGHWAY_BRANCH_PROBABILITY)
                {
                    float rightAngle = previousRoad.DirectionAngle + 90 + UnityEngine.Random.Range(-DEFAULT_ROAD_RANDOM_ANGLE, DEFAULT_ROAD_RANDOM_ANGLE);
                    var rightHighwayBranch = Road.RoadWithDirection(previousRoad.End, rightAngle, HIGHWAY_SEGMENT_LENGTH, 0, previousRoad.IsHighway);
                    newRoads.Add(rightHighwayBranch);
                }
            } 
        }
        else if (straightPopulation > NORMAL_BRANCH_POPULATION_THRESHOLD)
        {
            Debug.Log("straightpop: " + straightPopulation);
            newRoads.Add(straightRoad);
        }

        //secondary road branching 
        if (straightPopulation > NORMAL_BRANCH_POPULATION_THRESHOLD)
        {
            int timeDelay = 0;
            if (previousRoad.IsHighway)
            {
                timeDelay = NORMAL_BRANCH_TIME_DELAY_FROM_HIGHWAY;
            }

            if (UnityEngine.Random.value < DEFAULT_BRANCH_PROBABILITY)
            {
                float leftAngle = previousRoad.DirectionAngle - 90 + UnityEngine.Random.Range(-DEFAULT_ROAD_RANDOM_ANGLE, DEFAULT_ROAD_RANDOM_ANGLE);
                var leftBranch = Road.RoadWithDirection(previousRoad.End, leftAngle, BRANCH_SEGMENT_LENGTH, timeDelay, false);
                //Debug.Log("pop when branching 1: " + straightPopulation);
                newRoads.Add(leftBranch);
            }
            else if (UnityEngine.Random.value < DEFAULT_BRANCH_PROBABILITY)
            {
                float rightAngle = previousRoad.DirectionAngle + 90 + UnityEngine.Random.Range(-DEFAULT_ROAD_RANDOM_ANGLE, DEFAULT_ROAD_RANDOM_ANGLE);
                var rightBranch = Road.RoadWithDirection(previousRoad.End, rightAngle, BRANCH_SEGMENT_LENGTH, timeDelay, false);
                //Debug.Log("pop when branching 2: " + straightPopulation);
                newRoads.Add(rightBranch);
            }  
        }

        return newRoads;
    }

    private void GeneratePopulationHeatMap(int width, int height)
    {
        //var seed = UnityEngine.Random.Range(0, 100); 
        //heatMap = new double[width, height];
        /*for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                //heatMap[i, j] = (int)(Mathf.PerlinNoise(i * 0.151f + seed, j * 0.151f + seed) * 10);
                var value1 = (Mathf.PerlinNoise(i / 10000.0f, j / 10000.0f) + 1) / 2.0f;
                var value2 = (Mathf.PerlinNoise(i / 20000.0f + 500, j / 20000.0f + 500) + 1) / 2.0f;
                var value3 = (Mathf.PerlinNoise(i / 20000.0f + 1000, j / 20000.0f + 1000) + 1) / 2.0f;
                heatMap[i, j] = Math.Pow((value1 * value2 + value3) / 2, 2);
            }
        }*/

        DrawHeatMap(width, height);
        GetHeatMapAtLog();
    }

    private double GetHeatMapAt(float i, float j)
    {
        return (Mathf.PerlinNoise(i / 30f, j / 30f) * 8);
    }

    public void GetHeatMapAtLog()
    {
        Debug.Log((Mathf.PerlinNoise(49.29263f / 30f, 31.5672f / 30f) * 8));
    }

    private void DrawHeatMap(int width, int height)
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                double result = GetHeatMapAt(i,j);

                Vector3 pos = new Vector3(i, -2, j);

                //Debug.Log(GetHeatMapAt(i, j));
                var cube = Instantiate(heatMapField);
                //cube.transform.localScale = new Vector3(heatMapScale, 1, heatMapScale);
                cube.transform.position = pos * heatMapScale;
                //if(result <= NORMAL_BRANCH_POPULATION_THRESHOLD)
                //{
                //    //cube.GetComponent<Renderer>().material.color = new Color(0,0,0);
                //}
                //else
                //{
                //}
                cube.GetComponent<Renderer>().material.color = new Color(5 * (float)result/ 255.0f, 25.5f * (float)result/ 255.0f, 40 / 255.0f);
            }
        }

    }

    private Vector3 GetRotatedVectorByAngle(int angle, Vector3 inputVector)
    {
        return Quaternion.Euler(0, angle, 0) * inputVector;
    }

    private bool lineSegmentsIntersect(Vector3 lineOneA, Vector3 lineOneB, Vector3 lineTwoA, Vector3 lineTwoB)
    {
        bool ret = false;
        if (!(lineOneA == lineTwoA || lineOneA == lineTwoB || lineOneB == lineTwoA || lineOneB == lineTwoB))
        {
            ret = (((lineTwoB.z - lineOneA.z) * (lineTwoA.x - lineOneA.x) > (lineTwoA.z - lineOneA.z) * (lineTwoB.x - lineOneA.x))
            != ((lineTwoB.z - lineOneB.z) * (lineTwoA.x - lineOneB.x) > (lineTwoA.z - lineOneB.z) * (lineTwoB.x - lineOneB.x))
            && ((lineTwoA.z - lineOneA.z) * (lineOneB.x - lineOneA.x) > (lineOneB.z - lineOneA.z) * (lineTwoA.x - lineOneA.x))
            != ((lineTwoB.z - lineOneA.z) * (lineOneB.x - lineOneA.x) > (lineOneB.z - lineOneA.z) * (lineTwoB.x - lineOneA.x)));
        }
        return ret;
    }
}
