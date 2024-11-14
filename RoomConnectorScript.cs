using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomConnectorScript : MonoBehaviour
{
    Mesh mesh;
    [SerializeField] private DatabaseHelperScript databaseHelper;
    [SerializeField] private UserSettingsScript userSettings;
    public Vector3[] linePoints;
    public int[] lineTriangles;
    private bool floor;

    // line properties
    public float lineWidth = 0.05f; // line width
    public Color lineColour = Color.blue;

    void Start() {
        mesh = new Mesh();
        this.GetComponent<MeshFilter>().mesh = mesh;
    }

    void Update() {
        floor = userSettings.floor;
    }
}
