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

            descriptor.numOfSteps = EditorGUILayout.IntSlider("Number of steps", descriptor.numOfSteps, 1, 30000);

            descriptor.defaultBranchProbability = EditorGUILayout.Slider("Default branch probability", (float)descriptor.defaultBranchProbability, 0, 1);

            descriptor.highwayBranchProbability = EditorGUILayout.Slider("Highway branch probability", (float)descriptor.highwayBranchProbability, 0, 1);

            descriptor.highwayBranchPopulationThreshold = EditorGUILayout.Slider("Highway branch population threshold", (float)descriptor.highwayBranchPopulationThreshold, 1, 10);

            descriptor.normalBranchPopulationThreshold = EditorGUILayout.Slider("Normal branch population threshold", (float)descriptor.normalBranchPopulationThreshold, 1, 10);
            
            descriptor.switchToOldTownThreshold = EditorGUILayout.Slider("Switch to old town threshold", (float)descriptor.switchToOldTownThreshold, 0, 8);

            descriptor.normalBranchTimeDelayFromHighway = EditorGUILayout.IntSlider("Normal branch time delay from highway", descriptor.normalBranchTimeDelayFromHighway, 1, 10);

            descriptor.highwaySegmentLength = EditorGUILayout.IntSlider("Highway segment length", descriptor.highwaySegmentLength, 1, 10);

            descriptor.branchSegmentLength = EditorGUILayout.IntSlider("Branch segment length", descriptor.branchSegmentLength, 1, 10);

            descriptor.highwayRandomAngle = EditorGUILayout.IntSlider("Highway random angle", descriptor.highwayRandomAngle, -90, 90);

            descriptor.defaultRoadRandomAngle = EditorGUILayout.IntSlider("Default road random angle", descriptor.defaultRoadRandomAngle, -90, 90);
            
            descriptor.roadSnapDistance = EditorGUILayout.Slider("Road snap distance", descriptor.roadSnapDistance, 0, 3);

            descriptor.mapHeight = EditorGUILayout.Slider("Map height", descriptor.mapHeight, 10, 500);

            descriptor.mapWidth = EditorGUILayout.Slider("Map width", descriptor.mapWidth, 10, 500);

            descriptor.maxCrossingNumber = EditorGUILayout.IntSlider("Max number of roads in crossing", descriptor.maxCrossingNumber, 2, 4);

            descriptor.modernCityStructureExtent = EditorGUILayout.IntSlider("Modern city percentage",
                descriptor.modernCityStructureExtent, 0, 100);

            descriptor.highwayColor = EditorGUILayout.ColorField("Highway color", descriptor.highwayColor);

            descriptor.secondaryRoadColor = EditorGUILayout.ColorField("Secondary road color", descriptor.secondaryRoadColor);
    
            GUILayout.BeginHorizontal();
            descriptor.minBaseAngle = EditorGUILayout.FloatField("Random base angle range", descriptor.minBaseAngle);
            EditorGUILayout.MinMaxSlider(ref descriptor.minBaseAngle, ref descriptor.maxBaseAngle, -90f, 90f, null);
            descriptor.maxBaseAngle = EditorGUILayout.FloatField(descriptor.maxBaseAngle);
            GUILayout.EndHorizontal();

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
