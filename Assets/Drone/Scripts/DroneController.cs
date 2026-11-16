using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The Dronecontroller is the interface the AI interacts with the drone through.
/// It translates inputs and applies them to the drone.
/// It stabilizes the drone and handles its parameters so the AI can worry about the big stuff like "where to go" and "what path to take".
/// This allows us to switch out the drone body aswell. Letting the AI controll helicopter or octocopter without having to change any code.
/// </summary>
[System.Serializable]
public class DroneController
{
    [Tooltip("The motors of the drone")]
    [SerializeField] protected List<DroneMotor> motors = new List<DroneMotor>();



    [Header("Stabilization (PID)")]
    [Tooltip("The PID controller (Proportional-Integral-Derivative controller) for the pitch of the drone")]
    [SerializeField] protected PIDController pitchPID;
    [Tooltip("The PID controller (Proportional-Integral-Derivative controller) for the roll of the drone")]
    [SerializeField] protected PIDController rollPID;
    [Tooltip("The PID controller (Proportional-Integral-Derivative controller) for the yaw of the drone")]
    [SerializeField] protected PIDController yawPID;



    [Header("Throttle")]
    [SerializeField] protected ThrottleController throttleController;



    [Tooltip("Maximum targeted rotation rate (rad/s or deg/s depending on your PID tuning).")]
    public float maxRate = 3f;

    /// <summary>
    /// This hands out the inputs to the diffrent PIDs and hands the values to the motors.
    /// </summary>
    /// <param name="rb">Drone's rigidbody</param>
    /// <param name="pitchInput">Drone's Pitch input (between 1 and -1)</param>
    /// <param name="rollInput">Drone's Roll input (beteen 1 and -1)</param>
    /// <param name="yawInput">Drone's Yaw input (beteen 1 and -1)</param>
    /// <param name="throttleStickInput">Drone's Throttle input (beteen 1 and -1 like a stick on a drone controller)</param>
    public void ApplyInputs(Rigidbody rb, float pitchInput, float rollInput, float yawInput, float throttleStickInput)
    {
        //get the global rotation speeds of x, y, z directions and translate it into our local space
        Vector3 w = rb.transform.InverseTransformDirection(rb.angularVelocity);


        //This is for dynamic corrections. If input is 0 but the drone's actual velocity is above that, this tries to correct it by applying oposing thrust.
        //Pitch is x. This is tilting up and down. Increasing X means the nose it pointing upwards. Decreasing X means the nose is pointing downwards.
        float pitchCorrection = pitchPID.Update(w.x, pitchInput * maxRate, Time.fixedDeltaTime);

        //Roll is rotating around the z axis. This is tilting right or left. Increasing z means the drone is tilting right. Decreasing x means the drone is tilting left.
        float rollCorrection = rollPID.Update(w.z, rollInput * maxRate, Time.fixedDeltaTime);

        //yaw is rotations around the y axis which turns the agent left and right. Increasing y means we are rotating clockwise. Decreasing y means we are rotating counterclockwise.
        float yawCorrection = yawPID.Update(w.y, yawInput * maxRate, Time.fixedDeltaTime);


        //We send the inputs and corrections to the throttlecontroller  and get our final throttle
        float totalMaxThrust = 0f;
        foreach (DroneMotor m in motors) totalMaxThrust += m.maxThrust;
        float throttle = throttleController.Compute(rb, throttleStickInput, totalMaxThrust, Time.fixedDeltaTime);

        //get the flightmixer to compute our motor outputs
        FligtMixer.CalculateMotorOutputs(motors, rb, throttle, pitchCorrection, rollCorrection, yawCorrection);


        //apply the forces
        foreach (DroneMotor m in motors)
        {
            m.ApplyForce(rb);
        }
    }

    /// <summary>
    /// This to randomize the motors for domain randomization
    /// </summary>
    /// <param name="expoMin">Smallest value the exponent can take</param>
    /// <param name="expoMax">Largest value the exponent can take</param>
    /// <param name="tauMin">Smallest value the tau (the time constant) can take</param>
    /// <param name="tauMax">Largest value the tau (the time constant) can take</param>
    public void RandomizeMotors(float expoMin, float expoMax, float tauMin, float tauMax)
    {
        //Just goes through each motor and assigns them a random value between max in min
        foreach (DroneMotor m in motors)
        {
            m.thrustExpo = UnityEngine.Random.Range(expoMin, expoMax);
            m.timeConstant = UnityEngine.Random.Range(tauMin, tauMax);
        }
    }

    /// <summary>
    /// Resets the controller and its subcomponents
    /// </summary>
    /// <param name="rb">The rigid body of the agent</param>
    public void ResetController(Rigidbody rb)
    {
        //Resets all the motors and gets the total max thrust for the throttle controll
        float totalMaxThrust = 0f;
        foreach (DroneMotor m in motors)
        {
            m.ResetMotor();
            totalMaxThrust += m.maxThrust;
        }
        //resets the pids and throttle controll
        pitchPID.Reset();
        rollPID.Reset();
        yawPID.Reset(); ;
        throttleController.Reset(rb, totalMaxThrust);
    }
    public void DrawThrustDebug()
    {
        foreach (DroneMotor motor in motors)
        {
            motor.DrawThrustDebug();
        }
    }
}



