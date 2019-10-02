﻿using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(RoadNetwork))]
    public class RoadNetworkEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var roadNetwork = (RoadNetwork)target;

            roadNetwork.numOfSteps = EditorGUILayout.IntSlider("Number of steps", roadNetwork.numOfSteps, 1, 4000);

            roadNetwork.defaultBranchProbability = EditorGUILayout.Slider("Default branch probability", (float)roadNetwork.defaultBranchProbability, 0, 1);

            roadNetwork.highwayBranchProbability = EditorGUILayout.Slider("Highway branch probability", (float)roadNetwork.highwayBranchProbability, 0, 1);

            roadNetwork.highwayBranchPopulationThreshold = EditorGUILayout.Slider("Highway branch population threshold", (float)roadNetwork.highwayBranchPopulationThreshold, 1, 10);

            roadNetwork.normalBranchPopulationThreshold = EditorGUILayout.Slider("Normal branch population threshold", (float)roadNetwork.normalBranchPopulationThreshold, 1, 10);

            roadNetwork.normalBranchTimeDelayFromHighway = EditorGUILayout.IntSlider("Normal branch time delay from highway", roadNetwork.normalBranchTimeDelayFromHighway, 1, 10);

            roadNetwork.highwaySegmentLength = EditorGUILayout.IntSlider("Highway segment length", roadNetwork.highwaySegmentLength, 1, 10);

            roadNetwork.branchSegmentLength = EditorGUILayout.IntSlider("Branch segment length", roadNetwork.branchSegmentLength, 1, 10);

            roadNetwork.highwayRandomAngle = EditorGUILayout.IntSlider("Highway random angle", roadNetwork.highwayRandomAngle, 1, 360);

            roadNetwork.defaultRoadRandomAngle = EditorGUILayout.IntSlider("Default road random angle", roadNetwork.defaultRoadRandomAngle, 1, 360);

            roadNetwork.minimumIntersectionDeviation = EditorGUILayout.IntSlider("Minimum intersection deviation", roadNetwork.minimumIntersectionDeviation, 1, 360);

            roadNetwork.roadSnapDistance = EditorGUILayout.Slider("Road snap distance", roadNetwork.roadSnapDistance, 1, 5);

            roadNetwork.mapHeight = EditorGUILayout.Slider("Map height", roadNetwork.mapHeight, 10, 500);

            roadNetwork.mapWidth = EditorGUILayout.Slider("Map width", roadNetwork.mapWidth, 10, 500);

            roadNetwork.highwayColor = EditorGUILayout.ColorField("Highway color", roadNetwork.highwayColor);

            roadNetwork.secondaryRoadColor = EditorGUILayout.ColorField("Secondary road color", roadNetwork.secondaryRoadColor);

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Update city"))
            {
                roadNetwork.UpdateCity();
            }

            if (GUILayout.Button("Reset values"))
            {
                roadNetwork.SetValuesToDefault();
            }

            GUILayout.EndHorizontal();
        }
    }
}
