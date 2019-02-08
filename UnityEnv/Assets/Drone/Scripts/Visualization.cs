using UnityEngine;
using System.Collections.Generic;
using Popcron.Gizmos;

public enum DrawFilter : int
{
    World = 1 << 0,
    Octree = 1 << 1,
    LeafNodes = 1 << 2,
    Points = 1 << 3,
    ScanDensity = 1 << 4
}

public class Visualization : MonoBehaviour
{
    [SerializeField]
    [BitMask(typeof(DrawFilter))]
    private DrawFilter drawFilter;
    [SerializeField]
    private PointType pointFilter;
    [SerializeField]
    [Range(2f, 32f)]
    private float drawRadius = 8f;
    private float sqrRadius;
    private Vector3 center;
    [SerializeField]
    [Range(0.25f, 32f)]
    private float minNodeSize = 2f;
    [SerializeField]
    [Range(0.25f, 32f)]
    private float maxNodeSize = 32f;

    private DroneAgent agent;
    private List<Point> points;
    private List<OctreeNode> nodes;
    private LeafNodeDrawer leafNodeDrawer;
    private bool isReady => agent != null;

    private void OnValidate()
    {
        if (isReady)
        {
            agent.Cam.gameObject.SetActive((int)drawFilter > 0);
            agent.World.SetVisible(drawFilter.HasFlag(DrawFilter.World));
        }

        minNodeSize = Mathf.Pow(2f, Mathf.Round(Mathf.Log(minNodeSize, 2f)));
        maxNodeSize = Mathf.Pow(2f, Mathf.Round(Mathf.Log(maxNodeSize, 2f)));
        maxNodeSize = Mathf.Max(maxNodeSize, minNodeSize);
        sqrRadius = drawRadius * drawRadius;

        if (drawFilter.HasFlag(DrawFilter.LeafNodes))
        {
            if (pointFilter == PointType.ScanOutOfRange || pointFilter == PointType.Any)
            {
                Debug.LogWarning("Leaf nodes are not drawn for point type " + pointFilter);
            }
        }
    }

    private void Start()
    {
        agent = GetComponent<DroneAgent>();
        points = new List<Point>();
        nodes = new List<OctreeNode>();
        OnValidate();
    }

    private void LateUpdate()
    {
        if ((int)drawFilter > 0)
        {
            center = agent.Drone.Position;

            if (drawFilter.HasFlag(DrawFilter.Octree))
            {
                agent.Data.Tree.GetNodesAt(center, drawRadius, minNodeSize, maxNodeSize, nodes);
                DrawBounds();
            }

            if (drawFilter.HasFlag(DrawFilter.Points) || drawFilter.HasFlag(DrawFilter.ScanDensity))
            {
                // TODO This call is redundant in case radius <= DroneData.lookRadius
                // Should rather use DroneData's localNodes list.
                agent.Data.Tree.GetLeafNodesAt(center, drawRadius, nodes);

                if (drawFilter.HasFlag(DrawFilter.Points))
                {
                    DrawPoints();
                }
                if (drawFilter.HasFlag(DrawFilter.ScanDensity))
                {
                    DrawScanDensity();
                }
            }

            if (drawFilter.HasFlag(DrawFilter.LeafNodes))
            {
                if (leafNodeDrawer == null)
                {
                    leafNodeDrawer = new LeafNodeDrawer(agent.Data.LeafNodeSize);
                }

                if (pointFilter == PointType.DronePos)
                {
                    DrawLeafNodes(agent.Data.LeafNodeInfo[pointFilter],
                        new ColorGradient(Color.yellow, Color.white));
                }
                else if (pointFilter == PointType.ScanPoint)
                {
                    DrawLeafNodes(agent.Data.LeafNodeInfo[pointFilter],
                        new ColorGradient(Color.green, Color.white));
                }
            }
        }
    }

    private void DrawBounds()
    {
        ColorGradient cg = new ColorGradient(Color.red, Color.blue);
        foreach (OctreeNode node in nodes)
        {
            if (node.GetPoints(pointFilter, points))
            {
                // Assuming node size range between 0.25 and 64
                float t = Mathf.Log(node.Size * 4f, 2f) / 8f;
                float sqrDist = (center - node.Center).sqrMagnitude;
                float dim = Mathf.Max(0f, 1f - sqrDist / sqrRadius);
                Color col = cg.GetColor(t, dim);
                // Slight offset to prevent overlapping lines.
                Vector3 size = Vector3.one * (node.Size - t * 0.01f);
                Gizmos.Cube(node.Center, Quaternion.identity, size, col);
            }
        }
    }

    private void DrawPoints()
    {
        Vector3 size = Vector3.one * 0.02f;
        Dictionary<PointType, Color> colors = new Dictionary<PointType, Color>()
        {
            { PointType.DronePos, Color.yellow },
            { PointType.ScanPoint, Color.green },
            { PointType.ScanOutOfRange, Color.red }
        };

        foreach (OctreeNode node in nodes)
        {
            if (node.GetPoints(pointFilter, points))
            {
                foreach (Point point in points)
                {
                    Gizmos.Cube(point.Position, Quaternion.identity, size, colors[point.Type]);
                }
            }
        }
    }

    private void DrawScanDensity()
    {
        float length = agent.Data.LeafNodeSize;
        ColorGradient cg = new ColorGradient(Color.green, Color.white);
        foreach (OctreeNode node in nodes)
        {
            float t = Mathf.Min(1f, node.IntersectCount * 0.1f);
            Vector3 line = (node.Center - center).normalized * length * t;
            Gizmos.Line(node.Center, node.Center + line, cg.GetColor(t));
        }
    }

    private void DrawLeafNodes(Dictionary<Vector3, int> leafNodeInfo, ColorGradient cg)
    {
        foreach (KeyValuePair<Vector3, int> info in leafNodeInfo)
        {
            Vector3 pos = info.Key;
            if ((center - pos).sqrMagnitude <= sqrRadius)
            {
                int val = (1 << 12) - 1;
                for (int i = 0; i < 6; i++)
                {
                    if (leafNodeInfo.ContainsKey(pos + leafNodeDrawer.Neighbors[i]))
                    {
                        val &= ~leafNodeDrawer.Faces[i];
                    }
                }
                leafNodeDrawer.Draw(pos, val, cg.GetColor(info.Value * 0.2f));
            }
        }
    }
}

public class LeafNodeDrawer
{
    public int[] Faces;
    public Vector3[] Neighbors;

    private Vector3[][] edges;

    public LeafNodeDrawer(float size)
    {
        // Edge index -> bit.
        Faces = new int[]
        {
            (1 << 4) | (1 << 5) | (1 << 10) | (1 << 11),
            (1 << 1) | (1 << 2) | (1 << 7) | (1 << 8),
            (1 << 3) | (1 << 5) | (1 << 6) | (1 << 8),
            (1 << 0) | (1 << 2) | (1 << 9) | (1 << 11),
            (1 << 6) | (1 << 7) | (1 << 9) | (1 << 10),
            (1 << 0) | (1 << 1) | (1 << 3) | (1 << 4)
        };

        float s = size;
        Neighbors = new Vector3[] {
            Vector3.right * s,
            Vector3.left * s,
            Vector3.up * s,
            Vector3.down * s,
            Vector3.forward * s,
            Vector3.back * s
        };

        s = size / 2f;
        edges = new Vector3[12][];

        edges[0] = new Vector3[] { new Vector3(-s, -s, -s), new Vector3(s, -s, -s) };
        edges[1] = new Vector3[] { new Vector3(-s, -s, -s), new Vector3(-s, s, -s) };
        edges[2] = new Vector3[] { new Vector3(-s, -s, -s), new Vector3(-s, -s, s) };
        edges[3] = new Vector3[] { new Vector3(s, s, -s), new Vector3(-s, s, -s) };
        edges[4] = new Vector3[] { new Vector3(s, s, -s), new Vector3(s, -s, -s) };
        edges[5] = new Vector3[] { new Vector3(s, s, -s), new Vector3(s, s, s) };
        edges[6] = new Vector3[] { new Vector3(-s, s, s), new Vector3(s, s, s) };
        edges[7] = new Vector3[] { new Vector3(-s, s, s), new Vector3(-s, -s, s) };
        edges[8] = new Vector3[] { new Vector3(-s, s, s), new Vector3(-s, s, -s) };
        edges[9] = new Vector3[] { new Vector3(s, -s, s), new Vector3(-s, -s, s) };
        edges[10] = new Vector3[] { new Vector3(s, -s, s), new Vector3(s, s, s) };
        edges[11] = new Vector3[] { new Vector3(s, -s, s), new Vector3(s, -s, -s) };
    }

    public void Draw(Vector3 pos, int val, Color col)
    {
        for (int i = 0; i < 12; i++)
        {
            Color c = col * ((val & (1 << i)) > 0 ? 1f : 0.4f);
            Gizmos.Line(pos + this.edges[i][0], pos + this.edges[i][1], c);
        }
    }
}

public class ColorGradient
{
    private Color col1;
    private Color col2;

    public ColorGradient(Color col1, Color col2)
    {
        this.col1 = col1;
        this.col2 = col2;
    }

    public Color GetColor(float t, float dim1 = 1f, float dim2 = 1f)
    {
        return Color.Lerp(col1 * dim1, col2 * dim2, t);
    }
}