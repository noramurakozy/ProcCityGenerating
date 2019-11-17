using System;
using UnityEngine;

[Serializable]
public class RoadNetworkDescriptor
{
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
        defaultRoadRandomAngle = 3;
        //roadSnapDistance = 1;
        minBaseAngle = 80;
        normalBranchPopulationThreshold = 5;
    }
     
    public void SetValuesToModern()
    {
        highwayRandomAngle = 2;
        defaultRoadRandomAngle = 0;
        //roadSnapDistance = 0.3f;
        minBaseAngle = 90;
        normalBranchPopulationThreshold = 4;
    }
}

