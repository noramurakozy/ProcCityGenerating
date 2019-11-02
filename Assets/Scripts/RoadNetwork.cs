
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
    private double[,] populationMap;
    private int[] roadCounters = {0, 0};

    [SerializeField]
    private RoadView roadViewPrefab;

    [SerializeField]
    private GameObject populationMapBase;
    
    [SerializeField]
    private Terrain terrain;

    public RoadNetworkDescriptor descriptor;
    
    private static int lastAddedTime;
    private float modernRoadSteps;
    private float oldTownRoadSteps;

    public void UpdateCity()
    {
        oldTownRoadSteps = descriptor.numOfSteps - (descriptor.numOfSteps * (descriptor.modernCityStructureExtent / 100f));
        modernRoadSteps = descriptor.numOfSteps - oldTownRoadSteps;
        //UnityEngine.Random.InitState(12345678);
        descriptor.switchToOldTownThreshold = oldTownRoadSteps.Equals(descriptor.numOfSteps) ? 8 : 4;
        var numOfStepsToDecrease = descriptor.numOfSteps;
        roadCounters = new[] {0, 0};
        DestroyAllObjects();
        qTree = new QuadTreeRect<Road>(new RectangleF(-5000, -5000, 10000, 10000));
        DrawMap((int) descriptor.mapWidth / descriptor.heatMapScale, (int) descriptor.mapHeight / descriptor.heatMapScale, PlaneType.Population);
        //DrawMap((int) descriptor.mapWidth / descriptor.HeatMapScale, (int) descriptor.mapHeight / descriptor.HeatMapScale, PlaneType.District);
        var startRoadType = oldTownRoadSteps > descriptor.numOfSteps / 2f ? RoadType.OldTown : RoadType.Modern;
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
                Start = new Vector3(descriptor.mapHeight / 2, 0, descriptor.mapWidth / 2),
                End = new Vector3(descriptor.mapHeight / 2 + descriptor.highwaySegmentLength, 0, descriptor.mapWidth / 2), 
                Number = 0, 
                IsHighway = true,
                Type = startRoadType
            },
            new Road()
            {
                Start = new Vector3(descriptor.mapHeight / 2, 0, descriptor.mapWidth / 2),
                End = new Vector3(descriptor.mapHeight / 2 - descriptor.highwaySegmentLength, 0, descriptor.mapWidth / 2), 
                Number = 0, 
                IsHighway = true,
                DirectionAngle = 180, 
                Type = startRoadType
            }
        };
    }

    private void DestroyAllObjects()
    {
        //plane.gameobjectre kell
        var planesInHierarchy = FindObjectsOfType<PlaneGenerator>();
        var roads = GameObject.Find("Roads");
        
        //futtatas kozben nem lesz jo
        Destroyer.SafeDestroy(roads);

        for (var i = 0; i < planesInHierarchy.Length; i++)
        {
            planesInHierarchy[i] = Destroyer.SafeDestroyGameObject(planesInHierarchy[i]);
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
                roadView.GetComponent<LineRenderer>().endColor = descriptor.highwayColor;
                roadView.GetComponent<LineRenderer>().startColor = descriptor.highwayColor;
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
            if (MathHelper.LineSegmentsIntersect(r.Start, r.End, segment.Start, segment.End)
                || MathHelper.SegmentsAreAlmostParallel(r.Start, segment.Start, r.End, segment.End, descriptor.roadSnapDistance))
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
                if (MathHelper.MinDegreeDifference(r.DirectionAngle, item.DirectionAngle) <= 10 &&
                    MathHelper.SegmentsAreAlmostParallel(r.Start, item.Start, r.End, item.End,descriptor.roadSnapDistance))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void SnapToCrossing(ref Road road1, Road road2)
    {
        if (MathHelper.PointsAreClose(road1.Start, road2.Start, descriptor.roadSnapDistance))
        {
            road1.Start = road2.Start;
        }

        if (MathHelper.PointsAreClose(road1.End, road2.End, descriptor.roadSnapDistance))
        {
            road1.End = road2.End;
        }

        if (MathHelper.PointsAreClose(road1.Start, road2.End, descriptor.roadSnapDistance))
        {
            road1.Start = road2.End;
        }

        if (MathHelper.PointsAreClose(road2.Start, road1.End, descriptor.roadSnapDistance))
        {
            road1.End = road2.Start;
        }
        
        //snap to crossing if the roads are like |--
        if (MathHelper.PointsAreDifferent(road1.Start, road1.End, road2.Start, road2.End))
        {
            if (MathHelper.FindDistanceToSegment(road1.End, road2.Start, road2.End, out _, out _) < descriptor.roadSnapDistance)
            {
                road1.End = Vector3.Distance(road1.End, road2.Start) < Vector3.Distance(road1.End, road2.End) ? road2.Start : road2.End;
            }
            
            if (MathHelper.FindDistanceToSegment(road1.Start, road2.Start, road2.End, out _, out _) < descriptor.roadSnapDistance)
            {
                road1.Start = Vector3.Distance(road1.Start, road2.Start) < Vector3.Distance(road1.Start, road2.End) ? road2.Start : road2.End;
            }
        }
    }

    private bool ReachMaxCrossingNumber(Road r)
    {
        var pointsInCrossing = qTree.GetObjects(r.Rectangle).Count;
        return pointsInCrossing >= descriptor.maxCrossingNumber;
    }

    private List<Road> GlobalGoals(Road previousRoad)
    {
        var roadType = RoadType.OldTown;
        var newRoads = new List<Road>();

        var straightRoad = Road.RoadWithDirection(previousRoad.End, previousRoad.DirectionAngle, previousRoad.Length, 0, previousRoad.IsHighway);
        var straightPopulation = PlaneGenerator.GetHeatMapAt(straightRoad.End.x, straightRoad.End.z, PlaneType.Population);
        var districtValue = PlaneGenerator.GetHeatMapAt(straightRoad.End.x, straightRoad.End.z, PlaneType.District);
        if (districtValue < descriptor.switchToOldTownThreshold
            && !Mathf.Approximately(oldTownRoadSteps, 0f)
            && roadCounters[(int)RoadType.OldTown] < oldTownRoadSteps)
        {
            descriptor.SetValuesToOldTown();
            roadType = RoadType.OldTown;
            
        }
        else if (!Mathf.Approximately(modernRoadSteps, 0f)
                && roadCounters[(int) RoadType.Modern] < modernRoadSteps)
        {
            descriptor.SetValuesToModern();
            roadType = RoadType.Modern;
        }
        else return new List<Road>();

        descriptor.normalBranchBaseAngle = UnityEngine.Random.Range(descriptor.minBaseAngle, descriptor.maxBaseAngle);
        if (previousRoad.IsHighway)
        {
            var angle = previousRoad.DirectionAngle + UnityEngine.Random.Range(-descriptor.highwayRandomAngle, descriptor.highwayRandomAngle);
            var randomAngleRoad = Road.RoadWithDirection(previousRoad.End, angle, previousRoad.Length, 0, previousRoad.IsHighway);

            var randomRoadPopulation = PlaneGenerator.GetHeatMapAt(randomAngleRoad.End.x, randomAngleRoad.End.z, PlaneType.Population);

            double roadPopulation;

            //highway continues
            if (randomRoadPopulation > straightPopulation)
            {
                newRoads.Add(randomAngleRoad);
                roadPopulation = randomRoadPopulation;
                randomAngleRoad.Population = (float)roadPopulation;
                previousRoad.NextRoad = randomAngleRoad;
            }
            else
            {
                newRoads.Add(straightRoad);
                roadPopulation = straightPopulation;
                straightRoad.Population = (float)roadPopulation;
                previousRoad.NextRoad = straightRoad;
            }
            
            //highway from highway
            if (roadPopulation > descriptor.highwayBranchPopulationThreshold)
            {
                if (UnityEngine.Random.value < descriptor.highwayBranchProbability)
                {
                    var leftAngle = previousRoad.DirectionAngle - descriptor.normalBranchBaseAngle + UnityEngine.Random.Range(-descriptor.defaultRoadRandomAngle, descriptor.defaultRoadRandomAngle);
                    var leftHighwayBranch = Road.RoadWithDirection(previousRoad.End, leftAngle, descriptor.highwaySegmentLength, 0, previousRoad.IsHighway);
                    newRoads.Add(leftHighwayBranch);
                }
                else if (UnityEngine.Random.value < descriptor.highwayBranchProbability)
                {
                    var rightAngle = previousRoad.DirectionAngle + descriptor.normalBranchBaseAngle + UnityEngine.Random.Range(-descriptor.defaultRoadRandomAngle, descriptor.defaultRoadRandomAngle);
                    var rightHighwayBranch = Road.RoadWithDirection(previousRoad.End, rightAngle, descriptor.highwaySegmentLength, 0, previousRoad.IsHighway);
                    newRoads.Add(rightHighwayBranch);
                }
            } 
        }
        else if (straightPopulation > descriptor.normalBranchPopulationThreshold)
        {
            newRoads.Add(straightRoad);
        }

        //secondary road branching 
        if (straightPopulation > descriptor.normalBranchPopulationThreshold)
        {
            var timeDelay = 0;
            if (previousRoad.IsHighway)
            {
                timeDelay = descriptor.normalBranchTimeDelayFromHighway;
            }

            if (UnityEngine.Random.value < descriptor.defaultBranchProbability)
            {
                var leftAngle = previousRoad.DirectionAngle - descriptor.normalBranchBaseAngle +
                                UnityEngine.Random.Range(-descriptor.defaultRoadRandomAngle, descriptor.defaultRoadRandomAngle);
                var leftBranch =
                    Road.RoadWithDirection(previousRoad.End, leftAngle, descriptor.branchSegmentLength, timeDelay, false);
                newRoads.Add(leftBranch);
            }
            else if (UnityEngine.Random.value < descriptor.defaultBranchProbability)
            {
                var rightAngle = previousRoad.DirectionAngle + descriptor.normalBranchBaseAngle +
                                 UnityEngine.Random.Range(-descriptor.defaultRoadRandomAngle, descriptor.defaultRoadRandomAngle);
                var rightBranch =
                    Road.RoadWithDirection(previousRoad.End, rightAngle, descriptor.branchSegmentLength, timeDelay, false);
                newRoads.Add(rightBranch);
            }
        }

        newRoads.ForEach(road => road.Type = roadType);
        roadCounters[(int)roadType]+= newRoads.Count;
        return newRoads;
    }

    private void AddSegment(Road segment, List<Road> segments, QuadTreeRect<Road> quadTree)
    {
        segments.Add(segment);
        quadTree.Add(segment);
        lastAddedTime++;
        segment.addedToQtreeTime = lastAddedTime;
    }

    private void DrawMap(int width, int height, PlaneType planeType)
    {
        var plane = Instantiate(populationMapBase);
        var planeGenerator = plane.GetComponent<PlaneGenerator>();
        planeGenerator.Generate(width, height, planeType);
    }

    public void GenerateTexturedCity()
    {
//        var terrain = Instantiate(this.terrain);
//        var roadArchitectSystem = Instantiate(this.roadArchitectSystem);
//        var gsdSplineC = roadArchitectSystem.AddRoad().GetComponent<GSDRoad>().GSDSpline;
//        gsdSplineC.mNodes.Add(new GSDSplineN());
//        var points = finalSegments.Select(road => road.End).ToList();
//        points.Insert(0, new Vector3(100,0,100));
//        gameObject.GetComponent<Roadifier>().GenerateRoad(points, Terrain.activeTerrain);
//        DestroyAllObjects();

        var points = new List<Vector3>();
        var roadsToTexture = new List<Road>(finalSegments);
        var lastRoad = roadsToTexture.Last();
        points.Add(lastRoad.End);
        points.Add(lastRoad.Start);
        roadsToTexture.Remove(lastRoad);
        var startRoad = GetInitialRoads(RoadType.OldTown)[0];
        var startRoad2 = GetInitialRoads(RoadType.OldTown)[1];
        var prevRoad = roadsToTexture.Find(road => road.End.Equals(lastRoad.Start));
        // just to stop the possible infinite loops
        int counter = 0;
        while (prevRoad != null || counter >= 100)
        {
            points.Add(prevRoad.Start);
            //Debug.Log("Counter " + counter++);
            roadsToTexture.Remove(prevRoad);
            prevRoad = roadsToTexture.Find(road => road.End.Equals(prevRoad.Start));
        }
        
        var points2 = new List<Vector3>();
        var firstRoad = roadsToTexture.First();
        points2.Add(firstRoad.Start);
        points2.Add(firstRoad.End);
        roadsToTexture.Remove(firstRoad);
        var nextRoad2 = roadsToTexture.Find(road => road.Start.Equals(firstRoad.End));
        // just to stop the possible infinite loop
        int counter2 = 0;
        while (nextRoad2 != null || counter2 >= 100)
        {
            points2.Add(nextRoad2.End);
            //Debug.Log("Counter " + counter2++);
            roadsToTexture.Remove(nextRoad2);
            nextRoad2 = roadsToTexture.Find(road => road.Start.Equals(nextRoad2.End));
        }
        gameObject.GetComponent<Roadifier>().GenerateRoad(points, Terrain.activeTerrain);
        gameObject.GetComponent<Roadifier>().GenerateRoad(points2, Terrain.activeTerrain);
        
        CombineMeshes();
    }

    private void CombineMeshes()
    {
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
        CombineInstance[] combine = new CombineInstance[meshFilters.Length];
        
        Debug.Log(name + " is combining " + meshFilters.Length + "meshes!");
        
        int i = 0;
        while (i < meshFilters.Length)
        {
            combine[i].subMeshIndex = 0;
            combine[i].mesh = meshFilters[i].sharedMesh;
            combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
            meshFilters[i].gameObject.SetActive(false);
            //Debug.Log(combine[i].mesh.name);

            i++;
        }

        Mesh finalMesh = new Mesh();
        finalMesh.CombineMeshes(combine, true);
        GetComponent<MeshFilter>().sharedMesh = finalMesh;
        transform.gameObject.SetActive(true);
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.DrawWireMesh(GetComponent<MeshFilter>().sharedMesh);
    }
}
