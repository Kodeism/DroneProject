using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class TargetSpawner : MonoBehaviour
{
    public Transform target;
    public float reachRadius = 1f;

    /// <summary>
    /// Tries to place the target and blockers near the agent.
    /// </summary>
    public void PlaceTarget(Vector3 droneLocalPos,TraningArea traningArea, float targetRange)
    {
        //Debug.Log("target being placed " + GetCumulativeReward() + "(time scale: " + Time.timeScale + ")");
        //tries to place the target 
        const int maxAttempts = 100;

        for (int i = 0; i < maxAttempts; i++)
        {
            //How far within the given target range the target wil be placed from the agent.
            Vector3 o = new Vector3(Random.Range(-1f, 1f), Random.Range(-0.5f, 0.5f), Random.Range(-1f, 1f)).normalized * Random.Range(2f, targetRange);

            //create the candidate position and check with the obstacle spawner if it is blocked.
            Vector3 candidate = droneLocalPos + o;
            //check if it is winthin the bounds of the enviorment
            if (!(candidate.x < 95 && candidate.x > -95) || !(candidate.z < 95 && candidate.z > -95) || !(candidate.y < 188 && candidate.y > 2))
                continue;

            if (!traningArea.IsBlocked(candidate, reachRadius + 0.5f))
            {
                target.transform.localPosition = candidate;
                break;
            }
        }
    }
}
