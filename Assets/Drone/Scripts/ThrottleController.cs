using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//the angle compensation is not wokring correctly
public enum ThrottleMode
{
    ClimbRate,  //input = target climb speed, PID holds height when centered (if input is 0.10 it hovers as long as the required thrust is below max)
    Direct,     //input = thrust directly (0.1 as input will result in 10% of max thrust)
    Ramp        //input = rate of change of a stored throttle value (if input is 0.1 the thrust increase by 10% of thrust increase rate for the time it is held, once let go the thrust stays at the stored thrust until a negative or positive input is given to increase or decrease it)
}
/// <summary>
/// Throttle controller is to help manage how the drone controller manages throttle. 
/// It allows us to quickly switch how much the controller manages for the AI.
/// 
/// Direct mode means the AI has to manage hovering, vertical stabilization, and tilt compensation on its own.
/// This mode also allows it to have more freedom in how it moves.
/// So it increases freedom at the cost of complexity
/// 
/// Climbrate mode means the drone automatically hovers, and applies tilt compensation.
/// </summary>
[System.Serializable]
public class ThrottleController
{
    [Header("Mode")]
    public ThrottleMode mode = ThrottleMode.ClimbRate;

    [Header("Climb rate mode")]
    public PIDController climbPID;
    [Tooltip("Climb speed in m/s at full input.")]
    public float maxClimbRate = 3f;
    [Tooltip("input values smaller than this value become 0. Sp tiny noise doesn't cause drift.")]
    public float deadzone = 0.1f;
    [Tooltip("Compute hover throttle from mass and motor thrust instead of using the value below.")]
    public bool autoHover = true;
    [Range(0f, 1f)] public float hoverThrottle = 0.5f;
    [Tooltip("Divide throttle by how upright the drone is, so tilting doesn't make it sink.")]
    public bool tiltCompensation = true;
    [Tooltip("Minimum upright dot used for computing tilt compensation blending (0..1). Lower values allow stronger compensation at steep tilts.")]
    [Range(0.01f, 0.9f)] public float minUprightForBlend = 0.5f;
    [Tooltip("Maximum multiplier applied to throttle for tilt compensation to avoid extreme thrust commands.")]
    [Range(1f, 4f)] public float maxTiltMultiplier = 2f;

    [Header("Ramp mode")]
    [Tooltip("Throttle units per second at full input.")]
    public float throttleRate = 0.5f;

    //stored throttle for ramp mode.
    public float StoredThrottle { get; private set; }

    ThrottleMode lastMode;
    bool initialized;

    /// <summary>
    /// input: -1..1. totalMaxThrust: sum of maxThrust over all motors.
    /// Returns a throttle in 0..1 for the flight mixer.
    /// </summary>
    public float Compute(Rigidbody rb, float input, float totalMaxThrust, float dt)
    {
        //clamp input to make sure we don't get above 100% or below -100%
        input = Mathf.Clamp(input, -1f, 1f);

        //switching modes clears old state so nothing jumps.
        if (!initialized || mode != lastMode) //if the controller hasn't been initialized before or the mode has changed
        {
            //we reset the values so it computes the thrust correctly
            Reset(rb, totalMaxThrust);
            //then we set the last mode and say that it has been initialized
            lastMode = mode;
            initialized = true;
        }


        //depending on the mode we are in we want diffrent thrust
        switch (mode)
        {
            //if it is direct we jsut return the input
            case ThrottleMode.Direct:
                float h = HoverThrottle(rb, totalMaxThrust);
                return input >= 0f ? h + input * (1f - h) : h + input * h;
                //return input >= 0f ? input : 0f;

            case ThrottleMode.Ramp:
                StoredThrottle = Mathf.Clamp01(StoredThrottle + input * throttleRate * dt); //stored throttle is also between 0 and 1. Deltatime is just the amount of time the input is given in. So we are just timing the term: "input * throttlerate" by the number of seconds that the input was given. And then we clamp it incase it is too big.
                return ApplyTilt(rb, StoredThrottle); //we return the throttle with til applied

            default: //climbRate
                float stick = Mathf.Abs(input) < deadzone ? 0f : input; //if the input is below 0.1 then we want the drone to fall in altitude so we put the stick to 0 and increase from there. otherwise we let it be input since that means we want it to either hover or climb in altitude 
                float targetClimb = stick * maxClimbRate; //if input is 0.2 we increase thrust by 20% of climbrate (which could be 2 or something) this adds the value to the current hover value which depends on what the mass of the drone is and other evniormental parameters.
                float adjustment = climbPID.Update(rb.velocity.y, targetClimb, dt); //then we hand it to the PID to help guide our climbrate
                float hover = autoHover ? HoverThrottle(rb, totalMaxThrust) : hoverThrottle; //if autohover is on, we compute the hover value dynamically, otherwise we use our hardcoded hover value. 
                return ApplyTilt(rb, hover + adjustment); //then we apply the tilt 
        }
    }
    /// <summary>
    /// Reset the throttle controller when a new episode begins
    /// </summary>
    /// <param name="rb">Rigidbody of drone</param>
    /// <param name="totalMaxThrust">Max thrust of all the motors summed</param>
    public void Reset(Rigidbody rb, float totalMaxThrust)
    {
        //Reset our PID
        climbPID.Reset();
        //Compute a new hover throttle value so the drone doesn't just fall at the start of an episode
        StoredThrottle = HoverThrottle(rb, totalMaxThrust);
    }





    /// <summary>
    /// Computes the throttle needed to hover the drone in the air
    /// </summary>
    /// <param name="rb">Rigidbody of drone</param>
    /// <param name="totalMaxThrust">Max thrust of all the motors summed</param>
    /// <returns></returns>
    float HoverThrottle(Rigidbody rb, float totalMaxThrust)
    {
        //if the motors can't give any thrust we just return hoverthrottle
        if (totalMaxThrust <= 0f) return hoverThrottle;
        //else we compute the force needed to hover the drone in a given gravity and then devide it by the maxthrust to get how much of the total thrust we need to use
        return Mathf.Clamp01(rb.mass * -Physics.gravity.y / totalMaxThrust);
    }




    /// <summary>
    /// Tilt Compensation system. 
    /// Its job is to automatically boost the drone's throttle when it tilts so that it doesn't lose altitude while flying forward, backward, or sideways.
    /// </summary>
    /// <param name="rb">Rigidbody of drone</param>
    /// <param name="throttle">The given throttle</param>
    /// <returns></returns>
    float ApplyTilt(Rigidbody rb, float hoverThrottle)
    {
        //if we want tilt to be compensated (being tilted foward requires more thrust to hover or climb)
        if (!tiltCompensation) return hoverThrottle;
        //the dot product of the local up vector of the drone and global up vector will give a value between 1 and -1.
        //where -1 is upside down and 1 is flat upwards
        float upright = Vector3.Dot(rb.transform.up, Vector3.up);
        float mult = 1f / Mathf.Max(minUprightForBlend, upright);
        mult = Mathf.Min(mult, maxTiltMultiplier);
        return hoverThrottle * mult;
        //then we divide the throttle by this value.
        //if the value is below 0.5 that means it has gone beyond a 90 degress angle and is flipping upside down
        //this would be a problem if the value ever became 0, because deviding by 0 would crash unity
        //so instead we simply set the hard cap at a 90 degree angle or 0.5 upright value which doubles the throttle
        //The smaller the upright value the lager the multiplication as dividing by 0.5 is the same as multiplying by 2
        //then we clamp the value to avoid throttle outside of max...
    }
}



