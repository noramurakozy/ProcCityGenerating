using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RoadNetwork))]
public class RoadNetworkEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // base.OnInspectorGUI();

        RoadNetwork roadNetwork = (RoadNetwork)target;

        roadNetwork.NumOfSteps = EditorGUILayout.IntSlider("Number of steps", roadNetwork.NumOfSteps, 1, 4000);

        roadNetwork.DefaultBranchProbability = EditorGUILayout.Slider("Default branch probability", (float)roadNetwork.DefaultBranchProbability, 0, 1);

        roadNetwork.HighwayBranchProbability = EditorGUILayout.Slider("Highway branch probability", (float)roadNetwork.HighwayBranchProbability, 0, 1);

        roadNetwork.HighwayBranchPopulationThreshold = EditorGUILayout.Slider("Highway branch population threshold", (float)roadNetwork.HighwayBranchPopulationThreshold, 1, 10);

        roadNetwork.NormalBranchPopulationThreshold = EditorGUILayout.Slider("Normal branch population threshold", (float)roadNetwork.NormalBranchPopulationThreshold, 1, 10);

        roadNetwork.NormalBranchTimeDelayFromHighway = EditorGUILayout.IntSlider("Normal branch time delay from highway", roadNetwork.NormalBranchTimeDelayFromHighway, 1, 10);

        roadNetwork.HighwaySegmentLength = EditorGUILayout.IntSlider("Highway segment length", roadNetwork.HighwaySegmentLength, 1, 10);

        roadNetwork.BranchSegmentLength = EditorGUILayout.IntSlider("Branch segment length", roadNetwork.BranchSegmentLength, 1, 10);

        roadNetwork.HighwayRandomAngle = EditorGUILayout.IntSlider("Highway random angle", roadNetwork.HighwayRandomAngle, 1, 360);

        roadNetwork.DefaultRoadRandomAngle = EditorGUILayout.IntSlider("Default road random angle", roadNetwork.DefaultRoadRandomAngle, 1, 360);

        roadNetwork.MinimumIntersectionDeviation = EditorGUILayout.IntSlider("Minimum intersection deviation", roadNetwork.MinimumIntersectionDeviation, 1, 360);

        roadNetwork.RoadSnapDistance = EditorGUILayout.Slider("Road snap distance", roadNetwork.RoadSnapDistance, 1, 5);

        roadNetwork.MapHeight = EditorGUILayout.Slider("Map height", roadNetwork.MapHeight, 10, 500);

        roadNetwork.MapWidth = EditorGUILayout.Slider("Map width", roadNetwork.MapWidth, 10, 500);

        roadNetwork.HighwayColor = EditorGUILayout.ColorField("Highway color", roadNetwork.HighwayColor);

        roadNetwork.SecondaryRoadColor = EditorGUILayout.ColorField("Secondary road color", roadNetwork.SecondaryRoadColor);

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Update city"))
        {
            roadNetwork.UpdateCity();
        }

        if (GUILayout.Button("Reset city"))
        {
            roadNetwork.SetValuesToDefault();
        }

        GUILayout.EndHorizontal();
    }
}
