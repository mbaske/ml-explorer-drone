using System.Collections.Generic;
using UnityEngine;
using System;

// Code adapted from https://github.com/Nition/UnityOctree
public class Octree
{
    private OctreeNode rootNode;
    private readonly float initialSize;
    private readonly float minSize;

    public Octree(float size, Vector3 pos, float minNodeSize)
    {
        initialSize = size;
        minSize = minNodeSize;
        rootNode = new OctreeNode(initialSize, minSize, pos);
    }

    public OctreeNode AddPoint(Point point)
    {
        OctreeNode leafNode;
        while (!rootNode.AddPoint(point, out leafNode))
        {
            Grow(point.Position - rootNode.Center);
        }
        return leafNode;
    }

    public int Intersect(Vector3 start, Vector3 end)
    {
        int newLeafNodesCount = 0;
        Vector3 d = end - start;
        rootNode.Intersect(new Ray(start, d), d.magnitude, ref newLeafNodesCount);
        return newLeafNodesCount;
    }

    public bool GetNodesAt(Vector3 position, float radius, float min, float max, List<OctreeNode> result)
    {
        result.Clear();
        rootNode.GetNodesAt(position, radius * radius, min, max, result);
        return result.Count > 0;
    }

    public bool GetLeafNodesAt(Vector3 position, float radius, List<OctreeNode> result)
    {
        result.Clear();
        rootNode.GetLeafNodesAt(position, radius * radius, result);
        return result.Count > 0;
    }

    private void Grow(Vector3 direction)
    {
        direction.x = direction.x >= 0 ? 1 : -1;
        direction.y = direction.y >= 0 ? 1 : -1;
        direction.z = direction.z >= 0 ? 1 : -1;
        OctreeNode oldRoot = rootNode;
        float halfSize = rootNode.Size / 2;
        float doubleSize = rootNode.Size * 2;
        Vector3 newCenter = rootNode.Center + direction * halfSize;
        rootNode = new OctreeNode(doubleSize, minSize, newCenter);
        int rootPos = rootNode.GetOctant(oldRoot.Center);
        OctreeNode[] children = new OctreeNode[8];
        for (int i = 0; i < 8; i++)
        {
            if (i == rootPos)
            {
                children[i] = oldRoot;
            }
            else
            {
                direction.x = i % 2 == 0 ? -1 : 1;
                direction.y = i > 3 ? -1 : 1;
                direction.z = (i < 2 || (i > 3 && i < 6)) ? -1 : 1;
                children[i] = new OctreeNode(oldRoot.Size, minSize, newCenter + direction * halfSize);
            }
        }
        rootNode.SetChildren(children);
    }
}