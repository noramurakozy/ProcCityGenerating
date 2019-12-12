using System.Collections.Generic;
using UnityEngine;

public class Intersection
{
    public List<Road> RoadsIn { get; set; }
    public List<Road> RoadsOut { get; set; }
    public Vector3 Center { get; set; }

    public Intersection(List<Road> roadsIn, List<Road> roadsOut, Vector3 center)
    {
        RoadsIn = roadsIn;
        RoadsOut = roadsOut;
        Center = center;
    }
}
