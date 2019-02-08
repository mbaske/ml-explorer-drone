using System.Collections.Generic;
using UnityEngine;

public class BlockWorld : MonoBehaviour
{
    private int extent = 10;
    private int length;

    private float offset = 4f;
    private float amplitude = 5.5f;
    private float perlinModFreq = 19f;
    private float perlinModAmp = 3f;
    private float perlinModOffset = 4f;
    private float curveModFreq = 43f;
    private float curveModAmp = 5f;

    private Vector2 perlinOffset;
    private float distance;
    private float blockScale;
    private Block[] blocks;
    private Drone drone;
    // Local positions.
    private Vector2Int dronePos;
    private Vector2Int prevDronePos;
    private Dictionary<Vector2Int, Block> blocks2D;

    public void Initialize()
    {
        length = extent * 2 + 1;
        blocks = new Block[length * length];
        blocks2D = new Dictionary<Vector2Int, Block>();
        blockScale = Mathf.Max(1f, amplitude);
        drone = transform.parent.GetComponentInChildren<Drone>();

        ReSet(Resources.Load<Material>("BlockMaterial"));
    }

    public void ReSet(Material mat = null)
    {
        distance = 0f;
        dronePos.x = Mathf.RoundToInt(drone.LocalPosition.x);
        dronePos.y = Mathf.RoundToInt(drone.LocalPosition.y);
        prevDronePos = dronePos;
        SetRandomOffset();
        ResetBlocks(mat);
    }

    public void SetVisible(bool b)
    {
        for (int i = 0; i < blocks.Length; i++)
        {
            blocks[i].SetVisible(b);
        }
    }

    public bool StepUpdate()
    {
        dronePos.x = Mathf.RoundToInt(drone.LocalPosition.x);
        dronePos.y = Mathf.RoundToInt(drone.LocalPosition.y);

        if (dronePos != prevDronePos)
        {
            distance = drone.LocalPosition.magnitude;
            Shift();
        }
        // Depending on settings, there might be holes the drone can slip through.
        return blocks2D[dronePos].InnerBounds.Contains(drone.LocalPosition);
    }

    private void Shift()
    {
        Vector2Int p = Vector2Int.zero;

        int dx = dronePos.x - prevDronePos.x;
        if (dx > 0)
        {
            int sx = prevDronePos.x - extent;
            int tx = sx + dx;
            int sy = prevDronePos.y - extent;
            int ty = prevDronePos.y + extent;
            for (int x = sx; x < tx; x++)
            {
                for (int y = sy; y <= ty; y++)
                {
                    p.x = x;
                    p.y = y;
                    Block block = blocks2D[p];
                    blocks2D.Remove(p);
                    p.x += length;
                    blocks2D.Add(p, block);
                    UpdateBlock(p);
                }
            }
            prevDronePos.x = dronePos.x;
        }
        else if (dx < 0)
        {
            int sx = prevDronePos.x + extent;
            int tx = sx + dx;
            int sy = prevDronePos.y - extent;
            int ty = prevDronePos.y + extent;
            for (int x = sx; x > tx; x--)
            {
                for (int y = sy; y <= ty; y++)
                {
                    p.x = x;
                    p.y = y;
                    Block block = blocks2D[p];
                    blocks2D.Remove(p);
                    p.x -= length;
                    blocks2D.Add(p, block);
                    UpdateBlock(p);
                }
            }
            prevDronePos.x = dronePos.x;
        }

        int dy = dronePos.y - prevDronePos.y;
        if (dy > 0)
        {
            int sy = prevDronePos.y - extent;
            int ty = sy + dy;
            int sx = prevDronePos.x - extent;
            int tx = prevDronePos.x + extent;
            for (int y = sy; y < ty; y++)
            {
                for (int x = sx; x <= tx; x++)
                {
                    p.x = x;
                    p.y = y;
                    Block block = blocks2D[p];
                    blocks2D.Remove(p);
                    p.y += length;
                    blocks2D.Add(p, block);
                    UpdateBlock(p);
                }
            }
            prevDronePos.y = dronePos.y;
        }
        else if (dy < 0)
        {
            int sy = prevDronePos.y + extent;
            int ty = sy + dy;
            int sx = prevDronePos.x - extent;
            int tx = prevDronePos.x + extent;
            for (int y = sy; y > ty; y--)
            {
                for (int x = sx; x <= tx; x++)
                {
                    p.x = x;
                    p.y = y;
                    Block block = blocks2D[p];
                    blocks2D.Remove(p);
                    p.y -= length;
                    blocks2D.Add(p, block);
                    UpdateBlock(p);
                }
            }
            prevDronePos.y = dronePos.y;
        }
    }

    private void UpdateBlock(Vector2Int p)
    {
        float curve = Mathf.Sin(distance / curveModFreq) * curveModAmp;
        blocks2D[p].SetPosition(new Vector3(p.x, p.y, GetPerlin(p)), amplitude, offset, curve);
    }

    private float GetPerlin(Vector2Int p)
    {
        float mod = Mathf.Sin(distance / perlinModFreq) * perlinModAmp + perlinModOffset;
        return Mathf.PerlinNoise((p.x + perlinOffset.x) / mod, (p.y + perlinOffset.y) / mod);
    }

    private void ResetBlocks(Material mat)
    {
        blocks2D.Clear();
        int i = 0;
        for (int x = -extent; x <= extent; x++)
        {
            for (int y = -extent; y <= extent; y++)
            {
                Block block;
                if (mat != null)
                {
                    block = new Block(transform, mat, blockScale);
                    blocks[i++] = block;
                }
                else
                {
                    block = blocks[i++];
                }
                Vector2Int pos = new Vector2Int(x, y);
                blocks2D.Add(pos, block);
                UpdateBlock(pos);
            }
        }
    }

    private void SetRandomOffset()
    {
        int abortCount = 0;
        float range = 10f;
        do // keep center clear
        {
            perlinOffset = new Vector2(Random.Range(-range, range), Random.Range(-range, range));
            // TODO Constrain settings so there will always be enough room.
            abortCount++;
        }
        while (GetPerlin(Vector2Int.zero) < 0.2f && abortCount < 100);
    }
}
