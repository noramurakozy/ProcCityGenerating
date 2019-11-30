using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class PlaneGenerator : MonoBehaviour
{
    private Vector3[] vertices;

    /*private void OnDrawGizmos()
    {
        if (vertices == null)
        {
            return;
        }
        Gizmos.color = Color.black;
        for (int i = 0; i < vertices.Length; i++)
        {
            Gizmos.DrawSphere(vertices[i], 0.1f);
        }
    }*/

    public void Generate(int xSize, int zSize, PlaneType planeType)
    {
        Mesh mesh;
        GetComponent<MeshFilter>().mesh = mesh = new Mesh();
        mesh.name = "Procedural Grid";
        mesh.indexFormat = IndexFormat.UInt32;
        vertices = new Vector3[(xSize + 1) * (zSize + 1)];
        var uv = new Vector2[(xSize + 1) * (zSize + 1)];
        for (int i = 0, y = 0; y <= zSize; y++)
        {
            for (var x = 0; x <= xSize; x++, i++)
            {
                vertices[i] = new Vector3(x, 0, y);
                uv[i] = new Vector2((float) x / xSize, (float) y / zSize);
            }
        }
        mesh.vertices = vertices;
        mesh.uv = uv;

        var triangles = new int[xSize * zSize * 6];
        for (int ti = 0, vi = 0, y = 0; y < zSize; y++, vi++)
        {
            for (var x = 0; x < xSize; x++, ti += 6, vi++)
            {
                triangles[ti] = vi;
                triangles[ti + 3] = triangles[ti + 2] = vi + 1;
                triangles[ti + 4] = triangles[ti + 1] = vi + xSize + 1;
                triangles[ti + 5] = vi + xSize + 2;
            }
        }
        mesh.triangles = triangles;
        var colors = new Color[(zSize + 1) * (xSize + 1)];
        for (int i = 0, y = 0; y <= zSize; y++)
        {
            for (var x = 0; x <= xSize; x++)
            {
                var result = GetHeatMapAt(x,y, planeType);
                colors[i++] = new Color(5 * (float)result / 255.0f, 40 * (float)result / 255.0f, 40 / 255.0f);
            }
        }
        mesh.colors = colors;
        transform.position = new Vector3(0, -2, 0);
    }

    public static double GetHeatMapAt(float i, float j, PlaneType planeType)
    {
        //return (Mathf.PerlinNoise(i / 30f, j / 30f) * 8);
        switch (planeType)
        {
            case PlaneType.Population:
                return Mathf.PerlinNoise(i / 60f, j / 60f) * 10;
            case PlaneType.District:
                return Mathf.PerlinNoise(i / 35f+10, j / 35f+10) * 10;
            default:
                return Mathf.PerlinNoise(i / 60f, j / 60f) * 10;
        }
    }

//    private Color[] DrawHeatMap(int width, int height)
//    {
//        var k = 0;
//        var colors = new Color[(width + 1) * (height + 1)];
//        for (var i = 0; i < width; i++)
//        {
//            for (var j = 0; j < height; j++)
//            {
//                var result = GetHeatMapAt(i, j);
//                colors[k++] = new Color(5 * (float)result / 255.0f, 25.5f * (float)result / 255.0f, 40 / 255.0f);
//            }
//        }
//        return colors;
//    }

}
