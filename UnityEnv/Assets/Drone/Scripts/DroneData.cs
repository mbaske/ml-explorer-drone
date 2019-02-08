using UnityEngine;
using System;
using System.Collections.Generic;

public class DroneData
{
    public static int GetOctant(Vector3 center, Vector3 p)
    {
        Vector3 d = p - center;
        return (d.x > 0 ? 1 : 0) + (d.y > 0 ? 2 : 0) + (d.z > 0 ? 4 : 0);
    }

    public static float Normalize(float val)
    {
        return Sigmoid(val);
    }

    public static Vector3 Normalize(Vector3 v)
    {
        return new Vector3(Sigmoid(v.x), Sigmoid(v.y), Sigmoid(v.z));
    }

    // Linear, Max distance is 10.
    public static float NormalizeDistance(float v)
    {
        return Mathf.Min(1f, v * 0.2f - 1f);
    }

    private static float Sigmoid(float val)
    {
        return 2f / (1f + Mathf.Exp(-2f * val)) - 1f;
    }

    public Octree Tree { get; private set; }
    public float LeafNodeSize { get; private set; }
    public float LookRadius { get; private set; }
    public float LookRadiusNorm { get; private set; }

    // Populated leafs by point type and node position.
    public Dictionary<PointType, Dictionary<Vector3, int>> LeafNodeInfo;
    // Local octants:
    // Accumulated leaf node volume / volume.
    public float[] NodeDensities { get; private set; }
    // Number of leaf nodes / accumulated number of node intersections.
    public float[] IntersectRatios { get; private set; }

    private float localVolume; // sphere
    private float partialVolume; // 1/8 sphere
    private float leafNodeVolume;

    private int[] nodeCounts = new int[8];
    private int[] intersectCounts = new int[8];
    // Leafs within LookRadius from drone position.
    private List<OctreeNode> localLeafNodes;

    public DroneData()
    {
        localLeafNodes = new List<OctreeNode>();
        LeafNodeInfo = new Dictionary<PointType, Dictionary<Vector3, int>>()
        {
            { PointType.DronePos, new Dictionary<Vector3, int>() },
            { PointType.ScanPoint, new Dictionary<Vector3, int>() }
        };
    }

    public void Reset(Vector3 dronePos, float lookRadius, float leafNodeSize)
    {
        LeafNodeSize = leafNodeSize;
        leafNodeVolume = leafNodeSize * leafNodeSize * leafNodeSize;
        Tree = new Octree(16, dronePos, leafNodeSize);
        LeafNodeInfo[PointType.DronePos].Clear();
        LeafNodeInfo[PointType.ScanPoint].Clear();
        localLeafNodes.Clear();
        LookRadius = lookRadius;
        LookRadiusNorm = NormalizeDistance(lookRadius);
        localVolume = (4f / 3f) * Mathf.PI * Mathf.Pow(lookRadius, 3f);
        partialVolume = localVolume / 8f;
        NodeDensities = new float[8];
        IntersectRatios = new float[8];
    }

    public void AddPoint(Point point)
    {
        OctreeNode node = Tree.AddPoint(point);

        if (LeafNodeInfo.ContainsKey(point.Type))
        {
            if (!LeafNodeInfo[point.Type].ContainsKey(node.Center))
            {
                LeafNodeInfo[point.Type].Add(node.Center, 0);
            }
            LeafNodeInfo[point.Type][node.Center]++;
        }
    }

    public void StepUpdate(Vector3 dronePos)
    {
        if (Tree.GetLeafNodesAt(dronePos, LookRadius, localLeafNodes))
        {
            Array.Clear(nodeCounts, 0, 8);
            Array.Clear(intersectCounts, 0, 8);
            foreach (OctreeNode node in localLeafNodes)
            {
                int o = GetOctant(dronePos, node.Center);
                nodeCounts[o]++;
                intersectCounts[o] += node.IntersectCount;
            }

            Array.Clear(IntersectRatios, 0, 8);
            for (int i = 0; i < 8; i++)
            {
                NodeDensities[i] = (nodeCounts[i] * leafNodeVolume) / partialVolume;
                NodeDensities[i] = NodeDensities[i] * 2f - 1f; // Normalize linear.

                if (nodeCounts[i] > 0)
                {
                    IntersectRatios[i] = nodeCounts[i] / Mathf.Max(1f, (float)intersectCounts[i]);
                    // Normalize linear below 0 (more intersects than nodes), quadratic inv. above.
                    IntersectRatios[i] = IntersectRatios[i] < 1f ?
                        IntersectRatios[i] - 1f : Mathf.Pow(1f - 1f / IntersectRatios[i], 2f);

                }
            }
        }
    }
}
