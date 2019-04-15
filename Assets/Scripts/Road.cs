using QuadTreeLib;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Drawing;
using System;

[Serializable]
public class Road : IHasRect
{
    public Vector3 Start { get; set; }
    public Vector3 End { get; set; }
    public float DirectionAngle { get; set; }
    public int Number { get; set; }
    public float Length { get => (End - Start).magnitude; }
    public bool IsHighway { get; set; }
    public float Population { get; set; }
    public UnityEngine.Color color { get; set; } = UnityEngine.Color.red;

    //Quadtree
    public RectangleF Rectangle
    {
        get
        {
            return new RectangleF(Bounds.x, Bounds.y, Bounds.width, Bounds.height);
        }
    }

    public Rect Bounds {
        get
        {
            var r = new Rect();
            r.width = Math.Abs(End.x - Start.x);
            r.height = Math.Abs(End.z - Start.z);
            r.center = new Vector2((Start.x + End.x)/2, (Start.z+End.z)/2);
            return r;
        }
    }

    public static Road RoadWithDirection(Vector3 start, float directionAngle, float length, int number, bool isHighway)
    {
        return new Road()
        {
            Start = start,
            End = start + Quaternion.Euler(0, directionAngle, 0) * Vector3.right * length,
            DirectionAngle = directionAngle,
            Number = number,
            IsHighway = isHighway
        };
    }
}
