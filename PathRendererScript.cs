using System.Collections.Generic;
using UnityEngine;

public class PathRendererScript : MonoBehaviour
{
    Mesh mesh;
    [SerializeField] private DijkstraPathfinderScript dijkstraPathfinder;
    [SerializeField] private UserSettingsScript userSettings;
    public Vector3[] linePoints;
    public int[] lineTriangles;
    private bool floor;
    public bool drawPath;

    // line properties
    public float lineWidth = 0.05f; // line width
    public Color lineColour = Color.yellow;

    void Start() {
        mesh = new Mesh();
        this.GetComponent<MeshFilter>().mesh = mesh;
        floor = false;
        drawPath = false;
    }

    void Update() {

        // check if showResults in dp is different from draw Path
        // if so, set drawPath as apprpriate and then change path edges
        // only draw/clear path when dijkstra starts or ends or is defocussed
        if (dijkstraPathfinder.showResults != drawPath || userSettings.floor != floor) {
            floor = userSettings.floor;
            drawPath = dijkstraPathfinder.showResults;
            if (drawPath == true) {
                // draw path
                DrawPath();
            }
            else {
                // clear path
                mesh.Clear();
            }
        }
        DrawPath();
    }

    void DrawPath() {

        // get the correct set of coordinates from dijkstra pathfinder
        List<double[]> pathCoordinates;
        if (floor == false) { // floor 0
            pathCoordinates = dijkstraPathfinder.floor0Path;
        }
        else { // floor1
            pathCoordinates = dijkstraPathfinder.floor1Path;
        }

        // check if no coordinates as otherwise there are errors
        if (pathCoordinates.Count == 0) {
            // just clear the mesh
            mesh.Clear();
        }
        else {
            // do normal thing
            linePoints = GetLinePoints(pathCoordinates);
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
    }
    
    Vector3[] GetLinePoints(List<double[]> pathCoordinates) {

        List<Vector3> points = new List<Vector3>();

        double[,] pathEdges = new double[pathCoordinates.Count-1, 4];

        // iterate through pathCoordinates and turn them into edges
        // only to count - 1 as needs to be able to take next index and for n nodes, n-1 edges
        for (int i = 0; i < pathCoordinates.Count - 1; i++) {
            pathEdges[i, 0] = pathCoordinates[i][0]; // current node on path 
            pathEdges[i, 1] = pathCoordinates[i][1]; // current node on path
            pathEdges[i, 2] = pathCoordinates[i+1][0]; // next node
            pathEdges[i, 3] = pathCoordinates[i+1][1]; // next node
        }

        // now draw them out
        for (int i = 0; i < pathEdges.GetLength(0); i++) {

            // get two points for the line
            Vector3 point1 = new Vector4((float)pathEdges[i, 0], (float)pathEdges[i, 1], 0);
            Vector3 point2 = new Vector4((float)pathEdges[i, 2], (float)pathEdges[i, 3], 0);

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
