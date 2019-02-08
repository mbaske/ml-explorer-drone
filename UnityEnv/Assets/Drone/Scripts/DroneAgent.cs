using UnityEngine;
using MLAgents;

public class DroneAgent : Agent
{
    public DroneData Data { get; private set; }
    public Drone Drone { get; private set; }
    public BlockWorld World { get; private set; }
    public Cam Cam { get; private set; }
    
    [SerializeField]
    [Range(2f, 10f)]
    private float lookRadius = 5f;
    [SerializeField]
    [Range(0.25f, 1f)]
    private float leafNodeSize = 0.5f;

    private Point scanPoint;
    private Vector3Int prevPos;
    private int lingerCount; 

    private void OnValidate()
    {
        leafNodeSize = Mathf.Pow(2f, Mathf.Round(Mathf.Log(leafNodeSize, 2f)));
    }

    public override void InitializeAgent()
    {
        Data = new DroneData();

        Drone = GetComponentInChildren<Drone>();
        Drone.Initialize();
        World = GetComponentInChildren<BlockWorld>();
        World.Initialize();
        Cam = GetComponentInChildren<Cam>();
        Cam.Initialize();
    }

    public override void AgentReset()
    {
        Data.Reset(Drone.Position, lookRadius, leafNodeSize);

        Drone.ReSet();
        World.ReSet();
        Cam.ReSet();

        scanPoint = default(Point);
        prevPos = GetVector3Int(Drone.Position);
        lingerCount = 0;
    }

    public override void CollectObservations()
    {
        Vector3 pos = Drone.Position;
        if (IsNewGridPosition(pos))
        {
            Data.AddPoint(new Point(PointType.DronePos, pos, Time.time));
        }

        Data.AddPoint(scanPoint);
        // Number of new leaf nodes created by this scan.
        int nodeCount = Data.Tree.Intersect(pos, scanPoint.Position);
        float scanReward = (nodeCount * 0.1f) / Data.LookRadius;
        AddReward(scanReward);
        
        Data.StepUpdate(pos);

        float linger = lingerCount / 100f; // 0 - 2
        float lingerPenalty = -linger * 0.1f;
        AddReward(lingerPenalty);

        Vector3 velocity = Drone.VelocityNorm;
        Vector4 proximity = Drone.GetForwardProximity();
        float proxPenalty = (1f - 1f / Mathf.Max(proximity.w, 0.1f)) * velocity.sqrMagnitude * 0.25f;
        AddReward(proxPenalty);

        AddVectorObs(linger - 1f); // 1
        AddVectorObs(velocity); // 3 
        AddVectorObs((Vector3)proximity); // 3
        AddVectorObs(proximity.w * 2f - 1f); // 1 
        AddVectorObs(Data.LookRadiusNorm); // 1 
        AddVectorObs(Data.NodeDensities); // 8
        AddVectorObs(Data.IntersectRatios); // 8 
        AddVectorObs(Drone.ScanBuffer.ToArray()); // 30
    }

    public override void AgentAction(float[] vectorAction, string textAction)
    {
        scanPoint = Drone.Scan(vectorAction[0], vectorAction[1], Data.LookRadius);
        Drone.Move(new Vector3(vectorAction[2], vectorAction[3], vectorAction[4]));

        if (!World.StepUpdate())
        {
            AgentReset();
        }
    }

    private bool IsNewGridPosition(Vector3 dronePos)
    {
        Vector3Int pos = GetVector3Int(dronePos);
        if (pos != prevPos)
        {
            prevPos = pos;
            lingerCount = 0;
            return true;
        }

        lingerCount = Mathf.Min(200, lingerCount + 1);
        return false;
    }

    private Vector3Int GetVector3Int(Vector3 pos)
    {
        float s = Data.LeafNodeSize;
        return new Vector3Int(
            Mathf.RoundToInt(pos.x / s),
            Mathf.RoundToInt(pos.y / s),
            Mathf.RoundToInt(pos.z / s)
        );
    }
}
