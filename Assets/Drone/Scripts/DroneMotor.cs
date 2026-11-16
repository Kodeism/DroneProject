using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;



[System.Serializable]
public class DroneMotor
{
    [Tooltip("The thrust history of the drone")]
    public AnimationCurve thrustHistory;
    [Tooltip("Progress bar for the current thrust")]
    [ProgressBar("Current thrust", "maxThrust", EColor.Red)] public float currentThrust;
    [Tooltip("Progress bar for the raw thrust command")]
    [ProgressBar("Target thrust", "maxThrust", EColor.Blue)] public float targetThrust;


    [Header("Setup")]
    [Tooltip("The transform of the motor object")]
    public Transform motorTransform;
    [Tooltip("The max thrust this motor can output")]
    public float maxThrust = 15;
    [Tooltip("The spindirection of the motor 1 for clockwise -1 for counter-clockwise")]
    public float spinDirection;
  

    [Header(" ")]
    [Header("Settings")]
    [Tooltip("Yaw reaction torque per unit of thrust. Tune until yaw feels right.")]
    public float torqueCoefficient = 0.02f;
    [Tooltip("The time it takes for the motor to increase throttle 63.2% of the gap between current output and 100% output")]//exponent is behind it being 63.2%. if output is at 10% thrust would tell you how much time it takes for output to increase 63.2% of the remnaing gap, meaning => 63.2% of the remaning 90% = 56.88% => meaning it would take "thrust" amount of time to go from 10% to 56.88% output => this continues with the gap between 56.88% and 100%, meaning it takes more and more time to increase throttle
    public float timeConstant = 0.015f;
    [Tooltip("The exponent which dictates the blend-ratio between linear and quadradic thrust from a given command where 0 means 100% linear, 0% quadradic. 0.2 means 20% quadradic 80% linear. 1 means 100% quadradic and 0% linear")]
    [Range(0f, 0.95f)] public float thrustExpo = 0.7f;


    //enviorment variables
    private Vector3 LastForce;
    float t;
    float targetCommand;
    float currentCommand;



    /// <summary>
    /// The command to thrust curve where linear and quadratic thrust is mixed
    /// In real life a bunch of things like voltage sag and the like can result in this curve being inaccurate.
    /// But since we are just simulating for now this is fine.
    /// </summary>
    /// <param name="command">The command from the controller</param>
    /// <param name="exponent">The ratio of mix between quadratic and linear</param>
    /// <returns>the current thrust</returns>
    static float ThrustCurve(float command, float exponent)
    {
        //this is the expo function it can be spperated into 2 parts
        //linear part: (1f - exponent) * command => if the exponent increases the linear influence decreases
        //quadratic part: exponent * command*command => if the exponent increases the quadratic influence increases
        return exponent * command*command + (1f - exponent) * command;
    }
    /// <summary>
    /// Inverse of the thrust curve used for assigning commands from a thrust given by the flightmixer 
    /// </summary>
    /// <param name="thrust">The new target thrust</param>
    /// <param name="exponent">the exponent used for the thrust curve</param>
    /// <returns>the target command</returns>
    static float InverseThrustCurve(float thrust, float exponent)
    {
        //if the exponent is small enough the quadratic influence is so small it doesn't matter
        if (exponent < 1e-4f) return thrust;
        //the rest is just the quadratic formular
        float b = 1f - exponent;
        return (-b + Mathf.Sqrt(b * b + 4f * exponent * thrust)) / (2f * exponent);
    }


    /// <summary>
    /// Function to apply the current thrust command to the drone
    /// </summary>
    /// <param name="droneRigidbody">The drones rigid body</param>
    public void ApplyForce(Rigidbody droneRigidbody)
    {
        //if the motor script has no motor then we can't have any real thrust as we need it to compute how much leverage it has in the flight mixer
        if (motorTransform == null) return;

        //a is how much the new command should effect the thrust output.
        //If lag accurs deltatime increases as proccesing time increases which causes a to aproach 1 meaning it effects the thrust output quite highly
        //this insures that the smoothing deppends on how long a command was given for and not the framerate
        float a = 1f - Mathf.Exp(-Time.fixedDeltaTime / timeConstant);
        //smoothing (the more frames the command is given for the closer the thrust gets to the actual target command
        currentCommand += a * (targetCommand - currentCommand);
        //use the thrust curve to get the thrust based on the smoothed command and multiply it by maxthrust to get actual thrust output
        currentThrust = ThrustCurve(currentCommand, thrustExpo) * maxThrust;
        
        //then we update our history for the inspector
        t += Time.fixedDeltaTime;
        thrustHistory.AddKey(t, currentThrust);
        if (thrustHistory.length > 300) 
            thrustHistory.RemoveKey(0); //if the length goes beyond 300 we just delete the first value

        //then we make a force vector and store it
        Vector3 force = motorTransform.up * currentThrust;
        LastForce = force;

        //then finally we add the force at the motor's position and add the torque aswell to make sure yaw and the like works
        droneRigidbody.AddForceAtPosition(force, motorTransform.position);
        droneRigidbody.AddTorque(-motorTransform.up * spinDirection * currentThrust * torqueCoefficient, ForceMode.Force);

    }
    /// <summary>
    /// Method to reset the motor for end of episodes and respawns
    /// </summary>
    public void ResetMotor()
    {
        currentThrust = 0f;
        targetCommand = 0f;
        currentCommand = 0f;
        targetThrust = 0f;
    }

    /// <summary>
    /// Set thrust command for the flight mixer
    /// </summary>
    /// <param name="newThrust">thrust computed by the flight mixer</param>
    public void SetThrust(float newThrust)
    {
        float fraction = maxThrust > 0f ? Mathf.Clamp01(newThrust / maxThrust) : 0f;
        targetCommand = InverseThrustCurve(fraction, thrustExpo);
        targetThrust = newThrust;
    }
    public void DrawThrustDebug()
    {
        if (motorTransform == null || maxThrust <= 0f)
            return;

        Debug.DrawRay(
            motorTransform.position,
            motorTransform.up * (currentThrust / maxThrust),
            Color.red,
            0f,
            false
        );
    }
}



