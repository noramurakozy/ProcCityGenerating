﻿// Converted from UnityScript to C# at http://www.M2H.nl/files/js_to_c.php - by Mike Hergaarden
// Do test the code! You usually need to change a few small bits.

using System;
using UnityEngine;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using Color = UnityEngine.Color;

public class Roadifier : MonoBehaviour {
	
	public float roadWidth = 1.0f;
	public float roadLength = 2.0f;
	public float smoothingFactor = 0.2f;
	public int smoothingIterations = 3;
	public float intersectionOffset = 0.5f;
	public Material material;
	public float terrainClearance = 0.05f;
	private Mesh mesh;
	private Vector2[] uvs;
	private static readonly float ROAD_MESH_OFFSET = 0.8f;

	public List<Mesh> GenerateIntersections(List<Intersection> intersections)
	{
		var interMeshes = GenerateQuadIntersections(intersections);
		interMeshes.AddRange(GenerateTriIntersections(intersections));
		return interMeshes;
	}

	private List<Mesh> GenerateQuadIntersections(List<Intersection> intersections)
	{
		List<Mesh> interMeshes = new List<Mesh>();
		var quadIntersections =
			intersections.Where(intersection => (intersection.RoadsIn.Count + intersection.RoadsOut.Count) == 4);

		var boxMaxSide = 0f;
		foreach (var intersection in quadIntersections)
		{
			List<Vector3> cornerPoints = new List<Vector3>();
			Vector3[] roadVectors = new Vector3[4];
			var counter = 0;
			foreach (var road in intersection.RoadsIn)
			{
				roadVectors[counter] = road.Start - road.End;
				road.MeshEnd = road.End + roadVectors[counter].normalized * ROAD_MESH_OFFSET;
				counter++;
			}

			foreach (var road in intersection.RoadsOut)
			{
				roadVectors[counter] = road.End - road.Start;
				road.MeshStart = road.Start + roadVectors[counter].normalized * ROAD_MESH_OFFSET;
				counter++;
			}

			int oppositeIdx = 0;
			float maxAngle = 0;
			for (int i = 1; i < roadVectors.Length; i++)
			{
				var angle = Vector3.Angle(roadVectors[0], roadVectors[i]);
				if (angle > maxAngle)
				{
					maxAngle = angle;
					oppositeIdx = i;
				}
			}

			var triangles = new List<int>();
			var roadVectorsSorted = new Vector3[4];
			roadVectorsSorted[0] = roadVectors[0];
			roadVectorsSorted[3] = roadVectors[oppositeIdx];

			var j = 2;
			for (int i = 1; i < roadVectors.Length; i++)
			{
				if (i == oppositeIdx)
				{
					continue;
				}

				// Vector3.Cross.magnitude --> sin(alfa), alfa --> angle between roadvec[0] and roadvec[i]
				var y = (roadWidth / 2) / Vector3.Cross(roadVectors[0].normalized, roadVectors[i].normalized).magnitude;
				Vector3 cornerPoint = (roadVectors[0].normalized + roadVectors[i].normalized) * y + intersection.Center;
				cornerPoints.Add(cornerPoint);
				roadVectorsSorted[j] = roadVectors[i];
				j = 1;
			}

			for (int i = 1; i < roadVectors.Length; i++)
			{
				if (i == oppositeIdx)
				{
					continue;
				}

				// Vector3.Cross.magnitude --> sin(alfa), alfa --> angle between roadvec[0] and roadvec[i]
				var y = (roadWidth / 2) /
				        Vector3.Cross(roadVectors[oppositeIdx].normalized, roadVectors[i].normalized).magnitude;
				Vector3 cornerPoint = (roadVectors[oppositeIdx].normalized + roadVectors[i].normalized) * y +
				                      intersection.Center;
				cornerPoints.Add(cornerPoint);
			}

			var nextPoints = new[] {1, 3, 0, 2};
			var intersectionPoints = new List<Vector3>(cornerPoints);
			for (int i = 0; i < cornerPoints.Count; i++)
			{
				int nextI = nextPoints[i];
				Vector3 d = (intersection.Center + roadVectorsSorted[i].normalized * ROAD_MESH_OFFSET)
				            - (cornerPoints[i] + cornerPoints[nextI]) / 2;


				Vector3 x = cornerPoints[i] - cornerPoints[nextI];
				intersectionPoints.Add(cornerPoints[i] + (d -
				                                          x.magnitude / 2 *
				                                          Vector3.Dot(x.normalized, roadVectorsSorted[i].normalized) *
				                                          roadVectorsSorted[i].normalized));
				intersectionPoints.Add(cornerPoints[nextI] + (d +
				                                              x.magnitude / 2 *
				                                              Vector3.Dot(x.normalized, roadVectorsSorted[i].normalized) *
				                                              roadVectorsSorted[i].normalized));
			}

			if (Vector3.Cross(cornerPoints[1] - cornerPoints[0], cornerPoints[2] - cornerPoints[0]).y > 0)
			{
				triangles.Add(0);
				triangles.Add(1);
				triangles.Add(2);
				triangles.Add(1);
				triangles.Add(3);
				triangles.Add(2);

				triangles.Add(4);
				triangles.Add(5);
				triangles.Add(0);
				triangles.Add(5);
				triangles.Add(1);
				triangles.Add(0);

				triangles.Add(6);
				triangles.Add(7);
				triangles.Add(1);
				triangles.Add(7);
				triangles.Add(3);
				triangles.Add(1);

				triangles.Add(10);
				triangles.Add(11);
				triangles.Add(3);
				triangles.Add(11);
				triangles.Add(2);
				triangles.Add(3);

				triangles.Add(8);
				triangles.Add(9);
				triangles.Add(2);
				triangles.Add(9);
				triangles.Add(0);
				triangles.Add(2);
			}
			else
			{
				triangles.Add(1);
				triangles.Add(0);
				triangles.Add(2);
				triangles.Add(1);
				triangles.Add(2);
				triangles.Add(3);

				triangles.Add(5);
				triangles.Add(4);
				triangles.Add(0);
				triangles.Add(5);
				triangles.Add(0);
				triangles.Add(1);

				triangles.Add(7);
				triangles.Add(6);
				triangles.Add(1);
				triangles.Add(7);
				triangles.Add(1);
				triangles.Add(3);

				triangles.Add(11);
				triangles.Add(10);
				triangles.Add(3);
				triangles.Add(11);
				triangles.Add(3);
				triangles.Add(2);

				triangles.Add(9);
				triangles.Add(8);
				triangles.Add(2);
				triangles.Add(9);
				triangles.Add(2);
				triangles.Add(0);
			}

			var boundingBox = GetBoundingBox(cornerPoints.ToArray());
			if ((boundingBox.height > boundingBox.width ? boundingBox.height : boundingBox.width) > boxMaxSide)
			{
				boxMaxSide = boundingBox.height > boundingBox.width ? boundingBox.height : boundingBox.width;
			}

			var uvs = new Vector2[intersectionPoints.Count];
			for (int i = 0; i < intersectionPoints.Count; i++)
			{
				uvs[i] = new Vector2(intersectionPoints[i].x - boundingBox.x, intersectionPoints[i].z - boundingBox.y);
			}

			var mesh = new Mesh();
			mesh.vertices = intersectionPoints.ToArray();
			mesh.triangles = triangles.ToArray();
			mesh.uv = uvs;

			interMeshes.Add(mesh);
			mesh.RecalculateNormals();
			//CreateGameObject(mesh);
		}

		foreach (var mesh in interMeshes)
		{
			var newUvs = new Vector2[mesh.uv.Length];
			for (int i = 0; i < newUvs.Length; i++)
			{
				newUvs[i] = mesh.uv[i] / boxMaxSide;
			}

			mesh.uv = newUvs;
		}

		return interMeshes;
	}

	private IEnumerable<Mesh> GenerateTriIntersections(List<Intersection> intersections)
	{
		List<Mesh> interMeshes = new List<Mesh>();
		var triIntersections =
			intersections.Where(intersection => (intersection.RoadsIn.Count + intersection.RoadsOut.Count) == 3);
		
		var boxMaxSide = 0f;
		foreach (var intersection in triIntersections)
		{
			List<Vector3> cornerPoints = new List<Vector3>();
			Vector3[] roadVectors = new Vector3[3];
			var counter = 0;
			foreach (var road in intersection.RoadsIn)
			{
				roadVectors[counter] = road.Start - road.End;
				//road.MeshEnd = road.End + roadVectors[counter].normalized * ROAD_MESH_OFFSET;
				counter++;
			}
			
			foreach (var road in intersection.RoadsOut)
			{
				roadVectors[counter] = road.End - road.Start;
				//road.MeshStart = road.Start + roadVectors[counter].normalized * ROAD_MESH_OFFSET;
				counter++;
			}
			
			//find the two opposite roads
			int oppositeIdx1 = 0;
			int oppositeIdx2 = 0;
			float maxAngle = 0;
			for (int i = 0; i < roadVectors.Length; i++)
			{
				for (int j = 0; j < roadVectors.Length; j++)
				{
					if (i == j)
					{
						continue;
					}
					var angle = Vector3.Angle(roadVectors[i], roadVectors[j]);
					if (angle > maxAngle)
					{
						maxAngle = angle;
						oppositeIdx1 = i;
						oppositeIdx2 = j;
					}
				}
			}

			var triangles = new List<int>();
			var mainRoadIdx = 0;
			for (int i = 0; i < roadVectors.Length; i++)
			{
				if (i == oppositeIdx1 || i == oppositeIdx2)
				{
					continue;
				}

				mainRoadIdx = i;
				// Vector3.Cross.magnitude --> sin(alfa), alfa --> angle between roadvec[0] and roadvec[i]
				var y = (roadWidth / 2) / Vector3.Cross(roadVectors[i].normalized, roadVectors[oppositeIdx1].normalized).magnitude;
				Vector3 cornerPoint = (roadVectors[i].normalized + roadVectors[oppositeIdx1].normalized) * y + intersection.Center;
				cornerPoints.Add(cornerPoint);

				var point = cornerPoint + roadVectors[i].normalized * (roadWidth / 2);
				cornerPoints.Add(point);

				var point1 = cornerPoint + roadVectors[oppositeIdx1].normalized * (roadWidth / 2);
				cornerPoints.Add(point1);
			}

			for (int i = 0; i < roadVectors.Length; i++)
			{
				if (i == oppositeIdx1 || i == oppositeIdx2)
				{
					continue;
				}

				// Vector3.Cross.magnitude --> sin(alfa), alfa --> angle between roadvec[0] and roadvec[i]
				var y = (roadWidth / 2) / Vector3.Cross(roadVectors[i].normalized, roadVectors[oppositeIdx2].normalized)
					        .magnitude;
				Vector3 cornerPoint = (roadVectors[i].normalized + roadVectors[oppositeIdx2].normalized) * y +
				                      intersection.Center;
				cornerPoints.Add(cornerPoint);

				var point = cornerPoint + roadVectors[i].normalized * (roadWidth / 2);
				cornerPoints.Add(point);

				var point1 = cornerPoint + roadVectors[oppositeIdx2].normalized * (roadWidth / 2);
				cornerPoints.Add(point1);
			}
			
			if (Vector3.Cross(cornerPoints[1] - cornerPoints[0], cornerPoints[3] - cornerPoints[0]).y > 0)
			{
				triangles.Add(0);
				triangles.Add(1);
				triangles.Add(3);
				triangles.Add(1);
				triangles.Add(4);
				triangles.Add(3);
			}
			else
			{
				triangles.Add(1);
				triangles.Add(0);
				triangles.Add(3);
				triangles.Add(1);
				triangles.Add(3);
				triangles.Add(4);
			}
			
			//calculate remaining points
			var perp1 = Vector3.Cross(cornerPoints[0] - cornerPoints[2], Vector3.up);
			if (Vector3.Cross(cornerPoints[0] - cornerPoints[2], roadVectors[mainRoadIdx]).y < 0)
			{
				perp1 = Vector3.Cross(cornerPoints[2] - cornerPoints[0], Vector3.up);
			}
			cornerPoints.Add(cornerPoints[2] + perp1.normalized * roadWidth);
			cornerPoints.Add(cornerPoints[0] + perp1.normalized * roadWidth);
			
			var perp2 = Vector3.Cross(cornerPoints[5] - cornerPoints[3], Vector3.up);
			if (Vector3.Cross(cornerPoints[5] - cornerPoints[3], roadVectors[mainRoadIdx]).y < 0)
			{
				perp2 = Vector3.Cross(cornerPoints[3] - cornerPoints[5], Vector3.up);
			}
			cornerPoints.Add(cornerPoints[3] + perp2.normalized * roadWidth);
			cornerPoints.Add(cornerPoints[5] + perp2.normalized * roadWidth);
			
			if (Vector3.Cross(cornerPoints[0] - cornerPoints[7], cornerPoints[8] - cornerPoints[7]).y > 0)
			{
				triangles.Add(7);
				triangles.Add(0);
				triangles.Add(8);
				triangles.Add(0);
				triangles.Add(3);
				triangles.Add(8);
			}
			else
			{
				triangles.Add(0);
				triangles.Add(7);
				triangles.Add(8);
				triangles.Add(0);
				triangles.Add(8);
				triangles.Add(3);
			}
			
			if (Vector3.Cross(cornerPoints[2] - cornerPoints[6], cornerPoints[7] - cornerPoints[6]).y > 0)
			{
				triangles.Add(6);
				triangles.Add(2);
				triangles.Add(7);
				triangles.Add(2);
				triangles.Add(0);
				triangles.Add(7);
			}
			else
			{
				triangles.Add(2);
				triangles.Add(6);
				triangles.Add(7);
				triangles.Add(2);
				triangles.Add(7);
				triangles.Add(0);
			}
			
			if (Vector3.Cross(cornerPoints[3] - cornerPoints[8], cornerPoints[9] - cornerPoints[8]).y > 0)
			{
				triangles.Add(8);
				triangles.Add(3);
				triangles.Add(9);
				triangles.Add(3);
				triangles.Add(5);
				triangles.Add(9);
			}
			else
			{
				triangles.Add(3);
				triangles.Add(8);
				triangles.Add(9);
				triangles.Add(3);
				triangles.Add(9);
				triangles.Add(5);
			}

			var boundingBox = GetBoundingBox(cornerPoints.ToArray());
			if ((boundingBox.height > boundingBox.width ? boundingBox.height : boundingBox.width) > boxMaxSide)
			{
				boxMaxSide = boundingBox.height > boundingBox.width ? boundingBox.height : boundingBox.width;
			}

			var uvs = new Vector2[cornerPoints.Count];
			for (int i = 0; i < cornerPoints.Count; i++)
			{
				uvs[i] = new Vector2(cornerPoints[i].x - boundingBox.x, cornerPoints[i].z - boundingBox.y);
			}

			var mesh = new Mesh();
			mesh.vertices = cornerPoints.ToArray();
			mesh.triangles = triangles.ToArray();
			mesh.uv = uvs;

			interMeshes.Add(mesh);
			mesh.RecalculateNormals();
		}

		return interMeshes;
	}

	private Rect GetBoundingBox(params Vector3[] points)
	{
		Vector3 max = points[0];
		Vector3 min = points[0];
		foreach (var point in points)
		{
			if (point.x > max.x)
			{
				max.x = point.x;
			}

			if (point.z > max.z)
			{
				max.z = point.z;
			}
			
			if (point.x < min.x)
			{
				min.x = point.x;
			}

			if (point.z < min.z)
			{
				min.z = point.z;
			}
		}
		return new Rect(min.x, min.z, max.x - min.x, max.z - min.z);
	}

	public Mesh GenerateRoad(List<Vector3> points, Terrain terrain = null) {
		// parameters validation
		//ok
		CheckParams(points, smoothingFactor);
		//ok
		if (smoothingFactor > 0.0f) {
			for (int smoothingPass = 0; smoothingPass < smoothingIterations; smoothingPass++) {
				AddSmoothingPoints(points);
			}
		}

		float roadLength = 0;
		for (int i = 0; i < points.Count - 1; i++)
		{
			roadLength += Vector3.Distance(points[i], points[i + 1]);
		}

		// if a terrain parameter was specified, replace the y-coordinate
		// of every point with the height of the terrain (+ an offset)
		if (terrain) {
			AdaptPointsToTerrainHeight(points, terrain);
		}

		Vector3 perpendicularDirection;
		Vector3 nextPoint;
		Vector3 nextNextPoint;
		Vector3 point1;
		Vector3 point2;
		Vector3 cornerPoint1;
		Vector3 cornerPoint2;
		Vector3 tangent;
		Vector3 cornerNormal;

		mesh = new Mesh();
		mesh.name = "Roadifier Road Mesh";

		var vertices = new List<Vector3>();
		var triangles = new List<int>();
		uvs = new Vector2[points.Count * 2];

		var idx= 0;
		float currentLength = 0f;
		// iterate on defined points
		foreach(Vector3 currentPoint in points) {
			// last point
			if (idx == points.Count - 1) {
				// no need to do anything in the last point, all triangles
				// have been created in previous iterations
				break; 
				// utolso elotti point 
			} else if (idx == points.Count - 2) {
				// second to last point, we need to make up a "next next point"
				nextPoint = points[idx + 1];
				// assuming the 'next next' imaginary segment has the same
				// direction as the real last one
				nextNextPoint = nextPoint + (nextPoint - currentPoint);
			} else {
				nextPoint = points[idx + 1];
				nextNextPoint = points[idx + 2];
			}

			Vector3 terrainNormal1 = Vector3.up; // default normal: straight up
			Vector3 terrainNormal2 = Vector3.up; // default normal: straight up
			if (terrain) {
				// if there's a terrain, calculate the actual normals
				RaycastHit hit;
				Ray ray;

				ray = new Ray(currentPoint + Vector3.up, Vector3.down);
				terrain.GetComponent<Collider>().Raycast(ray, out hit, 100.0f);
				terrainNormal1 = hit.normal;

				ray = new Ray(nextPoint + Vector3.up, Vector3.down);
				terrain.GetComponent<Collider>().Raycast(ray, out hit, 100.0f);
				terrainNormal2 = hit.normal;
			}

			// calculate the normal to the segment, so we can displace 'left' and 'right' of
			// the point by half the road width and create our first vertices there
			perpendicularDirection = (Vector3.Cross(terrainNormal1, nextPoint - currentPoint)).normalized;
			point1 = currentPoint + perpendicularDirection * roadWidth * 0.5f;
			point2 = currentPoint - perpendicularDirection * roadWidth * 0.5f;

			// here comes the tricky part...
			// we calculate the tangent to the corner between the current segment and the next
			tangent = ((nextNextPoint - nextPoint).normalized + (nextPoint - currentPoint).normalized).normalized;
			cornerNormal = (Vector3.Cross(terrainNormal2, tangent)).normalized;
			// project the normal line to the corner to obtain the correct length
			var cornerWidth= (roadWidth * 0.5f) / Vector3.Dot(cornerNormal, perpendicularDirection);
			cornerPoint1 = nextPoint + cornerWidth * cornerNormal;
			cornerPoint2 = nextPoint - cornerWidth * cornerNormal;

			// first point has no previous vertices set by past iterations
			if (idx == 0) {
				vertices.Add(point1);
				vertices.Add(point2);
				
				uvs[0] = new Vector2(0, 0);
				uvs[1] = new Vector2(1, 0);
			}
			vertices.Add(cornerPoint1);
			vertices.Add(cornerPoint2);
			
			int doubleIdx = (idx) * 2;
			currentLength += Vector3.Distance(currentPoint, nextPoint);
			//float completionPercent = (idx+1) / (float) (points.Count - 1);
			float completionPercent =  currentLength/ roadLength;
			//float v = 1 - Mathf.Abs(2 * completionPercent - 1);
			uvs[doubleIdx+2] = new Vector2(0, completionPercent);
			uvs[doubleIdx+1+2] = new Vector2(1, completionPercent);
			
			// add first triangle
			triangles.Add(doubleIdx);
			triangles.Add(doubleIdx + 1);
			triangles.Add(doubleIdx + 2);

			// add second triangle
			triangles.Add(doubleIdx + 3);
			triangles.Add(doubleIdx + 2);
			triangles.Add(doubleIdx + 1);

			idx++;
		}

		mesh.SetVertices(vertices);
		//mesh.SetUVs(0, GenerateUVs(vertices));
		mesh.SetUVs(0, uvs.ToList());
		mesh.triangles = triangles.ToArray();
		mesh.RecalculateNormals();
		CreateGameObject(mesh);
		return mesh;
	}

	private void AddSmoothingPoints(List<Vector3> points) {
		for (int i = 0; i < points.Count - 2; i++) {
			Vector3 currentPoint = points[i];
			Vector3 nextPoint = points[i + 1];
			Vector3 nextNextPoint = points[i + 2];

			float distance1 = Vector3.Distance(currentPoint, nextPoint);
			float distance2 = Vector3.Distance(nextPoint, nextNextPoint);

			Vector3 dir1 = (nextPoint - currentPoint).normalized;
			Vector3 dir2 = (nextNextPoint - nextPoint).normalized;

			points.RemoveAt(i + 1);
			points.Insert(i + 1, currentPoint + dir1 * distance1 * (1.0f - smoothingFactor));
			points.Insert(i + 2, nextPoint + dir2 * distance2 * (smoothingFactor));
			i++;
		}
	}

	private void AdaptPointsToTerrainHeight(List<Vector3> points, Terrain terrain) {
		for (int i = 0; i < points.Count; i++) {
			Vector3 point = points[i];
			points[i] = new Vector3(point.x, terrain.transform.position.y + terrainClearance + terrain.SampleHeight(new Vector3(point.x, 0, point.z)), point.z);
		}
	}

	public void  CreateGameObject ( Mesh mesh ){
		var parent = GameObject.Find("Road meshes");
		GameObject obj = new GameObject("Roadifier Road", typeof(MeshRenderer), typeof(MeshFilter), typeof(MeshCollider));
		obj.GetComponent<MeshFilter>().mesh = mesh;
		obj.transform.SetParent(parent.transform);
		obj.transform.position += Vector3.up*0.2f;
		
		MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
		var materials= renderer.sharedMaterials;
		for (int i = 0; i < materials.Length; i++) {
			materials[i] = material;
		}
		renderer.materials = materials;
	}

	private List<Vector2> GenerateUVs(List<Vector3> vertices) {
		List<Vector2> uvs = new List<Vector2>();

		for (int vertIdx = 0; vertIdx < vertices.Count; vertIdx++) {
			if (vertIdx % 4 == 0) {
				uvs.Add(new Vector2(0, 0));
			} else if (vertIdx % 4 == 1) {
				uvs.Add(new Vector2(0, 1));
			} else if (vertIdx % 4 == 2) {
				uvs.Add(new Vector2(1, 0));
			} else {
				uvs.Add(new Vector2(1, 1));
			}
		}
		return uvs;
	}

	private void CheckParams(List<Vector3> points, float smoothingFactor) {
		if (points.Count < 2) {
			throw new Exception("At least two points are required to make a road");
		}

		if (smoothingFactor < 0.0f || smoothingFactor > 0.5f) {
			throw new Exception("Smoothing factor should be between 0 and 0.5f");
		}
	}

	public List<Mesh> GenerateRoadMeshes(List<Intersection> intersections)
	{
		Stack<Road> roadsToVisit = new Stack<Road>();

		foreach (var intersection in intersections)
		{
			if (intersection.RoadsIn.Count + intersection.RoadsOut.Count > 2)
			{
				foreach (var roadOut in intersection.RoadsOut)
				{
					roadsToVisit.Push(roadOut);
				}	
			}
		}

		var meshes = new List<Mesh>();
		var roadPoints = new List<Vector3>();
		while (roadsToVisit.Count != 0)
		{
			var road = roadsToVisit.Pop();
			roadPoints.Add(road.MeshStart);

			if (road.NextIntersection.RoadsIn.Count + road.NextIntersection.RoadsOut.Count > 2
				|| road.NextIntersection.RoadsIn.Count == 1 && road.NextIntersection.RoadsOut.Count == 0)
			{
				roadPoints.Add(road.MeshEnd);
				meshes.Add(GenerateRoad(roadPoints));
				roadPoints.Clear();
			}
			else if(road.NextIntersection.RoadsIn.Count + road.NextIntersection.RoadsOut.Count == 2)
			{
				if (road.NextIntersection.RoadsOut.Count == 1)
				{
					roadsToVisit.Push(road.NextIntersection.RoadsOut[0]);
				}
				else
				{
					roadPoints.Add(road.MeshEnd);
					meshes.Add(GenerateRoad(roadPoints));
					roadPoints.Clear();
				}
			}
		}
		return meshes;
	}
}