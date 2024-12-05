using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovementScript : MonoBehaviour
{
    [SerializeField] private new Camera camera; // maybe delete new
    private Vector3 dragOrigin; // to hold position of mouse in world space when click


    void Update() {
        PanCamera();
    }

    private void PanCamera() {

        // save position of the camera in world space when the drag starts (click down)
        if (Input.GetMouseButtonDown(0)) { // drag starts (mouse button DOWN)
            dragOrigin = camera.ScreenToWorldPoint(Input.mousePosition);
        }

        // if stil held
        if (Input.GetMouseButton(0)) {
            // calculate difference from drag origin
            Vector3 difference = dragOrigin - camera.ScreenToWorldPoint(Input.mousePosition);

            // move camera
            camera.transform.position += difference;
        }
    }
}
