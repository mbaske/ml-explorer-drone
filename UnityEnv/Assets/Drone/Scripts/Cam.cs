using UnityEngine;

public class Cam : MonoBehaviour
{
    [SerializeField]
    private Vector3 upwardsAxis;

    private enum camMode
    {
        Follow = 0,
        Orbit = 1
    }
    [Space]
    [SerializeField]
    private camMode mode = 0;

    [Space]
    [Header("Orbit Settings")]
    [SerializeField]
    [Range(-2f, 2f)]
    private float rotationSpeed;
    private float angle;

    [Space]
    [SerializeField]
    [Range(0f, 64f)]
    private float radius;
    [SerializeField]
    [Range(0f, 1f)]
    private float radiusModAmp;
    [SerializeField]
    [Range(0f, 2f)]
    private float radiusModFreq;
    [Space]
    [SerializeField]
    [Range(-64f, 64f)]
    private float height;
    [SerializeField]
    [Range(0f, 64f)]
    private float heightModAmp;
    [SerializeField]
    [Range(0f, 2f)]
    private float heightModFreq;

    private Vector3 camPos;
    private Quaternion camRot;
    private Vector3 defPos;
    private Quaternion defRot;
    private float frames;
    private Drone drone;
    private Transform spot;

    public void Initialize()
    {
        defPos = transform.position;
        defRot = transform.rotation;
        frames = Application.targetFrameRate > 0 ? Application.targetFrameRate : 60;
        drone = transform.parent.GetComponentInChildren<Drone>();
        spot = transform.Find("FXLight");
        ReSet();
    }

    public void ReSet()
    {
        // AFTER Drone.ReSet()
        transform.position = defPos;
        transform.rotation = defRot;
        camPos = defPos;
        camRot = defRot;
    }

    private void Update()
    {
        if (mode == camMode.Orbit)
        {
            OrbitAroundDrone();
        }
        else
        {
            FollowDrone();
        }
    }

    private void OrbitAroundDrone()
    {
        angle += rotationSpeed * Time.deltaTime;
        float r = radius + radius * radiusModAmp * Mathf.Sin(Time.time * radiusModFreq);
        float h = height + heightModAmp * Mathf.Sin(Time.time * heightModFreq);

        Vector3 lookTarget = drone.Path.Center;
        Vector3 pos = new Vector3(r * Mathf.Cos(angle), h, r * Mathf.Sin(angle));
        camPos = Vector3.Lerp(camPos, lookTarget + pos, 0.01f);
        camRot = Quaternion.Slerp(camRot,
            Quaternion.LookRotation(lookTarget - camPos, upwardsAxis), 0.05f);
    }

    private void FollowDrone()
    {
        Vector3 dronePos = drone.Position;
        Vector3 direction = (transform.position - dronePos).normalized;
        spot.position = dronePos + direction;
        spot.LookAt(dronePos); // FX Light.

        float distance = Mathf.Max(2f, drone.Velocity.sqrMagnitude * 0.05f);
        Vector3 lookTarget = drone.Path.Center;
        Vector3 newCamPos = lookTarget + direction * distance; 

        camPos = Vector3.Lerp(camPos, newCamPos, 0.05f);
        camRot = Quaternion.Slerp(camRot,
            Quaternion.LookRotation(lookTarget - camPos, upwardsAxis), 0.25f);
    }

    private void LateUpdate()
    {
        float t = Time.deltaTime * frames;
        transform.position = Vector3.Lerp(transform.position, camPos, t);
        transform.rotation = Quaternion.Slerp(transform.rotation, camRot, t);
    }
}
