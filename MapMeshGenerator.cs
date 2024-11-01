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

    // for database stuff
    Vector3 click1;
    Vector3 click2;
    bool click1Active;
    bool click2Active;
    Vector3 worldClick;

    void Start() {
        mesh = new Mesh();
        this.GetComponent<MeshFilter>().mesh = mesh;
    }

    void Update() {
        // check for a mouse click
        if (Input.GetMouseButtonDown(0)) {

            // get the mouse position in screen coordinates
            Vector3 screenPosition = Input.mousePosition;

            // convert screen coordinates to world position
            worldClick = Camera.main.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 0));
            
            if (!click1Active && !click2Active) {
                click1 = new Vector3(worldClick.x, worldClick.y, worldClick.z);
                click1Active = true;
            }
            else if (click1Active && !click2Active) {
                click2 = new Vector3(worldClick.x, worldClick.y, worldClick.z);
                click2Active = true;
            }
            else if (click1Active && click2Active) {
                // then save stuff and reset
                databaseHelper.SaveMapEdge(click1.x, click1.y, click2.x, click2.y);
                click1Active = false;
                click2Active = false;
            }
        }
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
