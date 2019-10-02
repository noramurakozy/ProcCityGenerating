
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
    private GameObject populationMapBase;

    //public int numOfSteps = 3000;

    //public double defaultBranchProbability = 0.3;

    //public double highwayBranchProbability = 0.05;

    //public double highwayBranchPopulationThreshold = 6;

    //public double normalBranchPopulationThreshold = 4;

    //public int normalBranchTimeDelayFromHighway = 5;

    //public int highwaySegmentLength = 3;

    //public int branchSegmentLength = 2;

    //public int highwayRandomAngle = 15;

    //public int defaultRoadRandomAngle = 3;

    //public int minimumIntersectionDeviation = 30;

    //public float roadSnapDistance = 1;

    //public float mapHeight = 200;

    //public float mapWidth = 200;

    //public UnityEngine.Color highwayColor = UnityEngine.Color.black;

    //public UnityEngine.Color secondaryRoadColor = UnityEngine.Color.red;

    public int numOfSteps = 3000;

    public double defaultBranchProbability = 0.3;

    public double highwayBranchProbability = 0.05;

    public double highwayBranchPopulationThreshold = 6;

    public double normalBranchPopulationThreshold = 4;

    public int normalBranchTimeDelayFromHighway = 5;

    public int highwaySegmentLength = 3;

    public int branchSegmentLength = 2;

    public int highwayRandomAngle = 15;

    public int defaultRoadRandomAngle = 3;

    public int minimumIntersectionDeviation = 30;

    public float roadSnapDistance = 1;

    public float mapHeight = 200;

    public float mapWidth = 200;

    public UnityEngine.Color highwayColor = UnityEngine.Color.black;

    public UnityEngine.Color secondaryRoadColor = UnityEngine.Color.red;


    private Rect bounds;
    private static int lastAddedTime = 0;
    private const int HeatMapScale = 1;

    //public int NumOfSteps { get => numOfSteps; set => numOfSteps = value; }
    //public double DefaultBranchProbability { get => defaultBranchProbability; set => defaultBranchProbability = value; }
    //public double HighwayBranchProbability { get => highwayBranchProbability; set => highwayBranchProbability = value; }
    //public double HighwayBranchPopulationThreshold { get => highwayBranchPopulationThreshold; set => highwayBranchPopulationThreshold = value; }
    //public double NormalBranchPopulationThreshold { get => normalBranchPopulationThreshold; set => normalBranchPopulationThreshold = value; }
    //public int NormalBranchTimeDelayFromHighway { get => normalBranchTimeDelayFromHighway; set => normalBranchTimeDelayFromHighway = value; }
    //public int HighwaySegmentLength { get => highwaySegmentLength; set => highwaySegmentLength = value; }
    //public int BranchSegmentLength { get => branchSegmentLength; set => branchSegmentLength = value; }
    //public int HighwayRandomAngle { get => highwayRandomAngle; set => highwayRandomAngle = value; }
    //public int DefaultRoadRandomAngle { get => defaultRoadRandomAngle; set => defaultRoadRandomAngle = value; }
    //public int MinimumIntersectionDeviation { get => minimumIntersectionDeviation; set => minimumIntersectionDeviation = value; }
    //public float RoadSnapDistance { get => roadSnapDistance; set => roadSnapDistance = value; }
    //public float MapHeight { get => mapHeight; set => mapHeight = value; }
    //public float MapWidth { get => mapWidth; set => mapWidth = value; }
    //public UnityEngine.Color HighwayColor { get => highwayColor; set => highwayColor = value; }
    //public UnityEngine.Color SecondaryRoadColor { get => secondaryRoadColor; set => secondaryRoadColor = value; }

    //Rect rect = new Rect(40, 40, 10, 10);

    
    // Start is called before the first frame update
    private void Start()
    {
        SetValuesToDefault();
        //UnityEngine.Random.InitState(12345678);
        qTree = new QuadTreeRect<Road>(new RectangleF(-5000, -5000, 10000, 10000));
        //bounds = new Rect(0, 0, MapWidth, MapHeight);
        DrawHeatMap((int)mapWidth / HeatMapScale, (int)mapHeight / HeatMapScale);
        primaryQueue = new List<Road>() {
            new Road() { Start = new Vector3(mapHeight/2, 0, mapWidth/2), End = new Vector3(mapHeight/2+highwaySegmentLength, 0, mapWidth/2), Number = 0, IsHighway = true },
            new Road() { Start = new Vector3(mapHeight/2, 0, mapWidth/2), End = new Vector3(mapHeight/2-highwaySegmentLength, 0, mapWidth/2), Number = 0, IsHighway = true, DirectionAngle = 180 }
        };
        finalSegments = new List<Road>();

        while (primaryQueue.Count != 0 && numOfSteps != 0)
        {
            numOfSteps--;
            var min = primaryQueue.Aggregate((r1, r2) => r1.Number < r2.Number ? r1 : r2);
            primaryQueue.Remove(min);

            var modified = CheckLocalConstraints(min);
            if (modified == null)
            {
                continue;
            }

            AddSegment(modified, finalSegments, qTree);

            foreach (var road in GlobalGoals(modified))
            {
                primaryQueue.Add(new Road() { Number = min.Number + road.Number + 1, Start = road.Start, End = road.End, DirectionAngle = road.DirectionAngle, IsHighway = road.IsHighway });
            }
        }

        //StartCoroutine(DrawSegments());
        DrawSegments();
        Debug.Log("start end");
    }

    public void UpdateCity()
    {
        var numOfStepsToDecrease = numOfSteps;
        DestroyAllObjects();
        qTree = new QuadTreeRect<Road>(new RectangleF(-5000, -5000, 10000, 10000));
        DrawHeatMap((int)mapWidth / HeatMapScale, (int)mapHeight / HeatMapScale);
        primaryQueue = new List<Road>() {
            new Road() { Start = new Vector3(mapHeight/2, 0, mapWidth/2), End = new Vector3(mapHeight/2+highwaySegmentLength, 0, mapWidth/2), Number = 0, IsHighway = true },
            new Road() { Start = new Vector3(mapHeight/2, 0, mapWidth/2), End = new Vector3(mapHeight/2-highwaySegmentLength, 0, mapWidth/2), Number = 0, IsHighway = true, DirectionAngle = 180 }
        };
        finalSegments = new List<Road>();

        while (primaryQueue.Count != 0 && numOfStepsToDecrease != 0)
        {
            numOfStepsToDecrease--;
            var min = primaryQueue.Aggregate((r1, r2) => r1.Number < r2.Number ? r1 : r2);
            primaryQueue.Remove(min);

            var modified = CheckLocalConstraints(min);
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

        DrawSegments();
    }

    public void SetValuesToDefault()
    {
        numOfSteps = 3000;
        defaultBranchProbability = 0.3;
        highwayBranchProbability = 0.05;
        highwayBranchPopulationThreshold = 6;
        normalBranchPopulationThreshold = 4;
        normalBranchTimeDelayFromHighway = 5;
        highwaySegmentLength = 3;
        branchSegmentLength = 2;
        highwayRandomAngle = 15;
        defaultRoadRandomAngle = 3;
        minimumIntersectionDeviation = 30;
        roadSnapDistance = 1;
        mapHeight = 200;
        mapWidth = 200;
        highwayColor = UnityEngine.Color.black;
        secondaryRoadColor = UnityEngine.Color.red;
    }

    private void DestroyAllObjects()
    {
        //foreach (RoadView o in FindObjectsOfType<RoadView>())
        //{
        //    o = SafeDestroyGameObject(o);
        //}

        var roadsInHierarchy = FindObjectsOfType<RoadView>();
        var planesInHierarchy = FindObjectsOfType<PlaneGenerator>();

        for (var i = 0; i < roadsInHierarchy.Length; i++)
        {
            roadsInHierarchy[i] = SafeDestroyGameObject(roadsInHierarchy[i]);
        }

        for (var i = 0; i < planesInHierarchy.Length; i++)
        {
            planesInHierarchy[i] = SafeDestroyGameObject(planesInHierarchy[i]);
        }
    }

    /*void OnDrawGizmos()
    {
        Gizmos.color = UnityEngine.Color.green;
        Gizmos.DrawWireCube(new Vector3(rect.center.x, 1, rect.center.y), new Vector3(rect.size.x, 1, rect.size.y));
    }*/

    private void DrawSegments()
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

            road.Color = secondaryRoadColor;

            if (road.IsHighway)
            {
                roadView.GetComponent<LineRenderer>().endColor = highwayColor;
                roadView.GetComponent<LineRenderer>().startColor = highwayColor;
            }
            else
            {
                roadView.GetComponent<LineRenderer>().endColor = roadView.road.Color;
                roadView.GetComponent<LineRenderer>().startColor = roadView.road.Color;
            }
            roadView.Draw();
            //yield return new WaitForSeconds(0.2f);
            //yield return null;
        }
        //yield return null;

        Debug.Log("Done");
    }

    private Road CheckLocalConstraints(Road r)
    {
        foreach (var otherSegment in qTree.GetObjects(r.Rectangle))
        {
            //snap to crossing
            if (Vector3.Distance(r.End, otherSegment.End) < roadSnapDistance)
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
        foreach (var segment in finalSegments)
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
        var newRoads = new List<Road>();

        var straightRoad = Road.RoadWithDirection(previousRoad.End, previousRoad.DirectionAngle, previousRoad.Length, 0, previousRoad.IsHighway);
        var straightPopulation = GetHeatMapAt(straightRoad.End.x, straightRoad.End.z);

        if (previousRoad.IsHighway)
        {
            var angle = previousRoad.DirectionAngle + UnityEngine.Random.Range(-highwayRandomAngle, highwayRandomAngle);
            var randomAngleRoad = Road.RoadWithDirection(previousRoad.End, angle, previousRoad.Length, 0, previousRoad.IsHighway);

            var randomRoadPopulation = GetHeatMapAt(randomAngleRoad.End.x, randomAngleRoad.End.z);

            double roadPopulation;

            //highway continues
            if (randomRoadPopulation > straightPopulation)
            {
                newRoads.Add(randomAngleRoad);
                roadPopulation = randomRoadPopulation;
                randomAngleRoad.Population = (float)roadPopulation;
            }
            else
            {
                newRoads.Add(straightRoad);
                roadPopulation = straightPopulation;
                straightRoad.Population = (float)roadPopulation;
            }

            //highway from highway
            if (roadPopulation > highwayBranchPopulationThreshold)
            {
                if (UnityEngine.Random.value < highwayBranchProbability)
                {
                    var leftAngle = previousRoad.DirectionAngle - 90 + UnityEngine.Random.Range(-defaultRoadRandomAngle, defaultRoadRandomAngle);
                    var leftHighwayBranch = Road.RoadWithDirection(previousRoad.End, leftAngle, highwaySegmentLength, 0, previousRoad.IsHighway);
                    newRoads.Add(leftHighwayBranch);
                }
                else if (UnityEngine.Random.value < highwayBranchProbability)
                {
                    var rightAngle = previousRoad.DirectionAngle + 90 + UnityEngine.Random.Range(-defaultRoadRandomAngle, defaultRoadRandomAngle);
                    var rightHighwayBranch = Road.RoadWithDirection(previousRoad.End, rightAngle, highwaySegmentLength, 0, previousRoad.IsHighway);
                    newRoads.Add(rightHighwayBranch);
                }
            } 
        }
        else if (straightPopulation > normalBranchPopulationThreshold)
        {
            //Debug.Log("straightpop: " + straightPopulation);
            newRoads.Add(straightRoad);
        }

        //secondary road branching 
        if (straightPopulation > normalBranchPopulationThreshold)
        {
            var timeDelay = 0;
            if (previousRoad.IsHighway)
            {
                timeDelay = normalBranchTimeDelayFromHighway;
            }

            if (UnityEngine.Random.value < defaultBranchProbability)
            {
                var leftAngle = previousRoad.DirectionAngle - 90 + UnityEngine.Random.Range(-defaultRoadRandomAngle, defaultRoadRandomAngle);
                var leftBranch = Road.RoadWithDirection(previousRoad.End, leftAngle, branchSegmentLength, timeDelay, false);
                //Debug.Log("pop when branching 1: " + straightPopulation);
                newRoads.Add(leftBranch);
            }
            else if (UnityEngine.Random.value < defaultBranchProbability)
            {
                var rightAngle = previousRoad.DirectionAngle + 90 + UnityEngine.Random.Range(-defaultRoadRandomAngle, defaultRoadRandomAngle);
                var rightBranch = Road.RoadWithDirection(previousRoad.End, rightAngle, branchSegmentLength, timeDelay, false);
                //Debug.Log("pop when branching 2: " + straightPopulation);
                newRoads.Add(rightBranch);
            }  
        }

        return newRoads;
    }


    private bool CheckSegmentsEndPointsDistance(Vector3 start1, Vector3 start2, Vector3 end1, Vector3 end2)
    {
        return (Vector3.Distance(start1, start2) <= roadSnapDistance && Vector3.Distance(end1, end2) <= roadSnapDistance || Vector3.Distance(start1, end2) <= roadSnapDistance && Vector3.Distance(start2, end1) <= roadSnapDistance);
    }
    private static void AddSegment(Road segment, List<Road> segments, QuadTreeRect<Road> quadTree)
    {
        segments.Add(segment);
        quadTree.Add(segment);
        lastAddedTime++;
        segment.addedToQtreeTime = lastAddedTime;
    }

    private static float MinDegreeDifference(float firstDeg, float secDeg)
    {
        //-92,-271
        var diff = Math.Abs(firstDeg - secDeg) % 180.0f;
        return Math.Min(diff, Math.Abs(diff - 180.0f));
    }

    private static double GetHeatMapAt(float i, float j)
    {
        return (Mathf.PerlinNoise(i / 30f, j / 30f) * 8);
    }

    private void DrawHeatMap(int width, int height)
    {
        var plane = Instantiate(populationMapBase);
        var planeGenerator = plane.GetComponent<PlaneGenerator>();
        planeGenerator.Generate(width, height);
    }

    private Vector3 GetRotatedVectorByAngle(int angle, Vector3 inputVector)
    {
        return Quaternion.Euler(0, angle, 0) * inputVector;
    }

    private static bool LineSegmentsIntersect(Vector3 lineOneA, Vector3 lineOneB, Vector3 lineTwoA, Vector3 lineTwoB)
    {
        var ret = false;
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
        var a1 = lineOneB.z - lineOneA.z;
        var b1 = lineOneB.x - lineOneA.x;
        var c1 = a1 * lineOneA.x + b1 * lineOneA.z;

        //Line2: A2x + B2y = C2
        var a2 = lineTwoB.z - lineTwoA.z;
        var b2 = lineTwoB.x - lineTwoA.x;
        var c2 = a2 * lineTwoA.x + b2 * lineTwoA.z;

        var delta = a1 * b2 - a2 * b1;

        if (Math.Abs(delta) < 0.000001)
        {
            //throw new ArgumentException("Lines are parallel");
            return null;
        }

        var x = (b2 * c1 - b1 * c2) / delta;
        var z = (a1 * c2 - a2 * c1) / delta;

        return new Vector3(x, 0, z);

    }

    // Calculate the distance between
    // point pt and the segment p1 --> p2.
    private double FindDistanceToSegment(Vector3 point, Vector3 segmentStart, Vector3 segmentEnd, out float t, out Vector3 closest)
    {
        var dx = segmentEnd.x - segmentStart.x;
        var dz = segmentEnd.z - segmentStart.z;

        // Calculate the t that minimizes the distance.
        t = ((point.x - segmentStart.x) * dx + (point.z - segmentStart.z) * dz) /
            (dx * dx + dz * dz);

        closest = new Vector3(segmentStart.x + t * dx, 0, segmentStart.z + t * dz);
        dx = point.x - closest.x;
        dz = point.z - closest.z;
        
        return Math.Sqrt(dx * dx + dz * dz);
    }

    private static T SafeDestroy<T>(T obj) where T : UnityEngine.Object
    {
        if (Application.isEditor)
            DestroyImmediate(obj);
        else
            Destroy(obj);

        return null;
    }
    private static T SafeDestroyGameObject<T>(T component) where T : Component
    {
        if (component != null)
            SafeDestroy(component.gameObject);
        return null;
    }
}
