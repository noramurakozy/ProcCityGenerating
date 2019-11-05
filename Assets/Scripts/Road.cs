using QuadTreeLib;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Drawing;
using System;
using QuadTrees.QTreeRect;

[Serializable]
public class Road : IRectQuadStorable
{
    public Vector3 Start { get; set; }
    public Vector3 End { get; set; }
    public float DirectionAngle { get; set; }
    public int Number { get; set; }
    public float Length => (End - Start).magnitude;
    public bool IsHighway { get; set; }
    public float Population { get; set; }
    public UnityEngine.Color Color { get; set; } = UnityEngine.Color.red;
    public Road NextRoad { get; set; }
    public Road PrevRoad { get; set; }

    public RoadType Type { get; set; }

    public int addedToQtreeTime;
    
    // TODO: somehow get from RoadNetworkDescriptor
    private float roadSnapDistance = 1;

    //Quadtree
    public RectangleF Rectangle => new RectangleF(Bounds.x, Bounds.y, Bounds.width, Bounds.height);

    private RectangleF StartRectangle
    {
        get
        {
            var r = new Rect {width = 0.5f, height = 0.5f, center = new Vector2(Start.x, Start.z)};
            return new RectangleF(r.x, r.y, r.width, r.height);
        }
    }
    
    public RectangleF EndRectangle
    {
        get
        {
            var r = new Rect {width = 0.5f, height = 0.5f, center = new Vector2(End.x, End.z)};
            return new RectangleF(r.x, r.y, r.width, r.height);
        }
    }
    
    public Vector3 GetStartRectangleCenter()
    {
        return new Vector3(StartRectangle.X + StartRectangle.Width / 2,
            1,StartRectangle.Y + StartRectangle.Height / 2);
    }

    public Rect Bounds {
        get
        {
            var r = new Rect
            {
                width = Math.Abs(End.x - Start.x) + roadSnapDistance,
                height = Math.Abs(End.z - Start.z) + roadSnapDistance,
                center = new Vector2((Start.x + End.x) / 2, (Start.z + End.z) / 2)
            };
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
