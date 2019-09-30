
using QuadTrees;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;

public class RoadNetwork : MonoBehaviour
{
    private QuadTreeRect<Road> qTree;
    private List<Road> primaryQueue;
    private List<Road> finalSegments;
    private double[,] heatMap;

    [SerializeField]
    private RoadView roadViewPrefab;

    [SerializeField]
    private GameObject heatMapField;

    [SerializeField]
    [HideInInspector]
    private int numOfSteps;

    [SerializeField]
    [HideInInspector]
    private double defaultBranchProbability = 0.3;

    [SerializeField]
    [HideInInspector]
    private double highwayBranchProbability = 0.05;

    [SerializeField]
    [HideInInspector]
    private double highwayBranchPopulationThreshold = 6;

    [SerializeField]
    [HideInInspector]
    private double normalBranchPopulationThreshold = 4;

    [SerializeField]
    [HideInInspector]
    private int normalBranchTimeDelayFromHighway = 5;

    [SerializeField]
    [HideInInspector]
    private int highwaySegmentLength = 3;

    [SerializeField]
    [HideInInspector]
    private int branchSegmentLength = 2;

    [SerializeField]
    [HideInInspector]
    private int highwayRandomAngle = 15;

    [SerializeField, Range(1,360)]
    private int defaultRoadRandomAngle = 3;

    [SerializeField]
    [HideInInspector]
    private int minimumIntersectionDeviation = 30;

    [SerializeField]
    [HideInInspector]
    private float roadSnapDistance = 1;

    [SerializeField]
    [HideInInspector]
    private float mapHeight = 200;

    [SerializeField]
    [HideInInspector]
    private float mapWidth = 200;

    [SerializeField]
    [HideInInspector]
    private UnityEngine.Color highwayColor = UnityEngine.Color.black;

    [SerializeField]
    [HideInInspector]
    private UnityEngine.Color secondaryRoadColor = UnityEngine.Color.red;
    private Rect bounds;
    public static int lastAddedTime = 0;

    int heatMapScale = 1;

    public int NumOfSteps { get => numOfSteps; set => numOfSteps = value; }
    public double DefaultBranchProbability { get => defaultBranchProbability; set => defaultBranchProbability = value; }
    public double HighwayBranchProbability { get => highwayBranchProbability; set => highwayBranchProbability = value; }
    public double HighwayBranchPopulationThreshold { get => highwayBranchPopulationThreshold; set => highwayBranchPopulationThreshold = value; }
    public double NormalBranchPopulationThreshold { get => normalBranchPopulationThreshold; set => normalBranchPopulationThreshold = value; }
    public int NormalBranchTimeDelayFromHighway { get => normalBranchTimeDelayFromHighway; set => normalBranchTimeDelayFromHighway = value; }
    public int HighwaySegmentLength { get => highwaySegmentLength; set => highwaySegmentLength = value; }
    public int BranchSegmentLength { get => branchSegmentLength; set => branchSegmentLength = value; }
    public int HighwayRandomAngle { get => highwayRandomAngle; set => highwayRandomAngle = value; }
    public int DefaultRoadRandomAngle { get => defaultRoadRandomAngle; set => defaultRoadRandomAngle = value; }
    public int MinimumIntersectionDeviation { get => minimumIntersectionDeviation; set => minimumIntersectionDeviation = value; }
    public float RoadSnapDistance { get => roadSnapDistance; set => roadSnapDistance = value; }
    public float MapHeight { get => mapHeight; set => mapHeight = value; }
    public float MapWidth { get => mapWidth; set => mapWidth = value; }
    public UnityEngine.Color HighwayColor { get => highwayColor; set => highwayColor = value; }
    public UnityEngine.Color SecondaryRoadColor { get => secondaryRoadColor; set => secondaryRoadColor = value; }

    //Rect rect = new Rect(40, 40, 10, 10);

    
    // Start is called before the first frame update
    void Start()
    {
        //UnityEngine.Random.InitState(12345678);
        qTree = new QuadTreeRect<Road>(new RectangleF(-5000, -5000, 10000, 10000));
        //bounds = new Rect(0, 0, MapWidth, MapHeight);
        //DrawHeatMap((int)MapWidth / heatMapScale, (int)MapHeight / heatMapScale);
        primaryQueue = new List<Road>() {
            new Road() { Start = new Vector3(MapHeight/2, 0, MapWidth/2), End = new Vector3(MapHeight/2+HighwaySegmentLength, 0, MapWidth/2), Number = 0, IsHighway = true },
            new Road() { Start = new Vector3(MapHeight/2, 0, MapWidth/2), End = new Vector3(MapHeight/2-HighwaySegmentLength, 0, MapWidth/2), Number = 0, IsHighway = true, DirectionAngle = 180 }
        };
        finalSegments = new List<Road>();

        while (primaryQueue.Count != 0 && NumOfSteps != 0)
        {
            NumOfSteps--;
            Road min = primaryQueue.Aggregate((r1, r2) => r1.Number < r2.Number ? r1 : r2);
            primaryQueue.Remove(min);

            Road modified = CheckLocalConstraints(min);
            if (modified == null)
            {
                continue;
            }

            AddSegment(modified, finalSegments, qTree);

            foreach (Road road in GlobalGoals(modified))
            {
                primaryQueue.Add(new Road() { Number = min.Number + road.Number + 1, Start = road.Start, End = road.End, DirectionAngle = road.DirectionAngle, IsHighway = road.IsHighway });
            }
        }

        StartCoroutine(DrawSegments());
        //DrawSegments();
        Debug.Log("start end");
    }

    /*void OnDrawGizmos()
    {
        Gizmos.color = UnityEngine.Color.green;
        Gizmos.DrawWireCube(new Vector3(rect.center.x, 1, rect.center.y), new Vector3(rect.size.x, 1, rect.size.y));
    }*/

    private System.Collections.IEnumerator DrawSegments()
    {
        //var intersects = qTree.GetObjects(new RectangleF(rect.min.x, rect.min.y, rect.width, rect.height));

        /*foreach (var item in intersects)
        {
            item.color = UnityEngine.Color.white;
        }*/
        foreach (var road in finalSegments)
        {
            var roadView = Instantiate(roadViewPrefab);
            roadView.Road = road;

            road.color = SecondaryRoadColor;

            if (road.IsHighway)
            {
                roadView.GetComponent<LineRenderer>().endColor = HighwayColor;
                roadView.GetComponent<LineRenderer>().startColor = HighwayColor;
            }
            else
            {
                roadView.GetComponent<LineRenderer>().endColor = roadView.road.color;
                roadView.GetComponent<LineRenderer>().startColor = roadView.road.color;
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
        foreach (var otherSegment in qTree.GetObjects(r.Rectangle))
        {
            //snap to crossing
            if (Vector3.Distance(r.End, otherSegment.End) < RoadSnapDistance)
            {
                r.End = otherSegment.End;

                //check if it's still fits after the end was updated
                foreach (var item in qTree.GetObjects(r.Rectangle))
                {
                    //2. condition is in order to not filter \/ <-- this kind of roads, just these: ||
                    if (MinDegreeDifference(r.DirectionAngle, item.DirectionAngle) <= 10 && CheckSegmentsEndPointsDistance(r.Start, item.Start, r.End, item.End))
                    {
                        return null;
                    }
                }
            }


            //snap to crossing if the roads are like |--
            /*if (FindDistanceToSegment(r.End, otherSegment.Start, otherSegment.End, out _, out _) < RoadSnapDistance || FindDistanceToSegment(r.Start, otherSegment.Start, otherSegment.End, out _, out _) < RoadSnapDistance)
            {
                //r.End = Vector3.Distance(r.End, otherSegment.Start) < Vector3.Distance(r.End, otherSegment.End) ? otherSegment.Start : otherSegment.End;
                //return r;
                if (!r.End.Equals(otherSegment.End) && !r.End.Equals(otherSegment.Start) && !r.Start.Equals(otherSegment.Start) && !r.Start.Equals(otherSegment.End))
                {
                    //r.color = UnityEngine.Color.magenta;
                    //otherSegment.color = UnityEngine.Color.white;
                }
            }*/

        }

        //check if more than 4 roads are in the crossing
        if(qTree.GetObjects(r.StartRectangle).Count >= 4)
        {
            //roadInCrossing.color = UnityEngine.Color.cyan;
            //r.color = UnityEngine.Color.white;
            return null;
        }
        
        //check intersections
        foreach (Road segment in finalSegments)
        {
            if (LineSegmentsIntersect(r.Start, r.End, segment.Start, segment.End)
                || CheckSegmentsEndPointsDistance(r.Start, segment.Start, r.End, segment.End))
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
            var angle = previousRoad.DirectionAngle + UnityEngine.Random.Range(-HighwayRandomAngle, HighwayRandomAngle);
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
            if (roadPopluation > HighwayBranchPopulationThreshold)
            {
                if (UnityEngine.Random.value < HighwayBranchProbability)
                {
                    float leftAngle = previousRoad.DirectionAngle - 90 + UnityEngine.Random.Range(-DefaultRoadRandomAngle, DefaultRoadRandomAngle);
                    var leftHighwayBranch = Road.RoadWithDirection(previousRoad.End, leftAngle, HighwaySegmentLength, 0, previousRoad.IsHighway);
                    newRoads.Add(leftHighwayBranch);
                }
                else if (UnityEngine.Random.value < HighwayBranchProbability)
                {
                    float rightAngle = previousRoad.DirectionAngle + 90 + UnityEngine.Random.Range(-DefaultRoadRandomAngle, DefaultRoadRandomAngle);
                    var rightHighwayBranch = Road.RoadWithDirection(previousRoad.End, rightAngle, HighwaySegmentLength, 0, previousRoad.IsHighway);
                    newRoads.Add(rightHighwayBranch);
                }
            } 
        }
        else if (straightPopulation > NormalBranchPopulationThreshold)
        {
            //Debug.Log("straightpop: " + straightPopulation);
            newRoads.Add(straightRoad);
        }

        //secondary road branching 
        if (straightPopulation > NormalBranchPopulationThreshold)
        {
            int timeDelay = 0;
            if (previousRoad.IsHighway)
            {
                timeDelay = NormalBranchTimeDelayFromHighway;
            }

            if (UnityEngine.Random.value < DefaultBranchProbability)
            {
                float leftAngle = previousRoad.DirectionAngle - 90 + UnityEngine.Random.Range(-DefaultRoadRandomAngle, DefaultRoadRandomAngle);
                var leftBranch = Road.RoadWithDirection(previousRoad.End, leftAngle, BranchSegmentLength, timeDelay, false);
                //Debug.Log("pop when branching 1: " + straightPopulation);
                newRoads.Add(leftBranch);
            }
            else if (UnityEngine.Random.value < DefaultBranchProbability)
            {
                float rightAngle = previousRoad.DirectionAngle + 90 + UnityEngine.Random.Range(-DefaultRoadRandomAngle, DefaultRoadRandomAngle);
                var rightBranch = Road.RoadWithDirection(previousRoad.End, rightAngle, BranchSegmentLength, timeDelay, false);
                //Debug.Log("pop when branching 2: " + straightPopulation);
                newRoads.Add(rightBranch);
            }  
        }

        return newRoads;
    }


    private bool CheckSegmentsEndPointsDistance(Vector3 start1, Vector3 start2, Vector3 end1, Vector3 end2)
    {
        return (Vector3.Distance(start1, start2) <= RoadSnapDistance && Vector3.Distance(end1, end2) <= RoadSnapDistance || Vector3.Distance(start1, end2) <= RoadSnapDistance && Vector3.Distance(start2, end1) <= RoadSnapDistance);
    }
    private void AddSegment(Road segment, List<Road> segments, QuadTreeRect<Road> quadTree)
    {
        segments.Add(segment);
        quadTree.Add(segment);
        lastAddedTime++;
        segment.AddedToQtreeTime = lastAddedTime;
    }

    private float MinDegreeDifference(float firstDeg, float secDeg)
    {
        //-92,-271
        var diff = Math.Abs(firstDeg - secDeg) % 180.0f;
        return Math.Min(diff, Math.Abs(diff - 180.0f));
    }

    private double GetHeatMapAt(float i, float j)
    {
        return (Mathf.PerlinNoise(i / 30f, j / 30f) * 8);
    }

    private void DrawHeatMap(int width, int height)
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                double result = GetHeatMapAt(i,j);

                Vector3 pos = new Vector3(i, -2, j);

                var cube = Instantiate(heatMapField);
                cube.transform.position = pos * heatMapScale;
                cube.transform.localScale = new Vector3(heatMapScale, 0.1f, heatMapScale);
                cube.GetComponent<Renderer>().material.color = new UnityEngine.Color(5 * (float)result/ 255.0f, 25.5f * (float)result/ 255.0f, 40 / 255.0f);
            }
        }

    }

    private Vector3 GetRotatedVectorByAngle(int angle, Vector3 inputVector)
    {
        return Quaternion.Euler(0, angle, 0) * inputVector;
    }

    private bool LineSegmentsIntersect(Vector3 lineOneA, Vector3 lineOneB, Vector3 lineTwoA, Vector3 lineTwoB)
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

    private Vector3? LineIntersect(Vector3 lineOneA, Vector3 lineOneB, Vector3 lineTwoA, Vector3 lineTwoB)
    {
        //Line1: A1x + B1y = C1
        float A1 = lineOneB.z - lineOneA.z;
        float B1 = lineOneB.x - lineOneA.x;
        float C1 = A1 * lineOneA.x + B1 * lineOneA.z;

        //Line2: A2x + B2y = C2
        float A2 = lineTwoB.z - lineTwoA.z;
        float B2 = lineTwoB.x - lineTwoA.x;
        float C2 = A2 * lineTwoA.x + B2 * lineTwoA.z;

        float delta = A1 * B2 - A2 * B1;

        if (delta == 0)
        {
            //throw new ArgumentException("Lines are parallel");
            return null;
        }

        float x = (B2 * C1 - B1 * C2) / delta;
        float z = (A1 * C2 - A2 * C1) / delta;

        return new Vector3(x, 0, z);

    }

    // Calculate the distance between
    // point pt and the segment p1 --> p2.
    private double FindDistanceToSegment(Vector3 point, Vector3 segmentStart, Vector3 segmentEnd, out float t, out Vector3 closest)
    {
        float dx = segmentEnd.x - segmentStart.x;
        float dz = segmentEnd.z - segmentStart.z;

        // Calculate the t that minimizes the distance.
        t = ((point.x - segmentStart.x) * dx + (point.z - segmentStart.z) * dz) /
            (dx * dx + dz * dz);

        closest = new Vector3(segmentStart.x + t * dx, 0, segmentStart.z + t * dz);
        dx = point.x - closest.x;
        dz = point.z - closest.z;
        
        return Math.Sqrt(dx * dx + dz * dz);
    }
}
