using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(RoadNetwork))]
    public class RoadNetworkEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var roadNetwork = ((RoadNetwork)target);
            var descriptor = roadNetwork.descriptor;

            descriptor.numOfRoadSegments = EditorGUILayout.IntSlider("Number of road segments", descriptor.numOfRoadSegments, 1, 30000);

            descriptor.regularBranchPopulationThreshold = EditorGUILayout.Slider("Regular branch population threshold", (float)descriptor.regularBranchPopulationThreshold, 1, 10);
            
            descriptor.highwayBranchPopulationThreshold = EditorGUILayout.Slider("Highway branch population threshold", (float)descriptor.highwayBranchPopulationThreshold, 1, 10);

            descriptor.regularBranchProbability = EditorGUILayout.Slider("Regular branch probability", (float)descriptor.regularBranchProbability, 0, 1);

            descriptor.highwayBranchProbability = EditorGUILayout.Slider("Highway branch probability", (float)descriptor.highwayBranchProbability, 0, 1);

            descriptor.switchToOldTownThreshold = EditorGUILayout.Slider("Switch to old town threshold", (float)descriptor.switchToOldTownThreshold, 0, 8);

            descriptor.segmentLength = EditorGUILayout.IntSlider("Segment length", descriptor.segmentLength, 1, 10);

            descriptor.highwayRandomAngle = EditorGUILayout.IntSlider("Highway random angle", descriptor.highwayRandomAngle, 0, 90);

            descriptor.regularRoadRandomAngle = EditorGUILayout.IntSlider("Regular road random angle", descriptor.regularRoadRandomAngle, 0, 90);
            
            GUILayout.BeginHorizontal();
            descriptor.minBaseAngle = EditorGUILayout.FloatField("Base angle range", descriptor.minBaseAngle);
            EditorGUILayout.MinMaxSlider(ref descriptor.minBaseAngle, ref descriptor.maxBaseAngle, -90f, 90f, null);
            descriptor.maxBaseAngle = EditorGUILayout.FloatField(descriptor.maxBaseAngle);
            GUILayout.EndHorizontal();
            
            descriptor.roadSnapDistance = EditorGUILayout.Slider("Road snap distance", descriptor.roadSnapDistance, 0, 3);

            descriptor.mapHeight = EditorGUILayout.Slider("Map height", descriptor.mapHeight, 10, 500);

            descriptor.mapWidth = EditorGUILayout.Slider("Map width", descriptor.mapWidth, 10, 500);

            descriptor.maxCrossingNumber = EditorGUILayout.IntSlider("Max number of roads in crossing", descriptor.maxCrossingNumber, 2, 4);

            descriptor.modernCityStructureExtent = EditorGUILayout.IntSlider("Modern city percentage",
                descriptor.modernCityStructureExtent, 0, 100);

            descriptor.highwayColor = EditorGUILayout.ColorField("Highway color", descriptor.highwayColor);

            descriptor.secondaryRoadColor = EditorGUILayout.ColorField("Secondary road color", descriptor.secondaryRoadColor);

            if (GUILayout.Button("Update city"))
            {
                roadNetwork.UpdateCity();
            }

            if (GUILayout.Button("Generate textured city"))
            {
                roadNetwork.GenerateTexturedCity();
            }
        }
    }
}
