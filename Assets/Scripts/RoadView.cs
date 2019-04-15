using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoadView : MonoBehaviour
{
    public Road road;

    public Road Road { get => road; set => road = value; }

    private LineRenderer Line { get; set; }

    public void Draw()
    {
        Line.SetPosition(0, Road.Start);
        Line.SetPosition(1, Road.End);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(new Vector3(road.Bounds.center.x, 1, road.Bounds.center.y), new Vector3(road.Bounds.size.x, 1, road.Bounds.size.y));

    }

    void Awake()
    {
        Line = GetComponent<LineRenderer>();
    }
}
