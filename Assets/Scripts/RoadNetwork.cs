﻿
using QuadTrees;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using UnityEngine;
using Color = UnityEngine.Color;
using Debug = UnityEngine.Debug;

public class RoadNetwork : MonoBehaviour
{
    private QuadTreeRect<Road> qTree;
    private List<Road> primaryQueue;
    private List<Road> finalSegments;
    private double[,] heatMap;
    private int[] roadCounters = {0, 0};

    [SerializeField]
    private RoadView roadViewPrefab;

    [SerializeField]
    private GameObject populationMapBase;
    
    public int numOfSteps = 3000;

    public double defaultBranchProbability = 0.3;

    public double highwayBranchProbability = 0.05;

    public double highwayBranchPopulationThreshold = 6;

    public double normalBranchPopulationThreshold = 4;
    
    public double switchToOldTownThreshold = 4;

    public int normalBranchTimeDelayFromHighway = 5;

    public int highwaySegmentLength = 3;

    public int branchSegmentLength = 2;

    public int highwayRandomAngle = 15;

    public int defaultRoadRandomAngle = 3;
    
    public float normalBranchBaseAngle = 90;
    
    public static float RoadSnapDistance = 1;

    public float mapHeight = 200;

    public float mapWidth = 200;
    
    public int maxCrossingNumber = 4;

    public Color highwayColor = Color.black;

    public Color secondaryRoadColor = Color.red;
    
    private static int lastAddedTime = 0;
    public static float MinBaseAngle = 80;
    public static float MaxBaseAngle = 90;
    public int modernCityStructureExtent = 0;
    private const int HeatMapScale = 1;
    private float modernRoadSteps;
    private float oldTownRoadSteps;

    public void UpdateCity()
    {
        oldTownRoadSteps = numOfSteps - (numOfSteps * (modernCityStructureExtent / 100f));
        modernRoadSteps = numOfSteps - oldTownRoadSteps;
        //UnityEngine.Random.InitState(12345678);
        switchToOldTownThreshold = oldTownRoadSteps.Equals(numOfSteps) ? 8 : 4;
        var numOfStepsToDecrease = numOfSteps;
        roadCounters = new[] {0, 0};
        DestroyAllObjects();
        qTree = new QuadTreeRect<Road>(new RectangleF(-5000, -5000, 10000, 10000));
        DrawMap((int) mapWidth / HeatMapScale, (int) mapHeight / HeatMapScale, PlaneType.Population);
        //DrawMap((int) mapWidth / HeatMapScale, (int) mapHeight / HeatMapScale, PlaneType.District);
        var startRoadType = oldTownRoadSteps > numOfSteps / 2f ? RoadType.OldTown : RoadType.Modern;
        primaryQueue = GetInitialRoads(startRoadType);
        finalSegments = new List<Road>();

        while (primaryQueue.Count != 0 && numOfStepsToDecrease != 0)
        {
            var min = primaryQueue.Aggregate((r1, r2) => r1.Number < r2.Number ? r1 : r2);
            primaryQueue.Remove(min);

            var modified = CheckLocalConstraints(min);
            if (modified == null)
            {
                continue;
            }
            AddSegment(modified, finalSegments, qTree);
            numOfStepsToDecrease--;
            
            foreach (Road road in GlobalGoals(modified))
            {
                primaryQueue.Add(new Road()
                {
                    Number = min.Number + road.Number + 1, 
                    Start = road.Start, 
                    End = road.End, 
                    DirectionAngle = road.DirectionAngle, 
                    IsHighway = road.IsHighway, 
                    Color = road.Color, 
                    Type = road.Type
                });
            }
        }
        DrawSegments();
    }

    private List<Road> GetInitialRoads(RoadType startRoadType)
    {
        return new List<Road>()
        {
            new Road()
            {
                Start = new Vector3(mapHeight / 2, 0, mapWidth / 2),
                End = new Vector3(mapHeight / 2 + highwaySegmentLength, 0, mapWidth / 2), 
                Number = 0, 
                IsHighway = true,
                Type = startRoadType
            },
            new Road()
            {
                Start = new Vector3(mapHeight / 2, 0, mapWidth / 2),
                End = new Vector3(mapHeight / 2 - highwaySegmentLength, 0, mapWidth / 2), 
                Number = 0, 
                IsHighway = true,
                DirectionAngle = 180, 
                Type = startRoadType
            }
        };
    }

    private void SetValuesToOldTown()
    {
        highwayRandomAngle = 15;
        defaultRoadRandomAngle = 3;
        RoadSnapDistance = 1;
        MinBaseAngle = 80;
        normalBranchPopulationThreshold = 5;
    }

    private void SetValuesToModern()
    {
        highwayRandomAngle = 2;
        defaultRoadRandomAngle = 0;
        RoadSnapDistance = 0.3f;
        MinBaseAngle = 90;
        normalBranchPopulationThreshold = 4;
    }

    public void ResetValuesToDefault()
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
        RoadSnapDistance = 1;
        mapHeight = 200;
        mapWidth = 200;
        highwayColor = UnityEngine.Color.black;
        secondaryRoadColor = UnityEngine.Color.red;
        MinBaseAngle = 80;
        MaxBaseAngle = 90;
        normalBranchBaseAngle = 90;
        maxCrossingNumber = 4;
        modernCityStructureExtent = 0;
        switchToOldTownThreshold = 4;
    }

    private void DestroyAllObjects()
    {
        //plane.gameobjectre kell
        var planesInHierarchy = FindObjectsOfType<PlaneGenerator>();
        var roads = GameObject.Find("Roads");
        
        //futtatas kozben nem lesz jo
        SafeDestroy(roads);

        for (var i = 0; i < planesInHierarchy.Length; i++)
        {
            planesInHierarchy[i] = SafeDestroyGameObject(planesInHierarchy[i]);
        }
    }

    private void DrawSegments()
    {
        var roadsParent = new GameObject("Roads");
        foreach (var road in finalSegments)
        {
            var roadView = Instantiate(roadViewPrefab, roadsParent.transform);
            roadView.Road = road;

            //road.Color = secondaryRoadColor;
            road.Color = road.Type.Equals(RoadType.Modern) ? Color.yellow : Color.cyan;

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
        }
        Debug.Log("Old: " + roadCounters[(int)RoadType.OldTown]);
        Debug.Log("Modern: " + roadCounters[(int)RoadType.Modern]);
        Debug.Log("Done");
    }

    private Road CheckLocalConstraints(Road r)
    {
        if (CheckConstraintsOnNeighbours(ref r, r.Rectangle))
        {
            roadCounters[(int) r.Type]--;
            return null;
        }

        //check if more than x roads are in the crossing
        if (ReachMaxCrossingNumber(r))
        {
            roadCounters[(int) r.Type]--;
            return null;
        }
        
        //check intersections
        foreach (var segment in finalSegments)
        {
            if (LineSegmentsIntersect(r.Start, r.End, segment.Start, segment.End)
                || SegmentsAreAlmostParallel(r.Start, segment.Start, r.End, segment.End))
            {
                roadCounters[(int) r.Type]--;
                return null;
            }
        }

        return r;
    }

    private bool CheckConstraintsOnNeighbours(ref Road r, RectangleF area)
    {
        foreach (var otherSegment in qTree.GetObjects(area))
        {
            //snap to crossing
            SnapToCrossing(ref r, otherSegment);

            //check if it still fits after the end was updated
            foreach (var item in qTree.GetObjects(area))
            {
                //2. condition is in order to not filter \/ <-- this kind of roads, just these: ||
                if (MinDegreeDifference(r.DirectionAngle, item.DirectionAngle) <= 10 &&
                    SegmentsAreAlmostParallel(r.Start, item.Start, r.End, item.End))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void SnapToCrossing(ref Road road1, Road road2)
    {
        if (StartsAreClose(road1.Start, road2.Start))
        {
            road1.Start = road2.Start;
        }

        if (EndsAreClose(road1.End, road2.End))
        {
            road1.End = road2.End;
        }

        if (Start1End2AreClose(road1.Start, road2.End))
        {
            road1.Start = road2.End;
        }

        if (Start2End1AreClose(road2.Start, road1.End))
        {
            road1.End = road2.Start;
        }
        
        //snap to crossing if the roads are like |--
        if (PointsAreDifferent(road1.Start, road1.End, road2.Start, road2.End))
        {
            if (FindDistanceToSegment(road1.End, road2.Start, road2.End, out _, out _) < RoadSnapDistance)
            {
                road1.End = Vector3.Distance(road1.End, road2.Start) < Vector3.Distance(road1.End, road2.End) ? road2.Start : road2.End;
            }
            
            if (FindDistanceToSegment(road1.Start, road2.Start, road2.End, out _, out _) < RoadSnapDistance)
            {
                road1.Start = Vector3.Distance(road1.Start, road2.Start) < Vector3.Distance(road1.Start, road2.End) ? road2.Start : road2.End;
            }
        }
    }

    private bool PointsAreDifferent(Vector3 road1Start, Vector3 road1End, Vector3 road2Start, Vector3 road2End)
    {
        return !road1Start.Equals(road2Start)
               && !road1Start.Equals(road2End)
               && !road1End.Equals(road2Start)
               && !road1End.Equals(road2End);
    }

    private bool ReachMaxCrossingNumber(Road r)
    {
        var pointsInCrossing = qTree.GetObjects(r.Rectangle).Count;
        return pointsInCrossing >= maxCrossingNumber;
    }

    private List<Road> GlobalGoals(Road previousRoad)
    {
        var roadType = RoadType.OldTown;
        var newRoads = new List<Road>();

        var straightRoad = Road.RoadWithDirection(previousRoad.End, previousRoad.DirectionAngle, previousRoad.Length, 0, previousRoad.IsHighway);
        var straightPopulation = PlaneGenerator.GetHeatMapAt(straightRoad.End.x, straightRoad.End.z, PlaneType.Population);
        var districtValue = PlaneGenerator.GetHeatMapAt(straightRoad.End.x, straightRoad.End.z, PlaneType.District);
        if (districtValue < switchToOldTownThreshold
            && !Mathf.Approximately(oldTownRoadSteps, 0f)
            && roadCounters[(int)RoadType.OldTown] < oldTownRoadSteps)
        {
            SetValuesToOldTown();
            roadType = RoadType.OldTown;
            
        }
        else if (!Mathf.Approximately(modernRoadSteps, 0f)
                && roadCounters[(int) RoadType.Modern] < modernRoadSteps)
        {
            SetValuesToModern();
            roadType = RoadType.Modern;
        }
        else return new List<Road>();

        normalBranchBaseAngle = UnityEngine.Random.Range(MinBaseAngle, MaxBaseAngle);
        if (previousRoad.IsHighway)
        {
            var angle = previousRoad.DirectionAngle + UnityEngine.Random.Range(-highwayRandomAngle, highwayRandomAngle);
            var randomAngleRoad = Road.RoadWithDirection(previousRoad.End, angle, previousRoad.Length, 0, previousRoad.IsHighway);

            var randomRoadPopulation = PlaneGenerator.GetHeatMapAt(randomAngleRoad.End.x, randomAngleRoad.End.z, PlaneType.Population);

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
                    var leftAngle = previousRoad.DirectionAngle - normalBranchBaseAngle + UnityEngine.Random.Range(-defaultRoadRandomAngle, defaultRoadRandomAngle);
                    var leftHighwayBranch = Road.RoadWithDirection(previousRoad.End, leftAngle, highwaySegmentLength, 0, previousRoad.IsHighway);
                    newRoads.Add(leftHighwayBranch);
                }
                else if (UnityEngine.Random.value < highwayBranchProbability)
                {
                    var rightAngle = previousRoad.DirectionAngle + normalBranchBaseAngle + UnityEngine.Random.Range(-defaultRoadRandomAngle, defaultRoadRandomAngle);
                    var rightHighwayBranch = Road.RoadWithDirection(previousRoad.End, rightAngle, highwaySegmentLength, 0, previousRoad.IsHighway);
                    newRoads.Add(rightHighwayBranch);
                }
            } 
        }
        else if (straightPopulation > normalBranchPopulationThreshold)
        {
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
                var leftAngle = previousRoad.DirectionAngle - normalBranchBaseAngle +
                                UnityEngine.Random.Range(-defaultRoadRandomAngle, defaultRoadRandomAngle);
                var leftBranch =
                    Road.RoadWithDirection(previousRoad.End, leftAngle, branchSegmentLength, timeDelay, false);
                newRoads.Add(leftBranch);
            }
            else if (UnityEngine.Random.value < defaultBranchProbability)
            {
                var rightAngle = previousRoad.DirectionAngle + normalBranchBaseAngle +
                                 UnityEngine.Random.Range(-defaultRoadRandomAngle, defaultRoadRandomAngle);
                var rightBranch =
                    Road.RoadWithDirection(previousRoad.End, rightAngle, branchSegmentLength, timeDelay, false);
                newRoads.Add(rightBranch);
            }
        }

        newRoads.ForEach(road => road.Type = roadType);
        roadCounters[(int)roadType]+= newRoads.Count;
        return newRoads;
    }

    //Filter those rads that are (almost) parallel: ||
    private bool SegmentsAreAlmostParallel(Vector3 start1, Vector3 start2, Vector3 end1, Vector3 end2)
    {
        return (StartsAreClose(start1, start2) 
                && EndsAreClose(end1, end2) 
                || Start1End2AreClose(start1, end2) 
                && Start2End1AreClose(start2, end1));
    }

    private bool Start2End1AreClose(Vector3 start2, Vector3 end1)
    {
        return Vector3.Distance(start2, end1) <= RoadSnapDistance;
    }

    private bool Start1End2AreClose(Vector3 start1, Vector3 end2)
    {
        return Vector3.Distance(start1, end2) <= RoadSnapDistance;
    }

    private bool EndsAreClose(Vector3 end1, Vector3 end2)
    {
        return Vector3.Distance(end1, end2) <= RoadSnapDistance;
    }

    private bool StartsAreClose(Vector3 start1, Vector3 start2)
    {
        return Vector3.Distance(start1, start2) <= RoadSnapDistance;
    }

    private void AddSegment(Road segment, List<Road> segments, QuadTreeRect<Road> quadTree)
    {
        segments.Add(segment);
        quadTree.Add(segment);
        lastAddedTime++;
        segment.addedToQtreeTime = lastAddedTime;
    }

    private static float MinDegreeDifference(float firstDeg, float secDeg)
    {
        var diff = Math.Abs(firstDeg - secDeg) % 180.0f;
        return Math.Min(diff, Math.Abs(diff - 180.0f));
    }

    private void DrawMap(int width, int height, PlaneType planeType)
    {
        var plane = Instantiate(populationMapBase);
        var planeGenerator = plane.GetComponent<PlaneGenerator>();
        planeGenerator.Generate(width, height, planeType);
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
            // Lines are parallel
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
