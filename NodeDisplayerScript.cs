using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.SearchService;
using UnityEngine;

public class NodeDisplayerScript : MonoBehaviour
{
    Mesh mesh;
    [SerializeField] private DatabaseHelperScript databaseHelper;
    [SerializeField] private UserSettingsScript userSettings;
    public Vector3[] linePoints;
    public int[] lineTriangles;
    private bool floor;

    // line properties
    public float lineWidth = 0.05f; // line width
    public Color lineColour = Color.red;

    // for database stuff
    Vector3 worldClick;

    void Start() {
        mesh = new Mesh();
        this.GetComponent<MeshFilter>().mesh = mesh;
    }

    void Update() {
        floor = userSettings.floor;
        
        DrawLines();
    }

    void DrawLines() {

        linePoints = GetLinePoints();
        lineTriangles = GetLineTriangles(linePoints);
        // now draw on mesh
        mesh.Clear();
        mesh.vertices = linePoints;
        mesh.triangles = lineTriangles;

        // create an array of colors and assign to each vertex
        Color[] colors = new Color[linePoints.Length];
        for (int i = 0; i < colors.Length; i++) {
            colors[i] = lineColour;
        }
        mesh.colors = colors;
    }
    
    Vector3[] GetLinePoints() {

        List<Vector3> points = new List<Vector3>();

        double[,] edgeCoordinates = databaseHelper.GetEdgeCoordinates(floor);
        
        // now draw them out
        for (int i = 0; i < edgeCoordinates.GetLength(0); i++) {

            // get two points for the line
            Vector3 point1 = new Vector4((float)edgeCoordinates[i, 0], (float)edgeCoordinates[i, 1], 0);
            Vector3 point2 = new Vector4((float)edgeCoordinates[i, 2], (float)edgeCoordinates[i, 3], 0);

            // calculate direction, so can get points
            Vector3 direction = (point2 - point1).normalized;

            // calculate perp offset so can make line have width
            Vector3 perpOffset = new Vector3(-direction.y, direction.x, 0) * (lineWidth / 2f);

            // calculate parallel offset so can make new line fill one half width past the point
            // necessary to look good when two lines meet at a single point
            Vector3 paraOffset = new Vector3(direction.x, direction.y, 0) * (lineWidth / 2f);

            // define the four vertices of the line
            // perp offset is rotated 90 degree clockwise to the line
            // para offset is same direction as line from point 1 to point 2
            points.Add(point1 + perpOffset - paraOffset);
            points.Add(point1 - perpOffset - paraOffset);
            points.Add(point2 + perpOffset + paraOffset);
            points.Add(point2 - perpOffset + paraOffset);
        }

        return points.ToArray();
    }

    int[] GetLineTriangles(Vector3[] points) {

        // define num triangles as num vertices / 4 (=num lines) * 2
        int numPoints = points.Length;
        List<int> triangles = new List<int>();

        for (int i = 0; i < numPoints; i+=4) {

            // add first triangle
            triangles.Add(i);
            triangles.Add(i+2);
            triangles.Add(i+1);

            // add second triangle
            triangles.Add(i+1);
            triangles.Add(i+2);
            triangles.Add(i+3);
        }

        return triangles.ToArray();
    }
}
