using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeControl : MonoBehaviour
{
    // Adjust this in the Unity Inspector to make the cube move faster or slower
    [SerializeField] private float speed = 5.0f;

    void Update()
    {
        // Get horizontal input (A/D keys or Left/Right arrows)
        float moveX = Input.GetAxis("Horizontal");

        // Get vertical input (W/S keys or Up/Down arrows) 
        float moveZ = Input.GetAxis("Vertical");

        // Combine inputs into a 3D movement vector (keeping Y at 0 so it stays on the ground)
        Vector3 movement = new Vector3(moveX, 0.0f, moveZ);

        // Move the cube over time smoothly
        transform.Translate(movement * speed * Time.deltaTime);
    }
}
