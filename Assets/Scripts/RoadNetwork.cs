
using QuadTrees;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using UnityEngine;
using Color = UnityEngine.Color;
using Debug = UnityEngine.Debug;

[RequireComponent(typeof(Roadifier))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class RoadNetwork : MonoBehaviour
{
    private QuadTreeRect<Road> qTree;
    private List<Road> primaryQueue;
    private List<Road> finalSegments;
    private List<Intersection> intersections;
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
        var roadMeshesParent = GameObject.Find("Road meshes");
        Destroyer.SafeDestroy(roadMeshesParent);
        roadMeshesParent = new GameObject("Road meshes");
        roadMeshesParent.transform.SetParent(transform);
        
        oldTownRoadSteps = descriptor.numOfRoadSegments - (descriptor.numOfRoadSegments * (descriptor.modernCityStructureExtent / 100f));
        modernRoadSteps = descriptor.numOfRoadSegments - oldTownRoadSteps;
        //UnityEngine.Random.InitState(12345678);
        //UnityEngine.Random.InitState(3456789);
        descriptor.switchToOldTownThreshold = oldTownRoadSteps.Equals(descriptor.numOfRoadSegments) ? 8 : 4;
        var numOfStepsToDecrease = descriptor.numOfRoadSegments;
        roadCounters = new[] {0, 0};
        DestroyAllObjects();
        qTree = new QuadTreeRect<Road>(new RectangleF(-5000, -5000, 10000, 10000));
        DrawMap((int) descriptor.mapWidth / descriptor.heatMapScale, (int) descriptor.mapHeight / descriptor.heatMapScale, PlaneType.Population);
        //DrawMap((int) descriptor.mapWidth / descriptor.HeatMapScale, (int) descriptor.mapHeight / descriptor.HeatMapScale, PlaneType.District);
        var startRoadType = oldTownRoadSteps > descriptor.numOfRoadSegments / 2f ? RoadType.OldTown : RoadType.Modern;
        primaryQueue = GetInitialRoads(startRoadType);
        finalSegments = new List<Road>();
        intersections = new List<Intersection>();

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
                primaryQueue.Add(new Road(road.Start, road.End)
                {
                    Number = min.Number + road.Number + 1,
                    DirectionAngle = road.DirectionAngle, 
                    IsHighway = road.IsHighway, 
                    Color = road.Color, 
                    Type = road.Type
                });
            }
        }

        //FixIntersectionsWithoutOutRoads();

        //intersections.RemoveAll(intersection => (intersection.RoadsIn.Count + intersection.RoadsOut.Count) < 3);
        DrawSegments();
        //DrawIntersections();
    }

    private void FixIntersectionsWithoutOutRoads()
    {
        var withoutOutRoads = intersections.FindAll(intersection => intersection.RoadsIn.Count == 2 && intersection.RoadsOut.Count == 0);
        foreach (var intersection in withoutOutRoads)
        {
        	var road1 = intersection.RoadsIn[0];
            
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = road1.End;
            sphere.transform.localScale = Vector3.one * 0.1f;
            
        	var roadStart = new Vector3(road1.Start.x, road1.Start.y, road1.Start.z);
        	var roadEnd = new Vector3(road1.End.x, road1.End.y, road1.End.z);
        	var roadPrevIntersection = road1.PrevIntersection;
        	var roadNextIntersection = road1.NextIntersection;
        	
        	road1.Start = roadEnd;
        	road1.End = roadStart;

            intersection.RoadsIn.Remove(road1);
            intersection.RoadsOut.Add(road1);
            
            road1.PrevIntersection = roadNextIntersection;
            road1.NextIntersection = roadPrevIntersection;

            road1.PrevIntersection.RoadsOut.Remove(road1);
            road1.PrevIntersection.RoadsIn.Add(road1);
            
            var sphere1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere1.transform.position = road1.End;
            sphere1.transform.localScale = Vector3.one * 0.1f;
            
            var sphere2 = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            sphere2.transform.position = road1.MeshEnd;
            sphere2.transform.localScale = Vector3.one * 0.2f;
        }
    }

    private void DrawIntersections()
    {
        foreach (var intersection in intersections)
        {
            if (intersection.CenterCornerPoints != null)
            {
                foreach (var cornerPoint in intersection.CenterCornerPoints)
                {
                    var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    sphere.transform.position = cornerPoint ?? Vector3.zero;
                }
            }
        }
    }

    private List<Road> GetInitialRoads(RoadType startRoadType)
    {
        return new List<Road>()
        {
            new Road(new Vector3(descriptor.mapHeight / 2, 0, descriptor.mapWidth / 2), new Vector3(descriptor.mapHeight / 2 + descriptor.segmentLength, 0, descriptor.mapWidth / 2))
            {
                Number = 0, 
                IsHighway = true,
                Type = startRoadType
            },
            new Road(new Vector3(descriptor.mapHeight / 2, 0, descriptor.mapWidth / 2), new Vector3(descriptor.mapHeight / 2 - descriptor.segmentLength, 0, descriptor.mapWidth / 2))
            {
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
                //roadView.GetComponent<LineRenderer>().endColor = Color.magenta;
                roadView.GetComponent<LineRenderer>().endColor = descriptor.highwayColor;
                roadView.GetComponent<LineRenderer>().startColor = descriptor.highwayColor;
            }
            else
            {
                //roadView.GetComponent<LineRenderer>().endColor = Color.green;
                roadView.GetComponent<LineRenderer>().endColor = roadView.road.Color;
                roadView.GetComponent<LineRenderer>().startColor = roadView.road.Color;
            }
            roadView.Draw();
        }
        Debug.Log("Old: " + roadCounters[(int)RoadType.OldTown]);
        Debug.Log("Modern: " + roadCounters[(int)RoadType.Modern]);
        Debug.Log("Done");
        Debug.Log("Number of intersections: " + intersections.Count);
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

    private void AddRoadToIntersection(Road road)
    {
        var startIsCenter = intersections.Find(intersection => intersection.Center.Equals(road.Start));
        if (startIsCenter != null)
        {
            startIsCenter.RoadsOut.Add(road);
            road.PrevIntersection = startIsCenter;
        }
        else
        {
            var newIntersection = new Intersection(new List<Road>(), new List<Road>(), road.Start);
            newIntersection.RoadsOut.Add(road);
            intersections.Add(newIntersection);
            road.PrevIntersection = newIntersection;
        }
        
        var endIsCenter = intersections.Find(intersection => intersection.Center.Equals(road.End));
        if (endIsCenter != null)
        {
            endIsCenter.RoadsIn.Add(road);
            road.NextIntersection = endIsCenter;
        }
        else
        {
            var newIntersection2 = new Intersection(new List<Road>(), new List<Road>(), road.End);
            newIntersection2.RoadsIn.Add(road);
            intersections.Add(newIntersection2);
            road.NextIntersection = newIntersection2;
        }
        
    }

    private bool CheckConstraintsOnNeighbours(ref Road r, RectangleF area)
    {
        foreach (var otherSegment in qTree.GetObjects(area))
        {
            //snap to crossing
            SnapToCrossing(ref r, otherSegment);

            if (r == null)
            {
                return true;
            }

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
                //road1.End = Vector3.Distance(road1.End, road2.Start) < Vector3.Distance(road1.End, road2.End) ? road2.Start : road2.End;
                //road1 = null;
            }
            else if (MathHelper.FindDistanceToSegment(road1.Start, road2.Start, road2.End, out _, out _) < descriptor.roadSnapDistance)
            {
                road1.Start = Vector3.Distance(road1.Start, road2.Start) < Vector3.Distance(road1.Start, road2.End) ? road2.Start : road2.End;
                road1.Color = Color.magenta;
            }
        }
        
        if (MathHelper.PointsAreClose(road1.End, road2.End, descriptor.roadSnapDistance))
        {
            //road1.End = road2.End;
            //road1 = null;
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
                randomAngleRoad.PrevRoad = previousRoad;
            }
            else
            {
                newRoads.Add(straightRoad);
                roadPopulation = straightPopulation;
                straightRoad.Population = (float)roadPopulation;
                previousRoad.NextRoad = straightRoad;
                straightRoad.PrevRoad = previousRoad;
            }
            
            //highway from highway
            if (roadPopulation > descriptor.highwayBranchPopulationThreshold)
            {
                if (UnityEngine.Random.value < descriptor.highwayBranchProbability)
                {
                    var leftAngle = previousRoad.DirectionAngle - descriptor.normalBranchBaseAngle + UnityEngine.Random.Range(-descriptor.regularRoadRandomAngle, descriptor.regularRoadRandomAngle);
                    var leftHighwayBranch = Road.RoadWithDirection(previousRoad.End, leftAngle, descriptor.segmentLength, 0, previousRoad.IsHighway);
                    newRoads.Add(leftHighwayBranch);
                    previousRoad.NextRoad = leftHighwayBranch;
                    leftHighwayBranch.PrevRoad = previousRoad;
                }
                else if (UnityEngine.Random.value < descriptor.highwayBranchProbability)
                {
                    var rightAngle = previousRoad.DirectionAngle + descriptor.normalBranchBaseAngle + UnityEngine.Random.Range(-descriptor.regularRoadRandomAngle, descriptor.regularRoadRandomAngle);
                    var rightHighwayBranch = Road.RoadWithDirection(previousRoad.End, rightAngle, descriptor.segmentLength, 0, previousRoad.IsHighway);
                    newRoads.Add(rightHighwayBranch);
                    previousRoad.NextRoad = rightHighwayBranch;
                    rightHighwayBranch.PrevRoad = previousRoad;
                }
            } 
        }
        else if (straightPopulation > descriptor.regularBranchPopulationThreshold)
        {
            newRoads.Add(straightRoad);
            previousRoad.NextRoad = straightRoad;
            straightRoad.PrevRoad = previousRoad;
        }

        //secondary road branching 
        if (straightPopulation > descriptor.regularBranchPopulationThreshold)
        {
            var timeDelay = 0;
            if (previousRoad.IsHighway)
            {
                timeDelay = descriptor.normalBranchTimeDelayFromHighway;
            }

            if (UnityEngine.Random.value < descriptor.regularBranchProbability)
            {
                var leftAngle = previousRoad.DirectionAngle - descriptor.normalBranchBaseAngle +
                                UnityEngine.Random.Range(-descriptor.regularRoadRandomAngle, descriptor.regularRoadRandomAngle);
                var leftBranch =
                    Road.RoadWithDirection(previousRoad.End, leftAngle, descriptor.segmentLength, timeDelay, false);
                newRoads.Add(leftBranch);
                previousRoad.NextRoad = leftBranch;
                leftBranch.PrevRoad = previousRoad;
            }
            else if (UnityEngine.Random.value < descriptor.regularBranchProbability)
            {
                var rightAngle = previousRoad.DirectionAngle + descriptor.normalBranchBaseAngle +
                                 UnityEngine.Random.Range(-descriptor.regularRoadRandomAngle, descriptor.regularRoadRandomAngle);
                var rightBranch =
                    Road.RoadWithDirection(previousRoad.End, rightAngle, descriptor.segmentLength, timeDelay, false);
                newRoads.Add(rightBranch);
                previousRoad.NextRoad = rightBranch;
                rightBranch.PrevRoad = previousRoad;
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
        AddRoadToIntersection(segment);
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
        foreach (var finalSegment in finalSegments)
        {
            finalSegment.MeshStart = finalSegment.Start;
            finalSegment.MeshEnd = finalSegment.End;
        }

        var intersectionMeshes = gameObject.GetComponent<Roadifier>().GenerateIntersections(intersections);
        var roadMeshes = gameObject.GetComponent<Roadifier>().GenerateRoadMeshes(intersections);
        var initRoadMeshes = gameObject.GetComponent<Roadifier>().GenerateRoad(new List<Vector3>
        {
            finalSegments[0].MeshEnd,
            finalSegments[0].MeshStart,
            finalSegments[1].MeshEnd
        });
        roadMeshes.Add(initRoadMeshes);

        CombineInstance[] intersectionCombine = new CombineInstance[intersectionMeshes.Count];
        int i = 0;
        while (i < intersectionCombine.Length)
        {
            intersectionCombine[i].subMeshIndex = 0;
            intersectionCombine[i].mesh = intersectionMeshes[i];
            intersectionCombine[i].transform = transform.localToWorldMatrix;
            i++;
        }
        
        CombineInstance[] roadCombine = new CombineInstance[roadMeshes.Count];
        int j = 0;
        while (j < roadCombine.Length)
        {
            roadCombine[j].subMeshIndex = 0;
            roadCombine[j].mesh = roadMeshes[j];
            roadCombine[j].transform = transform.localToWorldMatrix;
            j++;
        }
        
        Mesh intersectionMesh = new Mesh();
        intersectionMesh.CombineMeshes(intersectionCombine, true);
        
        Mesh roadMesh = new Mesh();
        roadMesh.CombineMeshes(roadCombine, true);
        
        CombineMeshes(intersectionMesh, roadMesh);
    }

    private void CombineMeshes(Mesh intersectionMesh, Mesh roadMesh)
    {
        var localToWorldMatrix = transform.localToWorldMatrix;
        CombineInstance[] combine = new CombineInstance[2];
        
        combine[0].subMeshIndex = 0;
        combine[0].mesh = intersectionMesh;
        combine[0].transform = localToWorldMatrix;
        
        combine[1].subMeshIndex = 0;
        combine[1].mesh = roadMesh;
        combine[1].transform = localToWorldMatrix;
        
        Mesh finalMesh = new Mesh();
        finalMesh.subMeshCount = 2;
        finalMesh.name = "Road mesh";
        finalMesh.CombineMeshes(combine, false);
        //it must be copied because 'vertices' only a copy of the original vertices
        var verticesToAdapt = finalMesh.vertices;
        Roadifier.AdaptPointsToTerrainHeight(verticesToAdapt, Terrain.activeTerrain);
        finalMesh.vertices = verticesToAdapt;
        
        GetComponent<MeshFilter>().sharedMesh = finalMesh;
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.DrawWireMesh(GetComponent<MeshFilter>().sharedMesh);
    }
}
