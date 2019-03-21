using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraphEdge
{
    public Vector3 Start { get; set; }
    public Vector3 End { get; set; }

    public Color Color { get; set; }

    public GraphEdge()
    {
        Color = Color.red;
    }
}
