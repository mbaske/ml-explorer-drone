using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.Linq;
using System.Collections.Generic;

public class Drone : MonoBehaviour
{
    public Vector3 Position => transform.position;
    public Vector3 LocalPosition => transform.localPosition;
    public Vector3 Velocity => rb.velocity;
    public Vector3 VelocityNorm => DroneData.Normalize(rb.velocity);
    // The path is only used for smoothing cam movement.
    public DronePath Path { get; private set; }
    public DroneScanBuffer ScanBuffer { get; private set; }

    public const int scanBufferSize = 10;
    private const float proximitySensorRange = 1f;
    private const float maxVelocity = 0.2f;
    private const float pathExtent = 2f;
    private const int layerMask = 1 << 9;
    private const float rotationSpeed = 2f;

    private Rigidbody rb;
    private LineRenderer laser;
    private Transform ring1;
    private Transform ring2;
    private Transform ring3;
    private Vector3 rot1 = Vector3.zero;
    private Vector3 rot2 = Vector3.right * 90;
    private Vector3 rot3 = Vector3.forward * 90;

    public void Initialize()
    {
        Path = new DronePath(pathExtent);
        ScanBuffer = new DroneScanBuffer(scanBufferSize);

        rb = GetComponent<Rigidbody>();
        ring1 = transform.Find("Ring1");
        ring2 = transform.Find("Ring2");
        ring3 = transform.Find("Ring3");

        laser = new GameObject().AddComponent<LineRenderer>();
        laser.transform.parent = transform;
        laser.transform.name = "Laser";
        laser.material = new Material(Shader.Find("Sprites/Default"));
        laser.widthMultiplier = 0.01f;
        laser.receiveShadows = false;
        laser.shadowCastingMode = ShadowCastingMode.Off;
        laser.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        laser.positionCount = 2;
        laser.startColor = new Color(0f, 1f, 0.2f, 0.4f);
        laser.endColor = laser.startColor;

        ReSet();
    }

    public void ReSet()
    {
        ScanBuffer.Clear();
        Path.Clear(Position);
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.localPosition = Vector3.zero;
        laser.enabled = false;
    }

    public void Move(Vector3 velocity)
    {
        rb.AddForce(velocity * maxVelocity, ForceMode.VelocityChange);
        Path.Add(Position);
    }

    public Point Scan(float hrz, float vrt, float range)
    {
        RaycastHit hit;
        Vector3 scan = new Vector3(hrz, vrt, 1f);
        Ray ray = new Ray(Position, Quaternion.Euler(vrt * 180f, hrz * 180f, 0f) * Vector3.forward);
        Point point = new Point(PointType.ScanOutOfRange, Position + ray.direction * range, Time.time);
        laser.enabled = Physics.Raycast(ray.origin, ray.direction, out hit, range, layerMask);
        if (laser.enabled)
        {
            scan.z = DroneData.NormalizeDistance(hit.distance);
            // Grid nodes align with blocks:
            // Offset point slightly so it doesn't sit right on the boundary between two nodes.
            point.Position = ray.origin + ray.direction * (hit.distance + 0.01f);
            point.Type = PointType.ScanPoint;
            laser.SetPosition(0, ray.origin);
            laser.SetPosition(1, hit.point);
        }
        ScanBuffer.Add(scan);
        return point;
    }

    public Vector4 GetForwardProximity()
    {
        RaycastHit hit;
        Ray ray = new Ray(Position, rb.velocity);
        Vector4 result = new Vector4(0f, 0f, 0f, 1f);
        if (Physics.SphereCast(ray, 0.25f, out hit, proximitySensorRange, layerMask))
        {
            result = (hit.point - Position).normalized;
            result.w = hit.distance / proximitySensorRange;
        }
        return result;
    }

    private void Update()
    {
        // Each ring's rotation represents drone movement along a world axis.
        Vector3 v = rb.velocity;
        rot1.x += v.z * rotationSpeed;
        rot2.y += v.x * rotationSpeed;
        rot3.y += v.y * rotationSpeed;
        ring1.rotation = Quaternion.Euler(rot1);
        ring2.rotation = Quaternion.Euler(rot2);
        ring3.rotation = Quaternion.Euler(rot3);
    }
}

public class DroneScanBuffer
{
    private Queue<Vector3> queue;
    private int sizeV;
    private int sizeF;
    private float[] na;
    private float[] prox;

    public DroneScanBuffer(int size)
    {
        sizeV = size;
        sizeF = size * 3;
        queue = new Queue<Vector3>();
        na = Enumerable.Repeat(-1f, sizeF).ToArray();
    }

    public void Clear()
    {
        queue.Clear();
    }

    public void Add(Vector3 scan)
    {
        queue.Enqueue(scan);
        if (queue.Count > sizeV)
        {
            queue.Dequeue();
        }
    }

    public float[] ToArray()
    {
        if (queue.Count > 0)
        {
            float[] floats = new float[sizeF];
            Array.Copy(na, floats, sizeF);
            int i = sizeF - queue.Count * 3;
            foreach (Vector3 v in queue)
            {
                floats[i++] = v.x;
                floats[i++] = v.y;
                floats[i++] = v.z;
            }
            // Latest scan -> last 3 array elements.
            return floats;
        }

        return na;
    }
}

public class DronePath
{
    public float Extent
    {
        get { return Mathf.Sqrt(extentSqr); }
        set { extentSqr = value * value; }
    }
    public float Spacing
    {
        get { return Mathf.Sqrt(spacingSqr); }
        set { spacingSqr = value * value; }
    }
    public int Count => buffer.Count;
    public Vector3 Center => bounds.center;
    public IOrderedEnumerable<Vector4> Chronological => buffer.OrderBy(p => p.w);

    private HashSet<Vector4> buffer;
    private Vector4 latest;
    private Bounds bounds;
    private float extentSqr;
    private float spacingSqr;

    public DronePath(float extent = 1f, float spacing = 0)
    {
        Extent = extent;
        Spacing = spacing;
        buffer = new HashSet<Vector4>();
        bounds = new Bounds();
    }

    public void Clear(Vector3 pos)
    {
        Clear();
        AddChronological(pos);
        ResetBounds(pos);
    }

    public void Clear()
    {
        buffer.Clear();
        buffer.TrimExcess();
    }

    public void Add(Vector3 pos)
    {
        Trim();

        if (Spacing < Mathf.Epsilon || (pos - (Vector3)latest).sqrMagnitude >= spacingSqr)
        {
            if (AddChronological(pos))
            {
                UpdateBounds(pos);
            }
        }
    }

    public float GetLength()
    {
        IOrderedEnumerable<Vector4> sorted = Chronological;
        Vector4 s = Vector4.zero;
        float length = 0f;
        foreach (Vector4 p in sorted)
        {
            length += (s.Equals(Vector4.zero) ? 0 : Vector3.Distance(s, p));
            s = p;
        }
        return length;
    }

    public void Draw()
    {
        IOrderedEnumerable<Vector4> sorted = Chronological;
        Vector4 s = Vector4.zero;
        foreach (Vector4 p in sorted)
        {
            Gizmos.Line(s.Equals(Vector4.zero) ? p : s, p, Color.yellow);
            s = p;
        }
        Vector3 c = Center;
        // Bounding Box
        Gizmos.Cube(c, Quaternion.identity, bounds.size, Color.gray);
        // Crosshair
        float l = 0.25f;
        Gizmos.Line(c + Vector3.left * l, c + Vector3.right * l, Color.white);
        Gizmos.Line(c + Vector3.up * l, c + Vector3.down * l, Color.white);
        Gizmos.Line(c + Vector3.forward * l, c + Vector3.back * l, Color.white);
    }

    public void Trim()
    {
        if (buffer.RemoveWhere(IsOutOfBounds) > 0)
        {
            RecalcBounds();
        }
    }

    private bool IsOutOfBounds(Vector4 pos)
    {
        // Vector4 pos is a space&time location.
        // Not casting to Vector3 would also remove old points
        // with DronePath.Extent being the lifetime in seconds.
        return ((Vector3)pos - (Vector3)latest).sqrMagnitude > extentSqr;
    }

    private void ResetBounds(Vector3 pos)
    {
        bounds.center = pos;
        bounds.size = Vector3.zero;
    }

    private void RecalcBounds()
    {
        ResetBounds(latest);
        foreach (Vector3 p in buffer)
        {
            UpdateBounds(p);
        }
    }

    private void UpdateBounds(Vector3 pos)
    {
        bounds.min = Vector3.Min(bounds.min, pos);
        bounds.max = Vector3.Max(bounds.max, pos);
    }

    private bool AddChronological(Vector3 pos)
    {
        latest = pos;
        latest.w = Time.time;
        return buffer.Add(latest);
    }
}