using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiamensionJump : MonoBehaviour
{
    [SerializeField]
    DiamensionJump other;
    Camera self;
    [HideInInspector]
    public RenderTexture selfTex;
    [SerializeField]
    Material m;
    bool jumping = false;
    bool isCopying = false;
    float jumpTimer = 0;
    RenderTexture depthTexture;
    // Start is called before the first frame update
    void Start()
    {
        self = this.GetComponent<Camera>();
        self.depthTextureMode = DepthTextureMode.Depth;
        selfTex = new RenderTexture(self.pixelWidth, self.pixelHeight, 1);
        depthTexture = new RenderTexture(selfTex.width, selfTex.height, 16, RenderTextureFormat.Depth);
    }
    private void CopyDepthBuffer()
    {
        isCopying = true;
        self.targetTexture = depthTexture;
        self.Render();
        self.targetTexture = null;
        isCopying = false;        
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            //UpdateTexture();
            CopyDepthBuffer();
            Shader.SetGlobalTexture("_PreviousDepthTexture", depthTexture);
            this.gameObject.SetActive(false);
            other.gameObject.SetActive(true);
            //other.UpdateTexture();
            jumpTimer = 0f;
            jumping = true;
        }
        if (jumpTimer> 7f) jumping = false;
    }
    public void UpdateTexture()
    {
        if(!isCopying)Graphics.Blit(self.activeTexture, selfTex);
    }
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (!jumping || isCopying)
        {
            if (isCopying) Debug.Log("Copy escape true");
            Graphics.Blit(source, destination);
            return;
        }
        Matrix4x4 matrixCameraToWorld = self.cameraToWorldMatrix;
        Matrix4x4 matrixProjectionInverse = GL.GetGPUProjectionMatrix(self.projectionMatrix, false).inverse;
        Matrix4x4 matrixHClipToWorld = matrixCameraToWorld * matrixProjectionInverse;

        Shader.SetGlobalMatrix("_MatrixHClipToWorld", matrixHClipToWorld);
        Debug.Log(self.name);
        m.SetFloat(Shader.PropertyToID("_JumpTime"), jumpTimer);
        jumpTimer += Time.deltaTime;
        m.SetTexture(Shader.PropertyToID("_DstATex"), source);
        m.SetTexture(Shader.PropertyToID("_DstBTex"), other.selfTex);
        Graphics.Blit(source, destination, m);
    }
    private void OnPostRender()
    {
        UpdateTexture();
    }
}
