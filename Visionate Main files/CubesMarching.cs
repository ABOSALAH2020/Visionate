using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]

public class CubeMarching : MonoBehaviour
{   [SerializeField] private ComputeShader sdfComputeShader; // Reference to the compute shader
    [SerializeField] private RayMarchCamera rayMarchCamera;
    [SerializeField] private float gridScale = 1.0f; // Adjust this for resolution

    [SerializeField] private int width = 10;
    [SerializeField] private int height = 10;
    [SerializeField] private float noiseResolution = 1;
    [SerializeField] private bool  vizualizeNoise;
    [SerializeField] private float heightTresshold = 0.5f;

    private float[ , , ] heights;
    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();
    private MeshFilter meshFilter;
    private ComputeBuffer heightsBuffer; // Buffer to store SDF values
    private int computeKernel;

    void Start()
    
    {   meshFilter = GetComponent<MeshFilter>();
        // Initialize compute shader kernel and buffer  
        computeKernel = sdfComputeShader.FindKernel("CSMain");
        heightsBuffer = new ComputeBuffer((width + 1)* (height + 1) *(width +1 ), sizeof(float));
        StartCoroutine(UpdateAll());   //to keep updating every set of time (in this case 0.1 second to make it faster)  
    }

    private IEnumerator UpdateAll(){
        while(true)
        {
            setHeights();
            MarchCubes();
            setMesh();
            yield return new WaitForSeconds(1/10);
        }
    }

    private void MarchCubes()
    {
        vertices.Clear();
        triangles.Clear();
        for (int x = 0; x < width * gridScale; x++)
        {
             for (int y = 0; y < height * gridScale; y++)
            {
                 for (int z = 0; z < width * gridScale; z++)
                 {
                    float[] cubeCorners = new float[8]; //8 corners of a cube
                    
                        for (int i = 0; i < 8; i++) //to go through all corners:
                        {
                            Vector3Int corner = new Vector3Int(x, y, z) + MarchingTable.Corners[i]; //adding the Marchingtable corners of a cube
                            cubeCorners[i] = heights[corner.x, corner.y, corner.z]; //to get the discrete cube corner
                        }
                        MarchCube(new Vector3(x, y, z), GetConfigurationIndex(cubeCorners));
                 }
            }
        }
    }
    private void MarchCube (Vector3 position, int configIndex){
        if (configIndex == 0 || configIndex == 255)
        {
            return;
        }
        //rendering the mesh 
        int edgeIndex = 0;
        for (int t = 0; t < 5; t++)
        {
            for (int v = 0; v < 3; v++)
            {
                int triTableValue = MarchingTable.Triangles[configIndex, edgeIndex];

                if(triTableValue == -1)
                {
                    return;
                }

                Vector3 edgeStart = position + MarchingTable.Edges[triTableValue, 0];
                Vector3 edgeEnd = position + MarchingTable.Edges[triTableValue, 1];
                Vector3 vertex = (edgeStart + edgeEnd)/2; //might make it linear interpolation afterward for smoothier result
                
                //as for now, we're at the actual position of the vertex
                
                vertices.Add(vertex);
                triangles.Add(vertices.Count -1);
                edgeIndex++; 
            }
        }
    }
    private int GetConfigurationIndex(float[] cubeCorners)

    { //to decide whether the corner should be active or not, hence we add threshold
        int configIndex = 0;
        for (int i = 0; i < 8; i++)
        {
            if(cubeCorners[i] > heightTresshold){
                configIndex |= 1 << i; //add 1 to the bit of the configuration index then left shift by i
            }
        }   //note: make sure to make the tresshold is 0.5 so the inside surface is kinda stuck to the outside of the noise figure
        return configIndex; 
    }

    private void setMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles= triangles.ToArray();
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;
    }

    private void setHeights()
    {
         Vector3 gridOrigin = transform.position;

        // Set compute shader parameters
        sdfComputeShader.SetFloat("_GridScale", gridScale);
        sdfComputeShader.SetFloat("_NoiseResolution", noiseResolution);
        sdfComputeShader.SetVector("_GridOrigin", gridOrigin);
        sdfComputeShader.SetInt("_Width", width);
        sdfComputeShader.SetInt("_Height", height);


        // Bind the buffer
        sdfComputeShader.SetBuffer(computeKernel, "_HeightsBuffer", heightsBuffer);

        // Dispatch the compute shader
        int threadGroupsX = Mathf.CeilToInt((width + 1) / 8.0f);
        int threadGroupsY = Mathf.CeilToInt((height + 1) / 8.0f);
        int threadGroupsZ = Mathf.CeilToInt((width + 1) / 8.0f);
        sdfComputeShader.Dispatch(computeKernel, threadGroupsX, threadGroupsY, threadGroupsZ);

        // Read back the buffer to the heights array
        heights = new float[width + 1, height + 1, width + 1];
        float[] flatHeights = new float[heightsBuffer.count];
        heightsBuffer.GetData(flatHeights);

        int index = 0;
        for (int x = 0; x < width + 1; x++)
        {
            for (int y = 0; y < height + 1; y++)
            {
                for (int z = 0; z < width + 1; z++)
                {
                    heights[x, y, z] = flatHeights[index++];
                }
            }
        }
    
        /*heights = new float[width +1, height +1, width +1];
        Vector3 gridOrigin = transform.position;

        for (int x = 0; x < width + 1; x++)
        {
             for (int y = 0; y < height + 1; y++)
            {
                 for (int z = 0; z < width + 1; z++)
                 {
                     // Calculate the world position of the grid point
                Vector3 point = gridOrigin + new Vector3(x, y, z) / noiseResolution;

                // Sample the SDF from the RayMarchCamera
                float sdfValue = rayMarchCamera.EvaluateSDF(point);

                // Store the value in the heights array
                heights[x, y, z] = sdfValue;

                 }
            }
        }
    */
    }

    private void OnDestroy()
    {
        // Release the compute buffer
        heightsBuffer.Release();
    }

    private void OnDrawGizmosSelected()
    {
        if(!vizualizeNoise || !Application.isPlaying)
        {
            return;
        }
         for (int x = 0; x < width + 1; x++)
        {
             for (int y = 0; y < height + 1; y++)
            {
                 for (int z = 0; z < width + 1; z++)
                 {
                    Gizmos.color = new Color(heights[x, y, z], heights[x, y, z], heights[x, y, z], 1); //1 so we can see the colors
                    Gizmos.DrawSphere(new Vector3(x, y, z), 0.2f); //prebuild in c# to draw spheres based on the heights value
                 }
            }
        }
    }

}