using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using Unity.VisualScripting;
using UnityEngine;

public enum DerivativeMeasurement
{
    Velocity,
    ErrorRateOfChange
}
[System.Serializable]
public struct DebugData
{
    public float integral;
    public float lastError;
    public float lastValue;
    public float correctedValue;
    public float velocity;
    public void Reset()
    {
        integral = 0f;
        lastError = 0f;
        lastValue = 0f;
        correctedValue = 0f;
        velocity = 0f;
    }
}
[System.Serializable]
public struct Graphs
{
    public AnimationCurve iHistory;
    public AnimationCurve dHistory;
    public AnimationCurve pHistory;
    public AnimationCurve errorHistory;
    public AnimationCurve outputHistory;
    public void AddHistory(float p, float i, float d, float error, float output, float t)
    {
        pHistory.AddKey(t, p);
        if (pHistory.length > 300)
            pHistory.RemoveKey(0);

        iHistory.AddKey(t, i);
        if (iHistory.length > 300)
            iHistory.RemoveKey(0);

        dHistory.AddKey(t, d);
        if (dHistory.length > 300)
            dHistory.RemoveKey(0);

        errorHistory.AddKey(t, error);
        if (errorHistory.length > 300)
            errorHistory.RemoveKey(0);

        outputHistory.AddKey(t, output);
        if (outputHistory.length > 300)
            outputHistory.RemoveKey(0);
    }
}

[System.Serializable]
public class PIDController 
{



    [Header("Live Data")]
    [Tooltip("Expand to view internal PID calculations in real-time.")]
    public DebugData debug;
    public Graphs History;




    [Header("Settings")]
    [Tooltip("Proportional Gain: How hard to push based on current error.")]
    public float pGain = 1.0f;
    [Tooltip("Integral Gain: Corrects steady-state errors (drift over time like wind and stuff).")]
    public float iGain = 0.0f;

    [Tooltip("Derivative Gain: Dampens movements to prevent overshooting/oscillations.")]
    public float dGain = 0.001f;

    [Tooltip("Limits the maximum influence the integral term can have to prevent windup.")]
    public float iMax = 1.0f;

    [Tooltip("Limits the minimum output the controller can produce.")]
    public float outputMin = -0.3f;
    [Tooltip("Limits the maximum output the controller can produce.")]
    public float outputMax = 0.3f;
    [Tooltip("How quickly the controller should throttle motors based on imidiate input.")]
    public float feedforward = 0.05f;
    [Tooltip("Frequencies above this will be filtered out")]
    public float dFilterHz = 15f;


    float oldTarget = 0f;
    float t;
    private bool derivativeInitialized = false;

    float dFiltered;

    public float Update(float currentValue, float targetValue, float deltaTime)
    {
        if (deltaTime <= 0f) return 0f;
        float targetChange = targetValue - oldTarget;
        oldTarget = targetValue;
        float error = targetValue - currentValue;

        //derivative lowpass filter
        //if we have a last value we compute the derivative (the slope of the function) : else we simply return a 0
        float derivative = derivativeInitialized ? -(currentValue - debug.lastValue) / deltaTime : 0f;
        debug.lastValue = currentValue;
        derivativeInitialized = true;

        //alpha is our dynamic filter value
        //alpha aproaches 1 as deltatime increases
        //alpha is basically a noise filter
        //anything that appears above our filter hz value (meaning in this case more than 80 times a second) is ignored
        //imagine 1 big spike (thrust increasing) and on that spike are a bunch of smaller spikes appearing 80 times every second
        //that is the noice alpha helps us ignore
        //the more time passes between calls (deltatime increase) the closer alpha gets to 1... this is important in the line below
        float alpha = deltaTime / (deltaTime + 1f / (2f * Mathf.PI * dFilterHz));


        //here we compute the filtered D value.
        //our filtered d value is basically our running average for the derivative
        //if our simulation lags -> alpha becomes large -> big change between old average and new average because raw-D has likely changed alot in that time from our filtred D
        //if a big change happens -> derivative becomes large -> big change between old and new
        //if theres just a bit of noise and little change -> alpha is small -> diffrence between derivative and filtered is small -> small diffrence between old and new
        dFiltered += alpha * (derivative - dFiltered);


        //then we compute the p and d values.
        float p = pGain * error;
        float d = dGain * dFiltered;
        //feedfoward is us setting a little boost on how quickly we want to reach our target value.
        float ff = feedforward * (targetChange / deltaTime);



        //next we need a way to prevent our integral from oversaturating the output
        //first we figure out what the output would be if we simply added everything togeather
        float unsat = p + iGain * debug.integral + d + ff;

        //then we ask "is the motor already pushing 100% and is the error telling it to push more?" 
        //or "is the motor already saturated and pushing for more saturation?"
        bool pushingHigh = unsat >= outputMax && error > 0f;
        //and "is the motor already at 0% and is the error telling it to push less?"
        bool pushingLow = unsat <= outputMin && error < 0f;
        //if it is neither that means we have headroom to saturate the intergral futher
        if (!pushingHigh && !pushingLow)
            //then we clamp it if it goes over our max or min values
            debug.integral = Mathf.Clamp(debug.integral + error * deltaTime, -iMax, iMax);


        //then we compute the actual output store and clamp it as it returns.
        float output = p + iGain * debug.integral + d + ff;
        debug.correctedValue = output;
        t += deltaTime;
        History.AddHistory(p, iGain * debug.integral, d, error, output, t);

        return Mathf.Clamp(output, outputMin, outputMax);
    }

    // Reset this when restarting or landing so old errors don't carry over
    public void Reset()
    {
        debug.Reset();
        derivativeInitialized = false;

        oldTarget = 0f;
        dFiltered = 0f;
    }
}
