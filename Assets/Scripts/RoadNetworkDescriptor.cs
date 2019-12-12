using System;
using UnityEngine;

[Serializable]
public class RoadNetworkDescriptor
{
    public int numOfRoadSegments = 500;

    public double regularBranchProbability = 0.3;

    public double highwayBranchProbability = 0.05;

    public double highwayBranchPopulationThreshold = 6;

    public double regularBranchPopulationThreshold = 4;
    
    public double switchToOldTownThreshold = 4;

    public int normalBranchTimeDelayFromHighway = 5;
    
    public int segmentLength = 2;

    public int highwayRandomAngle = 15;

    public int regularRoadRandomAngle = 3;
    
    public float normalBranchBaseAngle = 90;
    
    public float roadSnapDistance = 1;

    public float mapHeight = 200;

    public float mapWidth = 200;
    
    public int maxCrossingNumber = 4;

    public Color highwayColor = Color.black;

    public Color secondaryRoadColor = Color.red;
    
    public float minBaseAngle = 90;
    public float maxBaseAngle = 90;
    public int modernCityStructureExtent = 0;
    public int heatMapScale = 1;

    public void SetValuesToOldTown()
    {
        highwayRandomAngle = 15;
        regularRoadRandomAngle = 3;
        //roadSnapDistance = 1;
        minBaseAngle = 80;
        regularBranchPopulationThreshold = 5;
    }
     
    public void SetValuesToModern()
    {
        highwayRandomAngle = 2;
        regularRoadRandomAngle = 0;
        //roadSnapDistance = 0.3f;
        minBaseAngle = 90;
        regularBranchPopulationThreshold = 4;
    }
}

