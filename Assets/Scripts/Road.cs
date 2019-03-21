using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Road
{
    public Vector3 Start { get; set; }
    public Vector3 End { get; set; }
    public float DirectionAngle { get; set; }
    public int Number { get; set; }
    public float Length { get => (End - Start).magnitude; }
    public bool IsHighway { get; set; }
    public float Population { get; set; }
    
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
