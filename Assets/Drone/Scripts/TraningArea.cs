using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class TraningArea : MonoBehaviour
{
    [Tooltip("The standard spawn posistion of agent when an episode begins")]
    public Vector3 agentSpawnPos = new Vector3(0, 50, 0);
    public bool traningMode = true;
    public DroneAgent agent;
    public EnviormentController enviormentController;
    [SerializeField] public Vector3 Dimensions;
    [SerializeField] private Transform boundaryNorth;
    [SerializeField] private Transform boundarySouth;
    [SerializeField] private Transform boundaryEast;
    [SerializeField] private Transform boundaryWest;
    [SerializeField] private Transform roof;
    [SerializeField] private Transform ground;
    [SerializeField] private float wallThickness = 0.5f;
    [SerializeField] private float roofThickness = 0.5f;
    [SerializeField] private float groundThickness = 0.5f;
    [SerializeField] private ObstacleSpawner oSpawner;
    [SerializeField] private TargetSpawner tSpawner;
    [SerializeField] public Transform area;
    public Vector3 AreaSize;
    private void OnValidate() => UpdateArea();
    public void UpdateArea()
    {
        if(ground != null && boundarySouth != null && boundaryNorth != null && boundaryEast != null && boundaryWest != null && roof != null)
        {
            float height = Dimensions.y;
            float width = Dimensions.x;
            float depth = Dimensions.z;

            //first handle the ground
            ground.localScale = new Vector3(Dimensions.x, groundThickness, Dimensions.z);
            ground.localPosition = new Vector3(0, groundThickness / 2, 0);

            //then the boundaries
            boundarySouth.localScale = new Vector3(width, height, wallThickness);
            boundarySouth.localPosition = new Vector3(0, height / 2, -depth / 2);

            boundaryNorth.localScale = new Vector3(width, height, wallThickness);
            boundaryNorth.localPosition = new Vector3(0, height / 2, depth / 2);

            boundaryEast.localScale = new Vector3(wallThickness, height, depth);
            boundaryEast.localPosition = new Vector3(width / 2, height / 2, 0);

            boundaryWest.localScale = new Vector3(wallThickness, height, depth);
            boundaryWest.localPosition = new Vector3(-width / 2, height / 2, 0);

            //then the roof
            roof.localScale = new Vector3(Dimensions.x, roofThickness, Dimensions.z);
            roof.localPosition = new Vector3(0, height + roofThickness / 2, 0);

            //then we compute the size
            AreaSize.x = width - wallThickness;
            AreaSize.y = height;
            AreaSize.z = depth - wallThickness;
        }
    }
    //check if the spot is taken
    public bool IsBlocked(Vector3 worldPos, float radius, List<LayerMask> masks)
    {
        return masks.Any(mask => Physics.CheckSphere(worldPos, radius, mask));
    }
    public bool IsBlocked(Vector3 worldPos, float radius)
    {
        return Physics.CheckSphere(worldPos, radius);
    }
    public bool TooCloseToDrone(Vector3 objectPos, Vector3 dronePos, Vector3 size, float spawnClearRadius, bool ShowChecks)
    {
        Vector3 distance = objectPos - dronePos;
        if (ShowChecks)
            Debug.DrawLine(dronePos, objectPos);
        //we ignore the y distance and just check if the horizontal distance is less than the spawnClearRadius + the radius of the box (so we know if the drone has enough space to act before hitting the object)
        float horizDistance = new Vector2(distance.x, distance.z).magnitude;
        float boxRadius = 0.5f * new Vector2(size.x, size.z).magnitude;
        return horizDistance < spawnClearRadius + boxRadius && Mathf.Abs(distance.y) < spawnClearRadius + size.y * 0.5f;
    }


    //checks if a spot is free
    public bool CanSpawn(Vector3 obstacleWorldPos, Vector3 droneWorldPos, Quaternion rotation, Vector3 size, float spawnClearRadius, List<LayerMask> masks, bool ShowChecks)
    {
        if (TooCloseToDrone(obstacleWorldPos, droneWorldPos, size, spawnClearRadius, ShowChecks))
            return false;
        if (ShowChecks)
            Debug.DrawLine(droneWorldPos, obstacleWorldPos);
        //then we go through each active obstacle and check if the gap between it and the new one will be good enough
        return !masks.Any(mask => Physics.CheckBox(obstacleWorldPos, size * 0.5f, rotation, mask));
    }
    /// <summary>
    /// Respawns the agent and target to start a new episode
    /// </summary>
    public void ResetArea(Vector3 spawnPos)
    {
        tSpawner.PlaceTarget(spawnPos, this, enviormentController.TargetRange);
        oSpawner.Randomize(spawnPos, this, enviormentController);
        //tSpawner.ResetTargets();

        if (enviormentController.BlockPath)
            oSpawner.PlaceBlocker(agent.transform.localPosition, tSpawner.target.localPosition, agent.DroneRigidBody.velocity,this, enviormentController); //then place a blocker between target and agent
    }

    public void PlaceTarget()
    {
        tSpawner.PlaceTarget(agent.transform.localPosition, this, enviormentController.TargetRange);
        if(enviormentController.BlockPath)
            oSpawner.PlaceBlocker(agent.transform.localPosition, tSpawner.target.localPosition, agent.DroneRigidBody.velocity, this, enviormentController); //then place a blocker between target and agent
        agent.ResetTargetProgress();
    }
}