using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

public class MarchinigCubeMeshGenerator : MonoBehaviour
{
    public ComputeShader marchingCubes;
    public ComputeShader volumeMapGen;
    public ComputeShader volumeMapEditor;
    public Texture3D volumeMap;
    public int numPointsPerAxis = 64;
    public float isoLevel = 0f;

    public Vector3 bounds = new Vector3(1, 1, 1);

    RenderTexture rt3d;
    GameObject meshHolder;
    // Buffers
    ComputeBuffer triangleBuffer;
    ComputeBuffer pointsBuffer;
    ComputeBuffer triCountBuffer;
    public struct VertexData
    {
	    public Vector3 position;
	    public Vector3 normal;
	    public Vector2Int id;
    }
    struct Triangle
    {
#pragma warning disable 649 // disable unassigned variable warning
        public VertexData a;
        public VertexData b;
        public VertexData c;

        public VertexData this[int i]
        {
            get
            {
                switch (i)
                {
                    case 0:
                        return a;
                    case 1:
                        return b;
                    default:
                        return c;
                }
            }
        }
    }

    private void CreateMesh(int numThreadsPerAxis)
    {
        triangleBuffer.SetCounterValue(0);
        marchingCubes.SetTexture(0, "DensityTexture", rt3d);
        marchingCubes.SetBuffer(0, "triangles", triangleBuffer);
        marchingCubes.SetInt("numPointsPerAxis", numPointsPerAxis);
        marchingCubes.SetInt("textureSize", rt3d.width);
        marchingCubes.SetFloat("isoLevel", isoLevel);
        marchingCubes.SetFloats("strideBetweenPoints", rt3d.width / numPointsPerAxis, rt3d.height / numPointsPerAxis, rt3d.volumeDepth / numPointsPerAxis);
        marchingCubes.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);

        // Get number of triangles in the triangle buffer
        ComputeBuffer.CopyCount(triangleBuffer, triCountBuffer, 0);
        int[] triCountArray = { 0 };
        triCountBuffer.GetData(triCountArray);
        int numTris = triCountArray[0];

        // Get triangle data from shader
        Triangle[] tris = new Triangle[numTris];
        triangleBuffer.GetData(tris, 0, 0, numTris);

        Mesh mesh = new Mesh();
        mesh.Clear();

        var vertices = new Vector3[numTris * 3];
        var meshTriangles = new int[numTris * 3];

        for (int i = 0; i < numTris; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                meshTriangles[i * 3 + j] = i * 3 + j;
                vertices[i * 3 + j] = tris[i][j].position;
                Debug.Log(vertices[i * 3 + j]);
            }
        }
        mesh.vertices = vertices;
        mesh.triangles = meshTriangles;

        mesh.RecalculateNormals();
        meshHolder = new GameObject();
        var mf = meshHolder.AddComponent<MeshFilter>();
        var mr = meshHolder.AddComponent<MeshRenderer>();
        mr.material = new Material(Shader.Find("Standard"));
        mf.sharedMesh = mesh;
    }
    private void Start()
    {
        CreateRenderTexture3D(ref rt3d, 256);

        volumeMapGen.SetTexture(0, "VolumeMap", rt3d);
        volumeMapGen.SetFloats("boxBounds", bounds.x,bounds.y, bounds.z);
        volumeMapGen.SetInt("pointsPerAxis", numPointsPerAxis);
        volumeMapGen.SetInts("resolution", rt3d.width, rt3d.height, rt3d.width);
        volumeMapGen.Dispatch(0, rt3d.width / 8, rt3d.height / 8, rt3d.volumeDepth/ 8);
        
        int numPoints = numPointsPerAxis * numPointsPerAxis * numPointsPerAxis;
        int numVoxelsPerAxis = numPointsPerAxis - 1;
        int numVoxels = numVoxelsPerAxis * numVoxelsPerAxis * numVoxelsPerAxis;
        int maxTriangleCount = numVoxels * 5;
        int numThreadsPerAxis = Mathf.CeilToInt(numVoxelsPerAxis / 8f);

        int stride = (sizeof(float) * 3 * 2 + sizeof(int) * 2) * 3;
        triangleBuffer = new ComputeBuffer(maxTriangleCount, stride, ComputeBufferType.Append);
        triCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        
        CreateMesh(numThreadsPerAxis);
        SaveRT3DToTexture3DAsset(rt3d, "MarchingCubes/Test3DAsset");

        triangleBuffer.Release();
        triCountBuffer.Release();
    }

    void SaveRT3DToTexture3DAsset(RenderTexture rt3D, string pathWithoutAssetsAndExtension)
    {
        int width = rt3D.width, height = rt3D.height, depth = rt3D.volumeDepth;
        var a = new NativeArray<float>(width * height * depth, Allocator.Persistent, NativeArrayOptions.UninitializedMemory); //change if format is not 8 bits (i was using R8_UNorm) (create a struct with 4 bytes etc)
        AsyncGPUReadback.RequestIntoNativeArray(ref a, rt3D, 0, (_) =>
        {
            Texture3D output = new Texture3D(width, height, depth, rt3D.graphicsFormat, TextureCreationFlags.None);
            output.SetPixelData(a, 0);
            output.Apply(updateMipmaps: false, makeNoLongerReadable: true);
            AssetDatabase.CreateAsset(output, $"Assets/{pathWithoutAssetsAndExtension}.asset");
            AssetDatabase.SaveAssets();
            a.Dispose();
            rt3D.Release();
        });
    }

    [MenuItem("CreateEmpty/3DTexture")]
    static void CreateTexture3D()
    {
        // Set the texture parameters
        int size = 64;
        TextureFormat format = TextureFormat.RGBA32;
        TextureWrapMode wrapMode = TextureWrapMode.Clamp;

        // Create the texture and apply the parameters
        Texture3D texture = new Texture3D(size, size, size, format, false);
        texture.wrapMode = wrapMode;

        // Create a 3-dimensional array to store color data
        Color[] colors = new Color[size * size * size];

        // Populate the array so that the x, y, and z values of the texture map to red, blue, and green colors
        float inverseResolution = 1.0f / (size - 1.0f);
        for (int z = 0; z < size; z++)
        {
            int zOffset = z * size * size;
            for (int y = 0; y < size; y++)
            {
                int yOffset = y * size;
                for (int x = 0; x < size; x++)
                {
                    colors[x + yOffset + zOffset] = new Color(x * inverseResolution,
                        y * inverseResolution, z * inverseResolution, 1.0f);
                }
            }
        }

        // Copy the color values to the texture
        texture.SetPixels(colors);

        // Apply the changes to the texture and upload the updated texture to the GPU
        texture.Apply();

        // Save the texture to your Unity Project
        AssetDatabase.CreateAsset(texture, "Assets/Empty3DTexture.asset");
    }

    static void CreateRenderTexture3D(ref RenderTexture texture, int size, string name = "Untitled")
    {
        var format = UnityEngine.Experimental.Rendering.GraphicsFormat.R16_SFloat;
        if (texture == null || !texture.IsCreated() || texture.width != size || texture.height != size || texture.volumeDepth != size || texture.graphicsFormat != format)
        {
            //Debug.Log ("Create tex: update noise: " + updateNoise);
            if (texture != null)
            {
                texture.Release();
            }
            const int numBitsInDepthBuffer = 0;
            texture = new RenderTexture(size, size, numBitsInDepthBuffer);
            texture.graphicsFormat = format;
            texture.volumeDepth = size;
            texture.enableRandomWrite = true;
            texture.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
            texture.Create();
        }
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;
        texture.name = name;
    }
}

