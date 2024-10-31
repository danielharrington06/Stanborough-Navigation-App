using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapMeshGenerator : MonoBehaviour
{
    [SerializeField] private DatabaseHelperScript databaseHelper;
    public Colour lineColour = Color.white; // line colour
    public float lineWidth = 0.05f; // line width

    // Start is called before the first frame update
    void Start()
    {
        double[,] mapEdges = databaseHelper.GetMapEdges();

        Mesh mesh = new Mesh();
        // store all vertices so they can be accessed later
        // 2 vertices per edge
        Vector3[] vertices = new Vector3[mapEdges.Length * 2];
        // store triangles made of three points, 2 triangles per edge
        int[] triangles = new int[mapEdges.Length * 6];

        for (int i = 0; i < mapEdges.GetLength(0); i++) {
            
            // get the start and end points of the egde
            Vector3 point1 = new Vector3((float)mapEdges[i, 0], (float)mapEdges[i, 1], 0);
            Vector3 point2 = new Vector3((float)mapEdges[i, 2], (float)mapEdges[i, 3], 0);

            // define vertices for the line
            vertices[i * 2] = point1;
            vertices[i * 2 + 1] = point2;

            // define triangles (two triangles for each line)
            int startIdx = i * 6;
            // first triangle
            triangles[startIdx] = i * 2;
            triangles[startIdx + 1] = i * 2 + 1;
            triangles[startIdx + 2] = i * 2 + 2;
            // second triangle
            triangles[startIdx + 3] = i * 2 + 1;
            triangles[startIdx + 4] = i * 2 + 3;
            triangles[startIdx + 5] = i * 2 + 2;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;

        // recalulate normals for proper lighting
        mesh.RecalculateNormals();

        // create a MeshFilter and MeshRenderer to display the mesh
        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();

        // set the mesh to the MeshFilter
        meshFilter.mesh = mesh;

        // Assign a material to the MeshRenderer
        meshRenderer.material = new Material(Shader.Find("Standard"));
        
    }

    

    // Update is called once per frame
    void Update()
    {
        
    }
}
