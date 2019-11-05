using System.Drawing;
using UnityEngine;

public class IntersectionSegment
{
    public Vector3 StartPoint { get; set; }
    public Vector3 EndPoint { get; set; }
    /**
     * 0: close to center - left
     * 1: close to center - right
     * 2: far from center - left
     * 3: far from center - right
     */
    public Vector3[] RectCornerPoints { get; set; }

    public Rect Rectangle =>
//        new Rect(
//            (int)RectCornerPoints[0].x, 
//            (int)RectCornerPoints[0].z, 
//            (int)Roadifier.roadWidth, 
//            (int)Vector3.Distance(RectCornerPoints[0], RectCornerPoints[2]));
        Rect.zero;
}
