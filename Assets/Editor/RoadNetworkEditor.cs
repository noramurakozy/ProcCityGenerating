using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RoadNetwork))]
public class RoadNetworkEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        RoadNetwork roadNetwork = (RoadNetwork)target;

        roadNetwork.NumOfSteps = EditorGUILayout.IntSlider("Number of steps", 3000, 1, 4000);

        roadNetwork.DefaultBranchProbability = EditorGUILayout.Slider("Default branch probability", 0.3f, 0, 1);

        roadNetwork.HighwayBranchProbability = EditorGUILayout.Slider("Highway branch probability", 0.05f, 0, 1);

        roadNetwork.HighwayBranchPopulationThreshold = EditorGUILayout.Slider("Highway branch population threshold", 6f, 1, 10);

        roadNetwork.NormalBranchPopulationThreshold = EditorGUILayout.Slider("Normal branch population threshold", 4f, 1, 10);

        roadNetwork.NormalBranchTimeDelayFromHighway = EditorGUILayout.IntSlider("Normal branch time delay from highway", 5, 1, 10);

        roadNetwork.HighwaySegmentLength = EditorGUILayout.IntSlider("Highway segment length", 3, 1, 10);

        roadNetwork.BranchSegmentLength = EditorGUILayout.IntSlider("Branch segment length", 2, 1, 10);

        roadNetwork.HighwayRandomAngle = EditorGUILayout.IntSlider("Highway random angle", 15, 1, 360);

        // roadNetwork.DefaultRoadRandomAngle = EditorGUILayout.IntSlider("Default road random angle", 3, 1, 360);

        roadNetwork.MinimumIntersectionDeviation = EditorGUILayout.IntSlider("Minimum intersection deviation", 30, 1, 360);

        roadNetwork.RoadSnapDistance = EditorGUILayout.Slider("Road snap distance", 1, 1, 5);

        roadNetwork.MapHeight = EditorGUILayout.Slider("Map height", 200, 10, 500);

        roadNetwork.MapWidth = EditorGUILayout.Slider("Map width", 200, 10, 500);

        roadNetwork.HighwayColor = EditorGUILayout.ColorField("Highway color", Color.black);

        roadNetwork.SecondaryRoadColor = EditorGUILayout.ColorField("Secondary road color", Color.red);
    }
}
