using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapMeshGenerator : MonoBehaviour
{
    Mesh mesh;
    [SerializeField] private DatabaseHelperScript databaseHelper;
    public Vector3[] linePoints;
    public int[] lineTriangles;

    // line properties
    public float lineWidth = 0.05f; // line width
    public Color lineColour = Color.black;

    void Start() {
        mesh = new Mesh();
        this.GetComponent<MeshFilter>().mesh = mesh;
    }

    void Update() {
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
        // get all edges from db
        double[,] mapEdges = databaseHelper.GetMapEdges();
        for (int i = 0; i < mapEdges.GetLength(0); i++) {

            // get two points for the line
            Vector3 point1 = new Vector4((float)mapEdges[i, 0], (float)mapEdges[i, 1], 0);
            Vector3 point2 = new Vector4((float)mapEdges[i, 2], (float)mapEdges[i, 3], 0);

            // calculate direction, so can get points
            Vector3 direction = (point2 - point1).normalized;

            // calculate offset so can get vertices
            Vector3 offset = new Vector3(-direction.y, direction.x, 0) * (lineWidth / 2f);

            // define the four vertices of the line
            points.Add(point1 + offset);
            points.Add(point1 - offset);
            points.Add(point2 + offset);
            points.Add(point2 - offset);
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
            triangles.Add(i+1);
            triangles.Add(i+2);

            // add second triangle
            triangles.Add(i+1);
            triangles.Add(i+3);
            triangles.Add(i+2);
        }

        return triangles.ToArray();
    }
}
