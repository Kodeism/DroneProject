using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class TraningArea : MonoBehaviour
{
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
    public bool IsBlocked(Vector3 localPos, float radius, List<LayerMask> masks, bool ShowChecks = false)
    {
        Vector3 worldPos = transform.TransformPoint(localPos);
        if (ShowChecks)
            Debug.DrawLine(worldPos, worldPos + Vector3.up * radius);
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
    public bool CanSpawn(Vector3 obstacleLocalPos, Vector3 droneLocalPos, Quaternion rotation, Vector3 size, float spawnClearRadius, List<LayerMask> masks, bool ShowChecks)
    {
        Vector3 obstacleWorldPos = transform.TransformPoint(obstacleLocalPos);
        Vector3 droneWorldPos = transform.TransformPoint(droneLocalPos);
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
    public void ResetArea(Vector3 spawnPos, Transform target, Vector3 velocity)
    {
        tSpawner.PlaceTarget(spawnPos, this, enviormentController.TargetRange, target);
        oSpawner.Randomize(spawnPos, this, enviormentController);
    }

    public void PlaceTarget(Vector3 spawnPos, Transform target, Vector3 velocity)
    {
        tSpawner.PlaceTarget(spawnPos, this, enviormentController.TargetRange, target);
    }
}