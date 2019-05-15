using QuadTreeLib;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Drawing;
using System;
using QuadTrees.QTreeRect;

[Serializable]
public class Road : IHasRect, IRectQuadStorable
{
    public Vector3 Start { get; set; }
    public Vector3 End { get; set; }
    public float DirectionAngle; //{ get; set; }
    public int Number { get; set; }
    public float Length { get => (End - Start).magnitude; }
    public bool IsHighway { get; set; }
    public float Population { get; set; }
    public UnityEngine.Color color { get; set; } = UnityEngine.Color.red;

    public int AddedToQtreeTime;
    //Quadtree
    public RectangleF Rectangle
    {
        get
        {
            return new RectangleF(Bounds.x, Bounds.y, Bounds.width, Bounds.height);
        }
    }

    public RectangleF StartRectangle
    {
        get
        {
            var r = new Rect();
            r.width = 0.5f;
            r.height = 0.5f;
            r.center = new Vector2(Start.x, Start.z);

            return new RectangleF(r.x, r.y, r.width, r.height);
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

    public RectangleF Rect => Rectangle;

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
