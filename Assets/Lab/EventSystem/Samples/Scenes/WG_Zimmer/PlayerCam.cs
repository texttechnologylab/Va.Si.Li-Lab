using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCam : MonoBehaviour
{
    public float sensX;
    public float sensY;

    public Transform orientation;

    float xRotation;
    float yRotation;
    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
    }

    // Update is called once per frame
    void Update()
    {
        // Input System delta is in pixels; * 0.1 matches the old "Mouse X/Y" axis scale so sensX/sensY stay the same
        Vector2 delta = Mouse.current != null ? Mouse.current.delta.ReadValue() * 0.1f : Vector2.zero;
        float mouseX = delta.x * Time.deltaTime *sensX;
        float mouseY = delta.y * Time.deltaTime *sensY;

        yRotation += mouseX;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        //Camera orientation rotation
        transform.rotation = Quaternion.Euler(xRotation,yRotation,0);
        orientation.rotation = Quaternion.Euler(0,yRotation,0);
        
    }
}
