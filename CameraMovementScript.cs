using System;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [SerializeField] private new Camera camera; // maybe delete new
    
    [SerializeField] private DatabaseHelperScript databaseHelper;
    [SerializeField] private UserSettingsScript userSettings;

    private float cameraZoomStep, maxCameraSize, minCameraSize;
    private Vector3 dragOrigin; // to hold position of mouse in world space when click

    private float mapBuffer, mapMaxX, mapMinX, mapMaxY, mapMinY;
    private int lastWidth, lastHeight;
    
    private bool mapFocussed;

    void Start() {

        mapFocussed = userSettings.mapFocussed;
        CalculateBounds();
        ResetCamera();
        
    }


    void Update() {
        
        if (userSettings.mapFocussed != mapFocussed) {
            mapFocussed = userSettings.mapFocussed;
        }

        if (mapFocussed) {
            PanCamera();
            ZoomScroll();
        }
        
        // check if screen width or height has changed
        if (lastWidth != Screen.width || lastHeight != Screen.height) {
            CalculateBounds();
        }
    }

    // calculates bounds for the camera
    private void CalculateBounds() {

        // get info for max bounds
        float[] mapBounds = databaseHelper.GetMapBounds();
        mapBuffer = databaseHelper.GetMapBuffer();
        mapMaxX = mapBounds[0] + mapBuffer;
        mapMinX = mapBounds[1] - mapBuffer;
        mapMaxY = mapBounds[2] + mapBuffer;
        mapMinY = mapBounds[3] - mapBuffer;

        // calculate max camera size
        // the greatest possible world width/height divided by the camera width/height scaled to size 1
        // camera aspect is the half the actual width scaled to size one
        float maxCameraSizeX = (mapMaxX-mapMinX)/(camera.aspect*2); 
        float maxCameraSizeY = (mapMaxY-mapMinY)/(1*2);
        // choose the minimum of the two possible maximums
        maxCameraSize = Math.Min(maxCameraSizeX, maxCameraSizeY);

        // get min camera size 
        minCameraSize = Convert.ToSingle(databaseHelper.GetMinCameraSize());

        // calculate the zoom step as the range of zoom divided by the number of increments
        int numZoomIncrements = Convert.ToInt32(databaseHelper.GetNumCameraZoomIncrements());
        cameraZoomStep = (maxCameraSize - minCameraSize) / numZoomIncrements;

        // get screen width and height so can check when it changes
        lastWidth = Screen.width;
        lastHeight = Screen.height;

        // zoom in just to avoid problems with resizing at max zoom out
        if (camera.orthographicSize >= maxCameraSize - 0.1 ) {
            ZoomIn();
        }
        // clamp camera
        camera.transform.position = ClampCamera(camera.transform.position);
        
    }

    private void ResetCamera() {

        // now start camera at a specific size and coordinates
        Vector3 dbStartCoordinates = databaseHelper.GetCameraStartCoordinates();
        camera.transform.position = new Vector3(dbStartCoordinates.x, dbStartCoordinates.y, camera.transform.position.z);

        // make sure size is on one of the increments
        int i = 0;
        while (minCameraSize + (cameraZoomStep * i) < databaseHelper.GetCameraStartSize()) {
            i++;
        }

        // now figure out if above or below desired is best
        if (minCameraSize + (cameraZoomStep * i) - databaseHelper.GetCameraStartSize() < databaseHelper.GetCameraStartSize() - minCameraSize + (cameraZoomStep * (i-1))) {
            // i is closer to desired, so set size to i's
            camera.orthographicSize = minCameraSize + (cameraZoomStep * i);
        }
        else {
            camera.orthographicSize = minCameraSize + (cameraZoomStep * (i-1));
        }

        // clamp camera just in case
        camera.transform.position = ClampCamera(camera.transform.position);
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

            // move camera and clamp
            camera.transform.position = ClampCamera(camera.transform.position + difference);
        }
    }

    private void ZoomScroll() {
        // on my mouse each 0.1 of scroll input should be one zoom increment
        double scrollInput = Math.Round(Input.GetAxis("Mouse ScrollWheel"), 1);
        if (scrollInput > 0) {
            // zoom in scroll input * 10 num times
            for (int i = 0; i < scrollInput*10; i++) {
                if (IsMouseWithinScreen()) {
                    if (!userSettings.invertScroll) {
                        ZoomIn();
                    } 
                    else {
                        ZoomOut();
                    };
                }
            }
        }
        else if (scrollInput < 0) {
            // zoom in scroll input * 10 num times
            // - as scroll input is negative
            for (int i = 0; i < -scrollInput*10; i++) {
                if (IsMouseWithinScreen()) {
                    if (!userSettings.invertScroll) {
                        ZoomOut();
                    } 
                    else {
                        ZoomIn();
                    };
                }
            }
        }
    }

    // zoom in function - public so can be called with a button click
    public void ZoomIn() {
        // change size
        camera.orthographicSize = Mathf.Clamp(camera.orthographicSize - cameraZoomStep, minCameraSize, maxCameraSize);

        // then clamp camera
        camera.transform.position = ClampCamera(camera.transform.position);
    }

    // zoom out function - public so can be called with a button click
    public void ZoomOut() {
        // change size
        camera.orthographicSize = Mathf.Clamp(camera.orthographicSize + cameraZoomStep, minCameraSize, maxCameraSize);

        // then clamp camera
        camera.transform.position = ClampCamera(camera.transform.position);
    }

    private Vector3 ClampCamera(Vector3 targetPosition) {

        // get camera width and height (which are both actually half the width and height)
        float cameraHeight = camera.orthographicSize;
        float cameraWidth = camera.orthographicSize * camera.aspect;

        // can be done as such as the camera height and width is half the actual
        // define max and min x and y for camera position
        float maxX = mapMaxX - cameraWidth;
        float minX = mapMinX + cameraWidth;
        float maxY = mapMaxY - cameraHeight;
        float minY = mapMinY + cameraHeight;

        // then clamp
        float newX = Mathf.Clamp(targetPosition.x, minX, maxX);
        float newY = Mathf.Clamp(targetPosition.y, minY, maxY);

        return new Vector3(newX, newY, targetPosition.z);
    }

    private bool IsMouseWithinScreen()
    {
        Vector3 mousePosition = Input.mousePosition;

        // Check if the mouse is within the screen bounds
        return mousePosition.x >= 0 && mousePosition.x <= Screen.width && mousePosition.y >= 0 && mousePosition.y <= Screen.height;
    }
}
