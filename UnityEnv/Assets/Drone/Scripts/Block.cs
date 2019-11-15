using UnityEngine;

public class Block
{
    // Enclosed space between cubeA & cubeB.
    public Bounds InnerBounds;

    private Color color = new Color(0f, 0.4f, 0.6f, 1f);
    private float scale;
    private Transform cubeA;
    private Transform cubeB;
    private Material matA;
    private Material matB;

    public Block(Transform parent, Material mat, float scale)
    {
        this.scale = scale;
        cubeA = CreateCube(parent, mat);
        matA = cubeA.GetComponent<Renderer>().material;
        cubeB = CreateCube(parent, mat);
        matB = cubeB.GetComponent<Renderer>().material;
        InnerBounds = new Bounds();
    }

    public void SetPosition(Vector3 pos, float amplitude, float offset, float curve)
    {
        // pos.z -> Perlin noise in 0/+1 range
        Color col = color * (1f - pos.z * 2.5f);
        matA.color = col;
        matB.color = col;

        Vector3 crv = Vector3.forward * curve;
         // Prevent overlap of mirrored cubes.
        pos.z = Mathf.Max(pos.z * amplitude + offset, scale);
        cubeA.localPosition = pos + crv;
        pos.z *= -1f;
        cubeB.localPosition = pos + crv;

        float dist = cubeA.localPosition.z - cubeB.localPosition.z;
        pos.z = cubeA.localPosition.z - dist / 2f;
        InnerBounds.center = pos;
        InnerBounds.size = new Vector3(1f, 1f, dist);
    }

    public void SetVisible(bool b)
    {
        cubeA.GetComponent<Renderer>().enabled = b;
        cubeB.GetComponent<Renderer>().enabled = b;
    }

    private Transform CreateCube(Transform parent, Material mat)
    {
        Transform cube = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
        cube.localScale = new Vector3(1f, 1f, scale * 2f);
        cube.parent = parent;
        cube.gameObject.layer = parent.gameObject.layer;
        cube.GetComponent<Renderer>().sharedMaterial = mat;
        return cube;
    }
}
