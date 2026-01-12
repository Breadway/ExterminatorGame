using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Manages player character rotation by using raycasts to find a gameobject in the ground layer and use the lookAt() function to rotate to face it.
public class PlayerAim : MonoBehaviour
{
    
    void Start()
    {
        
    }

    void FixedUpdate()
    {
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (groundPlane.Raycast(ray, out float distance)) {
            Vector3 worldPoint = ray.GetPoint(distance);
            worldPoint.y = transform.position.y;
            transform.LookAt(worldPoint);
        }
    }
}
