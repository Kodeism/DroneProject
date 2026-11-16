using System.Collections;
using System.Collections.Generic;
using Unity.Barracuda;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.Rendering;


/// <summary>
/// A drone machine learning agent that will be trained to fly, compete, dodge, and perform other tasks. 
/// This class will be used to control the drone's behavior and decision-making process during training and inference.
/// </summary>
public class DroneAgent : Agent
{
    [Header("Enviorment settings")]
    [Tooltip("The local area the drone is training in")]
    [SerializeField] private TraningArea traningArea;
    [Tooltip("The obstacle spawner for the area")]
    [SerializeField] private ObstacleSpawner oSpawner;
    [Tooltip("The taget the agent has to reach")]
    [SerializeField] private TargetFloatStabil target;
    [Tooltip("The Enviorment Controller for curriculums")]
    [SerializeField] private EnviormentController enviormentController;
    [Tooltip("The agent's camera (net yet used for anything)")]
    public Camera agentCamera;



    [Header(" ")]
    [Header("Agent settings")]
    [Tooltip("The standard spawn posistion of agent when an episode begins")]
    public Vector3 spawnPos = new Vector3(0, 50, 0);
    [Tooltip("The max speed the drone can move at")]
    public float maxSpeed = 10f;
    [Tooltip("The max Rotationspeed the drone can move at")]
    public float maxRotationalSpeed = 3f;
    [Tooltip("Agent pelanty recived for large action changes")]
    public float actionChangePenalty = 0.05f;
    [Tooltip("Agent pelanty recived for spinning too much")]
    public float spinPenalty = 0.002f;
    [SerializeField] private float progressMilestoneDistance = 1f;
    [SerializeField] private float progressMilestoneReward = 0.25f;
    [Tooltip("Rigidbody of the agent")]
    public Rigidbody DroneRigidBody;
    [Tooltip("Whether this is training mode or gameplay mode")]
    public bool trainingMode;
    [Tooltip("Whether the drone can land on the ground or not")]
    public bool groundIsLava;
    [Tooltip("Whether an obstacle should be placed as a blocker")]
    public bool placeBlocker;




    [Header(" ")]
    [Header("Battery Properties")]
    [Tooltip("The capacity of the drone's battery (not yet implemented)")]
    public float batteryCapacity = 100.0f;
    [Tooltip("The rate at which the drone's battery drains (not yet implemented)")]
    public float batteryDrainRate = 0.0f;


    [Header(" ")]
    [Header("Target-to-agent settings")]
    [Tooltip("The radius around the target the agent has to be within to get it")]
    public float reachRadius = 1f;
    [Tooltip("The the max distance the target can be spawned at from agent")]
    public float targetRange = 5f;
    [Tooltip("The max distance the agent can fly from the target before the epsiode ends")]
    public float maxDist = 20f;

    [Header(" ")]
    [Tooltip("The drone controller that manages the drone's physics and movement.")]
    public DroneController droneController;






    //Script variables
    [Tooltip("The previous distance between the agent and the target")]
    private float prevDist;
    [Tooltip("Whether to skip the next action (sometimes needed for syncing physics when resetting)")]
    private bool skipNextAction = false;

    //Action variables
    [Tooltip("The agents pitch input")]
    private float pitchInput;
    [Tooltip("The agents roll input")]
    private float rollInput;
    [Tooltip("The agents yaw input")]
    private float yawInput;
    [Tooltip("The agents throttle input")]
    private float throttleInput;





    public float CurrentVelocity;
    public float HighestVelocity;

    private Vector3 previousVelocity;

    private float bestDistance;



    #region Initializing and Updating


    /// <summary>
    /// Initializes the agent.
    /// </summary>
    public override void Initialize()
    {
        //here we simply get the rigidbody the traning area and respawn the entity.
        DroneRigidBody = GetComponent<Rigidbody>();
        traningArea = GetComponentInParent<TraningArea>();
        enviormentController = GetComponentInParent<EnviormentController>();
        enviormentController.UpdateDifficultyLevel();
        traningArea.ResetArea(spawnPos);
        //if not in training mode, no max step, play forever
        if (!trainingMode)
        {
            MaxStep = 0;
        }
    }


    /// <summary>
    /// Called every frame 
    /// </summary>
    private void Update()
    {
        //Nothing for now, the drone controller handles the physics and movement of the drone
    }


    /// <summary>
    /// called every .02 seconds (50 times a second) to update physics and move the agent
    /// </summary>
    private void FixedUpdate()
    {
        if (DroneRigidBody == null || droneController == null || traningArea == null)
            return;


        //apply the actions to the drone controller
        droneController.ApplyInputs(DroneRigidBody, pitchInput, rollInput, yawInput, throttleInput);

        CurrentVelocity = DroneRigidBody.velocity.magnitude;
        HighestVelocity = Mathf.Max(HighestVelocity, CurrentVelocity);

        previousVelocity = DroneRigidBody.velocity;

    }


    #endregion


    #region Triggers and Events


    /// <summary>
    /// Called when the agent's collider enters a trigger collider
    /// </summary>
    /// <param name="other">The trigger collider</param>
    private void OnTriggerEnter(Collider other)
    {

    }



    /// <summary>
    /// Called when the agent's collider stays in an entered trigger collider
    /// </summary>
    /// <param name="other">The trigger collider</param>
    private void OnTriggerStay(Collider other)
    {

    }


    /// <summary>
    /// Called when the agent collides with another object collider
    /// </summary>
    /// <param name="collision">The other object the agent colided with</param>
    private void OnCollisionEnter(Collision collision)
    {
        //If we are in traning mode we check the tag of the collider.
        //The punishment is based on what the agent collided with.
        if (trainingMode)
        {
            if (collision.collider.CompareTag("boundary"))
            {
                //collided with the boundary of the area, give a negative reward
                AddReward(-2f);
                EndEpisode();
            }
            else if (groundIsLava && collision.collider.CompareTag("ground"))
            {
                //crashed
                AddReward(-2f);
                EndEpisode();
            }
            else if (collision.collider.CompareTag("obstacle"))
            {
                //bumped into obstacle
                AddReward(-2f);
                EndEpisode();
            }
        }
    }


    #endregion


    #region Episode and Reset logic


    /// <summary>
    /// Reset the enviorment and agent when a new episode begins
    /// </summary>
    public override void OnEpisodeBegin()
    {
            enviormentController.UpdateDifficultyLevel();
            RespawnAgent();
            traningArea.ResetArea(spawnPos);
            //droneController.RandomizeMotors(0.5f, 0.9f, 0.02f, 0.05f);
            ResetTargetProgress();
    }
    /// <summary>
    /// Respawns the agent and target to start a new episode
    /// </summary>
    public void RespawnAgent()
    {
        //if the current value is high enough the yaml file will increase the random spawn tilt, otherwise it spawns at a -15 to 15 degree angle
        float tilt = enviormentController.SpawnTilt;
        ResetEntity(spawnPos, tilt); //reset the agent so it has no velocity, a new controller, and no input
        Physics.SyncTransforms();
    }

    /// <summary>
    /// Resets the agent's velocity, controller, and inputs
    /// </summary>
    public void ResetEntity(Vector3 spawnPos, float maxTilt)
    {
        pitchInput = rollInput = yawInput = throttleInput = 0f;
        //Put the agent at its spawn point
        transform.localPosition = spawnPos;
        DroneRigidBody.rotation = Quaternion.Euler(Random.Range(-maxTilt, maxTilt), Random.Range(0f, 360f), Random.Range(-maxTilt, maxTilt));
        DroneRigidBody.velocity = Vector3.zero;
        DroneRigidBody.angularVelocity = Vector3.zero;
        droneController.ResetController(DroneRigidBody);
    }

    public void ResetTargetProgress()
    {
        bestDistance = Vector3.Distance(DroneRigidBody.position, target.transform.position);
        prevDist = bestDistance;
    }



    #endregion


    #region Agent Cycle and Controls


    /// <summary>
    /// Called when the agent receives an action to take (from either the neural network or heuristic)
    /// 
    /// index 0: yawInput
    /// index 1: rollInput
    /// index 2: pitchInput
    /// index 3: throttleInput
    /// </summary>
    /// <param name="actions">actions to take</param>
    public override void OnActionReceived(ActionBuffers actions)
    {
        //if the enviorment has reset we skip the action to sync
        if (skipNextAction) { skipNextAction = false; return; }
        //Vector4 prev = new Vector4(yawInput, rollInput, pitchInput, throttleInput);
        //Store the new inputs
        yawInput = actions.ContinuousActions[0];
        rollInput = actions.ContinuousActions[1];
        pitchInput = actions.ContinuousActions[2];
        throttleInput = actions.ContinuousActions[3];
        //Vector4 now = new Vector4(yawInput, rollInput, pitchInput, throttleInput);
        //Debug.Log($"Pitch action: {pitchInput} ||  Yaw action:  {yawInput}  ||  Roll action:  {rollInput}  ||  Throttle action:  {throttleInput}");

        //if it isnt traning mode we don't add reward
        if (!trainingMode) return;
        //float actionChange = Vector4.Distance(now, prev);
        //Punish it for action change to reward smoothness
        //AddReward(-actionChangePenalty * actionChange);

        ////Punish it for yawing too much to reward stability
        //AddReward(-0.02f * Mathf.Abs(yawInput));

        //reward the agent for getting closer to the target.
        //we reward it for getting closer to the target
        //we punish it for getting further away from the target
        //but we have to make sure that when the target is placed after being reached that it doesn't get punished for being further away from the new target than it was from the old one.
        float dist = Vector3.Distance(DroneRigidBody.position, target.transform.position);
        RewardProgressToTarget(dist);

        //Punish it for spinning too much
        float normalizedSpin = DroneRigidBody.angularVelocity.magnitude / DroneRigidBody.maxAngularVelocity;
        AddReward(-spinPenalty * normalizedSpin);
        //if(-spinPenalty * normalizedSpin > 0)
        //    Debug.Log($"Spin penalty: {-spinPenalty * normalizedSpin} ||  Normalized Spin: {normalizedSpin} ||  Angular Velocity: {DroneRigidBody.angularVelocity.magnitude} ||  Max Angular Velocity: {DroneRigidBody.maxAngularVelocity}");

        //If it has come within the target radius we reward it nicely, move the target, and move the obstacles
        if (dist < reachRadius)
        {
            AddReward(5f);
            traningArea.PlaceTarget();
            //Debug.Log($"Rewarded for reaching target. Distance: {dist}. Reward: {5f}");
        }

        //If it is at a 90 dergree angle or more while close to the gound it is flipping to a unrecoverable position so we punish it and end the episode.
        if (DroneRigidBody.transform.up.y < 0f && DroneRigidBody.position.y < 2f)
        {
            AddReward(-2f);
            EndEpisode();
        }
        //Pelanty for taking too long to reach the target. This is a time penalty to encourage the agent to reach the target quickly.
        //AddReward(-0.001f);
    }
    //reached => place target => prev dist > currentdist => punish for getting further away from the new target than it was from the old one
    private void RewardProgressToTarget(float distance)
    {
        //positive when closer, negative when farther.
        float distanceChange = prevDist - distance;
        AddReward(distanceChange * 0.05f);

        //reward only meaningful new closest approaches.
        if (distance < bestDistance - progressMilestoneDistance)
        {
            AddReward(progressMilestoneReward);
            bestDistance = distance;
            //Debug.Log($"Rewarded for progress to target. New best distance: {bestDistance}. Reward: {progressMilestoneReward}");
        }
        prevDist = distance;

    }



    /// <summary>
    /// Collect vector observations from the environment for the agent to learn from
    /// We are trying to do sim-to-real so we only add observations that an IMU might have
    /// We normalize the values because that helps the agent learn faster and more effectively.
    /// </summary>
    /// <param name="sensor">The vector sensor</param>
    public override void CollectObservations(VectorSensor sensor)
    {
        //Vector3 normalizedToTarget = new Vector3(Mathf.Clamp(toTarget.x / Dimensions.x, -1f, 1f), Mathf.Clamp(toTarget.y / Dimensions.y, -1f, 1f), Mathf.Clamp(toTarget.z / Dimensions.z, -1f, 1f));
        //sensor.AddObservation(normalizedToTarget);
        
        

        //Add observations of the distance to target relative to the max possible distance.
        Vector3 toTarget = target.transform.position - DroneRigidBody.position;//distance to the target TargetRange
        Vector3 areaSize = new Vector3(200f, 190f, 200f); //this is hardcoded in because i have yet to create the traning enviorment script completly

        //First we add the distance to the target relative to the max possible distance. This is a single number making a total of 1 input
        float maxDistance = areaSize.magnitude;
        sensor.AddObservation(toTarget.magnitude / maxDistance); //+1 input
        //Debug.Log("Normalized distance: " + (toTarget.magnitude / maxDistance));
        //Next we add a normalized direction vector 
        sensor.AddObservation(toTarget.normalized);//+3 inputs
        //Debug.Log("Direction to target: " + toTarget.normalized);

        //Next we add a single number that tells the agent whether it is facing the target or not. This is needed for it to know if it is facing the target or not.
        sensor.AddObservation(Vector3.Dot(transform.forward, toTarget.normalized));//+1 input
        //Debug.Log("Whether the drone is facing the target: " + Vector3.Dot(t.forward, toTarget.normalized));

        //Next we add the normalized velocity of the drone relative to its max speed. This is needed for it to know how fast it is moving.
        //This vector has 3 numbers making a total of 7 inputs
        sensor.AddObservation(transform.InverseTransformDirection(DroneRigidBody.velocity) / maxSpeed);//+3 inputs
        //Debug.Log("Normalized velocity: " + (t.InverseTransformDirection(DroneRigidBody.velocity) / maxSpeed));


        //Next we add the normalized up vector of the drone relative to the world up vector. This is needed for it to know its attitude and orientation in the world.
        // This vector has 3 numbers making a total of 10 inputs
        sensor.AddObservation(transform.InverseTransformDirection(Vector3.up)); //+3 inputs
        //Debug.Log("Normalized up vector: " + t.InverseTransformDirection(Vector3.up));




        //Next we add the normalized angular velocity of the drone relative to its max angular velocity. This is needed for it to know how fast it is rotating.
        // This vector has 3 numbers making a total of 13 inputs
        float maxAngularVelocity = DroneRigidBody.maxAngularVelocity;
        Vector3 normAngularVelocity = DroneRigidBody.transform.InverseTransformDirection(DroneRigidBody.angularVelocity) / maxAngularVelocity;
        sensor.AddObservation(normAngularVelocity);//+3 inputs

        //we give it, its previous inputs to help it process what it has done and what is likely to happen next because of them
        // This is 4 inputs in all making a total of 17 inputs
        sensor.AddObservation(pitchInput); //+4 inputs
        sensor.AddObservation(rollInput);
        sensor.AddObservation(yawInput);
        sensor.AddObservation(throttleInput);


    //    Debug.Log($"Distance to target: {toTarget.magnitude / maxDistance}" +
    //$"Normalized: {normalizedToTarget}" +
    //$"Angular Velocity: {normAngularVelocity}" +
    //$"Velocity: {DroneRigidBody.velocity / maxSpeed}" +
    //$"Attitude: {t.InverseTransformDirection(Vector3.up)}");
        //Child sensors like raycasts are added in unity
    }


    /// <summary>
    /// This is just for keyboard controls.
    /// </summary>
    /// <param name="actionsOut"></param>
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        //create placeholder for moving and turning 
        float throttle = 0f;
        float roll = 0f;
        float pitch = 0f;
        float yaw = 0f;

        if (Input.GetKey(KeyCode.R))
        {
            EndEpisode();
            return;
        }

        //convert keyboard input into movement and turning
        //all values are between -1 and 1



        //foward backwards
        //We tilt the nose down to go forward. This means our pitch is negative.
        if (Input.GetKey(KeyCode.W))
        {
            pitch = 1;
        }
        //We tilt the nose up to go backward. This means our pitch is positive.
        else if (Input.GetKey(KeyCode.S))
        {
            pitch = -1;
        }



        //left right
        //We tilt the left side up to go right. This means our roll is positive
        if (Input.GetKey(KeyCode.D))
        {
            roll = -1;
        }
        //We tilt the left side down to go left. This means our roll is negative
        else if (Input.GetKey(KeyCode.A))
        {
            roll = 1;
        }



        //yaw rotate left right
        //Rotating clockwise increases yaw
        if (Input.GetKey(KeyCode.E))
        {
            yaw = 1f;
        }
        //rotating counterclockwise decrease yaw
        else if (Input.GetKey(KeyCode.Q))
        {
            yaw = -1f;
        }




        //up down
        if (Input.GetKey(KeyCode.Space))
        {
            throttle = 1;
        }
        else if (Input.GetKey(KeyCode.LeftShift))
        {
            throttle = -1;
        }


        //add 3 movement values to the actions
        actionsOut.ContinuousActions.Array[0] = yaw;
        actionsOut.ContinuousActions.Array[1] = roll;
        actionsOut.ContinuousActions.Array[2] = pitch;
        actionsOut.ContinuousActions.Array[3] = throttle;
    }
    private void LateUpdate()
    {
        if (DroneRigidBody == null || droneController == null)
            return;

        droneController.DrawThrustDebug();
        //draw a line so i can see it observing the target in unity
        Debug.DrawLine(transform.position, target.transform.position, Color.green);
    }
    #endregion

}



