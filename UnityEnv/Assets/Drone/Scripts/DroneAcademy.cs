using UnityEngine;
using MLAgents;

public class DroneAcademy : Academy
{
    public override void InitializeAcademy()
    {
        // Time.fixedDeltaTime = 0.01333f; // (75fps). default is .2 (60fps)
        // Time.maximumDeltaTime = .15f; // Default is .33
    }

    public override void AcademyReset()
    {
    }

    public override void AcademyStep()
    {
    }
}
