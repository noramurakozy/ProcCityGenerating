using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoadView : MonoBehaviour
{
    public Road Road { get; set; }
    private LineRenderer Line { get; set; }

    public void Draw()
    {
        Line.SetPosition(0, Road.Start);
        Line.SetPosition(1, Road.End);
    }

    void Awake()
    {
        Line = GetComponent<LineRenderer>();
    }
}
