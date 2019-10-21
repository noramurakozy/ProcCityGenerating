using System;
using UnityEngine;

public class MathHelper
{
    // Calculate the distance between
    // point pt and the segment p1 --> p2.
    public static double FindDistanceToSegment(Vector3 point, Vector3 segmentStart, Vector3 segmentEnd, out float t, out Vector3 closest)
    {
        var dx = segmentEnd.x - segmentStart.x;
        var dz = segmentEnd.z - segmentStart.z;

        // Calculate the t that minimizes the distance.
        t = ((point.x - segmentStart.x) * dx + (point.z - segmentStart.z) * dz) /
            (dx * dx + dz * dz);

        closest = new Vector3(segmentStart.x + t * dx, 0, segmentStart.z + t * dz);
        dx = point.x - closest.x;
        dz = point.z - closest.z;
        
        return Math.Sqrt(dx * dx + dz * dz);
    }
    
    public static bool LineSegmentsIntersect(Vector3 lineOneA, Vector3 lineOneB, Vector3 lineTwoA, Vector3 lineTwoB)
    {
        var ret = false;
        if (!(lineOneA == lineTwoA || lineOneA == lineTwoB || lineOneB == lineTwoA || lineOneB == lineTwoB))
        {
            ret = (((lineTwoB.z - lineOneA.z) * (lineTwoA.x - lineOneA.x) > (lineTwoA.z - lineOneA.z) * (lineTwoB.x - lineOneA.x))
                   != ((lineTwoB.z - lineOneB.z) * (lineTwoA.x - lineOneB.x) > (lineTwoA.z - lineOneB.z) * (lineTwoB.x - lineOneB.x))
                   && ((lineTwoA.z - lineOneA.z) * (lineOneB.x - lineOneA.x) > (lineOneB.z - lineOneA.z) * (lineTwoA.x - lineOneA.x))
                   != ((lineTwoB.z - lineOneA.z) * (lineOneB.x - lineOneA.x) > (lineOneB.z - lineOneA.z) * (lineTwoB.x - lineOneA.x)));
        }
        return ret;
    }
    
    public static float MinDegreeDifference(float firstDeg, float secDeg)
    {
        var diff = Math.Abs(firstDeg - secDeg) % 180.0f;
        return Math.Min(diff, Math.Abs(diff - 180.0f));
    }
    
    public static bool PointsAreDifferent(Vector3 road1Start, Vector3 road1End, Vector3 road2Start, Vector3 road2End)
    {
        return !road1Start.Equals(road2Start)
               && !road1Start.Equals(road2End)
               && !road1End.Equals(road2Start)
               && !road1End.Equals(road2End);
    }
    
    //Filter those rads that are (almost) parallel: ||
    public static bool SegmentsAreAlmostParallel(Vector3 start1, Vector3 start2, Vector3 end1, Vector3 end2, float distance)
    {
        return (PointsAreClose(start1, start2, distance) 
                && PointsAreClose(end1, end2, distance) 
                || PointsAreClose(start1, end2, distance) 
                && PointsAreClose(start2, end1, distance));
    }
    
    public static bool PointsAreClose(Vector3 point1, Vector3 point2, float distance)
    {
        return Vector3.Distance(point1, point2) <= distance;
    }
}
