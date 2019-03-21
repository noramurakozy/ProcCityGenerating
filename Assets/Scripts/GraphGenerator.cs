using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraphGenerator : MonoBehaviour
{

    [SerializeField]
    private GameObject vertex;

    [SerializeField]
    private LineRenderer edge;

    private int numberOfVertices;
    List<GameObject> vertices;
    List<GraphEdge> edges;

    // Start is called before the first frame update
    void Start()
    {
        numberOfVertices = 50;
        vertices = new List<GameObject>(numberOfVertices);
        edges = new List<GraphEdge>();
        int maxNumberOfEdges = (numberOfVertices * (numberOfVertices - 1) / 2);

        GenerateVertices(numberOfVertices);
        GenerateEdges(maxNumberOfEdges);

        //check intersection
        //CheckIntersection();

        DrawEdges(edges);

    }

    private bool CheckIntersection()
    {
        foreach (GraphEdge e1 in edges)
        {
            foreach (GraphEdge e2 in edges)
            {
                if (lineSegmentsIntersect(e1.Start, e1.End, e2.Start, e2.End) && e1 != e2)
                {
                    e1.Color = Color.blue;
                    e2.Color = Color.blue;
                    //remove from scene and list
                    return true;
                }
            }
        }

        return false;
    }

    private void DrawEdges(List<GraphEdge> edges)
    {
        LineRenderer edgeToDraw;
        foreach(GraphEdge e in edges)
        {
            edgeToDraw = Instantiate(edge);

            edgeToDraw.SetPosition(0, e.Start);
            edgeToDraw.SetPosition(1, e.End);
            edgeToDraw.SetColors(e.Color, e.Color);
        }
    }

    private void GenerateEdges(int max)
    {
        //int log = 0;
        int numberOfEdges = Random.Range(numberOfVertices - 1, max);
        for (int i = 0; i < numberOfEdges; i++)
        {
            GraphEdge e = GenerateRandomEdge();

            while (EdgeAlreadyExist(e))
            {
                //Debug.Log(++log + "new edge");
                e = GenerateRandomEdge();
            }
            edges.Add(e);
        }
    }

    private void GenerateVertices(int numberOfVertices)
    {
        for (int i = 0; i < numberOfVertices; i++)
        {
            int x = Random.Range(-10, 10);
            int z = Random.Range(-10, 10);

            GameObject vert = Instantiate(vertex, new Vector3(x, 0, z), Quaternion.identity);
            vertices.Add(vert);
        }
    }

    private GraphEdge GenerateRandomEdge()
    {
        GraphEdge e = new GraphEdge();

        int indexOfVertexStart = Random.Range(0, numberOfVertices);
        int indexOfVertexEnd = Random.Range(0, numberOfVertices);

        while (indexOfVertexStart == indexOfVertexEnd)
        {
            indexOfVertexStart = Random.Range(0, numberOfVertices);
            indexOfVertexEnd = Random.Range(0, numberOfVertices);  
        }

        e.Start = vertices[indexOfVertexStart].transform.position;
        e.End = vertices[indexOfVertexEnd].transform.position;

        return e;
    }

    private bool EdgeAlreadyExist(GraphEdge edge)
    {
       //the list not contains "edge"
       foreach(GraphEdge e in edges)
       {
          if((e.Start == edge.Start && e.End == edge.End) || (e.Start == edge.End && e.End == edge.Start))
          {
              return true;
          }
       }

       return false;
    }

    private bool lineSegmentsIntersect(Vector3 lineOneA, Vector3 lineOneB, Vector3 lineTwoA, Vector3 lineTwoB)
    {
        bool ret = false;
        if(!(lineOneA == lineTwoA || lineOneA == lineTwoB || lineOneB == lineTwoA || lineOneB == lineTwoB))
        {
            ret = (((lineTwoB.z - lineOneA.z) * (lineTwoA.x - lineOneA.x) > (lineTwoA.z - lineOneA.z) * (lineTwoB.x - lineOneA.x))
            != ((lineTwoB.z - lineOneB.z) * (lineTwoA.x - lineOneB.x) > (lineTwoA.z - lineOneB.z) * (lineTwoB.x - lineOneB.x))
            && ((lineTwoA.z - lineOneA.z) * (lineOneB.x - lineOneA.x) > (lineOneB.z - lineOneA.z) * (lineTwoA.x - lineOneA.x))
            != ((lineTwoB.z - lineOneA.z) * (lineOneB.x - lineOneA.x) > (lineOneB.z - lineOneA.z) * (lineTwoB.x - lineOneA.x)));
        }
        return ret;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
