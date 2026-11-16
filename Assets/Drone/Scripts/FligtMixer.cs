using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
/// <summary>
/// FlightMixer mix the drones motor thrusts to let it fly dynamically.
/// Neccesary if we want to let the ai try diffrent types of drones than just a quadcopter.
/// </summary>
public static class FligtMixer
{
    //we want static fields to prevent memory from constantly being reallocated during simulation
    //this mixer mixes the throttle of up to 16 motors

    //we need a way to store the combined roll and pitch [combinedRollPitch] and the yaw [yaw] of the motors
    static readonly float[] combinedRollPitch = new float[16]; 
    static readonly float[] yaw = new float[16];
    
    //here we store the position of the motors
    static readonly Vector3[] position = new Vector3[16];   

    /// <summary>
    /// imagine we input both yaw and roll to 100% at the same time 
    /// so both yaw and roll give engine A +0.6 thrust and engine B -0.6
    /// if we simply add this togeather we will end up with values outside of the max range 1.2 and -1.2
    /// to avoid this we use span here to calculate the spawn between the lowest and highest thrust on the motors
    /// this is being set up so that yaw has a multiplier so we can reduce its input first if needed
    /// </summary>
    /// <param name="numberOfMotors">Number of motors</param>
    /// <param name="YawMultiplier">Yaw Multiplier</param>
    /// <param name="low">lowest value</param>
    /// <param name="high">highest value</param>
    /// <returns>returns the span between the thrust of all the motors</returns>
    static float Span(int numberOfMotors, float YawMultiplier, out float low, out float high)
    {
        //initialize the highest and lowest values
        low = float.MaxValue; high = float.MinValue;
        for (int i = 0; i < numberOfMotors; i++)
        {
            //foreach motor we compute output value
            float value = combinedRollPitch[i] + YawMultiplier * yaw[i];
            //if the value is lower or higher than the last then we save it
            if (value < low) low = value;
            if (value > high) high = value;
        }
        return high - low; //then we return the diffrence between the highest and lowest outputs = output span or range)
    }

    /// <summary>
    /// Takes the drone's motors, inputs, and rigidbody as input. 
    /// It uses these to dinamically calculate the thrust of the individual motors.
    /// This calculation is based on: 
    /// 1.  Their distance from the center of mass
    /// 2.  Their position relative to the center of mass
    /// 3.  Their spin direction
    /// </summary>
    /// <param name="motors">The drone's motor</param>
    /// <param name="rb">The drones rigidbody</param>
    /// <param name="throttle">The throttle input</param>
    /// <param name="pitch">The pitch input</param>
    /// <param name="roll">The roll input</param>
    /// <param name="yaw">The yaw input</param>
    public static void CalculateMotorOutputs(List<DroneMotor> motors, Rigidbody rb, float throttle, float pitch, float roll, float yaw)
    {
        //if there are more motors than 16 then they will simply remain unused
        int numberOfMotors = Mathf.Min(motors.Count, combinedRollPitch.Length);

        //find the width and length of the drone's body by where the motors are placed
        float maxX = 0.0001f; 
        float maxZ = 0.0001f; //we don't set it to 0 incase we don't find any and the division later causes the program to crash because of a dividing by 0 error
        for (int i = 0; i < numberOfMotors; i++)
        {
            //we use center of mass so that even if the drone is imbalanced it still applies it correctly
            //the further away it is the more leverage the thrust has.
            position[i] = rb.transform.InverseTransformPoint(motors[i].motorTransform.position) - rb.centerOfMass; 
            maxX = Mathf.Max(maxX, Mathf.Abs(position[i].x));
            maxZ = Mathf.Max(maxZ, Mathf.Abs(position[i].z));
        }



        //mix the inputs by relative leverage 
        //roll is tilting left and right so we use the x demention to determin how much the thrust should be applied to it
        //if the motor is the one furthest away in the x demention then pos[i].x / maxX simply becomes 1 meaning the entire 100% if the roll input applies
        //if the motor is the one closest in the x demention then pos[i].x / maxX might become 0.1 meaning only 10% of the roll input applies to it.
        //this way, the further away you are, the more leverage you have, the more input needs to be applied.
        //this also means that if the motors is situated behind the center of mass pos[i].x / maxX becomes negative meaning -10% of input applies making it decrease thrust
        for (int i = 0; i < numberOfMotors; i++)
        {
            combinedRollPitch[i] = pitch * (-position[i].z / maxZ) + roll * (position[i].x / maxX);
            //since yaw is torque we simply apply it equally
            FligtMixer.yaw[i] = yaw * -motors[i].spinDirection;
        }




        //now we need to balance and desaturate the motors to make sure the input does not overstep its allowed output
        //here we use the span function from before
        float yawMultiplier = 1f;
        float scale = 1f;

        //get the span of the output if we get YawMultiplier (1 = 100%) of the requested yaw
        float span = Span(numberOfMotors, 1f, out float low, out float high); 
                
        //if the span is more than 1 (0 to 1) then we try to remix 
        if (span > 1f)
        {
            //first we check what the span would be if 0% of the yaw input is applied
            //this gives us the span of just applying pitch and roll
            float rpSpan = Span(numberOfMotors, 0f, out _, out _);
            
            //then, if the combinedRollPitch span is bellow 1, we set yawMultiplier to be the left over percentage of the max thrust
            if (rpSpan < 1f) 
                yawMultiplier = (1f - rpSpan) / (span - rpSpan);
            else 
            { 
                //else if the roll pitch span is still above 1 we reduce the scale of the inputs and cancel out yaw
                yawMultiplier = 0f;
                //if combinedRollPitch span is 1.1 dividing 1 by it gives us around 90% meaning we reduce the max and min motors' input by 10% to accomadate the max input
                scale = 1f / rpSpan; 
            }
            //then we compute the new span
            Span(numberOfMotors, yawMultiplier, out low, out high);
            //and if roll pitch span wasn't greater than 1, the sacle remains the same, otherwise it is reduced.
            low *= scale; 
            high *= scale;
        }



        //then we compute how much throttle shifts the inputs 
        //if low is 0.1 and high is 1 then this clamp becomes max = 0 and min = -0.1, so throttle turns to 0
        //while if the high is 0.7 the throttle might become 0.3 shifting the scale a bit
        //and if throttle is negative (meaning if we are in air/hover mode) a min value of -0.2 will shift everything by a reduced 20% thrust
        //while if the throttle is 0 but the lowest motor needs atleast 0.2 throttle it increase to that which shifts all the other values up in throttle to keep balance
        float shift = Mathf.Clamp(throttle, -low, 1f - high);

        //then we go through each motor and apply the new thrust
        for (int i = 0; i < numberOfMotors; i++)
        {
            //first we compute the output by combining all the terms
            float output = (combinedRollPitch[i] + yawMultiplier * FligtMixer.yaw[i]) * scale + shift;
            //then we set the thrust clamped between 0 and 1 so we get a percentage of max thrust
            motors[i].SetThrust(Mathf.Clamp01(output) * motors[i].maxThrust);
        }

    }


}

