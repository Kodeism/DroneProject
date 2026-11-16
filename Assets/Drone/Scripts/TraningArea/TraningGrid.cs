using System.Collections.Generic;
using UnityEngine;

public class TraningGrid : MonoBehaviour
{
    [Range(1, 50)] public int numberOfAreas = 1;
    [SerializeField] public TraningArea areaPrefab;
    [SerializeField] public int spacing = 10;
    [SerializeField] public List<TraningArea> trainingAreas = new List<TraningArea>();

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Can't destroy/instantiate directly inside OnValidate, so defer it
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null) return; // object may have been deleted meanwhile
            GenerateGrid();
        };
    }
#endif

    public void GenerateGrid()
    {
        if (areaPrefab == null) return;

        int side = Mathf.CeilToInt(Mathf.Pow(numberOfAreas, 1f / 3f) - 0.0001f);
        float spacingX = areaPrefab.Dimensions.x + spacing;
        float spacingY = areaPrefab.Dimensions.y + spacing;
        float spacingZ = areaPrefab.Dimensions.z + spacing;

        // Destroy all existing children
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }
        trainingAreas.Clear();
        for (int layer = 0; layer < side; layer++)
        {
            for (int row = 0; row < side; row++)
            {
                for (int column = 0; column < side; column++)
                {
                    if (trainingAreas.Count >= numberOfAreas)
                        return;

                    Vector3 spawnPosition = new Vector3(column * spacingX, spacingY * layer, row * spacingZ);

                    TraningArea newArea;
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        // Keeps prefab link intact when instantiating in edit mode
                        newArea = (TraningArea)UnityEditor.PrefabUtility.InstantiatePrefab(areaPrefab, transform);
                        newArea.transform.position = transform.position + spawnPosition;
                    }
                    else
#endif
                    {
                        newArea = Instantiate(areaPrefab, transform.position + spawnPosition, Quaternion.identity, transform);
                    }

                    newArea.name = "TrainingArea_" + trainingAreas.Count;
                    trainingAreas.Add(newArea);
                }
            }
        }
    }
}