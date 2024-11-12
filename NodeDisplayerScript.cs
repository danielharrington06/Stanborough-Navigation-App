using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    List<double[]> DefineLines() {
        // get all edge id's from db
        int[] edge_IDs = databaseHelper.GetEdgeIDs();
        int numEdges = edge_IDs.Length;

        List<double[]> pointsList = new List<double[]>();

        

        char[] allowedEdgeTypes = new char[] {'O', 'I'};

        // for each edge, check if fits requirements and if so add to map edges.
        for (int i = 0; i < numEdges; i++) {
            int currentEdgeID = edge_IDs[i];
            var record = databaseHelper.GetEdgeRecord(currentEdgeID);
            int currentNode1 = Convert.ToInt32(record[1]);
            int currentNode2 = Convert.ToInt32(record[2]);
            char currentEdgeType = Convert.ToChar(record[4]);

            if (!allowedEdgeTypes.Contains(currentEdgeType)) {
                // if not a current valid edge type then pass
                continue;
            }
            else {
                double[] node1Coordinates = databaseHelper.GetNodeCoordinates(currentNode1);
                double[] node2Coordinates = databaseHelper.GetNodeCoordinates(currentNode2);
                // check if there are edge vertices for this edge
                
                if (!databaseHelper.EdgeVerticesExist(currentEdgeID)) {
                    // there are no edge vertices other than those defined at the nodes
                    pointsList.Add(new double[] {node1Coordinates[0], node1Coordinates[1], node2Coordinates[0], node2Coordinates[1]});
                }

                else {
                    // there are edge vertices that need to be considered
                    double[,] edgeVertices = databaseHelper.GetEdgeVertices(currentEdgeID);
                    // from node 1 to first edge vertex
                    pointsList.Add(new double[] {node1Coordinates[0], node1Coordinates[1], edgeVertices[0, 0], edgeVertices[0, 1]});
                    int numVertices = edgeVertices.GetLength(0); // represnts the number of lines containing just vertices
                    // eg if 3 then loop no times as can connect from node 1 to vertex and from vertex to node 2

                    for (int j = 0; j < numVertices - 1; j++) { // num lines from just vertices is the numVertices - 1
                        // between j and j + 1
                        pointsList.Add(new double[] {edgeVertices[j,0], edgeVertices[j,1], edgeVertices[j+1,0], edgeVertices[j+1,1]});
                    }

                    // from last edge vertex to node 2
                    pointsList.Add(new double[] {edgeVertices[numVertices,0], edgeVertices[numVertices,1], node2Coordinates[0], node2Coordinates[1]});
                }
            }
        }
        return pointsList;
    }
    
    Vector3[] GetLinePoints() {

        List<Vector3> points = new List<Vector3>();

        List<double[]> pointsList = DefineLines();
        
        // now draw them out
        for (int i = 0; i < pointsList.Count; i++) {

            // get two points for the line
            Vector3 point1 = new Vector4((float)pointsList[i][0], (float)pointsList[i][1], 0);
            Vector3 point2 = new Vector4((float)pointsList[i][2], (float)pointsList[i][3], 0);

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
            triangles.Add(i+3);
            triangles.Add(i+2);
        }

        return triangles.ToArray();
    }
}
