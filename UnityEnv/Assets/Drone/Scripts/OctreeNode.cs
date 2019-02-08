using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Popcron.Gizmos;
using System;

public enum PointType
{
    DronePos = 0,
    ScanPoint = 1,
    ScanOutOfRange = 2,
    Any = 3
}

public struct Point
{
    public PointType Type;
    public Vector3 Position;
    public float Time { get; private set; }

    public Point(PointType type, Vector3 position, float time)
    {
        Type = type;
        Position = position;
        Time = time;
    }
}

// Code adapted from https://github.com/Nition/UnityOctree
public class OctreeNode
{
    public Vector3 Center { get; private set; }
    public Bounds Bounds = default(Bounds);
    public float Size { get; private set; }
    public int IntersectCount { get; private set; }

    private OctreeNode[] children = null;
    private bool hasChildren => children != null;
    private List<Point> points;
    private bool hasPoints => points != null;
    private float minSize;
    private float sqrHalfDiagonal;
    private Bounds[] childBounds;
    private bool isLeaf;

    public OctreeNode(float baseLengthVal, float minSizeVal, Vector3 centerVal)
    {
        isLeaf = baseLengthVal / 2f < minSizeVal;
        SetValues(baseLengthVal, minSizeVal, centerVal);
    }

    public bool AddPoint(Point point, out OctreeNode leafNode)
    {
        if (!Contains(point.Position))
        {
            leafNode = null;
            return false;
        }
        _AddPoint(point, out leafNode);
        return true;
    }

    public void Intersect(Ray ray, float distance, ref int newLeafNodesCount)
    {
        float d;
        if (Bounds.IntersectRay(ray, out d))
        {
            if (d <= distance)
            {
                if (isLeaf)
                {
                    if (IntersectCount == 0)
                    {
                        newLeafNodesCount++;
                    }
                    IntersectCount++;
                }
                else
                {
                    if (!hasChildren)
                    {
                        Split();
                    }
                    for (int i = 0; i < 8; i++)
                    {
                        children[i].Intersect(ray, distance, ref newLeafNodesCount);
                    }
                }
            }
        }
    }

    // TODO Bounds.ClosestPoint is slow!
    // (Bounds.ClosestPoint(position) - position).sqrMagnitude <= sqrRadius
    // Using half diagonal isn't accurate (adds some extra nodes), but close enough for now.

    public void GetNodesAt(Vector3 position, float sqrRadius, float min, float max, List<OctreeNode> result)
    {
        if (Size >= min)
        {
            if ((Center - position).sqrMagnitude - sqrHalfDiagonal <= sqrRadius)
            {
                if (Size <= max)
                {
                    result.Add(this);
                }

                if (hasChildren)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        children[i].GetNodesAt(position, sqrRadius, min, max, result);
                    }
                }
            }
        }
    }

    public void GetLeafNodesAt(Vector3 position, float sqrRadius, List<OctreeNode> result)
    {
        if ((Center - position).sqrMagnitude - sqrHalfDiagonal <= sqrRadius)
        {
            if (isLeaf)
            {
                if (IntersectCount > 0 || hasPoints)
                {
                    result.Add(this);
                }
            }
            else if (hasChildren)
            {
                for (int i = 0; i < 8; i++)
                {
                    children[i].GetLeafNodesAt(position, sqrRadius, result);
                }
            }
        }
    }

    public void SetChildren(OctreeNode[] childOctrees)
    {
        children = childOctrees;
    }

    public bool Contains(Vector3 point)
    {
        return Bounds.Contains(point);
    }

    public bool GetPoints(PointType type, List<Point> result)
    {
        result.Clear();
        _GetPoints(type, result);
        return result.Count > 0;
    }

    // TODO should match DroneData.GetOctant
    public int GetOctant(Vector3 pos)
    {
        return (pos.x <= Center.x ? 0 : 1) +
               (pos.y >= Center.y ? 0 : 4) +
               (pos.z <= Center.z ? 0 : 2);
    }

    private void _GetPoints(PointType type, List<Point> result)
    {
        if (hasPoints)
        {
            foreach (Point point in points)
            {
                if (type == PointType.Any || type == point.Type)
                {
                    result.Add(point);
                }
            }
        }

        if (hasChildren)
        {
            for (int i = 0; i < 8; i++)
            {
                children[i]._GetPoints(type, result);
            }
        }
    }

    private void _AddPoint(Point point, out OctreeNode leafNode)
    {
        leafNode = null;
        if (!hasChildren)
        {
            if (isLeaf)
            {
                if (!hasPoints)
                {
                    points = new List<Point>();
                }
                points.Add(point);
                leafNode = this;
                return;
            }
            else
            {
                Split();
            }
        }

        children[GetOctant(point.Position)]._AddPoint(point, out leafNode);
    }

    private void SetValues(float sizeVal, float minSizeVal, Vector3 centerVal)
    {
        Size = sizeVal;
        Center = centerVal;
        Bounds = new Bounds(Center, Vector3.one * Size);
        minSize = minSizeVal;

        float half = Size / 2f;
        float halfDiagonal = Mathf.Sqrt(3f) * half;
        sqrHalfDiagonal = halfDiagonal * halfDiagonal;
        float quarter = Size / 4f;
        Vector3 childSize = Vector3.one * half;
        childBounds = new Bounds[8];
        childBounds[0] = new Bounds(Center + new Vector3(-quarter, quarter, -quarter), childSize);
        childBounds[1] = new Bounds(Center + new Vector3(quarter, quarter, -quarter), childSize);
        childBounds[2] = new Bounds(Center + new Vector3(-quarter, quarter, quarter), childSize);
        childBounds[3] = new Bounds(Center + new Vector3(quarter, quarter, quarter), childSize);
        childBounds[4] = new Bounds(Center + new Vector3(-quarter, -quarter, -quarter), childSize);
        childBounds[5] = new Bounds(Center + new Vector3(quarter, -quarter, -quarter), childSize);
        childBounds[6] = new Bounds(Center + new Vector3(-quarter, -quarter, quarter), childSize);
        childBounds[7] = new Bounds(Center + new Vector3(quarter, -quarter, quarter), childSize);
    }

    private void Split()
    {
        float half = Size / 2f;
        float quarter = Size / 4f;
        children = new OctreeNode[8];
        children[0] = new OctreeNode(half, minSize, Center + new Vector3(-quarter, quarter, -quarter));
        children[1] = new OctreeNode(half, minSize, Center + new Vector3(quarter, quarter, -quarter));
        children[2] = new OctreeNode(half, minSize, Center + new Vector3(-quarter, quarter, quarter));
        children[3] = new OctreeNode(half, minSize, Center + new Vector3(quarter, quarter, quarter));
        children[4] = new OctreeNode(half, minSize, Center + new Vector3(-quarter, -quarter, -quarter));
        children[5] = new OctreeNode(half, minSize, Center + new Vector3(quarter, -quarter, -quarter));
        children[6] = new OctreeNode(half, minSize, Center + new Vector3(-quarter, -quarter, quarter));
        children[7] = new OctreeNode(half, minSize, Center + new Vector3(quarter, -quarter, quarter));
    }
}