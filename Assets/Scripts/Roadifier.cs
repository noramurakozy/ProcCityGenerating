// Converted from UnityScript to C# at http://www.M2H.nl/files/js_to_c.php - by Mike Hergaarden
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
	public float smoothingFactor = 0.2f;
	public int smoothingIterations = 3;
	private Material material;
	public static float terrainClearance = 0.07f;
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

			roadVectors = SortVectorsBasedOnAngle(roadVectors);

			var triangles = new List<int>();
			for (var i = 0; i < roadVectors.Length; i++)
			{
				int nextIdx = (i+1) % roadVectors.Length;
				// Vector3.Cross.magnitude --> sin(alfa), alfa --> angle between roadvec[0] and roadvec[i]
				//var y = (roadWidth / 2) / Vector3.Cross(roadVectors[i].normalized, roadVectors[nextIdx].normalized).magnitude;
				Vector3 cornerPoint;
				if (Mathf.Approximately(Mathf.Abs(Vector3.Dot(roadVectors[i].normalized, roadVectors[nextIdx].normalized)),1))
				{
					cornerPoint = Vector3.Cross(roadVectors[nextIdx], Vector3.up).normalized * (roadWidth / 2) + intersection.Center;
				}
				else
				{
					var signedAngle1 = Vector3.SignedAngle(roadVectors[i], roadVectors[nextIdx], Vector3.up);
					var y = (roadWidth / 2) / Mathf.Sin((signedAngle1/2) * Mathf.Deg2Rad);
					cornerPoint = (roadVectors[i].normalized + roadVectors[nextIdx].normalized).normalized * y +
					              intersection.Center;
				}
				cornerPoints.Add(cornerPoint);
			}

			var intersectionPoints = new List<Vector3>(cornerPoints);
			for (int i = 0; i < cornerPoints.Count; i++)
			{
				int nextI = (i+1) % cornerPoints.Count;
				Vector3 d = (intersection.Center + roadVectors[nextI].normalized * ROAD_MESH_OFFSET)
				            - (cornerPoints[i] + cornerPoints[nextI]) / 2;


				Vector3 x = cornerPoints[i] - cornerPoints[nextI];
				intersectionPoints.Add(cornerPoints[i] + (d -
				                                          x.magnitude / 2 * Vector3.Dot(x.normalized, roadVectors[nextI].normalized) *
				                                          roadVectors[nextI].normalized));
				intersectionPoints.Add(cornerPoints[nextI] + (d +
				                                              x.magnitude / 2 *
				                                              Vector3.Dot(x.normalized, roadVectors[nextI].normalized) *
				                                              roadVectors[nextI].normalized));
			}

			{
				triangles.Add(0);
				triangles.Add(1);
				triangles.Add(2);
				triangles.Add(2);
				triangles.Add(3);
				triangles.Add(0);

				triangles.Add(0);
				triangles.Add(4);
				triangles.Add(5);
				triangles.Add(0);
				triangles.Add(5);
				triangles.Add(1);

				triangles.Add(1);
				triangles.Add(6);
				triangles.Add(7);
				triangles.Add(1);
				triangles.Add(7);
				triangles.Add(2);

				triangles.Add(2);
				triangles.Add(8);
				triangles.Add(9);
				triangles.Add(2);
				triangles.Add(9);
				triangles.Add(3);

				triangles.Add(3);
				triangles.Add(10);
				triangles.Add(11);
				triangles.Add(3);
				triangles.Add(11);
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

	private Vector3[] SortVectorsBasedOnAngle(Vector3[] roadVectors)
	{
		Array.Sort(roadVectors, (v1, v2) =>
		{
			var signedAngle1 = Vector3.SignedAngle(roadVectors[0], v1, Vector3.up);
			var signedAngle2 = Vector3.SignedAngle(roadVectors[0], v2, Vector3.up);
			var angle1 = signedAngle1 >= 0 ? signedAngle1 : 360 + signedAngle1;
			var angle2 = signedAngle2 >= 0 ? signedAngle2 : 360 + signedAngle2;
			return angle1.CompareTo(angle2);
		});

		return roadVectors;
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
				road.MeshEnd = road.End + roadVectors[counter].normalized * ROAD_MESH_OFFSET;
				counter++;
			}
			
			foreach (var road in intersection.RoadsOut)
			{
				roadVectors[counter] = road.End - road.Start;
				road.MeshStart = road.Start + roadVectors[counter].normalized * ROAD_MESH_OFFSET;
				counter++;
			}
			
			roadVectors = SortVectorsBasedOnAngle(roadVectors);

			var triangles = new List<int>();
			for (var i = 0; i < roadVectors.Length; i++)
			{
				int nextIdx = (i+1) % roadVectors.Length;
				// Vector3.Cross.magnitude --> sin(alfa), alfa --> angle between roadvec[0] and roadvec[i]
				//var y = (roadWidth / 2) / Vector3.Cross(roadVectors[i].normalized, roadVectors[nextIdx].normalized).magnitude;
				Vector3 cornerPoint;
				if (Mathf.Approximately(Mathf.Abs(Vector3.Dot(roadVectors[i].normalized, roadVectors[nextIdx].normalized)),1))
				{
					cornerPoint = Vector3.Cross(roadVectors[nextIdx], Vector3.up).normalized * (roadWidth / 2) + intersection.Center;
				}
				else
				{
					var signedAngle1 = Vector3.SignedAngle(roadVectors[i], roadVectors[nextIdx], Vector3.up);
					var y = (roadWidth / 2) / Mathf.Sin(signedAngle1 * Mathf.Deg2Rad);
					cornerPoint = (roadVectors[i].normalized + roadVectors[nextIdx].normalized) * y +
					              intersection.Center;
				}
				cornerPoints.Add(cornerPoint);
			}
			
			var intersectionPoints = new List<Vector3>(cornerPoints);
			for (int i = 0; i < cornerPoints.Count; i++)
			{
				int nextI = (i+1) % cornerPoints.Count;
				Vector3 d = (intersection.Center + roadVectors[nextI].normalized * ROAD_MESH_OFFSET)
				            - (cornerPoints[i] + cornerPoints[nextI]) / 2;


				Vector3 x = cornerPoints[i] - cornerPoints[nextI];
				intersectionPoints.Add(cornerPoints[i] + (d -
				                                          x.magnitude / 2 *
				                                          Vector3.Dot(x.normalized, roadVectors[nextI].normalized) *
				                                          roadVectors[nextI].normalized));
				intersectionPoints.Add(cornerPoints[nextI] + (d +
				                                              x.magnitude / 2 *
				                                              Vector3.Dot(x.normalized, roadVectors[nextI].normalized) *
				                                              roadVectors[nextI].normalized));
			}
			
			{
				triangles.Add(0);
				triangles.Add(1);
				triangles.Add(2);
				
				triangles.Add(1);
				triangles.Add(3);
				triangles.Add(4);
				triangles.Add(1);
				triangles.Add(0);
				triangles.Add(3);
				
				triangles.Add(2);
				triangles.Add(7);
				triangles.Add(0);
				triangles.Add(7);
				triangles.Add(8);
				triangles.Add(0);
				
				triangles.Add(2);
				triangles.Add(1);
				triangles.Add(6);
				triangles.Add(6);
				triangles.Add(1);
				triangles.Add(5);
			}

			var boundingBox = GetBoundingBox(intersectionPoints.ToArray());
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
		CheckParams(points, smoothingFactor);
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
			Vector3 nextNextPoint;
			Vector3 nextPoint;
			if (idx == points.Count - 1) {
				// no need to do anything in the last point, all triangles
				// have been created in previous iterations
				break; 
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
			if (terrain != null) {
				var terrainCollider = terrain.GetComponent<Collider>();
				// if there's a terrain, calculate the actual normals
				RaycastHit hit;
				Ray ray;

				ray = new Ray(currentPoint + Vector3.up, Vector3.down);
				terrainCollider.Raycast(ray, out hit, 100.0f);
				terrainNormal1 = hit.normal;

				ray = new Ray(nextPoint + Vector3.up, Vector3.down);
				terrainCollider.Raycast(ray, out hit, 100.0f);
				terrainNormal2 = hit.normal;
			}

			// calculate the normal to the segment, so we can displace 'left' and 'right' of
			// the point by half the road width and create our first vertices there
			var perpendicularDirection = (Vector3.Cross(terrainNormal1, nextPoint - currentPoint)).normalized;
			var point1 = currentPoint + perpendicularDirection * roadWidth * 0.5f;
			var point2 = currentPoint - perpendicularDirection * roadWidth * 0.5f;

			// here comes the tricky part...
			// we calculate the tangent to the corner between the current segment and the next
			var tangent = ((nextNextPoint - nextPoint).normalized + (nextPoint - currentPoint).normalized).normalized;
			var cornerNormal = (Vector3.Cross(terrainNormal2, tangent)).normalized;
			// project the normal line to the corner to obtain the correct length
			var cornerWidth= (roadWidth * 0.5f) / Vector3.Dot(cornerNormal, perpendicularDirection);
			var cornerPoint1 = nextPoint + cornerWidth * cornerNormal;
			var cornerPoint2 = nextPoint - cornerWidth * cornerNormal;

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
			float completionPercent =  currentLength/ roadLength;
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
		mesh.SetUVs(0, uvs.ToList());
		mesh.triangles = triangles.ToArray();
		mesh.RecalculateNormals();
		//CreateGameObject(mesh);
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

	public static void AdaptPointsToTerrainHeight(Vector3[] points, Terrain terrain) {
		
		for (int i = 0; i < points.Length; i++) {
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
				meshes.Add(GenerateRoad(roadPoints/*, Terrain.activeTerrain*/));
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
					meshes.Add(GenerateRoad(roadPoints/*, Terrain.activeTerrain*/));
					roadPoints.Clear();
				}
			}
		}
		return meshes;
	}
}