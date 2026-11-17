using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.FilePathAttribute;
using static UnityEditor.PlayerSettings;
using static UnityEngine.Rendering.DebugUI.Table;

/// <summary>
/// I needed a way to spawn obsticales for the drone to dodge during traning so it learns to fly under and over them to reach the target.
/// This obstacle spawner class helps spawn obsticals around the target and agent.
/// </summary>
public class ObstacleSpawner : MonoBehaviour
{
    [Tooltip("The prefab that is to be spawned as an obstacle for the agent")]
    public GameObject obstaclePrefab;
    public EnviormentController environmentController;
    [Tooltip("How many obstacles can the spawner handel")]
    public float maxObstacles = 8;
    [SerializeField] public List<LayerMask> layerMasks = new List<LayerMask>();
    [Tooltip("The pool of instatiated obstecales")]
    Transform[] pool = new Transform[0];
    [Tooltip("The number of active obstecales")]
    int active;
    public bool showChecks = false;

    [Header("Blocker")]
    [Tooltip("Closest the blocker's edge may be to the drone")]
    public float minBlockerClearance = 5f;
    [Tooltip("Closest the blocker's edge may be to the target")]
    public float targetMargin = 2.5f;

    Vector3 clearCenter; // world position the keep-out zone is centered on




    /// <summary>
    /// Called each episode to randomize the spawns of the obstacles
    /// </summary>
    public void Randomize(Vector3 spawnCenter, TraningArea traningArea,EnviormentController enviormentController)
    {
        maxObstacles = enviormentController.NumberOfObstacles;
        foreach (Transform t in pool)
            Object.Destroy(t.gameObject);
        pool = new Transform[(int)maxObstacles + 1];   // last slot is reserved for the blocker
        for (int i = 0; i < pool.Length; i++)
        {
            pool[i] = Instantiate(obstaclePrefab, transform).transform;
            pool[i].gameObject.SetActive(false);
        }
        float count = enviormentController.NumberOfObstacles;
        active = 0; //initiate the number of active obstecles
        Vector3 arenaHalfExtents = traningArea.AreaSize / 2f; //get the half extents of the area so we can spawn within it

        //we go through each obstacle and set it to inactive
        for (int i = 0; i < pool.Length; i++) 
            pool[i].gameObject.SetActive(false);

        clearCenter = transform.TransformPoint(spawnCenter);

        //then we go through each and give them a spawn point
        for (int i = 0; i < count; i++)
        {
            bool placed = false;
            for (int attempt = 0; attempt < 20; attempt++) //if we have tried 20 times to find a free spot then we simply pass it
            {

                Vector3 size = RandomSize(enviormentController);
                Quaternion rotation = RandomRotation(enviormentController);
                Vector3 pos = new Vector3(
                    Random.Range(-arenaHalfExtents.x+size.x, arenaHalfExtents.x-size.x),
                    Random.Range(0, traningArea.AreaSize.y-size.y),
                    Random.Range(-arenaHalfExtents.z+size.z, arenaHalfExtents.z-size.z));
                if (!traningArea.CanSpawn(pos, spawnCenter, rotation, size, minBlockerClearance, layerMasks, showChecks)) //if the position isnt free we try again
                    continue;
                pool[i].SetLocalPositionAndRotation(pos, rotation);
                pool[i].localScale = size;
                Place(pool[i]); // Activate first so Renderer.bounds updates accurately

                Renderer ren = pool[i].GetComponent<Renderer>();
                Vector3 center = transform.position;

                bool insideY = ren.bounds.min.y >= center.y && ren.bounds.max.y <= center.y + traningArea.AreaSize.y;
                bool insideX = ren.bounds.min.x >= center.x - arenaHalfExtents.x && ren.bounds.max.x <= center.x + arenaHalfExtents.x;
                bool insideZ = ren.bounds.min.z >= center.z - arenaHalfExtents.z && ren.bounds.max.z <= center.z + arenaHalfExtents.z;

                // If it sticks outside ANY boundary, hide it and try a new random transform
                if (!(insideY && insideX && insideZ))
                {
                    pool[i].gameObject.SetActive(false);
                    continue;
                }

                active++;
                placed = true;
                break;
            }
            if (!placed) break; //if it wasnt placed then we stop spawning more as that means the area is probably already full
        }
        Physics.SyncTransforms();
    }




    //we randomize the size
    Vector3 RandomSize(EnviormentController enviormentController)
    {
        float max_x = enviormentController.ObstacleWidth;
        float max_z = max_x;
        float max_y = enviormentController.ObstacleHight;
        return new Vector3(UnityEngine.Random.Range(1f, max_x), UnityEngine.Random.Range(3f, max_y), UnityEngine.Random.Range(1f, max_z));
    }
    Quaternion RandomRotation(EnviormentController enviormentController)
    {
        float max_tilt = enviormentController.ObstacleRotation;
        return Quaternion.Euler(UnityEngine.Random.Range(0f, max_tilt), UnityEngine.Random.Range(0f, max_tilt), UnityEngine.Random.Range(0f, max_tilt));
    }






    //simply gives it the correct position values
    void Place(Transform t)
    {
        t.gameObject.SetActive(true);
    }


    //if this one is called we place an obstecale betweent the agent and the target
    public void PlaceBlocker(Vector3 dronePos, Vector3 targetPos, Vector3 droneVelocity, TraningArea traningArea, EnviormentController enviormentController)
    {
        //if (pool == null || pool.Length == 0) return;
        //Transform blocker = pool[pool.Length - 1];
        //blocker.gameObject.SetActive(false);
        //clearCenter = dronePos;

        //Vector3 dir = targetPos - dronePos;
        //float dist = dir.magnitude;
        //if (dist < 0.01f) 
        //    return;
        //dir /= dist;

        //// the faster the drone is heading at the target, the farther away the blocker must be
        //float closing = Mathf.Max(0f, Vector3.Dot(droneVelocity, dir));
        //float minFromDrone = minBlockerClearance + closing * reactionTime;


        //for (int i = 0; i<15;i++)
        //{
        //    Vector3 size = RandomSize(enviormentController);
        //    float r = 0.5f * new Vector2(size.x, size.z).magnitude;
        //    float lo = minFromDrone + r;             // blocker's near edge stays clear of the drone
        //    float hi = dist - targetMargin - r;      // and its far edge stays clear of the target
        //    if (hi <= lo) 
        //        continue;                  // no room for this size, try a smaller one
        //    Quaternion rot = RandomRotation(enviormentController);
        //    Vector3 obstaclePos = dronePos + dir * Random.Range(lo, hi);
        //    if (traningArea.CanSpawn(obstaclePos, dronePos, rot, size, minBlockerClearance, layerMasks, showChecks)) 
        //        continue;
        //    blocker.SetLocalPositionAndRotation(obstaclePos, rot);
        //    blocker.localScale = size;
        //    Place(blocker);
        //    Physics.SyncTransforms();
        //    if (active == 0)
        //        active += 1;
        //    break;// not enough room, so this target gets no blocker
        //}
    }



    public void SpawnObstaclesByDensity(float density, int perMeter, Vector3 areaSize)
    {
        //start at local position 0,0,0 try to spawn
        //then add per
        //nvm... i think its better to just spawn randomly throughout the entire area, set the distance raduius around them to large enough and remove the limit that makes it spawn around the target.
        //yea thats gonna work better...
    }
}