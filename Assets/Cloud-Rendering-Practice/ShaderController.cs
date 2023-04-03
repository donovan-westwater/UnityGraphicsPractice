using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ShaderController : MonoBehaviour
{
    public Vector3[] cellPoints;
    public Shader raymarchingShader = null;
    public ComputeShader noiseShader;
    public Material m_renderMaterial;
    public int res = 256;
    RenderTexture t;
    void Start()
    {
        if (raymarchingShader == null)
        {
            Debug.LogError("no awesome shader.");
            m_renderMaterial = null;
            return;
        }
        t = computeTextureTest();
        m_renderMaterial = new Material(raymarchingShader);
        m_renderMaterial.SetTexture("_NoiseTex", t);//GenerateNoise());
        //Destroy(t);
        //m_renderMaterial.mainTexture = GenerateNoise();
        

    }
    RenderTexture computeTextureTest()
    {
        Texture3D t = new Texture3D(res, res, res,TextureFormat.RGBA32, false);
        //Generate random points for noise
        cellPoints = new Vector3[10];

        for (int c = 0; c < cellPoints.Length; c++)
        {
            cellPoints[c] = new Vector3(Random.Range(0, 256) / (float)res, Random.Range(0, 256) / (float)res, Random.Range(0, 256) / (float)res);

        }
        //ComputeBuffer result = new ComputeBuffer(256 * 256, sizeof(float) * 4);
        int kernal = noiseShader.FindKernel("CSMain");
        uint[] groupSizes = { 0, 0 ,0};
        noiseShader.GetKernelThreadGroupSizes(kernal, out groupSizes[0], out groupSizes[1],out groupSizes[2]);
        
        RenderTexture r = new RenderTexture(res, res, 0);
        r.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        r.wrapMode = TextureWrapMode.Repeat;
        r.volumeDepth = res;
        r.enableRandomWrite = true;
        r.Create();
        noiseShader.SetTexture(kernal, "Result", r);
        noiseShader.SetInt("Resolution", res);
        ComputeBuffer buffer = new ComputeBuffer(cellPoints.Length, sizeof(float) * 3);
        buffer.SetData(cellPoints);
        noiseShader.SetBuffer(kernal, "CellPoints", buffer);
        noiseShader.Dispatch(kernal, r.width / (int)groupSizes[0], r.height / (int)groupSizes[1], r.volumeDepth / (int)groupSizes[2]);
        buffer.Dispose();
        return r;
    }
    //TO DO: Convert this into a compute shader
    //Use spatial hashing to split the texture into cells via %
    //Make sure the cells are small enough actually be useful while large enough to contain points
    Texture3D GenerateNoise()
    {
        //Generate random points for noise
        cellPoints = new Vector3[50];
        for (int c = 0; c < cellPoints.Length; c++)
        {
            cellPoints[c] = new Vector3(Random.Range(0, 256), Random.Range(0, 256), Random.Range(0, 256));
            
        }
        //TO DO: Normalize the x and y values so that the max dist will always be 1
        //(x,y) -> (x/256,y/256)
        Texture3D t = new Texture3D(256, 256,256,TextureFormat.RGBA32,false);
        for(int x = 0; x < 256; x++)
        {
            for(int y = 0; y < 256; y++)
            {
                for(int z = 0;z < 256; z++)
                {
                    Vector3 p = new Vector3(x, y, z);
                    float dist = 443; //256
                    //Main points
                    for (int k = 0; k < cellPoints.Length; k++)
                    {
                        float d = Vector3.Distance(p, cellPoints[k]);
                        //Check adjcent virtual tiles for points to create seamless tiling
                        //Dont need to copy whole set of points in order to do this!
                        dist = Mathf.Min(d, dist);
                        Vector3 offset = new Vector3(0, 0, 0);
                        if (cellPoints[k].x > 128) offset.x = -256;
                        else offset.x = 256;
                        if (cellPoints[k].y > 128) offset.y = -256;
                        else offset.y = 256;
                        if (cellPoints[k].z > 128) offset.z = -256;
                        else offset.z = 256;
                        //Compare straight up to the side and then corner
                        d = Vector3.Distance(p, cellPoints[k] + new Vector3(0, offset.y, 0));
                        dist = Mathf.Min(d, dist);
                        d = Vector3.Distance(p, cellPoints[k] + new Vector3(offset.x, 0, 0));
                        dist = Mathf.Min(d, dist);
                        d = Vector3.Distance(p, cellPoints[k] + new Vector3(0, 0, offset.z));
                        dist = Mathf.Min(d, dist);
                        //Corner cases
                        d = Vector3.Distance(p, cellPoints[k] + new Vector3(offset.x, 0, offset.z));
                        dist = Mathf.Min(d, dist);
                        d = Vector3.Distance(p, cellPoints[k] + new Vector3(offset.x, offset.y, 0));
                        dist = Mathf.Min(d, dist);
                        d = Vector3.Distance(p, cellPoints[k] + new Vector3(0, offset.y, offset.z));
                        dist = Mathf.Min(d, dist);
                        d = Vector3.Distance(p, cellPoints[k] + offset);
                        dist = Mathf.Min(d, dist);
                    }
                    float inputD = 1 - dist / 221;
                    Color c = new Color(inputD, inputD, inputD, inputD);
                    t.SetPixel(x, y,z, c);
                    /*
                    Color c = new Color();
                    float s = Mathf.PerlinNoise((float)x / 256 * 20, (float)y / 256 *20);
                    c.a = 1;
                    c.r = s;
                    c.b = s;
                    c.g = s;
                    t.SetPixel(x, y,c);
                    */
                }
            }
        }
        t.Apply();
        return t;
    }
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(source, destination, m_renderMaterial);
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.W))
        {
            this.transform.position += this.transform.forward * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.S))
        {
            this.transform.position -= this.transform.forward * Time.deltaTime;
        }
        
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Destroy(t);
            t = computeTextureTest();
            m_renderMaterial.SetTexture("_NoiseTex", t);
            //AssetDatabase.CreateAsset(t, "Assets/Cloud-Rendering-Practice/Test.asset");
        }
        
    }
    //private void OnDestroy()
    //{
    //    Destroy(t);
    //}
}
