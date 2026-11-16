using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

public enum DifficultyLevel
{
    none,
    easy,
    neutral,
    medium,
    hard,
    veryHard,
}

public class EnviormentController : MonoBehaviour
{
    //maybe make it into a data struct with a contructor instead of all of this repeated code
    public DifficultyLevel DifficultyLevel = DifficultyLevel.none;
    [HideInInspector] public float TargetRange = 5.0f;
    [HideInInspector] public float Randomness = 0f;
    [HideInInspector] public float SpawnTilt = 0f;
    [HideInInspector] public float NumberOfObstacles = 0f;
    [HideInInspector] public bool BlockPath = false;
    [HideInInspector] public float ObstacleRotation = 15;
    [HideInInspector] public float ObstacleHight = 12;
    [HideInInspector] public float ObstacleWidth = 3;
    [HideInInspector] public float SpinPenalty = 3;
    [HideInInspector] public float ActionChangePenalty = 3;
    public void UpdateDifficultyLevel()
    {
        TargetRange = Academy.Instance.EnvironmentParameters.GetWithDefault("TargetRange", 10f);
        SpawnTilt = Academy.Instance.EnvironmentParameters.GetWithDefault("SpawnTilt", 10f);
        BlockPath = Academy.Instance.EnvironmentParameters.GetWithDefault("BlockPath", 0) > 0.5f;
        ObstacleWidth = Academy.Instance.EnvironmentParameters.GetWithDefault("ObstacleWidth", 3f);
        ObstacleHight = Academy.Instance.EnvironmentParameters.GetWithDefault("ObstacleHight", 12f);
        ObstacleRotation = Academy.Instance.EnvironmentParameters.GetWithDefault("ObstacleRotation", 0f);
        NumberOfObstacles = Academy.Instance.EnvironmentParameters.GetWithDefault("NumberOfObstacles", 0);
        Randomness = 0f;
        if (DifficultyLevel == DifficultyLevel.easy)
        {
            TargetRange = 20f;
            Randomness = 0.4f;
            SpawnTilt = 35f;
            NumberOfObstacles = 0;
            BlockPath = true;
            ObstacleRotation = 0;
            ObstacleHight = 12;
            ObstacleWidth = 3;
        }
        if (DifficultyLevel == DifficultyLevel.neutral)
        {
            TargetRange = 30f;
            Randomness = 1.0f;
            SpawnTilt = 60f;
            NumberOfObstacles = 1f;
            BlockPath = UnityEngine.Random.value > 0.5f;
            ObstacleRotation = 30f;
            ObstacleHight = 24;
            ObstacleWidth = 6;
        }
        if (DifficultyLevel == DifficultyLevel.medium)
        {
            TargetRange = 30f;
            Randomness = 1.0f;
            SpawnTilt = 60f;
            NumberOfObstacles = 2f;
            BlockPath = UnityEngine.Random.value > 0.5f;
            ObstacleRotation = 60f;
            ObstacleHight = 36;
            ObstacleWidth = 9;
        }
         if (DifficultyLevel == DifficultyLevel.hard)
        {
            TargetRange = 30f;
            Randomness = 1.0f;
            SpawnTilt = 60f;
            NumberOfObstacles = 4f;
            BlockPath = UnityEngine.Random.value > 0.5f;
            ObstacleRotation = 90f;
        }
        if (DifficultyLevel == DifficultyLevel.veryHard)
        {
            TargetRange = 30f;
            Randomness = 1.0f;
            SpawnTilt = 60f;
            NumberOfObstacles = 16f;
            BlockPath = UnityEngine.Random.value > 0.5f;
            ObstacleRotation = 90f;
            ObstacleHight = 72;
            ObstacleWidth = 18;
        }
    }
}
